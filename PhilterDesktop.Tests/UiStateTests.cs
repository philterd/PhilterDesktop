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

using System.Drawing;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class UiStateTests
    {
        private static readonly Rectangle[] SingleScreen = { new Rectangle(0, 0, 1920, 1080) };
        private static readonly Rectangle[] TwoScreens =
        {
            new Rectangle(0, 0, 1920, 1080),
            new Rectangle(1920, 0, 1920, 1080),
        };

        [Fact]
        public void IsBoundsVisible_FullyOnScreen_True()
        {
            Assert.True(UiState.IsBoundsVisible(new Rectangle(100, 100, 800, 600), SingleScreen));
        }

        [Fact]
        public void IsBoundsVisible_OnSecondMonitor_True()
        {
            Assert.True(UiState.IsBoundsVisible(new Rectangle(2000, 100, 800, 600), TwoScreens));
        }

        [Fact]
        public void IsBoundsVisible_OffScreen_False()
        {
            // Saved on a monitor that's no longer connected (only the primary remains).
            Assert.False(UiState.IsBoundsVisible(new Rectangle(2000, 100, 800, 600), SingleScreen));
        }

        [Fact]
        public void IsBoundsVisible_OnlyASliverShowing_False()
        {
            // Just a few pixels poke onto the screen — not enough to grab the title bar.
            Assert.False(UiState.IsBoundsVisible(new Rectangle(-790, 100, 800, 600), SingleScreen));
        }

        [Fact]
        public void IsBoundsVisible_ZeroSize_False()
        {
            Assert.False(UiState.IsBoundsVisible(new Rectangle(0, 0, 0, 0), SingleScreen));
        }

        [Fact]
        public void Widths_RoundTrip()
        {
            string text = UiState.FormatWidths(new[] { 350, 180, 120, 120 });
            Assert.Equal("350,180,120,120", text);

            int[]? parsed = UiState.ParseWidths(text, 4);
            Assert.NotNull(parsed);
            Assert.Equal(new[] { 350, 180, 120, 120 }, parsed!);
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("350,180,120")]       // wrong count
        [InlineData("350,180,120,120,90")] // wrong count
        [InlineData("350,abc,120,120")]    // non-numeric
        [InlineData("350,0,120,120")]      // non-positive
        [InlineData("350,-5,120,120")]     // negative
        public void ParseWidths_InvalidInput_ReturnsNull(string? text)
        {
            Assert.Null(UiState.ParseWidths(text, 4));
        }

        private static readonly Rectangle WorkingArea = new Rectangle(0, 0, 1366, 728); // 768 minus a 40px taskbar

        [Fact]
        public void FitToWorkingArea_AlreadyFits_Unchanged()
        {
            var bounds = new Rectangle(100, 50, 640, 480);
            Assert.Equal(bounds, UiState.FitToWorkingArea(bounds, WorkingArea));
        }

        [Fact]
        public void FitToWorkingArea_TallerThanScreen_ShrinksAndStaysAboveTaskbar()
        {
            var fitted = UiState.FitToWorkingArea(new Rectangle(300, -60, 1100, 1050), WorkingArea);
            Assert.Equal(new Rectangle(266, 0, 1100, 728), fitted);
            Assert.True(WorkingArea.Contains(fitted));
        }

        [Fact]
        public void FitToWorkingArea_HangingOffBottomRight_MovedInside()
        {
            var fitted = UiState.FitToWorkingArea(new Rectangle(1000, 600, 640, 480), WorkingArea);
            Assert.Equal(new Rectangle(726, 248, 640, 480), fitted);
        }

        [Fact]
        public void FitToWorkingArea_SecondMonitor_StaysOnThatMonitor()
        {
            var secondary = new Rectangle(1920, 0, 1920, 1040);
            var fitted = UiState.FitToWorkingArea(new Rectangle(2200, 100, 700, 1300), secondary);
            Assert.Equal(new Rectangle(2200, 0, 700, 1040), fitted);
        }
    }
}
