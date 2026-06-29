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

using Phileas.Model;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// DrawingML text (&lt;a:t&gt; runs in shapes, SmartArt, and charts) is not made of WordprocessingML
    /// paragraphs, so it was never filtered — PII in a shape/SmartArt/chart label survived
    /// (philterd-website issue #479). These pin that DrawingML text is redacted across the package, in
    /// both the Redact and ApplySpans (Modify) paths.
    /// </summary>
    public sealed class WordDrawingTextRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> Filter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordDrawingTextRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-draw-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewPath(string name) => Path.Combine(_dir, name);

        [Fact]
        public void Redact_ChartTitleText_IsRedacted()
        {
            string input = NewPath("chart.docx");
            string output = NewPath("chart_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("Sales for chart@example.com"), "body plain");
            Assert.True(WordDocs.AnyPartContains(input, "chart@example.com"));

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "chart PII must not survive");
            Assert.Contains("REDACTED", WordDocs.AllDrawingText(output));
        }

        [Fact]
        public void Redact_SmartArtNodeText_IsRedacted()
        {
            string input = NewPath("sa.docx");
            string output = NewPath("sa_out.docx");
            WordDocs.CreateWithSmartArt(input, WordDocs.AParagraph("node smart@example.com"), "body plain");
            Assert.True(WordDocs.AnyPartContains(input, "smart@example.com"));

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"), "SmartArt PII must not survive");
            Assert.Contains("REDACTED", WordDocs.AllDrawingText(output));
        }

        [Fact]
        public void Redact_DrawingTextSplitAcrossRuns_IsRedacted()
        {
            // PII split across two <a:t> runs in one <a:p> must still be caught (runs are concatenated).
            string input = NewPath("split.docx");
            string output = NewPath("split_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("contact ", "split@example.com"), "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_MultiplePiiInOneDrawingParagraph_AllRedacted()
        {
            string input = NewPath("multi.docx");
            string output = NewPath("multi_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("a@example.com and b@example.com"), "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_NonPiiDrawingText_IsPreserved()
        {
            string input = NewPath("clean.docx");
            string output = NewPath("clean_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("Quarterly Revenue"), "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.Contains("Quarterly Revenue", WordDocs.AllDrawingText(output));
            Assert.DoesNotContain("REDACTED", WordDocs.AllDrawingText(output));
        }

        [Fact]
        public void Redact_ChartAndSmartArtAndBody_AllRedacted()
        {
            string input = NewPath("mix.docx");
            string output = NewPath("mix_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body b@example.com");
            // add a SmartArt part to the same doc
            using (var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(input, true))
            {
                var dp = doc.MainDocumentPart!.AddNewPart<DocumentFormat.OpenXml.Packaging.DiagramDataPart>();
                using var s = dp.GetStream(FileMode.Create, FileAccess.Write);
                using var w = new StreamWriter(s);
                w.Write("<dgm:dataModel xmlns:dgm=\"http://schemas.openxmlformats.org/drawingml/2006/diagram\" xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\"><dgm:ptLst><dgm:pt><dgm:t><a:p><a:r><a:t>smart s@example.com</a:t></a:r></a:p></dgm:t></dgm:pt></dgm:ptLst></dgm:dataModel>");
            }

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void ApplySpans_WithDrawingFilter_RedactsChartText()
        {
            string input = NewPath("apply.docx");
            string output = NewPath("apply_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body b@example.com");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false, drawingFilter: Filter);

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void ApplySpans_WithoutDrawingFilter_LeavesChartText()
        {
            // Without a drawing filter, only the span-based (w:p) redaction runs — documents that the
            // Modify/preview paths must pass a filter (they do) to also clean DrawingML text.
            string input = NewPath("nofilter.docx");
            string output = NewPath("nofilter_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false);

            Assert.True(WordDocs.AnyPartContains(output, "c@example.com"));
        }

        [Fact]
        public async Task RedactFileAsync_DrawingText_EndToEnd()
        {
            string input = NewPath("svc.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            Assert.True(File.Exists(output));
            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public async Task ApplySpansAsync_Modify_RedactsDrawingText()
        {
            string input = NewPath("modify.docx");
            string output = NewPath("modify_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body b@example.com");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, Filter);
            await RedactionService.ApplySpansAsync(input, output, ".docx", highlight: false, spans, EmailPolicy, new FilterService());

            Assert.False(WordDocs.AnyPartContains(output, "@example.com"));
        }

        [Fact]
        public void Redact_DocumentWithoutDrawings_StillWorks()
        {
            string input = NewPath("none.docx");
            string output = NewPath("none_out.docx");
            WordDocs.Create(input, "body has body@example.com");

            WordDocumentRedactor.Redact(input, output, Filter);

            Assert.DoesNotContain(WordDocs.BodyParagraphs(output), p => p.Contains("@example.com"));
        }

        [Fact]
        public void Redact_DrawingDocument_StaysValid()
        {
            string input = NewPath("valid.docx");
            string output = NewPath("valid_out.docx");
            WordDocs.CreateWithChart(input, WordDocs.AParagraph("chart c@example.com"), "body");

            WordDocumentRedactor.Redact(input, output, Filter);

            using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(output, false);
            Assert.NotNull(doc.MainDocumentPart?.Document?.Body);
            Assert.True(doc.MainDocumentPart!.GetPartsOfType<DocumentFormat.OpenXml.Packaging.ChartPart>().Any(),
                "the chart part must survive redaction");
        }
    }
}
