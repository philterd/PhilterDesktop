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
    /// <summary>
    /// The "Or drag a file here" zone, and the Find &amp; Redact and Redact Spreadsheet forms that use it
    /// alongside Browse.
    /// </summary>
    public sealed class FileDropZoneTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "dropzone-" + Guid.NewGuid().ToString("N"));

        public FileDropZoneTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string NewFile(string name, string content = "x")
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, content);
            return path;
        }

        private static DataObject Files(params string[] paths)
        {
            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, paths);
            return data;
        }

        private static DragEventArgs Drag(IDataObject? data) =>
            new(data, 0, 0, 0, DragDropEffects.Copy | DragDropEffects.Move, DragDropEffects.None);

        private static bool IsTxt(string p) => p.EndsWith(".txt", StringComparison.OrdinalIgnoreCase);

        private static T Field<T>(object owner, string name) where T : class =>
            (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

        private static void Sta(Action action) => DpiLayoutTests.Sta(action);

        // ---- PickFile ----

        [Fact]
        public void PickFile_NullData_ReturnsNull() =>
            Assert.Null(FileDropZone.PickFile(null, null));

        [Fact]
        public void PickFile_TextInsteadOfFiles_ReturnsNull() => Sta(() =>
        {
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, NewFile("a.txt"));
            Assert.Null(FileDropZone.PickFile(data, null));
        });

        [Fact]
        public void PickFile_EmptyList_ReturnsNull() => Sta(() =>
            Assert.Null(FileDropZone.PickFile(Files(), null)));

        [Fact]
        public void PickFile_SkipsMissingFoldersAndRejected_TakesFirstAccepted() => Sta(() =>
        {
            string missing = Path.Combine(_dir, "missing.txt");
            string folder = Directory.CreateDirectory(Path.Combine(_dir, "folder.txt")).FullName;
            string rejected = NewFile("b.pdf");
            string first = NewFile("c.txt");
            string second = NewFile("d.txt");

            Assert.Equal(first, FileDropZone.PickFile(Files(missing, folder, rejected, first, second), IsTxt));
        });

        [Fact]
        public void PickFile_IgnoresBlankEntries() => Sta(() =>
        {
            string file = NewFile("a.txt");
            Assert.Equal(file, FileDropZone.PickFile(Files("", "   ", file), IsTxt));
        });

        [Fact]
        public void PickFile_NoFilter_TakesAnyExistingFile() => Sta(() =>
        {
            string pdf = NewFile("a.pdf");
            Assert.Equal(pdf, FileDropZone.PickFile(Files(pdf), null));
        });

        [Fact]
        public void PickFile_AllRejected_ReturnsNull() => Sta(() =>
            Assert.Null(FileDropZone.PickFile(Files(NewFile("a.pdf"), NewFile("b.docx")), IsTxt)));

        // ---- the control ----

        [Fact]
        public void Defaults_AcceptDropsAndShowPrompt() => Sta(() =>
        {
            using var zone = new FileDropZone();
            Assert.True(zone.AllowDrop);
            Assert.False(zone.TabStop);
            Assert.Equal("Or drag a file here", zone.Text);
            Assert.Equal("Or drag a file here", zone.AccessibleName);
            Assert.False(zone.IsDragHover);
        });

        [Fact]
        public void DragEnter_AcceptedFile_AllowsCopyAndHighlights_LeaveClears() => Sta(() =>
        {
            using var zone = new FileDropZone { Accepts = IsTxt };
            DragEventArgs e = Drag(Files(NewFile("a.txt")));

            zone.HandleDragEnter(e);
            Assert.Equal(DragDropEffects.Copy, e.Effect);
            Assert.True(zone.IsDragHover);

            zone.HandleDragLeave();
            Assert.False(zone.IsDragHover);
        });

        [Fact]
        public void DragEnter_RejectedFile_RefusesAndDoesNotHighlight() => Sta(() =>
        {
            using var zone = new FileDropZone { Accepts = IsTxt };
            DragEventArgs e = Drag(Files(NewFile("a.pdf")));

            zone.HandleDragEnter(e);
            Assert.Equal(DragDropEffects.None, e.Effect);
            Assert.False(zone.IsDragHover);
        });

        [Fact]
        public void DragEnter_NonFileData_Refuses() => Sta(() =>
        {
            using var zone = new FileDropZone();
            var data = new DataObject();
            data.SetData(DataFormats.UnicodeText, "hello");
            DragEventArgs e = Drag(data);

            zone.HandleDragEnter(e);
            Assert.Equal(DragDropEffects.None, e.Effect);
        });

        [Fact]
        public void Drop_AcceptedFile_RaisesOnceWithPath_AndClearsHighlight() => Sta(() =>
        {
            using var zone = new FileDropZone { Accepts = IsTxt };
            string file = NewFile("a.txt");
            var dropped = new List<string>();
            zone.FileDropped += dropped.Add;

            zone.HandleDragEnter(Drag(Files(file)));
            zone.HandleDragDrop(Drag(Files(file)));

            Assert.Equal(new[] { file }, dropped);
            Assert.False(zone.IsDragHover);
        });

        [Fact]
        public void Drop_RejectedOrMissing_DoesNotRaise() => Sta(() =>
        {
            using var zone = new FileDropZone { Accepts = IsTxt };
            int raised = 0;
            zone.FileDropped += _ => raised++;

            zone.HandleDragDrop(Drag(Files(NewFile("a.pdf"))));
            zone.HandleDragDrop(Drag(Files(Path.Combine(_dir, "gone.txt"))));
            zone.HandleDragDrop(Drag(null));

            Assert.Equal(0, raised);
        });

        [Fact]
        public void Disabled_RefusesDragAndIgnoresDrop() => Sta(() =>
        {
            using var zone = new FileDropZone { Enabled = false };
            string file = NewFile("a.txt");
            int raised = 0;
            zone.FileDropped += _ => raised++;

            DragEventArgs enter = Drag(Files(file));
            zone.HandleDragEnter(enter);
            zone.HandleDragDrop(Drag(Files(file)));

            Assert.Equal(DragDropEffects.None, enter.Effect);
            Assert.False(zone.IsDragHover);
            Assert.Equal(0, raised);
        });

        [Theory]
        [InlineData(536, 60, false)]
        [InlineData(536, 60, true)]
        [InlineData(1, 1, false)]
        [InlineData(3, 3, true)]
        public void Paints_InEveryState_AndAtTinySizes(int width, int height, bool hover) => Sta(() =>
        {
            using var zone = new FileDropZone { Size = new Size(width, height) };
            if (hover)
            {
                zone.HandleDragEnter(Drag(Files(NewFile("a.txt"))));
                Assert.True(zone.IsDragHover);
            }
            using var bitmap = new Bitmap(width, height);
            zone.DrawToBitmap(bitmap, new Rectangle(0, 0, width, height));
        });

        // ---- Find & Redact ----

        [Fact]
        public void FindAndRedact_HasDropZoneBetweenDocumentRowAndTerms_AndKeepsBrowse() => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity());
            var zone = Field<FileDropZone>(form, "_dropZone");
            var source = Field<TextBox>(form, "_source");
            var browse = Field<Button>(form, "_browse");
            var termsLabel = Field<Label>(form, "_termsLabel");

            Assert.Same(form, zone.Parent);
            Assert.Equal("Or drag a file here", zone.Text);
            Assert.True(zone.Top >= Math.Max(source.Bottom, browse.Bottom));
            Assert.True(zone.Bottom <= termsLabel.Top);
            Assert.True(browse.Enabled);
            Assert.Equal("&Browse…", browse.Text);
        });

        [Theory]
        [InlineData("a.txt")]
        [InlineData("a.PDF")]
        [InlineData("a.docx")]
        [InlineData("a.csv")]
        [InlineData("a.msg")]
        public void FindAndRedact_DropSupportedDocument_FillsDocumentField(string name) => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity());
            string file = NewFile(name);

            Field<FileDropZone>(form, "_dropZone").HandleDragDrop(Drag(Files(file)));

            Assert.Equal(file, Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void FindAndRedact_DropUnsupported_KeepsCurrentDocument() => Sta(() =>
        {
            string original = NewFile("keep.txt");
            using var form = new FindAndRedactForm(new SettingsEntity(), original);

            DragEventArgs enter = Drag(Files(NewFile("a.exe"), NewFile("b.zip")));
            Field<FileDropZone>(form, "_dropZone").HandleDragEnter(enter);
            Field<FileDropZone>(form, "_dropZone").HandleDragDrop(Drag(Files(NewFile("c.exe"))));

            Assert.Equal(DragDropEffects.None, enter.Effect);
            Assert.Equal(original, Field<TextBox>(form, "_source").Text);
        });

        [Fact]
        public void FindAndRedact_DropMixed_TakesFirstSupported_ReplacingInitialDocument() => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity(), NewFile("initial.txt"));
            string pdf = NewFile("report.pdf");

            Field<FileDropZone>(form, "_dropZone").HandleDragDrop(Drag(Files(NewFile("x.exe"), pdf, NewFile("later.txt"))));

            Assert.Equal(pdf, Field<TextBox>(form, "_source").Text);
        });

        // ---- Redact Spreadsheet ----

        private static void Shown(Form form, Action test)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Location = new Point(0, 0);
            form.Show();
            Application.DoEvents();
            test();
            form.Close();
        }

        [Theory]
        [InlineData("a.xlsx", true)]
        [InlineData("a.CSV", true)]
        [InlineData("a.Xlsx", true)]
        [InlineData("a.xls", false)]
        [InlineData("a.txt", false)]
        [InlineData("a.csv.pdf", false)]
        [InlineData("xlsx", false)]
        public void Spreadsheet_IsSpreadsheet(string path, bool expected) =>
            Assert.Equal(expected, SpreadsheetRedactionForm.IsSpreadsheet(path));

        [Fact]
        public void Spreadsheet_HasDropZoneAboveThePolicyRow_AndKeepsBrowse() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            var zone = Field<FileDropZone>(form, "_dropZone");
            var browse = Field<Button>(form, "_browse");

            Assert.Equal("Or drag a file here", zone.Text);
            Assert.True(zone.Top >= Math.Max(Field<TextBox>(form, "_source").Bottom, browse.Bottom));
            Assert.True(zone.Bottom <= Field<ComboBox>(form, "_policy").Top);
            Assert.True(browse.Enabled);
        });

        [Fact]
        public void Spreadsheet_DropCsv_LoadsColumnsAndEnablesAddToQueue() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            string csv = NewFile("people.csv", "Name,Email\nJane Doe,jane@example.com\n");
            Shown(form, () =>
            {
                Assert.False(Field<Button>(form, "_redact").Enabled);

                Field<FileDropZone>(form, "_dropZone").HandleDragDrop(Drag(Files(csv)));

                Assert.Equal(csv, Field<TextBox>(form, "_source").Text);
                Assert.Equal(2, Field<CheckedListBox>(form, "_columns").Items.Count);
                Assert.True(Field<Button>(form, "_redact").Enabled);
                Assert.False(Field<ComboBox>(form, "_worksheet").Visible);
            });
        });

        [Fact]
        public void Spreadsheet_DropXlsx_LoadsWorksheetsAndColumns() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            string xlsx = Path.Combine(_dir, "people.xlsx");
            SpreadsheetTestHelper.CreateXlsx(xlsx, new[]
            {
                new string?[] { "Name", "Email", "Phone" },
                new string?[] { "Jane Doe", "jane@example.com", "555-0100" }
            }, sheetName: "People");
            Shown(form, () =>
            {
                Field<FileDropZone>(form, "_dropZone").HandleDragDrop(Drag(Files(xlsx)));

                var worksheet = Field<ComboBox>(form, "_worksheet");
                Assert.True(worksheet.Visible);
                Assert.Equal("People", worksheet.SelectedItem);
                Assert.Equal(3, Field<CheckedListBox>(form, "_columns").Items.Count);
                Assert.True(Field<Button>(form, "_redact").Enabled);
            });
        });

        [Fact]
        public void Spreadsheet_DropNonSpreadsheet_IsRefused_AndLeavesFormEmpty() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            Shown(form, () =>
            {
                var zone = Field<FileDropZone>(form, "_dropZone");
                DragEventArgs enter = Drag(Files(NewFile("notes.txt"), NewFile("old.xls")));
                zone.HandleDragEnter(enter);
                zone.HandleDragDrop(Drag(Files(NewFile("report.pdf"))));

                Assert.Equal(DragDropEffects.None, enter.Effect);
                Assert.Equal("", Field<TextBox>(form, "_source").Text);
                Assert.Empty(Field<CheckedListBox>(form, "_columns").Items);
                Assert.False(Field<Button>(form, "_redact").Enabled);
            });
        });

        [Fact]
        public void Spreadsheet_SecondDrop_ReplacesTheFirstFilesColumns() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            string first = NewFile("first.csv", "A,B,C,D\n1,2,3,4\n");
            string second = NewFile("second.csv", "Only\nx\n");
            Shown(form, () =>
            {
                var zone = Field<FileDropZone>(form, "_dropZone");
                zone.HandleDragDrop(Drag(Files(first)));
                Assert.Equal(4, Field<CheckedListBox>(form, "_columns").Items.Count);

                zone.HandleDragDrop(Drag(Files(second)));

                Assert.Equal(second, Field<TextBox>(form, "_source").Text);
                Assert.Single(Field<CheckedListBox>(form, "_columns").Items);
            });
        });

        [Fact]
        public void Spreadsheet_RejectedDropAfterGoodOne_KeepsTheGoodFile() => Sta(() =>
        {
            using var form = new SpreadsheetRedactionForm();
            string csv = NewFile("good.csv", "Name\nJane\n");
            Shown(form, () =>
            {
                var zone = Field<FileDropZone>(form, "_dropZone");
                zone.HandleDragDrop(Drag(Files(csv)));
                zone.HandleDragDrop(Drag(Files(NewFile("bad.docx"))));

                Assert.Equal(csv, Field<TextBox>(form, "_source").Text);
                Assert.Single(Field<CheckedListBox>(form, "_columns").Items);
                Assert.True(Field<Button>(form, "_redact").Enabled);
            });
        });
    }
}
