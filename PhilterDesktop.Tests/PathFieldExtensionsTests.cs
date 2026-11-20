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
using System.Reflection;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Path fields show the end of a long path (the file name), not the drive and folders.</summary>
    public sealed class PathFieldExtensionsTests : IDisposable
    {
        private readonly string _dir;

        public PathFieldExtensionsTests()
        {
            // A long folder name so every path overflows its field.
            _dir = Path.Combine(Path.GetTempPath(), "pathfield-" + Guid.NewGuid().ToString("N"), new string('f', 120));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(Path.GetDirectoryName(_dir)!, recursive: true); } catch { /* best effort */ }
        }

        private string NewFile(string name, string content = "x")
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private static T Field<T>(object owner, string name) where T : class =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

        private static void Drop(Form form, string path)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, new[] { path });
            Field<FileDropZone>(form, "_dropZone").HandleDragDrop(
                new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.None));
            Application.DoEvents();
        }

        private static void Show(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Show();
            Application.DoEvents();
        }

        private static void AssertEndVisible(TextBox box)
        {
            Assert.True(box.GetPositionFromCharIndex(0).X < 0, "the start of the path should be scrolled out of view");
            int last = box.GetPositionFromCharIndex(box.TextLength - 1).X;
            Assert.InRange(last, 0, box.ClientSize.Width);
        }

        private static void Sta(Action action) => DpiLayoutTests.Sta(action);

        // A plain form hosting one path field.
        private static (Form form, TextBox box) Host()
        {
            var form = new Form { ClientSize = new Size(400, 60) };
            var box = new TextBox { ReadOnly = true, Location = new Point(10, 10), Width = 300 };
            form.Controls.Add(box);
            box.KeepEndVisible();
            return (form, box);
        }

        [Fact]
        public void TextSetAfterShown_ShowsTheEnd() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                AssertEndVisible(box);
            }
        });

        [Fact]
        public void TextSetBeforeShown_ShowsTheEnd() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                Show(form);
                AssertEndVisible(box);
            }
        });

        [Fact]
        public void ShortPath_StaysAtTheStart() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = @"C:\a.pdf";
                Assert.True(box.GetPositionFromCharIndex(0).X >= 0);
            }
        });

        [Fact]
        public void AfterNarrowing_StillShowsTheEnd() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                box.Width = 150;
                Application.DoEvents();
                AssertEndVisible(box);
            }
        });

        [Fact]
        public void LongerThenShorterLongPath_ShowsTheNewEnd() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = Path.Combine(_dir, new string('x', 80) + ".pdf");
                box.Text = Path.Combine(_dir, "b.pdf");
                AssertEndVisible(box);
            }
        });

        [Fact]
        public void Resizing_KeepsTheUsersSelection() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                box.Select(3, 10);
                box.Width = 150;
                Application.DoEvents();
                Assert.Equal(3, box.SelectionStart);
                Assert.Equal(10, box.SelectionLength);
            }
        });

        [Fact]
        public void NewPath_ReplacesTheSelection_AndShowsTheEnd() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                Show(form);
                box.Text = Path.Combine(_dir, "a.pdf");
                box.Select(3, 10);
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                AssertEndVisible(box);
            }
        });

        [Fact]
        public void DoesNotTakeFocus() => Sta(() =>
        {
            var (form, box) = Host();
            using (form)
            {
                var other = new Button { Location = new Point(10, 35) };
                form.Controls.Add(other);
                Show(form);
                form.ActiveControl = other;
                box.Text = Path.Combine(_dir, "quarterly-report.pdf");
                Assert.Same(other, form.ActiveControl);
            }
        });

        // ---- the three forms ----

        [Fact]
        public void FindAndRedact_InitialDocument_ShowsTheEnd() => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity(), NewFile("quarterly-report.pdf"));
            Show(form);
            AssertEndVisible(Field<TextBox>(form, "_source"));
        });

        [Fact]
        public void FindAndRedact_DroppedDocument_ShowsTheEnd() => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity());
            Show(form);
            Drop(form, NewFile("quarterly-report.docx"));
            AssertEndVisible(Field<TextBox>(form, "_source"));
        });

        [Fact]
        public void Spreadsheet_DroppedFile_ShowsTheEnd() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            Show(form);
            Drop(form, NewFile("people.csv", "Name\nJane\n"));
            AssertEndVisible(Field<TextBox>(form, "_source"));
        });

        [Fact]
        public void PreviewSource_DroppedFile_ShowsTheEnd() => Sta(() =>
        {
            using var form = new PreviewSourceForm();
            Show(form);
            Drop(form, NewFile("quarterly-report.pdf"));
            AssertEndVisible(Field<TextBox>(form, "_source"));
        });
    }
}
