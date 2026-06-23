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

using System.IO.Compression;
using Phileas.Model;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Xceed.Words.NET;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Registers the Xceed license once for the test class (from the same sources the
    /// app uses). If no license is configured, <see cref="HasLicense"/> is false and the
    /// Word tests skip rather than fail.
    /// </summary>
    public sealed class XceedLicenseFixture
    {
        public bool HasLicense { get; }

        public XceedLicenseFixture()
        {
            string? key = LicenseConfig.GetXceedLicenseKey();
            if (!string.IsNullOrEmpty(key))
            {
                try
                {
                    Licenser.LicenseKey = key;
                    HasLicense = true;
                }
                catch
                {
                    HasLicense = false;
                }
            }
        }
    }

    public sealed class WordDocumentRedactorTests : IClassFixture<XceedLicenseFixture>, IDisposable
    {
        private const string SkipReason =
            "Xceed license not configured (place xceed-license.json next to the build output or set XCEED_LICENSE_KEY).";

        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress() }
        };

        // Real filter producing spans; emails redact to {{{REDACTED-email-address}}}.
        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly XceedLicenseFixture _license;
        private readonly string _tempDir;

        public WordDocumentRedactorTests(XceedLicenseFixture license)
        {
            _license = license;
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-word-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_tempDir, name);

        private string CreateDocx(params string[] paragraphs)
        {
            string path = NewPath("in_" + Guid.NewGuid().ToString("N") + ".docx");
            using var doc = DocX.Create(path);
            foreach (string p in paragraphs)
            {
                doc.InsertParagraph(p);
            }
            doc.Save();
            return path;
        }

        private static string[] ReadParagraphs(string path)
        {
            using var doc = DocX.Load(path);
            return doc.Paragraphs.Select(p => p.Text).ToArray();
        }

        // Regression test for the bug we hit: emptying a paragraph with the 2-arg
        // RemoveText overload deleted the whole paragraph, so the rebuild was lost.
        [SkippableFact]
        public void Redact_ReplacesChangedParagraphs_WithoutDeletingThem()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("keep one", "email aaa@example.com here", "email bbb@example.com too", "keep four");
            int originalCount = ReadParagraphs(input).Length;
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            string[] result = ReadParagraphs(output);
            Assert.Equal(originalCount, result.Length); // no paragraph deleted
            Assert.Contains("keep one", result);
            Assert.Contains("keep four", result);
            Assert.Contains("email {{{REDACTED-email-address}}} here", result);
            Assert.DoesNotContain(result, p => p.Contains("@example.com"));
        }

        [SkippableFact]
        public void Redact_UnchangedText_LeavesDocumentIdentical()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("alpha", "beta", "gamma"); // no PII
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Equal(ReadParagraphs(input), ReadParagraphs(output));
        }

        [SkippableFact]
        public void Redact_RedactsHeadersAndFooters()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = NewPath("hf.docx");
            using (var doc = DocX.Create(input))
            {
                doc.AddHeaders();
                doc.AddFooters();
                doc.DifferentFirstPage = false;
                doc.Headers.Odd.InsertParagraph("header hh@example.com");
                doc.Footers.Odd.InsertParagraph("footer ff@example.com");
                doc.InsertParagraph("body bb@example.com");
                doc.Save();
            }
            string output = NewPath("hf_out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            using var redacted = DocX.Load(output);
            string body = string.Join("|", redacted.Paragraphs.Select(p => p.Text));
            string header = string.Join("|", redacted.Headers.Odd.Paragraphs.Select(p => p.Text));
            string footer = string.Join("|", redacted.Footers.Odd.Paragraphs.Select(p => p.Text));

            Assert.DoesNotContain("@example.com", body + header + footer);
            Assert.Contains("REDACTED", header);
            Assert.Contains("REDACTED", footer);
            Assert.Contains("REDACTED", body);
        }

        [SkippableFact]
        public void Redact_LeavesInputFileUnchanged()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("contact data@example.com");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Contains(ReadParagraphs(input), p => p.Contains("@example.com"));
        }

        [SkippableFact]
        public void Redact_WithHighlight_HighlightsTheReplacement()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("email zzz@example.com end");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, Filter, highlight: true);

            Assert.Contains("email {{{REDACTED-email-address}}} end", ReadParagraphs(output));

            // The replacement run carries Word's native yellow highlight.
            string xml = ReadDocumentXml(output);
            Assert.Contains("highlight", xml);
            Assert.Contains("yellow", xml);
        }

        private static string ReadDocumentXml(string docxPath)
        {
            using ZipArchive zip = ZipFile.OpenRead(docxPath);
            ZipArchiveEntry entry = zip.GetEntry("word/document.xml")!;
            using var reader = new StreamReader(entry.Open());
            return reader.ReadToEnd();
        }
    }
}
