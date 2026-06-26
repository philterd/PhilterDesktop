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
    }
}
