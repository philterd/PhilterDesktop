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

using Phileas.Services;
using Phileas.Services.Pdf;
using PhilterDesktop;
using Windows.Media.Ocr;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class PdfTextLayerQualityTests : IDisposable
    {
        internal const string Prose =
            "Patient Name: Bays, Brandon    Service Date: 12/01/2014\n" +
            "DOB: June 14, 1983    SSN: 123-45-6789\n" +
            "Brandon is a 31-year-old male who has a history of severe pectus excavatum deformity.\n" +
            "He underwent a repair in 2003, which was complicated by sternal infection.";

        private readonly string _tempDir = Path.Combine(Path.GetTempPath(), "philter-tlq-" + Guid.NewGuid().ToString("N"));

        public PdfTextLayerQualityTests() => Directory.CreateDirectory(_tempDir);

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        // What an Identity-H font without /ToUnicode extracts as: each code point shifted down by 29.
        private static string Shift(string text) => new(text.Select(c => c == '\n' ? c : (char)(c - 29)).ToArray());

        private static bool OcrAvailable => OcrEngine.TryCreateFromUserProfileLanguages() is not null;

        private string WriteTemp(string name, byte[] bytes)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        // --- LooksUnreadable: readable text ---

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   \n\t ")]
        public void LooksUnreadable_EmptyOrWhitespace_False(string? text) =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(text));

        [Fact]
        public void LooksUnreadable_EnglishProse_False() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(Prose));

        [Fact]
        public void LooksUnreadable_AllCapsText_False() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(Prose.ToUpperInvariant()));

        [Theory]
        [InlineData("El paciente es un hombre de treinta y un años con antecedentes de deformidad torácica severa.")]
        [InlineData("Der Patient ist ein einunddreißigjähriger Mann mit einer schweren Brustkorbdeformität in der Vorgeschichte.")]
        [InlineData("Le patient est un homme de trente et un ans ayant des antécédents de déformation thoracique sévère.")]
        public void LooksUnreadable_OtherLatinLanguages_False(string text) =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(text));

        [Theory]
        [InlineData("Пациент мужчина тридцати одного года с тяжелой деформацией грудной клетки в анамнезе.")]
        [InlineData("患者は三十一歳の男性で、重度の漏斗胸の既往があります。二〇〇三年に修復術を受けました。")]
        public void LooksUnreadable_NonLatinScript_False(string text) =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(text));

        [Fact]
        public void LooksUnreadable_MostlyNumbersTable_False() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(
                "2014 12 01 09:58 3550267 78750577 6.45 2.37 12/09/2014 13:43 555-123-4567 100 200 300 400"));

        [Fact]
        public void LooksUnreadable_TabsAndNewlinesAreNotBad_False() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(Prose.Replace(" ", "\t").Replace(",", "\r\n")));

        // --- LooksUnreadable: unreadable text ---

        [Fact]
        public void LooksUnreadable_ShiftedGlyphIds_True() =>
            Assert.True(PdfTextLayerQuality.LooksUnreadable(Shift(Prose)));

        [Fact]
        public void LooksUnreadable_ShiftedLettersWithRealSpaces_TrueByVowelRatio()
        {
            // Some producers map the space glyph correctly; the letters alone must still give it away.
            string text = string.Join(" ", Prose.Split(' ', '\n').Select(w => Shift(new string(w.Where(char.IsLetter).ToArray()))));
            Assert.DoesNotContain(text, char.IsControl);
            Assert.True(PdfTextLayerQuality.LooksUnreadable(text));
        }

        [Fact]
        public void LooksUnreadable_ShortGibberish_FalseBecauseTooShortToJudge() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(Shift("Patient Name: Bays")));

        [Fact]
        public void LooksUnreadable_PrivateUseGlyphs_True() =>
            Assert.True(PdfTextLayerQuality.LooksUnreadable(new string('', 50)));

        [Fact]
        public void LooksUnreadable_ReplacementCharacters_True() =>
            Assert.True(PdfTextLayerQuality.LooksUnreadable(Prose[..60] + new string('�', 10)));

        [Fact]
        public void LooksUnreadable_FewStrayControlChars_False() =>
            Assert.False(PdfTextLayerQuality.LooksUnreadable(Prose + "\u0002"));

        // --- FormatPages / Warning ---

        [Theory]
        [InlineData(new[] { 3 }, "3")]
        [InlineData(new[] { 11, 12, 13 }, "11-13")]
        [InlineData(new[] { 2, 4 }, "2, 4")]
        [InlineData(new[] { 1, 11, 12, 13, 14, 15, 16, 17, 18, 21 }, "1, 11-18, 21")]
        public void FormatPages_CollapsesRuns(int[] pages, string expected) =>
            Assert.Equal(expected, PdfTextLayerQuality.FormatPages(pages));

        [Fact]
        public void Warning_NoPages_Null() =>
            Assert.Null(PdfTextLayerQuality.Warning(Array.Empty<int>(), ocrScannedPdfs: false));

        [Fact]
        public void Warning_OnePage_Singular()
        {
            string w = PdfTextLayerQuality.Warning(new[] { 11 }, ocrScannedPdfs: false)!;
            Assert.StartsWith("Page 11 of this PDF has text", w);
            Assert.Contains("it was not checked", w);
            Assert.Contains("Turn on OCR", w);
        }

        [Fact]
        public void Warning_SeveralPages_PluralWithRanges()
        {
            string w = PdfTextLayerQuality.Warning(new[] { 11, 12, 13, 21 }, ocrScannedPdfs: false)!;
            Assert.StartsWith("Pages 11-13, 21 of this PDF have text", w);
            Assert.Contains("they were not checked", w);
        }

        [Fact]
        public void Warning_OcrOnButUnavailable_SaysToInstallLanguage() =>
            Assert.Contains("no OCR language is installed", PdfTextLayerQuality.Warning(new[] { 1 }, ocrScannedPdfs: true));

        [Fact]
        public void Warning_HasNoEmDashes() =>
            Assert.DoesNotContain('—', PdfTextLayerQuality.Warning(new[] { 1, 2 }, ocrScannedPdfs: false)!);

        // --- UnreadablePages ---

        [Fact]
        public void UnreadablePages_FromLines_GroupsByPage()
        {
            var lines = new[]
            {
                new PdfLine(1, Prose, Array.Empty<CharBox?>()),
                new PdfLine(2, Shift(Prose[..50]), Array.Empty<CharBox?>()),
                new PdfLine(2, Shift(Prose[50..]), Array.Empty<CharBox?>()),
                new PdfLine(3, Prose, Array.Empty<CharBox?>())
            };
            Assert.Equal(new[] { 2 }, PdfTextLayerQuality.UnreadablePages(lines));
        }

        [Fact]
        public void UnmappedFixture_ExtractsAsGlyphIdGibberish()
        {
            // Guards the fixture itself: it must reproduce the real-world symptom.
            string text = string.Join("\n", new PdfTextExtractor().GetLines(MinimalPdf.Pages((Prose, true))).Select(l => l.Text));
            Assert.Contains("3DWLHQW", text);
            Assert.DoesNotContain("Patient", text);
        }

        [Fact]
        public void UnreadablePages_FromPdf_FindsOnlyUnmappedPages()
        {
            byte[] pdf = MinimalPdf.Pages((Prose, false), (Prose, true), (Prose, true), (Prose, false));
            Assert.Equal(new[] { 2, 3 }, PdfTextLayerQuality.UnreadablePages(pdf));
        }

        [Fact]
        public void UnreadablePages_ReadablePdf_None() =>
            Assert.Empty(PdfTextLayerQuality.UnreadablePages(MinimalPdf.Pages((Prose, false))));

        [Fact]
        public void UnreadablePages_NotAPdf_None() =>
            Assert.Empty(PdfTextLayerQuality.UnreadablePages(new byte[] { 1, 2, 3, 4 }));

        // --- WarningFor(path) ---

        [Fact]
        public void WarningFor_UnmappedPdf_OcrOff_NamesThePage()
        {
            string path = WriteTemp("u.pdf", MinimalPdf.Pages((Prose, false), (Prose, true)));
            string? w = PdfTextLayerQuality.WarningFor(path, ocrScannedPdfs: false);
            Assert.NotNull(w);
            Assert.StartsWith("Page 2 ", w);
        }

        [SkippableFact]
        public void WarningFor_UnmappedPdf_OcrOnAndAvailable_Null()
        {
            Skip.IfNot(OcrAvailable, "No OCR language is available on this machine.");
            string path = WriteTemp("u.pdf", MinimalPdf.Pages((Prose, true)));
            Assert.Null(PdfTextLayerQuality.WarningFor(path, ocrScannedPdfs: true));
        }

        [Fact]
        public void WarningFor_ReadablePdf_Null()
        {
            string path = WriteTemp("r.pdf", MinimalPdf.Pages((Prose, false)));
            Assert.Null(PdfTextLayerQuality.WarningFor(path, ocrScannedPdfs: false));
        }

        [Fact]
        public void WarningFor_NonPdf_Null()
        {
            string path = Path.Combine(_tempDir, "u.txt");
            File.WriteAllText(path, Shift(Prose));
            Assert.Null(PdfTextLayerQuality.WarningFor(path, ocrScannedPdfs: false));
        }

        [Fact]
        public void WarningFor_MissingFile_Null() =>
            Assert.Null(PdfTextLayerQuality.WarningFor(Path.Combine(_tempDir, "missing.pdf"), ocrScannedPdfs: false));

        [Fact]
        public void WarningFor_CorruptPdf_Null() =>
            Assert.Null(PdfTextLayerQuality.WarningFor(WriteTemp("bad.pdf", new byte[] { 0x25, 0x50, 0x44, 0x46 }), ocrScannedPdfs: false));

        // --- DroppedContentWarning ---

        [Fact]
        public void DroppedContentWarning_UnmappedPdf_IncludesUnreadableWarning()
        {
            string path = WriteTemp("u.pdf", MinimalPdf.Pages((Prose, true)));
            Assert.Contains("can't be read", DroppedContentWarning.For(path));
        }

        [Fact]
        public void DroppedContentWarning_ReadablePdf_Null() =>
            Assert.Null(DroppedContentWarning.For(WriteTemp("r.pdf", MinimalPdf.Pages((Prose, false)))));

        [Fact]
        public void DroppedContentWarning_AnnotatedReadablePdf_OnlyFidelityWarning()
        {
            string path = WriteTemp("a.pdf", MinimalPdf.WithAnnotationAndFormField(Prose.Replace("\n", " "), "note", "value"));
            Assert.Equal(PdfFidelity.Warning, DroppedContentWarning.For(path));
        }

        // --- OCR routing ---

        [Fact]
        public void DecideMode_UnreadableText_ReplaceEvenWithRealTextAndLargeImage() =>
            Assert.Equal(OcrPageMode.Replace,
                HybridTextExtractor.DecideMode(0.13 * 612 * 792, 612 * 792, 0.9, 0.01, 0.5, textUnreadable: true));

        [Fact]
        public void DecideMode_UnreadableText_ReplaceWithUnknownGeometry() =>
            Assert.Equal(OcrPageMode.Replace,
                HybridTextExtractor.DecideMode(5000, 0, 0, 0.01, 0.5, textUnreadable: true));

        [SkippableFact]
        public void CountPagesNeedingOcr_CountsUnmappedPagesOnly()
        {
            Skip.IfNot(OcrAvailable, "No OCR language is available on this machine.");
            byte[] pdf = MinimalPdf.Pages((Prose, false), (Prose, true), (Prose, false), (Prose, true));
            Assert.Equal(2, new HybridTextExtractor().CountPagesNeedingOcr(pdf));
        }

        [SkippableFact]
        public void GetLines_UnmappedPage_DropsGibberishTextLayer_KeepsReadablePage()
        {
            Skip.IfNot(OcrAvailable, "No OCR language is available on this machine.");
            byte[] pdf = MinimalPdf.Pages((Prose, false), (Prose, true));

            IReadOnlyList<PdfLine> lines = new HybridTextExtractor().GetLines(pdf);

            Assert.Contains(lines, l => l.PageNumber == 1 && l.Text.Contains("Patient Name"));
            Assert.DoesNotContain(lines, l => l.PageNumber == 2 && l.Text.Contains("3DWLHQW"));
        }

        // --- End to end ---

        [Fact]
        public async Task Redact_OcrOff_UnmappedPageYieldsNoSpans_AndIsFlagged()
        {
            // Documents the failure the warning exists for: the readable page's SSN is found, the
            // identical unmapped page's is not.
            var policy = new PhileasPolicy
            {
                Name = "ssn",
                Identifiers = new Phileas.Policy.Identifiers { Ssn = new Phileas.Policy.Filters.Ssn() }
            };
            string source = WriteTemp("mixed.pdf", MinimalPdf.Pages((Prose, false), (Prose, true)));
            string output = Path.Combine(_tempDir, "mixed_redacted.pdf");

            var spans = await RedactionService.RedactFileAsync(source, output, policy, "ctx", new FilterService(), ocrScannedPdfs: false);

            Assert.Contains(spans, s => s.PageNumber == 1);
            Assert.DoesNotContain(spans, s => s.PageNumber == 2);
            Assert.StartsWith("Page 2 ", DroppedContentWarning.For(source, ocrScannedPdfs: false));
        }
    }
}
