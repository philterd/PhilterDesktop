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
using System.Runtime.ExceptionServices;
using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>Editing a policy's JSON directly from the Policy Editor's Edit JSON button.</summary>
    public sealed class PolicyJsonFormTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "policy-json-" + Guid.NewGuid().ToString("N"));

        private const string SsnPolicy = "{\n  \"identifiers\": {\n    \"ssn\": {}\n  }\n}";

        public PolicyJsonFormTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        // ---- Check ----

        [Fact]
        public void Check_EmptyObject_IsValid()
        {
            var check = PolicyJsonForm.Check("{}");
            Assert.True(check.Ok, check.Error);
            Assert.NotNull(PolicySerializer.DeserializeFromJson(check.Json!));
        }

        [Fact]
        public void Check_FullPolicy_IsValid_AndKeepsEveryFilter()
        {
            const string json = """
                {
                  "identifiers": {
                    "ssn": {},
                    "emailAddress": {},
                    "identifiers": [
                      { "classification": "bank-account-number", "pattern": "\\b\\d{8,17}\\b" }
                    ]
                  }
                }
                """;
            var check = PolicyJsonForm.Check(json);

            Assert.True(check.Ok, check.Error);
            PhileasPolicy policy = PolicySerializer.DeserializeFromJson(check.Json!);
            Assert.NotNull(policy.Identifiers.Ssn);
            Assert.NotNull(policy.Identifiers.EmailAddress);
            Assert.Equal("bank-account-number", Assert.Single(policy.Identifiers.CustomIdentifiers!).Classification);
        }

        [Fact]
        public void Check_ReturnsTheEnginesJson_NotTheTypedText()
        {
            var check = PolicyJsonForm.Check(SsnPolicy);
            Assert.True(check.Ok, check.Error);
            Assert.Equal(PolicySerializer.SerializeToJson(PolicySerializer.DeserializeFromJson(SsnPolicy)), check.Json);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\r\n\t")]
        public void Check_Empty_SaysSo(string text)
        {
            var check = PolicyJsonForm.Check(text);
            Assert.False(check.Ok);
            Assert.Contains("empty", check.Error);
            Assert.Null(check.Json);
        }

        [Theory]
        [InlineData("[]")]
        [InlineData("\"policy\"")]
        [InlineData("42")]
        [InlineData("null")]
        public void Check_NotAnObject_SaysSo(string text)
        {
            var check = PolicyJsonForm.Check(text);
            Assert.False(check.Ok);
            Assert.Contains("JSON object", check.Error);
        }

        [Fact]
        public void Check_SyntaxError_GivesLineAndColumn_AndWhereToPutTheCaret()
        {
            // Line 3 is missing the colon after "ssn".
            const string text = "{\n  \"identifiers\": {\n    \"ssn\" {}\n  }\n}";
            var check = PolicyJsonForm.Check(text);

            Assert.False(check.Ok);
            Assert.Contains("line 3", check.Error);
            Assert.DoesNotContain("LineNumber:", check.Error);
            Assert.NotNull(check.ErrorIndex);
            int lineStart = text.IndexOf("    \"ssn\"", StringComparison.Ordinal);
            Assert.InRange(check.ErrorIndex!.Value, lineStart, lineStart + "    \"ssn\" {}".Length);
        }

        [Theory]
        [InlineData("{ \"identifiers\": { \"ssn\": {}, } }")] // trailing comma
        [InlineData("{ \"identifiers\": { \"ssn\": {} }")]    // missing brace
        [InlineData("{ identifiers: {} }")]                    // unquoted name
        [InlineData("{ // comment\n }")]                       // comment
        public void Check_MalformedJson_IsRejected(string text)
        {
            var check = PolicyJsonForm.Check(text);
            Assert.False(check.Ok);
            Assert.StartsWith("The JSON isn't valid", check.Error);
        }

        [Fact]
        public void Check_SchemaError_ListsWhatsWrong()
        {
            var check = PolicyJsonForm.Check("{ \"identifiers\": { \"ssn\": \"yes\" } }");
            Assert.False(check.Ok);
            Assert.Contains("schema", check.Error);
            Assert.Contains("•", check.Error);
            Assert.Null(check.ErrorIndex);
        }

        [Fact]
        public void Check_PolicyTheEngineCantLoad_IsRejected()
        {
            // Well-formed and schema-shaped, but the strategy condition can't be parsed.
            const string text = """
                { "identifiers": { "ssn": { "ssnFilterStrategies": [
                  { "strategy": "REDACT", "condition": "token ==== nonsense ((" } ] } } }
                """;
            var check = PolicyJsonForm.Check(text);
            Assert.False(check.Ok);
            Assert.StartsWith("The redaction engine can't load this policy", check.Error);
            Assert.Null(check.Json);
        }

        // ---- IndexOf ----

        [Theory]
        [InlineData("abc", 1, 0, 0)]
        [InlineData("abc", 1, 2, 2)]
        [InlineData("ab\ncd", 2, 0, 3)]
        [InlineData("ab\r\ncd", 2, 1, 5)]
        [InlineData("ab\ncd", 9, 0, 5)]  // past the last line
        [InlineData("ab\ncd", 1, 99, 2)] // past the end of the line stops at its end
        [InlineData("", 1, 0, 0)]
        [InlineData("éx", 1, 2, 1)]       // é is two UTF-8 bytes
        [InlineData("中x", 1, 3, 1)]       // 中 is three
        [InlineData("\U0001F600x", 1, 4, 2)]   // an emoji is four bytes and two chars
        public void IndexOf_MapsLineAndByteOffset(string text, long line, long byteOffset, int expected) =>
            Assert.Equal(expected, PolicyJsonForm.IndexOf(text, line, byteOffset));

        [Fact]
        public void Check_SyntaxErrorAfterNonAscii_ReportsTheCharacterColumn()
        {
            // The missing colon follows "é" (two bytes); the column counts characters.
            const string text = "{ \"café\" 1 }";
            var check = PolicyJsonForm.Check(text);

            Assert.False(check.Ok);
            int at = text.IndexOf('1');
            Assert.Equal(at, check.ErrorIndex);
            Assert.Contains($"column {at + 1}", check.Error);
        }

        // ---- The form ----

        [Fact]
        public void Form_ShowsTheJson_AndTheTitle() => Sta(() =>
        {
            using var form = Shown(SsnPolicy, "tax");
            Assert.Equal(SsnPolicy, Json(form).Text);
            Assert.Equal("Edit JSON: tax", form.Text);
            Assert.DoesNotContain("—", form.Text);
            Assert.Null(form.AcceptButton); // Enter is a new line, not Save
            Assert.Same(Field<Button>(form, "_cancel"), form.CancelButton);
        });

        [Fact]
        public void Save_Valid_ClosesWithTheJson() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            Json(form).Text = "{ \"identifiers\": { \"emailAddress\": {} } }";
            var errors = new List<string>();
            form.ShowError = errors.Add;

            Field<Button>(form, "_save").PerformClick();

            Assert.Empty(errors);
            Assert.Equal(DialogResult.OK, form.DialogResult);
            Assert.NotNull(PolicySerializer.DeserializeFromJson(form.ResultJson!).Identifiers.EmailAddress);
        });

        [Fact]
        public void Save_Invalid_StaysOpen_ShowsTheError_AndMovesTheCaret() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            const string bad = "{\n  \"identifiers\": {\n    \"ssn\" {}\n  }\n}";
            Json(form).Text = bad;
            var errors = new List<string>();
            form.ShowError = errors.Add;

            Field<Button>(form, "_save").PerformClick();

            Assert.Contains("line 3", Assert.Single(errors));
            Assert.Equal(DialogResult.None, form.DialogResult);
            Assert.Null(form.ResultJson);
            Assert.True(form.Visible);
            Assert.True(Json(form).SelectionStart >= bad.IndexOf("    \"ssn\"", StringComparison.Ordinal));
        });

        [Fact]
        public void Save_SchemaError_StaysOpen() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            Json(form).Text = "{ \"identifiers\": { \"ssn\": 5 } }";
            var errors = new List<string>();
            form.ShowError = errors.Add;

            Field<Button>(form, "_save").PerformClick();

            Assert.Contains("schema", Assert.Single(errors));
            Assert.Equal(DialogResult.None, form.DialogResult);
        });

        [Fact]
        public void Save_AfterFixingAnError_Succeeds() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            form.ShowError = _ => { };
            Json(form).Text = "{ \"identifiers\": ";
            Field<Button>(form, "_save").PerformClick();
            Assert.Equal(DialogResult.None, form.DialogResult);

            Json(form).Text = "{ \"identifiers\": {} }";
            Field<Button>(form, "_save").PerformClick();
            Assert.Equal(DialogResult.OK, form.DialogResult);
        });

        [Fact]
        public void Cancel_WithoutEdits_ClosesWithoutAsking() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            int asked = 0;
            form.Confirm = _ => { asked++; return false; };

            form.DialogResult = DialogResult.Cancel;
            form.Close();

            Assert.Equal(0, asked);
            Assert.False(form.Visible);
        });

        [Fact]
        public void Cancel_WithEdits_AsksFirst_AndStaysOpenOnNo() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            Json(form).Text = "{}";
            var prompts = new List<string>();
            form.Confirm = text => { prompts.Add(text); return false; };

            form.Close();

            Assert.Equal(PolicyJsonForm.DiscardPrompt, Assert.Single(prompts));
            Assert.True(form.Visible);
        });

        [Fact]
        public void Cancel_WithEdits_ClosesOnYes() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            Json(form).Text = "{}";
            form.Confirm = _ => true;

            form.Close();

            Assert.False(form.Visible);
            Assert.Null(form.ResultJson);
        });

        [Fact]
        public void Form_LaysOutCleanly() => Sta(() =>
        {
            using var form = Shown(SsnPolicy);
            LayoutAudit.AssertClean(LayoutAudit.Audit(form));
        });

        // ---- The Policy Editor ----

        [Fact]
        public void Editor_HasAnEditJsonButton_EnabledWithAPolicy() => WithEditor(SsnPolicy, (editor, _) =>
        {
            ToolStripButton button = Field<ToolStripButton>(editor, "_editJson");
            Assert.Equal("Edit JSON", button.Text);
            Assert.True(button.Enabled);
            Assert.Contains(button, Field<ToolStrip>(editor, "_toolStrip").Items.Cast<ToolStripItem>());
        });

        [Fact]
        public void Editor_EditJsonButton_DisabledWithoutAPolicy() => Sta(() =>
        {
            using var db = new LiteDatabase(Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".db"));
            using var editor = Editor(new PolicyRepository(db));
            Assert.False(Field<ToolStripButton>(editor, "_editJson").Enabled);
        });

        [Fact]
        public void Editor_OpensTheCurrentPolicy_IncludingUnsavedEdits() => WithEditor(SsnPolicy, (editor, _) =>
        {
            Policy(editor).Identifiers.EmailAddress = new EmailAddress(); // an unsaved edit
            string? shown = null;
            editor.ShowJsonDialog = dlg => { shown = Json(dlg).Text; return DialogResult.Cancel; };

            Invoke(editor, "OnEditJson");

            Assert.Contains("\"ssn\"", shown);
            Assert.Contains("\"emailAddress\"", shown);
            Assert.Contains("\n  ", shown); // indented for reading
        });

        [Fact]
        public void Editor_Save_StoresTheJson_AndReloads() => WithEditor(SsnPolicy, (editor, repo) =>
        {
            Field<TextBox>(editor, "_description").Text = "Tax returns";
            editor.ShowJsonDialog = dlg =>
            {
                Json(dlg).Text = "{ \"identifiers\": { \"emailAddress\": {} } }";
                typeof(PolicyJsonForm).GetMethod("OnSave", Flags)!.Invoke(dlg, new object?[] { null, EventArgs.Empty });
                return dlg.DialogResult;
            };

            Invoke(editor, "OnEditJson");

            PolicyEntity stored = repo.FindByName("default")!;
            PhileasPolicy saved = PolicySerializer.DeserializeFromJson(stored.Json);
            Assert.NotNull(saved.Identifiers.EmailAddress);
            Assert.Null(saved.Identifiers.Ssn);
            Assert.Equal("Tax returns", stored.Description);
            Assert.NotNull(Policy(editor).Identifiers.EmailAddress); // the editor shows the new policy
            Assert.Null(Policy(editor).Identifiers.Ssn);
            Assert.False((bool)typeof(PolicyEditorForm).GetField("_dirty", Flags)!.GetValue(editor)!);
            Assert.Equal("default", Field<ToolStripComboBox>(editor, "_policyCombo").SelectedItem);
        });

        [Fact]
        public void Editor_Cancel_ChangesNothing() => WithEditor(SsnPolicy, (editor, repo) =>
        {
            string before = repo.FindByName("default")!.Json;
            editor.ShowJsonDialog = dlg => { Json(dlg).Text = "{}"; return DialogResult.Cancel; };

            Invoke(editor, "OnEditJson");

            Assert.Equal(before, repo.FindByName("default")!.Json);
            Assert.NotNull(Policy(editor).Identifiers.Ssn);
        });

        // ---- helpers ----

        private static PolicyJsonForm Shown(string json, string name = "default")
        {
            var form = new PolicyJsonForm(json, name)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(0, 0)
            };
            form.Show();
            Application.DoEvents();
            return form;
        }

        private static TextBox Json(PolicyJsonForm form) => Field<TextBox>(form, "_json");

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        private static PhileasPolicy Policy(PolicyEditorForm editor) => Field<PhileasPolicy>(editor, "_policy");

        private static void Invoke(PolicyEditorForm editor, string handler) =>
            typeof(PolicyEditorForm).GetMethod(handler, Flags)!.Invoke(editor, new object?[] { null, EventArgs.Empty });

        private static void Sta(Action action) => DpiLayoutTests.Sta(action);

        private static PolicyEditorForm Editor(PolicyRepository repo)
        {
            var editor = new PolicyEditorForm(repo)
            {
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-32000, -32000),
                ShowInTaskbar = false
            };
            editor.Show(); // Load selects and loads the "default" policy
            Application.DoEvents();
            return editor;
        }

        private void WithEditor(string policyJson, Action<PolicyEditorForm, PolicyRepository> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var db = new LiteDatabase(Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".db"));
                    var repo = new PolicyRepository(db);
                    repo.Insert(new PolicyEntity { Name = "default", Json = policyJson });
                    using var editor = Editor(repo);
                    test(editor, repo);
                    typeof(PolicyEditorForm).GetField("_dirty", Flags)!.SetValue(editor, false); // close without the save prompt
                    editor.Close();
                }
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
