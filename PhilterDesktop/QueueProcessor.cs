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

        public static QueueRedactionResult Succeeded(
            string outputPath, IReadOnlyList<RedactionSpanEntity> spans, VerificationOutcome? verification = null, long durationMs = 0) =>
            new() { Success = true, OutputPath = outputPath, Spans = spans, Verification = verification, DurationMs = durationMs };

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

            try
            {
                // Time the whole operation end-to-end (redact + verify) for this file.
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var policy = PolicySerializer.DeserializeFromJson(policyEntity.Json);
                GlobalLists.Apply(policy, settings); // global always-redact/ignore on top of every policy
                string outputPath = RedactionService.GetOutputPath(entity.Name, settings);
                List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(
                    entity.Name, outputPath, policy, entity.Context, filterService, entity.Highlight,
                    fullyRedactedColumns: entity.FullyRedactedColumns,
                    wordScrub: DocumentMetadata.OptionsFor(settings),
                    ocrScannedPdfs: settings.OcrScannedPdfs,
                    ocrTextCoverage: settings.OcrTextCoverageThreshold,
                    ocrImageCoverage: settings.OcrImageCoverageThreshold,
                    ocrMaxPages: settings.OcrMaxPages,
                    scrubEmailHeaders: settings.ScrubEmailHeaders);

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
                    verification = await Task.Run(() =>
                        RedactionVerifier.Verify(outputPath, verifyPolicy, entity.Context, filterService));
                }

                stopwatch.Stop();
                return QueueRedactionResult.Succeeded(outputPath, spans, verification, stopwatch.ElapsedMilliseconds);
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
