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

using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// RTF redaction runs on the document body only, so headers/footers/footnotes aren't carried into the
    /// output. That must be surfaced (not silent) — these cover the detection and the warning plumbing (#541).
    /// </summary>
    public sealed class RtfFidelityTests : IDisposable
    {
        private readonly string _dir;

        public RtfFidelityTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-rtffid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string WriteRtf(string body)
        {
            string path = Path.Combine(_dir, "f" + Guid.NewGuid().ToString("N") + ".rtf");
            File.WriteAllText(path, @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}" + body + "}");
            return path;
        }

        // --- Detection ----------------------------------------------------------------

        [Theory]
        [InlineData(@"{\header\pard Header line\par}\f0 Body a@b.com\par")]
        [InlineData(@"{\footer\pard foot@example.com\par}\f0 Body\par")]
        [InlineData(@"{\headerl\pard Left header\par}\f0 Body\par")]
        [InlineData(@"{\footerr\pard Right footer\par}\f0 Body\par")]
        [InlineData(@"\f0 Body with a note{\footnote\pard 222-22-2222\par} here.\par")]
        public void HasDroppedContent_TrueForHeaderFooterFootnote(string body)
        {
            Assert.True(RtfFidelity.HasDroppedContent(WriteRtf(body)));
        }

        [Fact]
        public void HasDroppedContent_FalseForBodyOnlyRtf()
        {
            Assert.False(RtfFidelity.HasDroppedContent(WriteRtf(@"\f0\fs24 Just body text with a@b.com.\par")));
        }

        [Fact]
        public void HasDroppedContent_FalseForNonRtfFile()
        {
            string txt = Path.Combine(_dir, "note.txt");
            File.WriteAllText(txt, @"{\header this is not really rtf} a@b.com");
            Assert.False(RtfFidelity.HasDroppedContent(txt)); // extension gate
        }

        [Fact]
        public void HasDroppedContent_FalseForMissingFile()
        {
            Assert.False(RtfFidelity.HasDroppedContent(Path.Combine(_dir, "nope.rtf")));
        }

        [Fact]
        public void Warning_IsHedged_AndPointsToDocx()
        {
            // No over-promising: it says the parts *may* not carry over and to review, not that anything is
            // guaranteed kept or safely destroyed.
            Assert.Contains("may not", RtfFidelity.Warning);
            Assert.Contains("review", RtfFidelity.Warning, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(".docx", RtfFidelity.Warning);
        }

        // --- Warning surfacing (MainForm helpers) -------------------------------------

        [Fact]
        public void EffectiveVerificationStatus_CleanWithContentDropped_BecomesContentDropped()
        {
            Assert.Equal("ContentDropped", MainForm.EffectiveVerificationStatus("Clean", nameDetectionUnavailable: false, contentDropped: true));
            Assert.Equal("ContentDropped", MainForm.EffectiveVerificationStatus("NotRun", nameDetectionUnavailable: false, contentDropped: true));
        }

        [Fact]
        public void EffectiveVerificationStatus_NamesTakePrecedenceOverContentDropped()
        {
            Assert.Equal("NamesNotChecked", MainForm.EffectiveVerificationStatus("Clean", nameDetectionUnavailable: true, contentDropped: true));
        }

        [Fact]
        public void EffectiveVerificationStatus_SevereStatesUnaffectedByContentDropped()
        {
            Assert.Equal("ResidualsFound", MainForm.EffectiveVerificationStatus("ResidualsFound", nameDetectionUnavailable: false, contentDropped: true));
            Assert.Equal("Error", MainForm.EffectiveVerificationStatus("Error", nameDetectionUnavailable: false, contentDropped: true));
        }

        [Fact]
        public void EffectiveVerificationStatus_NoContentDropped_LeavesStatus()
        {
            Assert.Equal("Clean", MainForm.EffectiveVerificationStatus("Clean", nameDetectionUnavailable: false, contentDropped: false));
        }

        [Fact]
        public void IsVerificationWarning_TrueForContentDropped()
        {
            Assert.True(MainForm.IsVerificationWarning(MainForm.ContentDroppedStatus));
        }

        [Fact]
        public void CombinedRedactionWarning_ContentDroppedOnly_ReturnsRtfWarning()
        {
            string? warning = MainForm.CombinedRedactionWarning(verification: null, nameDetectionUnavailable: false, contentDropped: true);
            Assert.Equal(RtfFidelity.Warning, warning);
        }

        [Fact]
        public void CombinedRedactionWarning_NamesAndContentDropped_IncludesBoth()
        {
            string? warning = MainForm.CombinedRedactionWarning(verification: null, nameDetectionUnavailable: true, contentDropped: true);
            Assert.NotNull(warning);
            Assert.Contains(PhEyeModel.UnavailableWarning, warning!);
            Assert.Contains(RtfFidelity.Warning, warning!);
        }
    }
}
