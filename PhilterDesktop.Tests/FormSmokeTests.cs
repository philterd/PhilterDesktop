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

using System.Runtime.ExceptionServices;
using LiteDB;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Smoke tests that construct each WinForms form (on an STA thread) and dispose it,
    /// catching exceptions in constructors / layout / theming. They don't show the forms
    /// or drive interaction — just guard against initialization regressions.
    /// </summary>
    public sealed class FormSmokeTests
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

        private static void ConstructWithDb(Func<LiteDatabase, Form> create)
        {
            Sta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using (var db = new LiteDatabase(path))
                    using (Form form = create(db))
                    {
                        _ = form.Handle; // force handle creation (exercises theming/interop)
                    }
                }
                finally
                {
                    try { File.Delete(path); } catch { /* best effort */ }
                }
            });
        }

        // The app's primary window. Uses the internal ctor that takes a database instance so the test
        // never touches the real (encrypted, PII-bearing) user database. Guards the most important form
        // against initialization/theming/tray-setup regressions.
        [Fact]
        public void MainForm_Constructs() =>
            ConstructWithDb(db => new MainForm(db, startMinimized: false));

        // the empty-queue hint overlay must accept dropped files (it had no AllowDrop, so dropping
        // onto the hint — the exact onboarding gesture it invites — silently failed).
        [Fact]
        public void MainForm_EmptyStateOverlay_AcceptsDrops() => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using var form = new MainForm(db, startMinimized: false);

                var field = typeof(MainForm).GetField("_emptyStateLabel",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.NotNull(field);
                var label = (Label)field!.GetValue(form)!;
                Assert.True(label.AllowDrop, "the empty-state hint must accept dropped files");
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        [Fact]
        public void PolicyEditorForm_Constructs() =>
            ConstructWithDb(db => new PolicyEditorForm(new PolicyRepository(db)));

        [Fact]
        public void PolicyEditorForm_WithWatchedFolders_Constructs() =>
            ConstructWithDb(db => new PolicyEditorForm(new PolicyRepository(db), new WatchedFolderRepository(db)));

        [Fact]
        public void PolicyWizardForm_Constructs() =>
            Sta(() =>
            {
                using var form = new PolicyWizardForm(_ => false);
                _ = form.Handle;
            });

        [Fact]
        public void GlobalListsForm_Constructs() =>
            Sta(() =>
            {
                using var form = new GlobalListsForm("Acme\nBeta", "keep@example.com");
                _ = form.Handle;
            });

        [Fact]
        public void FindAndRedactForm_Constructs() =>
            Sta(() =>
            {
                using var form = new FindAndRedactForm(new SettingsEntity());
                _ = form.Handle;
            });

        [Fact]
        public void SpreadsheetRedactionForm_Constructs() =>
            Sta(() =>
            {
                using var form = new SpreadsheetRedactionForm();
                _ = form.Handle;
            });

        [Fact]
        public void RedactDocuments_Constructs() =>
            ConstructWithDb(db => new RedactDocuments(
                new PolicyRepository(db), new ContextRepository(db), new RedactionQueueRepository(db), loggingEnabled: false));

        [Fact]
        public void Contexts_Constructs() =>
            ConstructWithDb(db => new Contexts(new ContextRepository(db), new ContextEntryRepository(db)));

        [Fact]
        public void SettingsForm_Constructs() =>
            ConstructWithDb(db => new SettingsForm(new SettingsRepository(db)));

        // Enter saves, Esc cancels — the Settings dialog wires AcceptButton/CancelButton.
        [Fact]
        public void SettingsForm_HasAcceptAndCancelButtons() => Sta(() =>
        {
            string path = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using var db = new LiteDatabase(path);
                using var form = new SettingsForm(new SettingsRepository(db));

                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var save = form.GetType().GetField("btnSave", flags)!.GetValue(form);
                var cancel = form.GetType().GetField("btnCancel", flags)!.GetValue(form);
                Assert.Same(save, form.AcceptButton);
                Assert.Same(cancel, form.CancelButton);
            }
            finally { try { File.Delete(path); } catch { /* best effort */ } }
        });

        [Fact]
        public void SettingsForm_WithWatchedFolderTab_Constructs() =>
            ConstructWithDb(db => new SettingsForm(
                new SettingsRepository(db), new PolicyRepository(db), new ContextRepository(db), new WatchedFolderRepository(db)));

        [Fact]
        public void SettingsForm_AllTabsFitWithoutHorizontalScroll() =>
            Sta(() =>
            {
                string path = Path.Combine(Path.GetTempPath(), "smoke-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using var db = new LiteDatabase(path);
                    // Pass every dependency (including a key store) so all tabs are present — the same
                    // worst case the real app shows: General, Microsoft Office, PDF, Email, Notifications,
                    // Watched Folders, Limits, Security.
                    using var form = new SettingsForm(
                        new SettingsRepository(db), new PolicyRepository(db), new ContextRepository(db),
                        new WatchedFolderRepository(db), new WatchedFolderLogRepository(db),
                        DatabaseKeyStore.ForDatabase(path));
                    form.CreateControl(); // realize child handles so tab rectangles are laid out

                    var tabs = FindTabControl(form);
                    Assert.NotNull(tabs);
                    _ = tabs!.Handle;
                    Assert.True(tabs.TabCount >= 8, $"expected all settings tabs, got {tabs.TabCount}");

                    // If the tab strip needs scroll arrows, the last tab's right edge extends past the
                    // control's client width. Require it to fit so the whole row is visible at once.
                    System.Drawing.Rectangle last = tabs.GetTabRect(tabs.TabCount - 1);
                    Assert.True(last.Right <= tabs.ClientSize.Width,
                        $"tabs overflow the strip: last tab right={last.Right}, tab control width={tabs.ClientSize.Width}");
                }
                finally { try { File.Delete(path); } catch { /* best effort */ } }
            });

        private static TabControl? FindTabControl(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is TabControl tc)
                {
                    return tc;
                }
                TabControl? nested = FindTabControl(child);
                if (nested is not null)
                {
                    return nested;
                }
            }
            return null;
        }

        [Fact]
        public void WatchedFolderForm_Constructs() =>
            ConstructWithDb(db => new WatchedFolderForm(new PolicyRepository(db), new ContextRepository(db)));

        [Fact]
        public void WatchedFolderLogForm_Constructs() =>
            ConstructWithDb(db => new WatchedFolderLogForm(
                new WatchedFolderLogRepository(db), new WatchedFolderEntity { FolderPath = @"C:\watched" }));

        [Fact]
        public void ModifyRedactionForm_Constructs() =>
            ConstructWithDb(db => new ModifyRedactionForm(
                ObjectId.NewObjectId(),
                new RedactionVersionRepository(db),
                new RedactionSpanRepository(db),
                new PolicyRepository(db)));

        [Fact]
        public void ContextMenuRedactForm_Constructs() =>
            ConstructWithDb(db => new ContextMenuRedactForm(
                new[] { @"C:\docs\a.pdf", @"C:\docs\b.txt" },
                new PolicyRepository(db),
                new ContextRepository(db),
                new RedactionQueueRepository(db)));

        [Fact]
        public void FolderRedactForm_Constructs() =>
            ConstructWithDb(db => new FolderRedactForm(
                new PolicyRepository(db),
                new ContextRepository(db),
                new RedactionQueueRepository(db),
                new SettingsRepository(db)));

        [Fact]
        public void SpanEditForm_Constructs() => Sta(() =>
        {
            using var f = new SpanEditForm(
                "Add Redaction",
                SpanPositionKind.TextOffset,
                new RedactionSpanEntity { UserAdded = true, CharacterStart = 0, CharacterEnd = 5, Replacement = "[X]" },
                positionEditable: true);
            _ = f.Handle;
        });

        [Fact]
        public void VerificationResultForm_Constructs() => Sta(() =>
        {
            using var f = new VerificationResultForm();
            _ = f.Handle;
        });

        [Fact]
        public void RedactionDetailsForm_Constructs() => Sta(() =>
        {
            using var f = new RedactionDetailsForm("Details — note.txt", new List<(string, string)>
            {
                ("Source file", "note.txt"),
                ("Redactions", "3"),
            });
            _ = f.Handle;
        });

        [Fact]
        public void UpdateAvailableForm_Constructs() => Sta(() =>
        {
            using var f = new UpdateAvailableForm("1.0.0", "1.0.1", "2026-06-25");
            _ = f.Handle;
        });

        [Fact]
        public void AboutForm_Constructs() => Sta(() => { using var f = new AboutForm(); _ = f.Handle; });

        [Fact]
        public void LicenseForm_Constructs() => Sta(() => { using var f = new LicenseForm(); _ = f.Handle; });

        [Fact]
        public void RedactionNoticeForm_Constructs() => Sta(() => { using var f = new RedactionNoticeForm(); _ = f.Handle; });

        [Fact]
        public void RedactionNoticeForm_ViewOnly_ShowsClose() => Sta(() =>
        {
            using var f = new RedactionNoticeForm(viewOnly: true);
            _ = f.Handle;
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var ok = (System.Windows.Forms.Button)typeof(RedactionNoticeForm).GetField("_ok", Flags)!.GetValue(f)!;
            Assert.Contains("Close", ok.Text); // the single button just closes in view-only mode
        });

        [Fact]
        public void LicenseForm_LoadsBothLicenseTexts() => Sta(() =>
        {
            using var f = new LicenseForm();
            _ = f.Handle;
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var apache = (System.Windows.Forms.TextBox)typeof(LicenseForm).GetField("_licenseBody", Flags)!.GetValue(f)!;
            var eula = (System.Windows.Forms.TextBox)typeof(LicenseForm).GetField("_eulaBody", Flags)!.GetValue(f)!;
            Assert.Contains("Apache License", apache.Text);                    // the embedded Apache 2.0 text
            // The EULA is loaded from the loose philterd-eula.txt next to the exe; either the agreement
            // text or the fallback pointer mentions Philterd, so the box is never empty.
            Assert.Contains("Philterd", eula.Text, StringComparison.OrdinalIgnoreCase);
        });

        [Fact]
        public void LicenseForm_ViewOnly_HidesDisagree_AndShowsClose() => Sta(() =>
        {
            using var f = new LicenseForm(viewOnly: true);
            _ = f.Handle;
            const System.Reflection.BindingFlags Flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
            var disagree = (System.Windows.Forms.Button)typeof(LicenseForm).GetField("_disagree", Flags)!.GetValue(f)!;
            var agree = (System.Windows.Forms.Button)typeof(LicenseForm).GetField("_agree", Flags)!.GetValue(f)!;
            Assert.False(disagree.Visible);         // no "I Disagree" when just viewing
            Assert.Contains("Close", agree.Text);   // the primary button just closes
        });

        [Fact]
        public void PassphraseForm_Constructs() => Sta(() =>
        {
            using var f = new PassphraseForm(PassphraseFormMode.Set);
            _ = f.Handle;
        });

        [Fact]
        public void DiffViewerForm_Constructs() => Sta(() =>
        {
            using var f = new DiffViewerForm("line one\nsecret\nline three", "line one\n[REDACTED]\nline three", "a.txt", "a_redacted.txt");
            _ = f.Handle;
        });

        [Fact]
        public void PdfCompareForm_Constructs() => Sta(() =>
        {
            // Construction doesn't render (rendering happens on load), so no PDF bytes are needed.
            using var f = new PdfCompareForm(Array.Empty<byte>(), Array.Empty<byte>(), "a.pdf", "a_redacted.pdf");
            _ = f.Handle;
        });

        [Fact]
        public void TextRedactionPreviewForm_Constructs() =>
            ConstructWithDb(db => new TextRedactionPreviewForm(
                @"C:\docs\note.txt", new PolicyRepository(db), new ContextRepository(db), new SettingsEntity()));

        [Fact]
        public void PdfRedactionPreviewForm_Constructs() =>
            ConstructWithDb(db => new PdfRedactionPreviewForm(
                @"C:\docs\note.pdf", new PolicyRepository(db), new ContextRepository(db), new SettingsEntity()));

        [Fact]
        public void WordRedactionPreviewForm_Constructs() =>
            ConstructWithDb(db => new WordRedactionPreviewForm(
                @"C:\docs\note.docx", new PolicyRepository(db), new ContextRepository(db), new SettingsEntity()));

        [Fact]
        public void EmailRedactionPreviewForm_Constructs() =>
            ConstructWithDb(db => new EmailRedactionPreviewForm(
                @"C:\docs\note.eml", new PolicyRepository(db), new ContextRepository(db), new SettingsEntity()));

        [Fact]
        public void CreateContextDialog_Constructs() => Sta(() => { using var f = new CreateContextDialog(); _ = f.Handle; });

        [Fact]
        public void FilterStrategiesForm_Constructs() => Sta(() =>
        {
            using var f = new FilterStrategiesForm("SSN", Array.Empty<object>(), typeof(SsnFilterStrategy));
            _ = f.Handle;
        });

        [Fact]
        public void AddFilterStrategyForm_Constructs() => Sta(() =>
        {
            using var f = new AddFilterStrategyForm(new SsnFilterStrategy(), "SSN");
            _ = f.Handle;
        });

        [Fact]
        public void CustomIdentifiersForm_Constructs() => Sta(() =>
        {
            using var f = new CustomIdentifiersForm(new List<Identifier>());
            _ = f.Handle;
        });

        [Fact]
        public void IgnoredTermsForm_Constructs() => Sta(() =>
        {
            using var f = new IgnoredTermsForm(new List<string> { "Acme" }, caseSensitive: false);
            _ = f.Handle;
        });

        [Fact]
        public void AlwaysRedactTermsForm_Constructs() => Sta(() =>
        {
            using var f = new AlwaysRedactTermsForm(new List<string> { "Voldemort" });
            _ = f.Handle;
        });
    }
}
