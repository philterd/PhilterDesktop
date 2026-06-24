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

        [Fact]
        public void PolicyEditorForm_Constructs() =>
            ConstructWithDb(db => new PolicyEditorForm(new PolicyRepository(db)));

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

        [Fact]
        public void SettingsForm_WithWatchedFolderTab_Constructs() =>
            ConstructWithDb(db => new SettingsForm(
                new SettingsRepository(db), new PolicyRepository(db), new ContextRepository(db), new WatchedFolderRepository(db)));

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
        public void AboutForm_Constructs() => Sta(() => { using var f = new AboutForm(); _ = f.Handle; });

        [Fact]
        public void WelcomeForm_Constructs() => Sta(() => { using var f = new WelcomeForm(); _ = f.Handle; });

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
