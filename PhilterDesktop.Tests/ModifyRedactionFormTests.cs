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
    /// Modify Redaction's "Redact" re-writes the output (destructive), so pressing Enter must not
    /// trigger it, and the form must lock while the async re-redaction runs.
    /// </summary>
    public sealed class ModifyRedactionFormTests
    {
        private static void OnForm(Action<ModifyRedactionForm> body)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "mrf-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    using var form = new ModifyRedactionForm(
                        ObjectId.NewObjectId(),
                        new RedactionVersionRepository(db),
                        new RedactionSpanRepository(db),
                        new PolicyRepository(db));
                    form.CreateControl();
                    body(form);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { try { File.Delete(dbPath); } catch { /* best effort */ } }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static bool ControlEnabled(ModifyRedactionForm form, string fieldName)
        {
            FieldInfo field = typeof(ModifyRedactionForm).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"field {fieldName} not found");
            return ((Control)field.GetValue(form)!).Enabled;
        }

        private static void InvokeSetBusy(ModifyRedactionForm form, bool busy)
        {
            MethodInfo m = typeof(ModifyRedactionForm).GetMethod("SetBusy", BindingFlags.Instance | BindingFlags.NonPublic)!;
            m.Invoke(form, new object[] { busy });
        }

        [Fact]
        public void HasNoAcceptButton_SoEnterCannotTriggerRedact()
        {
            OnForm(form => Assert.Null(form.AcceptButton));
        }

        // The form must carry the configured output location so its re-redaction writes there (not always
        // next to the source). This guards the constructor wiring the OnRedact path relies on.
        [Fact]
        public void Constructor_StoresOutputLocationSettings()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "mrf-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    using var form = new ModifyRedactionForm(
                        ObjectId.NewObjectId(),
                        new RedactionVersionRepository(db),
                        new RedactionSpanRepository(db),
                        new PolicyRepository(db),
                        redactedSuffix: "_r",
                        outputToOriginalLocation: false,
                        customOutputFolder: @"C:\out\folder");
                    form.CreateControl();

                    Assert.False((bool)Field(form, "_outputToOriginalLocation"));
                    Assert.Equal(@"C:\out\folder", (string)Field(form, "_customOutputFolder"));
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { try { File.Delete(dbPath); } catch { /* best effort */ } }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static object Field(ModifyRedactionForm form, string name) =>
            typeof(ModifyRedactionForm).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(form)!;

        // LoadPolicy must merge the global always-redact/ignore lists (like every other redaction path),
        // so a Modify re-render still enforces them instead of letting terms reappear in re-detected content.
        [Fact]
        public void LoadPolicy_MergesGlobalAlwaysRedactList()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "mrf-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    using var db = new LiteDatabase(dbPath);
                    var policies = new PolicyRepository(db);
                    policies.Upsert(new PolicyEntity { Name = "p", Json = "{}" });

                    using var form = new ModifyRedactionForm(
                        ObjectId.NewObjectId(),
                        new RedactionVersionRepository(db),
                        new RedactionSpanRepository(db),
                        policies,
                        globalAlwaysRedact: "Projity");
                    form.CreateControl();

                    var loadPolicy = typeof(ModifyRedactionForm).GetMethod("LoadPolicy", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    var policy = (Phileas.Policy.Policy?)loadPolicy.Invoke(form, new object?[] { "p" });

                    Assert.NotNull(policy);
                    Assert.Contains(policy!.Identifiers!.CustomIdentifiers!, i => i.Classification == GlobalLists.AlwaysRedactName);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { try { File.Delete(dbPath); } catch { /* best effort */ } }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        [Fact]
        public void RedactionChanged_DefaultsToFalse_BeforeAnyReRedaction()
        {
            // The caller resets the document's verification verdict only when this becomes true.
            OnForm(form => Assert.False(form.RedactionChanged));
        }

        [Fact]
        public void CancelButtonIsClose_SoEscClosesSafely()
        {
            OnForm(form =>
            {
                var closeField = typeof(ModifyRedactionForm).GetField("_close", BindingFlags.Instance | BindingFlags.NonPublic)!;
                Assert.Same(closeField.GetValue(form), form.CancelButton);
            });
        }

        [Fact]
        public void SetBusy_True_DisablesInteractiveControls()
        {
            OnForm(form =>
            {
                InvokeSetBusy(form, true);

                Assert.True(form.UseWaitCursor);
                foreach (string control in new[] { "_versionTree", "_spanList", "_newVersion", "_close", "_redact", "_add", "_edit", "_remove", "_deleteVersion" })
                {
                    Assert.False(ControlEnabled(form, control), $"{control} must be disabled while busy");
                }
            });
        }

        [Fact]
        public void SetBusy_False_RestoresAlwaysAvailableControls()
        {
            OnForm(form =>
            {
                InvokeSetBusy(form, true);
                InvokeSetBusy(form, false);

                Assert.False(form.UseWaitCursor);
                // These are always usable again once the run finishes (independent of selection).
                foreach (string control in new[] { "_versionTree", "_spanList", "_newVersion", "_close" })
                {
                    Assert.True(ControlEnabled(form, control), $"{control} must be re-enabled after busy");
                }
            });
        }
    }
}
