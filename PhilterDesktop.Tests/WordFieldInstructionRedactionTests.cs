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
    /// A Word field's <b>instruction text</b> — the <c>w:instr</c> attribute of a simple field and the
    /// <c>w:instrText</c> runs of a complex field — carries PII (HYPERLINK mailto:/URL targets, INCLUDETEXT
    /// paths, merge sources) that the visible-result pass never touches. These pin that the instruction is
    /// redacted while the field keyword survives, across the Redact / Detect / Modify paths.
    /// </summary>
    public sealed class WordFieldInstructionRedactionTests : IDisposable
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Phileas.Policy.Identifiers { EmailAddress = new Phileas.Policy.Filters.EmailAddress() }
        };

        private static Func<string, TextFilterResult> EmailFilter =>
            t => new FilterService().Filter(EmailPolicy, "ctx", 0, t);

        private readonly string _dir;

        public WordFieldInstructionRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-field-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose() { try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ } }

        private string NewPath(string name) => Path.Combine(_dir, name);

        [Fact]
        public void Redact_ComplexHyperlinkField_InstructionEmailRemoved_KeywordKept()
        {
            string input = NewPath("complex.docx");
            string output = NewPath("complex_out.docx");
            WordDocs.CreateWithComplexField(input, new[] { " HYPERLINK \"mailto:john@example.com\" " }, "Contact us");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));
            Assert.True(WordDocs.AnyPartContains(output, "HYPERLINK")); // the field keyword stays
        }

        [Fact]
        public void Redact_SimpleField_InstructionEmailRemoved()
        {
            string input = NewPath("simple.docx");
            string output = NewPath("simple_out.docx");
            WordDocs.CreateWithSimpleField(input, " HYPERLINK \"mailto:jane@example.com\" ", "Email Jane");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "jane@example.com"));
            Assert.True(WordDocs.AnyPartContains(output, "HYPERLINK"));
        }

        [Fact]
        public void Redact_IncludeTextPath_EmailInPathRemoved()
        {
            string input = NewPath("include.docx");
            string output = NewPath("include_out.docx");
            WordDocs.CreateWithComplexField(input,
                new[] { " INCLUDETEXT \"C:\\\\mail\\\\john@example.com\\\\report.docx\" " }, "");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));
            Assert.True(WordDocs.AnyPartContains(output, "INCLUDETEXT"));
        }

        [Fact]
        public void Redact_InstructionSplitAcrossRuns_IsRedacted()
        {
            // The email is split across two <w:instrText> runs; the field-run merge must catch it.
            string input = NewPath("split.docx");
            string output = NewPath("split_out.docx");
            WordDocs.CreateWithComplexField(input,
                new[] { " HYPERLINK \"mailto:john@", "example.com\" " }, "Contact us");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));
            Assert.False(WordDocs.AnyPartContains(output, "example.com"));
        }

        [Fact]
        public void Redact_BenignInstruction_IsLeftIntact()
        {
            // A field with no PII in its instruction (PAGE) is untouched.
            string input = NewPath("page.docx");
            string output = NewPath("page_out.docx");
            WordDocs.CreateWithComplexField(input, new[] { " PAGE " }, "1");

            WordDocumentRedactor.Redact(input, output, EmailFilter);

            Assert.True(WordDocs.AnyPartContains(output, "PAGE"));
        }

        [Fact]
        public void Detect_FindsInstructionEmail()
        {
            string input = NewPath("detect.docx");
            WordDocs.CreateWithComplexField(input, new[] { " HYPERLINK \"mailto:john@example.com\" " }, "Contact");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, EmailFilter);

            RedactionSpanEntity? field = spans.FirstOrDefault(s => s.Classification == "field-instruction");
            Assert.NotNull(field);
            Assert.Contains("john@example.com", field!.Text);
        }

        [Fact]
        public void ApplySpans_WithDrawingFilter_RedactsInstruction()
        {
            string input = NewPath("apply.docx");
            string output = NewPath("apply_out.docx");
            WordDocs.CreateWithComplexField(input, new[] { " HYPERLINK \"mailto:john@example.com\" " }, "Contact");

            List<RedactionSpanEntity> spans = WordDocumentRedactor.Detect(input, EmailFilter);
            WordDocumentRedactor.ApplySpans(input, output, spans, highlight: false, drawingFilter: EmailFilter);

            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));
        }

        [Fact]
        public async Task RedactFileAsync_FieldInstruction_EndToEnd()
        {
            string input = NewPath("svc.docx");
            string output = NewPath("svc_out.docx");
            WordDocs.CreateWithComplexField(input, new[] { " HYPERLINK \"mailto:john@example.com\" " }, "Contact");

            await RedactionService.RedactFileAsync(input, output, EmailPolicy, "ctx", new SettingsEntity(), new FilterService());

            Assert.True(File.Exists(output));
            Assert.False(WordDocs.AnyPartContains(output, "john@example.com"));
        }
    }
}
