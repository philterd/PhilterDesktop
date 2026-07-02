/*
 * Copyright 2026 Philterd, LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Phileas.Policy;
using Phileas.Services;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>The outcome of redacting one queued document.</summary>
    internal sealed class QueueRedactionResult
    {
        public bool Success { get; private init; }
        public string? OutputPath { get; private init; }
        public IReadOnlyList<RedactionSpanEntity> Spans { get; private init; } = Array.Empty<RedactionSpanEntity>();
        public string? ErrorMessage { get; private init; }
        public Exception? Exception { get; private init; }

        /// <summary>The verification pass result, when verification ran for this redaction; null otherwise.</summary>
        public VerificationOutcome? Verification { get; init; }

        /// <summary>End-to-end time, in milliseconds, taken to redact (and verify) this document.</summary>
        public long DurationMs { get; init; }

        /// <summary>True when the policy asked for on-device name detection but the model wasn't installed,
        /// so names were silently skipped — the caller surfaces this as a warning.</summary>
        public bool NameDetectionUnavailable { get; private init; }

        /// <summary>True when the source was an RTF with header/footer/footnote content that RTF redaction
        /// does not carry into the output — the caller surfaces this as a fidelity warning.</summary>
        public bool ContentDropped { get; private init; }

        public static QueueRedactionResult Succeeded(
            string outputPath, IReadOnlyList<RedactionSpanEntity> spans, VerificationOutcome? verification = null,
            long durationMs = 0, bool nameDetectionUnavailable = false, bool contentDropped = false) =>
            new()
            {
                Success = true, OutputPath = outputPath, Spans = spans, Verification = verification,
                DurationMs = durationMs, NameDetectionUnavailable = nameDetectionUnavailable,
                ContentDropped = contentDropped
            };

        public static QueueRedactionResult Failed(string message, Exception? exception = null) =>
            new() { Success = false, ErrorMessage = message, Exception = exception };
    }

    /// <summary>
    /// The core "redact one queued document" step, extracted from the form's timer loop so the
    /// decision logic (missing policy, missing file, redaction errors, success) is unit-testable.
    /// It performs no UI or database writes — the caller reacts to the returned result.
    /// </summary>
    internal static class QueueProcessor
    {
        public static async Task<QueueRedactionResult> ProcessAsync(
            RedactionQueueEntity entity,
            PolicyRepository policies,
            SettingsEntity settings,
            FilterService filterService)
        {
            PolicyEntity? policyEntity = policies.FindByName(entity.Policy);
            if (policyEntity == null)
            {
                return QueueRedactionResult.Failed($"Policy '{entity.Policy}' was not found");
            }

            if (!File.Exists(entity.Name))
            {
                return QueueRedactionResult.Failed("File not found");
            }

            // Enforce the hard size cap here — the single choke point every queued item passes through,
            // whatever added it (drag-drop, Add Files, Redact Spreadsheet). A file over the limit is failed
            // cleanly rather than being loaded whole into memory, where a multi-GB file could spike memory
            // and OutOfMemoryException. The interactive add points still show the softer advisory prompt;
            // this is the same safety limit (default 500 MB, 0 = no limit) the watched-folder/CLI paths use.
            if (LargeFileWarning.ExceedsHardLimit(entity.Name, settings.MaxInputFileSizeMb))
            {
                return QueueRedactionResult.Failed($"Exceeds the {settings.MaxInputFileSizeMb} MB size limit");
            }

            try
            {
                // Time the whole operation end-to-end (redact + verify) for this file.
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var policy = PolicySerializer.DeserializeFromJson(policyEntity.Json);
                GlobalLists.Apply(policy, settings); // global always-redact/ignore on top of every policy

                // If names are requested but the model isn't installed, RedactFileAsync's PhEyeModel.Prepare
                // will silently strip name detection. Capture that now (before redaction) so the caller can
                // warn instead of reporting a clean success.
                bool nameDetectionUnavailable = PhEyeModel.RequestedButUnavailable(policy);
                // Don't overwrite an existing output (e.g. another source with the same file name from a
                // different folder, written into a shared output folder) — pick a free "name (n)" instead.
                string outputPath = RedactionService.GetUniqueOutputPath(RedactionService.GetOutputPath(entity.Name, settings));
                List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(
                    entity.Name, outputPath, policy, entity.Context, settings, filterService, entity.Highlight,
                    fullyRedactedColumns: entity.FullyRedactedColumns, worksheet: entity.Worksheet);

                // Self-check: re-scan the written output for residual PII (the false-negative case).
                // Optionally with a broad "all detectors on" policy to catch types the redaction policy
                // didn't cover; otherwise the same policy that performed the redaction.
                VerificationOutcome? verification = null;
                if (settings.VerifyAfterRedaction)
                {
                    Phileas.Policy.Policy verifyPolicy = policy;
                    if (settings.VerificationUseBroadPolicy)
                    {
                        verifyPolicy = VerificationPolicy.Broad();
                        GlobalLists.Apply(verifyPolicy, settings);
                    }
                    // Don't re-flag this redaction's own inserted replacements as residual PII.
                    IReadOnlySet<string> knownReplacements = RedactionVerifier.ReplacementsOf(spans);
                    verification = await Task.Run(() =>
                        RedactionVerifier.Verify(outputPath, verifyPolicy, entity.Context, filterService, knownReplacements, sourcePath: entity.Name, worksheet: entity.Worksheet));
                }

                stopwatch.Stop();
                bool contentDropped = RtfFidelity.HasDroppedContent(entity.Name); // RTF header/footer/footnote loss
                return QueueRedactionResult.Succeeded(outputPath, spans, verification, stopwatch.ElapsedMilliseconds, nameDetectionUnavailable, contentDropped);
            }
            catch (Exception ex)
            {
                return QueueRedactionResult.Failed(ex.Message, ex);
            }
        }

        /// <summary>
        /// A plain-language reason to persist/show for a failed result. File-operation exceptions are
        /// translated via <see cref="UserError"/>; the already-friendly cases (missing policy/file)
        /// use their own message.
        /// </summary>
        public static string DescribeFailure(QueueRedactionResult result, string path)
        {
            if (result.Exception is not null)
            {
                return UserError.Describe(result.Exception, path, writing: true);
            }
            return string.IsNullOrEmpty(result.ErrorMessage) ? "The redaction could not be completed." : result.ErrorMessage;
        }
    }
}
