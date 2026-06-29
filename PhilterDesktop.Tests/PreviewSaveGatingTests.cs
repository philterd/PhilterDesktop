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
                    InvokeLoad(form);
                    assert(form);
                }
                finally
                {
                    try { File.Delete(dbPath); } catch { /* best effort */ }
                    try { File.Delete(txtPath); } catch { /* best effort */ }
                }
            });
        }

        private static void InvokeLoad(Form form)
        {
            MethodInfo load = form.GetType().GetMethod(
                "TextRedactionPreviewForm_Load", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Load handler not found.");
            load.Invoke(form, new object?[] { form, EventArgs.Empty });
        }

        private static Button SaveButton(Form form)
        {
            FieldInfo field = form.GetType().GetField("_save", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("_save field not found.");
            return (Button)field.GetValue(form)!;
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
