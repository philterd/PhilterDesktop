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

using System.Reflection;
using System.Runtime.ExceptionServices;
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards the 1.0.0 UI-polish fixes: the preview label lists every supported type (#501),
    /// consistent action/dismiss terminology across forms (#503), and Alt-key mnemonics on
    /// buttons and menus (#506). Forms are built on an STA thread, like the smoke tests.
    /// </summary>
    public sealed class UiTerminologyAndMnemonicsTests
    {
        private static void Sta(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static void WithDb(Action<LiteDatabase> action) => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "ui-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                action(db);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        // Reads the Text of a private Control/ToolStripItem field by name (searching the type hierarchy).
        private static string TextOf(object form, string fieldName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            Type? t = form.GetType();
            FieldInfo? field = null;
            while (t is not null && field is null)
            {
                field = t.GetField(fieldName, flags);
                t = t.BaseType;
            }
            Assert.True(field is not null, $"field '{fieldName}' not found on {form.GetType().Name}");
            object? control = field!.GetValue(form);
            Assert.NotNull(control);
            return (string)control!.GetType().GetProperty("Text")!.GetValue(control)!;
        }

        // The listview context-menu labels are kept clean of parenthetical hints.
        [Fact]
        public void ContextMenuLabels_HaveNoParentheticalText() => WithDb(db =>
        {
            using var form = new MainForm(db, startMinimized: false);
            foreach (string name in new[]
            {
                "redactPreviewToolStripMenuItem", "findAndRedactToolStripMenuItem",
                "redactSpreadsheetToolStripMenuItem", "exportExplanationToolStripMenuItem"
            })
            {
                Assert.DoesNotContain("(", TextOf(form, name));
            }
        });

        // ---- #503: consistent terminology ----

        // Every form that adds documents to the redaction queue uses the SAME action label, instead of
        // some saying "Redact" and others "Add to Queue".
        [Fact]
        public void EnqueueForms_AllUseTheSameAddToQueueLabel() => WithDb(db =>
        {
            var policies = new PolicyRepository(db);
            var contexts = new ContextRepository(db);
            var queue = new RedactionQueueRepository(db);
            var settings = new SettingsRepository(db);

            using var contextMenu = new ContextMenuRedactForm(new[] { @"C:\a.txt" }, policies, contexts, queue);
            using var redactDocs = new RedactDocuments(policies, contexts, queue, loggingEnabled: false);
            using var spreadsheet = new SpreadsheetRedactionForm();
            using var folder = new FolderRedactForm(policies, contexts, queue, settings);

            var labels = new[]
            {
                TextOf(contextMenu, "_redact"),
                TextOf(redactDocs, "btnStartRedaction"),
                TextOf(spreadsheet, "_redact"),
                TextOf(folder, "_redact"),
            };

            Assert.All(labels, label => Assert.Equal("&Add to Queue", label));
        });

        // The dismiss button on those enqueue forms is consistently "Cancel" (abandon the pending add),
        // not a mix of "Close" and "Cancel".
        [Fact]
        public void EnqueueForms_AllUseCancelToDismiss() => WithDb(db =>
        {
            var policies = new PolicyRepository(db);
            var contexts = new ContextRepository(db);
            var queue = new RedactionQueueRepository(db);
            var settings = new SettingsRepository(db);

            using var contextMenu = new ContextMenuRedactForm(new[] { @"C:\a.txt" }, policies, contexts, queue);
            using var redactDocs = new RedactDocuments(policies, contexts, queue, loggingEnabled: false);
            using var spreadsheet = new SpreadsheetRedactionForm();
            using var folder = new FolderRedactForm(policies, contexts, queue, settings);

            Assert.Equal("&Cancel", TextOf(contextMenu, "_cancel"));
            Assert.Equal("&Cancel", TextOf(redactDocs, "btnClose"));
            Assert.Equal("&Cancel", TextOf(spreadsheet, "_close"));
            Assert.Equal("&Cancel", TextOf(folder, "_cancel"));
        });

        // Forms that redact immediately (not enqueue) keep the distinct "Redact" verb.
        [Fact]
        public void ImmediateRedactionForm_KeepsRedactLabel() => Sta(() =>
        {
            using var form = new FindAndRedactForm(new SettingsEntity());
            Assert.Equal("&Redact", TextOf(form, "_redact"));
        });

        // ---- #506: mnemonics on buttons and menus ----

        [Fact]
        public void DialogButtons_HaveMnemonics() => WithDb(db =>
        {
            using var globalLists = new GlobalListsForm("Acme", "keep@example.com");
            Assert.Equal("&OK", TextOf(globalLists, "btnOk"));
            Assert.Equal("&Cancel", TextOf(globalLists, "btnCancel"));

            using var contexts = new Contexts(new ContextRepository(db), new ContextEntryRepository(db));
            Assert.Equal("&New Context", TextOf(contexts, "btnCreate"));
            Assert.Equal("&Delete", TextOf(contexts, "btnDelete"));
            Assert.Equal("&Empty", TextOf(contexts, "btnEmpty"));
            Assert.Equal("&Close", TextOf(contexts, "btnClose"));
            Assert.Equal("&Help", TextOf(contexts, "helpButton"));
        });

        // Every visible, text-bearing button on a representative set of forms carries a mnemonic.
        [Fact]
        public void AllButtons_OnRepresentativeForms_HaveAMnemonic() => WithDb(db =>
        {
            var policies = new PolicyRepository(db);
            var contexts = new ContextRepository(db);
            var queue = new RedactionQueueRepository(db);
            var settings = new SettingsRepository(db);

            var forms = new Form[]
            {
                new ContextMenuRedactForm(new[] { @"C:\a.txt" }, policies, contexts, queue),
                new FolderRedactForm(policies, contexts, queue, settings),
                new FindAndRedactForm(new SettingsEntity()),
                new GlobalListsForm("Acme", "keep@example.com"),
                new CreateContextDialog(),
            };

            try
            {
                foreach (Form form in forms)
                {
                    foreach (Button button in DescendantButtons(form))
                    {
                        if (string.IsNullOrEmpty(button.Text))
                        {
                            continue; // icon-only buttons are exempt
                        }
                        Assert.True(button.Text.Contains('&'),
                            $"{form.GetType().Name}.{button.Name} ('{button.Text}') has no Alt mnemonic");
                    }
                }
            }
            finally
            {
                foreach (Form form in forms) { form.Dispose(); }
            }
        });

        [Fact]
        public void MainFormMenus_HaveMnemonics() => WithDb(db =>
        {
            using var form = new MainForm(db, startMinimized: false);
            // Toolbar "Redact" dropdown items and a representative context-menu item.
            Assert.Contains('&', TextOf(form, "redactDropDownItem"));
            Assert.Contains('&', TextOf(form, "previewDropDownItem"));
            Assert.Contains('&', TextOf(form, "findRedactDropDownItem"));
            Assert.Contains('&', TextOf(form, "refreshToolStripMenuItem"));
            Assert.Contains('&', TextOf(form, "modifyRedactionToolStripMenuItem"));
        });

        private static IEnumerable<Button> DescendantButtons(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is Button b)
                {
                    yield return b;
                }
                foreach (Button nested in DescendantButtons(child))
                {
                    yield return nested;
                }
            }
        }
    }
}
