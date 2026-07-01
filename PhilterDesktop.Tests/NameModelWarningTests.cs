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
    /// When a queued redaction's policy wants on-device names but the model is missing, the main queue
    /// must surface a warning instead of a clean success (#573, parity with #556).
    /// </summary>
    public sealed class NameModelWarningTests
    {
        [Theory]
        [InlineData("Clean", true, "NamesNotChecked")]      // a "Clean" row would be misleading -> override
        [InlineData("NotRun", true, "NamesNotChecked")]
        [InlineData("ResidualsFound", true, "ResidualsFound")] // already a warning -> leave as-is
        [InlineData("Error", true, "Error")]
        [InlineData("Clean", false, "Clean")]                 // model present -> unchanged
        [InlineData("NotRun", false, "NotRun")]
        public void EffectiveVerificationStatus_OverridesOnlyCleanOrNotRun_WhenNamesUnavailable(
            string input, bool nameMissing, string expected)
        {
            Assert.Equal(expected, MainForm.EffectiveVerificationStatus(input, nameMissing));
        }

        [Theory]
        [InlineData("ResidualsFound", true)]   // cue the Status cell (amber/red)
        [InlineData("NamesNotChecked", true)]
        [InlineData("Clean", false)]
        [InlineData("NotRun", false)]
        [InlineData("Error", false)]
        public void IsVerificationWarning_TrueOnlyForReviewableStates(string status, bool expected)
        {
            Assert.Equal(expected, MainForm.IsVerificationWarning(status));
        }

        [Fact]
        public void CombinedRedactionWarning_NameModelMissingOnly_ReturnsNameWarning()
        {
            string? warning = MainForm.CombinedRedactionWarning(verification: null, nameDetectionUnavailable: true);
            Assert.Equal(PhEyeModel.UnavailableWarning, warning);
        }

        [Fact]
        public void CombinedRedactionWarning_NothingToWarnAbout_IsNull()
        {
            Assert.Null(MainForm.CombinedRedactionWarning(verification: null, nameDetectionUnavailable: false));
        }

        [Fact]
        public void CombinedRedactionWarning_ResidualsAndNames_IncludesBoth()
        {
            var outcome = new VerificationOutcome
            {
                Status = VerificationStatus.ResidualsFound,
                Residuals = new List<RedactionSpanEntity> { new(), new() }
            };

            string? warning = MainForm.CombinedRedactionWarning(outcome, nameDetectionUnavailable: true);

            Assert.NotNull(warning);
            Assert.Contains("may remain", warning!);                  // the residual warning
            Assert.Contains(PhEyeModel.UnavailableWarning, warning!); // and the name-model warning
        }
    }
}
