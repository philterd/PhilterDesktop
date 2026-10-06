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
using Xunit;
using static PhilterDesktop.Tests.DpiLayoutTests;

namespace PhilterDesktop.Tests
{
    /// <summary>ModernTheme's DPI helpers, and the LayoutAudit oracle the DPI layout tests rely on.</summary>
    public sealed class ModernThemeScalingTests
    {
        // --- EnableDpiScaling -------------------------------------------------

        [Fact]
        public void Apply_EnablesDpiScaling_OnCodeBuiltForm() => Sta(() =>
        {
            using var form = new Form();
            Assert.Equal(AutoScaleMode.Inherit, form.AutoScaleMode);

            ModernTheme.Apply(form);

            Assert.Equal(AutoScaleMode.Dpi, form.AutoScaleMode);
            Assert.Equal(new SizeF(form.DeviceDpi, form.DeviceDpi), form.AutoScaleDimensions); // already scaled for this DPI
        });

        [Fact]
        public void EnableDpiScaling_LeavesDesignerScaledFormsAlone() => Sta(() =>
        {
            using var form = new Form { AutoScaleDimensions = new SizeF(7F, 15F), AutoScaleMode = AutoScaleMode.Font };
            SizeF dims = form.AutoScaleDimensions;

            ModernTheme.EnableDpiScaling(form);

            Assert.Equal(AutoScaleMode.Font, form.AutoScaleMode);
            Assert.Equal(dims, form.AutoScaleDimensions);
        });

        [Fact]
        public void EnableDpiScaling_TwiceDoesNotScaleTwice() => Sta(() =>
        {
            using var form = new Form { ClientSize = new Size(400, 300) };
            var button = new Button { Bounds = new Rectangle(10, 10, 100, 30) };
            form.Controls.Add(button);

            ModernTheme.EnableDpiScaling(form);
            Rectangle once = button.Bounds;
            Size clientOnce = form.ClientSize;
            ModernTheme.EnableDpiScaling(form);

            Assert.Equal(once, button.Bounds);
            Assert.Equal(clientOnce, form.ClientSize);
        });

        [SkippableFact]
        public void EnableDpiScaling_KeepsAuthoredSizes_At96Dpi() => Sta(() =>
        {
            using var form = new Form { ClientSize = new Size(400, 300) };
            Skip.If(form.DeviceDpi != 96, "authored sizes only stay as-is at 100%");
            var button = new Button { Bounds = new Rectangle(10, 10, 100, 30) };
            form.Controls.Add(button);

            ModernTheme.EnableDpiScaling(form);

            Assert.Equal(new Rectangle(10, 10, 100, 30), button.Bounds);
            Assert.Equal(new Size(400, 300), form.ClientSize);
        });

        // --- FitHeightToText --------------------------------------------------

        private const string LongText =
            "A deliberately long sentence that has to wrap onto several lines when the label is narrow, " +
            "so its height depends on how wide it is.";

        [Fact]
        public void FitHeightToText_GrowsAndShrinksWithWidth() => Sta(() =>
        {
            using var label = new Label { Text = LongText, Width = 600 };
            ModernTheme.FitHeightToText(label);
            int wide = label.Height;

            label.Width = 150;
            int narrow = label.Height;
            label.Width = 600;

            Assert.True(narrow > wide, $"narrow {narrow} should be taller than wide {wide}");
            Assert.Equal(wide, label.Height);
            Assert.Equal(label.GetPreferredSize(new Size(label.Width, 0)).Height, label.Height);
        });

        [Fact]
        public void FitHeightToText_TracksTextAndFontChanges() => Sta(() =>
        {
            using var label = new Label { Text = "Short", Width = 200 };
            ModernTheme.FitHeightToText(label);
            int oneLine = label.Height;

            label.Text = LongText;
            int wrapped = label.Height;
            label.Font = new Font(label.Font.FontFamily, label.Font.Size * 2);
            int bigger = label.Height;

            Assert.True(wrapped > oneLine);
            Assert.True(bigger > wrapped);
        });

        [Fact]
        public void FitHeightToText_EmptyTextAndZeroWidth_AreSafe() => Sta(() =>
        {
            using var empty = new Label { Text = string.Empty, Width = 200 };
            ModernTheme.FitHeightToText(empty);
            Assert.True(empty.Height >= 0);

            using var zero = new Label { Text = LongText, Width = 0, Height = 17 };
            ModernTheme.FitHeightToText(zero);
            Assert.Equal(17, zero.Height); // nothing to measure against yet
            Assert.False(zero.AutoSize);
        });

