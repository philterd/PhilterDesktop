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
    /// The preview-first redaction workspaces must not let the user Save when no policy/context is
    /// selected — doing so would write an unredacted copy that looks like a redacted draft
    /// (philterd-website issue #484). The fix is identical across the text/Word/email forms; this
    /// exercises the wiring through the plain-text form, which loads from a simple temp file.
    /// </summary>
    public sealed class PreviewSaveGatingTests
    {
        [Fact]
        public void Save_IsDisabled_WhenNoPolicyExists()
        {
            RunOnLoadedTextForm(seedDefaults: false, form =>
                Assert.False(SaveButton(form).Enabled,
                    "Save must be disabled when no policy/context is selected (would write an unredacted copy)."));
        }

        [Fact]
        public void Save_IsEnabled_WhenPolicyAndContextSelected()
        {
            RunOnLoadedTextForm(seedDefaults: true, form =>
                Assert.True(SaveButton(form).Enabled,
                    "Save should be enabled once a policy and context are selected."));
        }

        // The Word preview uses the same off-UI-thread detection (#486); prove it finds PII via the
        // (more complex) docx detector.
        [Fact]
        public void WordPreview_AsyncDetection_FindsPii()
        {
            Sta(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "wpreview-" + Guid.NewGuid().ToString("N") + ".db");
                string docxPath = Path.Combine(Path.GetTempPath(), "wpreview-" + Guid.NewGuid().ToString("N") + ".docx");
                WordDocs.Create(docxPath, "Contact john@example.com please.");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    var policies = new PolicyRepository(db);
                    var contexts = new ContextRepository(db);
                    policies.Insert(new PolicyEntity { Name = "default", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });
                    contexts.Insert(new ContextEntity { Name = "default" });

                    using var form = new WordRedactionPreviewForm(docxPath, policies, contexts, new SettingsEntity());
                    SynchronizationContext.SetSynchronizationContext(null);
                    InvokeLoad(form);
                    WaitUntilNotBusy(form);

                    FieldInfo spansField = form.GetType().GetField("_spans", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    var spans = (List<RedactionSpanEntity>)spansField.GetValue(form)!;
                    Assert.Contains(spans, s => s.Text == "john@example.com");
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(docxPath); } catch { /* best effort */ }
                }
            });
        }

        // The email preview uses the same off-UI-thread detection (#486).
        [Fact]
        public void EmailPreview_AsyncDetection_FindsPii()
        {
            Sta(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "epreview-" + Guid.NewGuid().ToString("N") + ".db");
                string emlPath = Path.Combine(Path.GetTempPath(), "epreview-" + Guid.NewGuid().ToString("N") + ".eml");
                File.WriteAllText(emlPath,
                    "From: sender@example.org\r\nTo: rcpt@example.org\r\nSubject: hi\r\n\r\nContact john@example.com please.\r\n");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    var policies = new PolicyRepository(db);
                    var contexts = new ContextRepository(db);
                    policies.Insert(new PolicyEntity { Name = "default", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });
                    contexts.Insert(new ContextEntity { Name = "default" });

                    using var form = new EmailRedactionPreviewForm(emlPath, policies, contexts, new SettingsEntity());
                    SynchronizationContext.SetSynchronizationContext(null);
                    InvokeLoad(form);
                    WaitUntilNotBusy(form);

                    FieldInfo spansField = form.GetType().GetField("_spans", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    var spans = (List<RedactionSpanEntity>)spansField.GetValue(form)!;
                    Assert.Contains(spans, s => s.Text == "john@example.com");
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(emlPath); } catch { /* best effort */ }
                }
            });
        }

        // The _busy guard must make an overlapping DetectAsync a no-op (so a re-detect can't trample an
        // in-flight one) (#486).
        [Fact]
        public void DetectAsync_WhileBusy_IsNoOp()
        {
            Sta(() =>
            {
                string txtPath = Path.Combine(Path.GetTempPath(), "busy-" + Guid.NewGuid().ToString("N") + ".txt");
                string dbPath = Path.Combine(Path.GetTempPath(), "busy-" + Guid.NewGuid().ToString("N") + ".db");
                File.WriteAllText(txtPath, "john@example.com");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    using var form = new TextRedactionPreviewForm(
                        txtPath, new PolicyRepository(db), new ContextRepository(db), new SettingsEntity());

                    var sentinel = new List<RedactionSpanEntity> { new() { Text = "sentinel" } };
                    SetField(form, "_busy", true);
                    SetField(form, "_spans", sentinel);

                    var task = (Task)form.GetType()
                        .GetMethod("DetectAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
                        .Invoke(form, null)!;
                    task.GetAwaiter().GetResult(); // completed synchronously because _busy short-circuits

                    FieldInfo spansField = form.GetType().GetField("_spans", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    Assert.Same(sentinel, spansField.GetValue(form)); // untouched
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(txtPath); } catch { /* best effort */ }
                }
            });
        }

        private static void SetField(object obj, string name, object? value)
        {
            FieldInfo field = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"field {name} not found");
            field.SetValue(obj, value);
        }

        // Proves the off-UI-thread detection (#486) actually runs the engine and populates spans.
        [Fact]
        public void AsyncDetection_RunsEngineOffUiThread_AndFindsPii()
        {
            Sta(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "preview-" + Guid.NewGuid().ToString("N") + ".db");
                string txtPath = Path.Combine(Path.GetTempPath(), "preview-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(txtPath, "Contact john@example.com please.");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    var policies = new PolicyRepository(db);
                    var contexts = new ContextRepository(db);
                    policies.Insert(new PolicyEntity { Name = "default", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });
                    contexts.Insert(new ContextEntity { Name = "default" });

                    using var form = new TextRedactionPreviewForm(txtPath, policies, contexts, new SettingsEntity());
                    SynchronizationContext.SetSynchronizationContext(null);
                    InvokeLoad(form);
                    WaitUntilNotBusy(form);

                    FieldInfo spansField = form.GetType().GetField("_spans", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    var spans = (List<RedactionSpanEntity>)spansField.GetValue(form)!;
                    Assert.Contains(spans, s => s.Text == "john@example.com");
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(txtPath); } catch { /* best effort */ }
                }
            });
        }

        // Builds a TextRedactionPreviewForm over a real temp .txt, fires its Load handler (which runs
        // detection and the gated Save-enable logic), then runs the assertion — all on an STA thread.
        private static void RunOnLoadedTextForm(bool seedDefaults, Action<Form> assert)
        {
            Sta(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "preview-" + Guid.NewGuid().ToString("N") + ".db");
                string txtPath = Path.Combine(Path.GetTempPath(), "preview-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(txtPath, "Contact John Smith at john@example.com.");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    var policies = new PolicyRepository(db);
                    var contexts = new ContextRepository(db);
                    if (seedDefaults)
                    {
                        policies.Insert(new PolicyEntity { Name = "default", Json = "{}" });
                        contexts.Insert(new ContextEntity { Name = "default" });
                    }

                    using var form = new TextRedactionPreviewForm(txtPath, policies, contexts, new SettingsEntity());

                    // Detection now runs off the UI thread (#486). Clear the WinForms sync context so the
                    // async continuation resumes on the thread pool (no message pump needed here), then
                    // wait for it to finish before asserting.
                    SynchronizationContext.SetSynchronizationContext(null);
                    InvokeLoad(form);
                    WaitUntilNotBusy(form);
                    assert(form);
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(txtPath); } catch { /* best effort */ }
                }
            });
        }

        private static void InvokeLoad(Form form) => InvokeLoad(form, form.GetType().Name + "_Load");

        private static void InvokeLoad(Form form, string handlerName)
        {
            MethodInfo load = form.GetType().GetMethod(handlerName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Load handler {handlerName} not found.");
            load.Invoke(form, new object?[] { form, EventArgs.Empty });
        }

        private static Button SaveButton(Form form)
        {
            FieldInfo field = form.GetType().GetField("_save", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("_save field not found.");
            return (Button)field.GetValue(form)!;
        }

        // Waits for the form's async detection (_busy) to finish; the no-policy path never sets _busy,
        // so this returns immediately for it.
        private static void WaitUntilNotBusy(Form form)
        {
            FieldInfo busy = form.GetType().GetField("_busy", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("_busy field not found.");
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 15000 && (bool)busy.GetValue(form)!)
            {
                Thread.Sleep(20);
            }
        }

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
    }
}
