using PhilterDesktop;
using Xceed.Words.NET;
using Xunit;

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
        // RemoveText overload deleted the whole paragraph, so Append was lost.
        [SkippableFact]
        public void Redact_ReplacesChangedParagraphs_WithoutDeletingThem()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("keep one", "SECRET two", "SECRET three", "keep four");
            int originalCount = ReadParagraphs(input).Length;
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, t => t.Replace("SECRET", "[X]"));

            string[] result = ReadParagraphs(output);
            Assert.Equal(originalCount, result.Length); // no paragraph deleted
            Assert.Contains("keep one", result);
            Assert.Contains("[X] two", result);
            Assert.Contains("[X] three", result);
            Assert.Contains("keep four", result);
            Assert.DoesNotContain(result, p => p.Contains("SECRET"));
        }

        [SkippableFact]
        public void Redact_UnchangedText_LeavesDocumentIdentical()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("alpha", "beta", "gamma");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, t => t); // identity filter

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
                doc.Headers.Odd.InsertParagraph("SECRET header");
                doc.Footers.Odd.InsertParagraph("SECRET footer");
                doc.InsertParagraph("SECRET body");
                doc.Save();
            }
            string output = NewPath("hf_out.docx");

            WordDocumentRedactor.Redact(input, output, t => t.Replace("SECRET", "[X]"));

            using var redacted = DocX.Load(output);
            string body = string.Join("|", redacted.Paragraphs.Select(p => p.Text));
            string header = string.Join("|", redacted.Headers.Odd.Paragraphs.Select(p => p.Text));
            string footer = string.Join("|", redacted.Footers.Odd.Paragraphs.Select(p => p.Text));

            Assert.DoesNotContain("SECRET", body);
            Assert.DoesNotContain("SECRET", header);
            Assert.DoesNotContain("SECRET", footer);
            Assert.Contains("[X] header", header);
            Assert.Contains("[X] footer", footer);
            Assert.Contains("[X] body", body);
        }

        [SkippableFact]
        public void Redact_LeavesInputFileUnchanged()
        {
            Skip.IfNot(_license.HasLicense, SkipReason);

            string input = CreateDocx("SECRET data");
            string output = NewPath("out.docx");

            WordDocumentRedactor.Redact(input, output, t => t.Replace("SECRET", "[X]"));

            Assert.Contains(ReadParagraphs(input), p => p.Contains("SECRET"));
        }
    }
}