        [Fact]
        public void FitHeightToText_DockedTop_FollowsParentResize() => Sta(() =>
        {
            using var form = new Form { ClientSize = new Size(900, 400) };
            var label = new Label { Text = LongText, Dock = DockStyle.Top };
            form.Controls.Add(label);
            ModernTheme.FitHeightToText(label);
            form.Show();
            Application.DoEvents();
            int wide = label.Height;

            form.ClientSize = new Size(form.LogicalToDeviceUnits(300), 400); // narrow, but above Windows' minimum width
            Application.DoEvents();

            Assert.True(label.Height > wide);
            LayoutAudit.AssertClean(LayoutAudit.Audit(form));
            form.Close();
        });

        // --- LayoutAudit (the oracle) ------------------------------------------

        private static List<string> AuditShown(Action<Form> build)
        {
            using var form = new Form { ClientSize = new Size(300, 200), StartPosition = FormStartPosition.Manual, Location = Point.Empty, ShowInTaskbar = false };
            build(form);
            form.Show();
            Application.DoEvents();
            List<string> problems = LayoutAudit.Audit(form);
            form.Close();
            return problems;
        }

        [Fact]
        public void LayoutAudit_CleanLayout_HasNoProblems() => Sta(() =>
        {
            LayoutAudit.AssertClean(AuditShown(f =>
            {
                f.Controls.Add(new Label { Text = "Name:", AutoSize = true, Location = new Point(10, 10) });
                f.Controls.Add(new TextBox { Bounds = new Rectangle(80, 8, 150, 23) });
            }));
        });

        [Fact]
        public void LayoutAudit_DetectsOverlappingSiblings() => Sta(() =>
        {
            List<string> problems = AuditShown(f =>
            {
                f.Controls.Add(new TextBox { Name = "a", Bounds = new Rectangle(10, 10, 100, 23) });
                f.Controls.Add(new TextBox { Name = "b", Bounds = new Rectangle(50, 15, 100, 23) });
            });
            Assert.Contains(problems, p => p.Contains("'a'") && p.Contains("overlaps") && p.Contains("'b'"));
        });

        [Fact]
        public void LayoutAudit_IgnoresTouchingSiblings() => Sta(() =>
        {
            LayoutAudit.AssertClean(AuditShown(f =>
            {
                f.Controls.Add(new TextBox { Bounds = new Rectangle(10, 10, 100, 23) });
                f.Controls.Add(new TextBox { Bounds = new Rectangle(110, 10, 100, 23) }); // shares an edge
            }));
        });

        [Fact]
        public void LayoutAudit_DetectsChildClippedByParent() => Sta(() =>
        {
            List<string> problems = AuditShown(f =>
            {
                var panel = new Panel { Bounds = new Rectangle(0, 0, 200, 40) };
                panel.Controls.Add(new Button { Name = "tall", Text = "x", Bounds = new Rectangle(0, 10, 50, 45) });
                f.Controls.Add(panel);
            });
            Assert.Contains(problems, p => p.Contains("'tall'") && p.Contains("clipped"));
        });

        [Fact]
        public void LayoutAudit_AllowsOverflowInScrollingPanels() => Sta(() =>
        {
            LayoutAudit.AssertClean(AuditShown(f =>
            {
                var panel = new Panel { Bounds = new Rectangle(0, 0, 200, 40), AutoScroll = true };
                panel.Controls.Add(new Button { Text = "x", Bounds = new Rectangle(0, 10, 50, 45) });
                f.Controls.Add(panel);
            }));
        });

        [Fact]
        public void LayoutAudit_DetectsTruncatedFixedSizeText() => Sta(() =>
        {
            List<string> problems = AuditShown(f =>
                f.Controls.Add(new Label { Name = "cramped", AutoSize = false, Bounds = new Rectangle(0, 0, 60, 15), Text = LongText }));
            Assert.Contains(problems, p => p.Contains("'cramped'") && p.Contains("text needs"));
        });

        [Fact]
        public void LayoutAudit_IgnoresHiddenControls() => Sta(() =>
        {
            LayoutAudit.AssertClean(AuditShown(f =>
            {
                f.Controls.Add(new TextBox { Bounds = new Rectangle(10, 10, 100, 23) });
                f.Controls.Add(new TextBox { Bounds = new Rectangle(50, 15, 100, 23), Visible = false });
            }));
        });

        [Fact]
        public void LayoutAudit_ChecksEveryTabPage() => Sta(() =>
        {
            List<string> problems = AuditShown(f =>
            {
                var tabs = new TabControl { Dock = DockStyle.Fill };
                tabs.TabPages.Add(new TabPage("clean"));
                var second = new TabPage("broken");
                second.Controls.Add(new TextBox { Name = "a", Bounds = new Rectangle(10, 10, 100, 23) });
                second.Controls.Add(new TextBox { Name = "b", Bounds = new Rectangle(50, 15, 100, 23) });
                tabs.TabPages.Add(second);
                f.Controls.Add(tabs);
            });
            Assert.Contains(problems, p => p.Contains("broken") && p.Contains("overlaps"));
        });
    }
}
