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
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>The "Redact with Preview" document box: Browse goes straight on, a drop waits for Continue.</summary>
    public sealed class PreviewSourceFormTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "previewsrc-" + Guid.NewGuid().ToString("N"));

        public PreviewSourceFormTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewFile(string name)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, "x");
            return path;
        }

        private static T Field<T>(object owner, string name) where T : class =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

        private static void Drop(PreviewSourceForm form, params string[] paths)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, paths);
            Field<FileDropZone>(form, "_dropZone").HandleDragDrop(
                new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.None));
        }

        private static DragDropEffects DragOver(PreviewSourceForm form, params string[] paths)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, paths);
            var e = new DragEventArgs(data, 0, 0, 0, DragDropEffects.Copy, DragDropEffects.None);
            Field<FileDropZone>(form, "_dropZone").HandleDragEnter(e);
            return e.Effect;
        }

        // A shown form (buttons only click when visible) whose Browse and warnings are stubbed.
        private static PreviewSourceForm Stubbed(Func<string?> pick, List<string> warnings)
        {
            var form = new PreviewSourceForm { StartPosition = FormStartPosition.Manual, Location = new Point(0, 0) };
            form.PickFile = _ => pick();
            form.ShowWarning = warnings.Add;
            form.Show();
            Application.DoEvents();
            return form;
        }

        private static void Sta(Action action) => DpiLayoutTests.Sta(action);

        [Theory]
        [InlineData("a.txt", true)]
        [InlineData("a.DOCX", true)]
        [InlineData("a.pdf", true)]
        [InlineData("a.rtf", true)]
        [InlineData("a.eml", true)]
        [InlineData("a.Msg", true)]
        [InlineData("a.xlsx", false)]
        [InlineData("a.csv", false)]
        [InlineData("a.doc", false)]
        [InlineData("a.pdf.exe", false)]
        [InlineData("pdf", false)]
        public void IsPreviewable(string path, bool expected) =>
            Assert.Equal(expected, PreviewSourceForm.IsPreviewable(path));

        [Fact]
        public void Opens_WithBrowseFocused_ContinueOff_AndTheDropZone() => Sta(() =>
        {
            using var form = new PreviewSourceForm();
            Assert.Same(Field<Button>(form, "_browse"), form.ActiveControl);
            Assert.False(Field<Button>(form, "_continue").Enabled);
            Assert.Same(Field<Button>(form, "_continue"), form.AcceptButton);
            Assert.Same(Field<Button>(form, "_cancel"), form.CancelButton);
            Assert.Equal("", Field<TextBox>(form, "_source").Text);
            Assert.Equal("Or drag a file here", Field<FileDropZone>(form, "_dropZone").Text);
            Assert.Equal("Redact with Preview", form.Text);
            Assert.Null(form.SelectedPath);
        });

        [Fact]
        public void Browse_PickedFile_FinishesStraightAway() => Sta(() =>
        {
            string pdf = NewFile("report.pdf");
            var warnings = new List<string>();
            using var form = Stubbed(() => pdf, warnings);

            Field<Button>(form, "_browse").PerformClick();

            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.Equal(pdf, form.SelectedPath);
            Assert.Empty(warnings);
        });

        [Fact]
        public void Browse_Cancelled_LeavesTheBoxOpenAndEmpty() => Sta(() =>
        {
            var warnings = new List<string>();
            using var form = Stubbed(() => null, warnings);

            Field<Button>(form, "_browse").PerformClick();

            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Null(form.SelectedPath);
            Assert.False(Field<Button>(form, "_continue").Enabled);
            Assert.Empty(warnings);
        });

        [Fact]
        public void Browse_UnsupportedTypedName_WarnsAndStaysOpen() => Sta(() =>
        {
            string xlsx = NewFile("book.xlsx");
            var warnings = new List<string>();
            using var form = Stubbed(() => xlsx, warnings);

            Field<Button>(form, "_browse").PerformClick();

            Assert.Equal(new[] { PreviewSourceForm.UnsupportedMessage }, warnings);
            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Null(form.SelectedPath);
            Assert.Equal("", Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void Browse_AfterADrop_ReplacesTheDroppedFile() => Sta(() =>
        {
            string picked = NewFile("picked.docx");
            using var form = Stubbed(() => picked, new List<string>());
            Drop(form, NewFile("dropped.txt"));

            Field<Button>(form, "_browse").PerformClick();

            Assert.Equal(picked, form.SelectedPath);
            Assert.Equal(picked, Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void Drop_Supported_FillsField_EnablesAndFocusesContinue_ButDoesNotClose() => Sta(() =>
        {
            string eml = NewFile("message.eml");
            using var form = new PreviewSourceForm();

            Drop(form, eml);

            Assert.Equal(eml, Field<TextBox>(form, "_source").Text);
            Assert.True(Field<Button>(form, "_continue").Enabled);
            Assert.Same(Field<Button>(form, "_continue"), form.ActiveControl);
            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Null(form.SelectedPath);
        });

        [Fact]
        public void Drop_Unsupported_IsRefused_AndChangesNothing() => Sta(() =>
        {
            using var form = new PreviewSourceForm();
            string xlsx = NewFile("book.xlsx");

            Assert.Equal(DragDropEffects.None, DragOver(form, xlsx, NewFile("data.csv")));
            Drop(form, xlsx);

            Assert.Equal("", Field<TextBox>(form, "_source").Text);
            Assert.False(Field<Button>(form, "_continue").Enabled);
        });

        [Fact]
        public void Drop_Mixed_TakesTheFirstPreviewable() => Sta(() =>
        {
            using var form = new PreviewSourceForm();
            string rtf = NewFile("letter.rtf");

            Assert.Equal(DragDropEffects.Copy, DragOver(form, NewFile("book.xlsx"), rtf));
            Drop(form, NewFile("book2.xlsx"), rtf, NewFile("later.txt"));

            Assert.Equal(rtf, Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void SecondDrop_ReplacesTheFirst() => Sta(() =>
        {
            using var form = new PreviewSourceForm();
            string second = NewFile("second.msg");
            Drop(form, NewFile("first.txt"));

            Drop(form, second);

            Assert.Equal(second, Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void Continue_AfterDrop_FinishesWithTheDroppedFile() => Sta(() =>
        {
            string txt = NewFile("notes.txt");
            var warnings = new List<string>();
            using var form = Stubbed(() => throw new InvalidOperationException("Browse must not open"), warnings);
            Drop(form, txt);

            Field<Button>(form, "_continue").PerformClick();

            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.Equal(txt, form.SelectedPath);
            Assert.Empty(warnings);
        });

        [Fact]
        public void Continue_WhenTheDroppedFileWasDeleted_WarnsAndResets() => Sta(() =>
        {
            string txt = NewFile("gone.txt");
            var warnings = new List<string>();
            using var form = Stubbed(() => null, warnings);
            Drop(form, txt);
            File.Delete(txt);

            Field<Button>(form, "_continue").PerformClick();

            Assert.Equal(new[] { PreviewSourceForm.MissingMessage }, warnings);
            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Null(form.SelectedPath);
            Assert.Equal("", Field<TextBox>(form, "_source").Text);
            Assert.False(Field<Button>(form, "_continue").Enabled);
            Assert.Same(Field<Button>(form, "_browse"), form.ActiveControl);
        });

        [Fact]
        public void Shown_LaysOutWithoutOverlaps() => Sta(() =>
        {
            using var form = new PreviewSourceForm { StartPosition = FormStartPosition.Manual, Location = new Point(0, 0) };
            form.Show();
            Application.DoEvents();
            LayoutAudit.AssertClean(LayoutAudit.Audit(form));
            form.Close();
        });

        // ---- the main window's menu item ----

        private static void WithMainForm(Action<MainForm, LiteDatabase> test) => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "previewmenu-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using var form = new MainForm(db, startMinimized: false);
                test(form, db);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        [Fact]
        public void MenuItem_OpensTheBox_InsteadOfAFilePicker() => WithMainForm((form, _) =>
        {
            var owners = new List<IWin32Window>();
            form.ChoosePreviewSource = owner => { owners.Add(owner); return null; };

            Field<ToolStripMenuItem>(form, "redactPreviewToolStripMenuItem").PerformClick();

            Assert.Equal(new IWin32Window[] { form }, owners);
        });

        [Fact]
        public void MenuItem_Cancelled_RecordsNothing() => WithMainForm((form, db) =>
        {
            var queue = new RedactionQueueRepository(db);
            int before = queue.GetAll().Count();
            form.ChoosePreviewSource = _ => null;

            Field<ToolStripMenuItem>(form, "redactPreviewToolStripMenuItem").PerformClick();

            Assert.Equal(before, queue.GetAll().Count());
        });
    }
}
