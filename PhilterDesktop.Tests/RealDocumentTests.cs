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

using System.Reflection;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using PhilterDesktop.PolicyEditing;
using UglyToad.PdfPig;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Integration tests over <b>real, public-domain documents</b> kept locally in the repo's
    /// <c>test-documents/real-documents/</c> folder (which is gitignored, so these documents are never committed).
    ///
    /// Each document has a sibling <c>&lt;name&gt;.expected.json</c> manifest describing the policy to
    /// run and the detections that must be found (and, for <c>.docx</c>/<c>.pdf</c>, whether the redacted
    /// output must be metadata-free). The test loads the document, runs the real detection/redaction
    /// pipeline, and verifies the result against the manifest.
    ///
    /// When <c>test-documents/real-documents/</c> is absent or empty (e.g. CI, a fresh clone), every case
    /// <see cref="Skip"/>s, so the suite stays green without the documents.
    /// </summary>
    public sealed class RealDocumentTests
    {
        private sealed class Manifest
        {
            public string File { get; set; } = string.Empty;
            public string Format { get; set; } = string.Empty;
            public string[] Policy { get; set; } = Array.Empty<string>();
            public ExpectedDetection[] Expected { get; set; } = Array.Empty<ExpectedDetection>();
            public bool MetadataRemoved { get; set; }
        }

        private sealed class ExpectedDetection
        {
            public string? Type { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        /// <summary>Walks up from the test output to find the repo's real-documents folder; null if absent.</summary>
        private static string? RealDocumentsDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "test-documents", "real-documents");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            return null;
        }

        public static IEnumerable<object[]> Manifests()
        {
            string? dir = RealDocumentsDir();
            string[] found = dir is null ? Array.Empty<string>() : Directory.GetFiles(dir, "*.expected.json");
            if (found.Length == 0)
            {
                yield return new object[] { string.Empty }; // sentinel: nothing to test -> the case skips
                yield break;
            }
            foreach (string manifest in found.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                yield return new object[] { manifest };
            }
        }

        [SkippableTheory]
        [MemberData(nameof(Manifests))]
        public async Task RealDocument_MatchesExpectations(string manifestPath)
        {
            Skip.If(string.IsNullOrEmpty(manifestPath),
                "No documents in test-documents/real-documents/ (the folder is gitignored; add documents + .expected.json manifests to run these).");

            Manifest manifest = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(manifestPath), JsonOptions)!;
            string docPath = Path.Combine(Path.GetDirectoryName(manifestPath)!, manifest.File);
            Skip.IfNot(File.Exists(docPath), $"Document '{manifest.File}' referenced by the manifest is missing.");

            PhileasPolicy policy = BuildPolicy(manifest.Policy);
            var filterService = new FilterService();

            // Detect across the document (RedactionVerifier reads each supported format and runs the
            // detector; here we point it at the source to enumerate everything it finds).
            VerificationOutcome detection = RedactionVerifier.Verify(docPath, policy, "ctx", filterService);
            Assert.NotEqual(VerificationStatus.Error, detection.Status);
            IReadOnlyList<RedactionSpanEntity> spans = detection.Residuals;

            // Every expected item must be detected (matched by text, and by type when the manifest gives one).
            foreach (ExpectedDetection expected in manifest.Expected)
            {
                Assert.True(
                    spans.Any(s => s.Text.Contains(expected.Text, StringComparison.Ordinal)
                                   && (string.IsNullOrEmpty(expected.Type)
                                       || string.Equals(s.FilterType, expected.Type, StringComparison.OrdinalIgnoreCase)
                                       || string.Equals(s.Classification, expected.Type, StringComparison.OrdinalIgnoreCase))),
                    $"Expected detection not found in {manifest.File}: {expected.Type} \"{expected.Text}\"");
            }

            // For .docx/.pdf, verify the redacted output carries no identifying metadata.
            if (manifest.MetadataRemoved)
            {
                await AssertMetadataRemoved(docPath, manifest.Format, policy, filterService);
            }
        }

        private static async Task AssertMetadataRemoved(string docPath, string format, PhileasPolicy policy, FilterService filterService)
        {
            string output = Path.Combine(Path.GetTempPath(),
                "philter-realdoc-" + Guid.NewGuid().ToString("N") + Path.GetExtension(docPath));
            try
            {
                await RedactionService.RedactFileAsync(docPath, output, policy, "ctx", filterService,
                    wordScrub: WordScrubOptions.Metadata);

                // Assert none of the source's identifying metadata values survive into the redacted output.
                if (format.Equals("docx", StringComparison.OrdinalIgnoreCase))
                {
#pragma warning disable OOXML0001
                    string[] sourceValues, outputValues;
                    using (var src = WordprocessingDocument.Open(docPath, isEditable: false))
                    {
                        var p = src.PackageProperties;
                        sourceValues = new[] { p.Creator, p.Title, p.Subject, p.Keywords, p.LastModifiedBy };
                    }
                    using (var outDoc = WordprocessingDocument.Open(output, isEditable: false))
                    {
                        var p = outDoc.PackageProperties;
                        outputValues = new[] { p.Creator, p.Title, p.Subject, p.Keywords, p.LastModifiedBy };
                    }
#pragma warning restore OOXML0001
                    AssertNoLeak(sourceValues, outputValues);
                }
                else if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    string[] sourceValues, outputValues;
                    using (var src = PdfDocument.Open(docPath))
                    {
                        var i = src.Information;
                        sourceValues = new[] { i.Title, i.Author, i.Subject, i.Keywords, i.Creator, i.Producer };
                    }
                    using (var outDoc = PdfDocument.Open(output))
                    {
                        var i = outDoc.Information;
                        outputValues = new[] { i.Title, i.Author, i.Subject, i.Keywords, i.Creator, i.Producer };
                    }
                    AssertNoLeak(sourceValues, outputValues);
                }
            }
            finally
            {
                try { File.Delete(output); } catch { /* best effort */ }
            }
        }

        // No non-empty source metadata value may appear among the output's metadata values.
        private static void AssertNoLeak(string?[] sourceValues, string?[] outputValues)
        {
            string outputBlob = string.Join("\n", outputValues.Where(v => !string.IsNullOrEmpty(v)));
            foreach (string value in sourceValues.Where(v => !string.IsNullOrWhiteSpace(v)).Cast<string>())
            {
                Assert.DoesNotContain(value, outputBlob);
            }
        }

        // Builds a policy enabling the named identifiers (by their Phileas property names, e.g.
        // "EmailAddress", "PhoneNumber"), discovered by reflection so it never drifts from the engine.
        private static PhileasPolicy BuildPolicy(IEnumerable<string> filterNames)
        {
            var identifiers = new Identifiers();
            Dictionary<string, PropertyInfo> properties = FilterCatalog.Discover();
            foreach (string name in filterNames)
            {
                if (properties.TryGetValue(name, out PropertyInfo? property)
                    && property.CanWrite
                    && property.PropertyType.GetConstructor(Type.EmptyTypes) is not null)
                {
                    property.SetValue(identifiers, Activator.CreateInstance(property.PropertyType));
                }
            }
            return new PhileasPolicy { Name = "real-document", Identifiers = identifiers };
        }
    }
}
