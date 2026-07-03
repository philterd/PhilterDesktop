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
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// PDF text living only in an annotation or form field isn't rendered into the rasterized output (so it
    /// is removed) — but it must still be detected and reported, and the user warned that the content was
    /// dropped. This covers the detection/reporting (engine) and the fidelity caveat (desktop).
    /// </summary>
    public sealed class PdfAnnotationRedactionTests : IDisposable
    {
        private readonly string _dir;
        private readonly FilterService _fs = new();

        public PdfAnnotationRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-pdfannot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose() { try { Directory.Delete(_dir, true); } catch { /* best effort */ } }

        private static PhileasPolicy EmailSsnPolicy() => new()
        {
            Name = "es",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        [Fact]
        public void HasDroppedContent_TrueForAnnotationsAndForms_FalseForPlain()
        {
            string withExtras = Path.Combine(_dir, "extras.pdf");
            File.WriteAllBytes(withExtras, MinimalPdf.WithAnnotationAndFormField("Body.", "a note", "a value"));
            Assert.True(PdfFidelity.HasDroppedContent(withExtras));

            string plain = Path.Combine(_dir, "plain.pdf");
            File.WriteAllBytes(plain, MinimalPdf.PlainText("Just the body."));
            Assert.False(PdfFidelity.HasDroppedContent(plain));
        }

        [Fact]
        public async Task Redact_ReportsPiiFoundInAnnotationsAndFormFields()
        {
            string input = Path.Combine(_dir, "in.pdf");
            await File.WriteAllBytesAsync(input,
                MinimalPdf.WithAnnotationAndFormField("Visible page text.", "SSN 123-45-6789", "george@fake.com"));
            string output = Path.Combine(_dir, "out.pdf");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, EmailSsnPolicy(), "ctx");

            Assert.Contains(spans, s => s.Text.Contains("123-45-6789"));    // FreeText annotation
            Assert.Contains(spans, s => s.Text.Contains("george@fake.com")); // AcroForm text field

            // The output is a flattened image-only PDF, so the annotation/form text isn't in it either.
            string outBytes = System.Text.Encoding.Latin1.GetString(await File.ReadAllBytesAsync(output));
            Assert.DoesNotContain("123-45-6789", outBytes);
            Assert.DoesNotContain("george@fake.com", outBytes);
        }

        [Fact]
        public async Task Verification_PdfWithAnnotations_CarriesFidelityCaveat()
        {
            string input = Path.Combine(_dir, "v-in.pdf");
            await File.WriteAllBytesAsync(input,
                MinimalPdf.WithAnnotationAndFormField("Body.", "SSN 123-45-6789", "george@fake.com"));
            string output = Path.Combine(_dir, "v-out.pdf");
            await RedactionService.RedactFileAsync(input, output, EmailSsnPolicy(), "ctx");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailSsnPolicy(), "ctx", _fs, sourcePath: input);

            Assert.NotNull(outcome.FidelityNote);
            Assert.Contains("annotation", outcome.FidelityNote!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task Verification_PlainPdf_HasNoFidelityCaveat()
        {
            string input = Path.Combine(_dir, "plain-in.pdf");
            await File.WriteAllBytesAsync(input, MinimalPdf.PlainText("Contact george@fake.com."));
            string output = Path.Combine(_dir, "plain-out.pdf");
            await RedactionService.RedactFileAsync(input, output, EmailSsnPolicy(), "ctx");

            VerificationOutcome outcome = RedactionVerifier.Verify(output, EmailSsnPolicy(), "ctx", _fs, sourcePath: input);

            Assert.Null(outcome.FidelityNote);
        }
    }
}
