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

using System.Text.Json;
using Phileas.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits a policy's JSON directly. Save checks the JSON, the policy schema and that the engine can
    /// load it, and keeps the dialog open with the problem shown until it passes.
    /// </summary>
    internal sealed class PolicyJsonForm : Form
    {
        internal const string DiscardPrompt = "Discard your changes to the policy JSON?";

        private readonly string _original;
        private readonly Font _mono = new("Consolas", 10f);
        private readonly TextBox _json = new()
        {
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            WordWrap = false,
            ScrollBars = ScrollBars.Both,
            Dock = DockStyle.Fill
        };
        private readonly Button _save = new() { Text = "&Save", Size = ModernTheme.StandardButtonSize };
        private readonly Button _cancel = new() { Text = "&Cancel", Size = ModernTheme.StandardButtonSize, DialogResult = DialogResult.Cancel };

        /// <summary>Shows a problem with the JSON; tests replace it to check the message without a dialog.</summary>
        internal Action<string> ShowError;

        /// <summary>Asks whether to discard edits; tests replace it to answer without a dialog.</summary>
        internal Func<string, bool> Confirm;

        /// <summary>The checked policy JSON, set when the form closes with OK.</summary>
        public string? ResultJson { get; private set; }

        public PolicyJsonForm(string json, string policyName)
        {
            _original = json;
            ShowError = text => MessageBox.Show(this, text, "Invalid policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            Confirm = text => MessageBox.Show(this, text, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

            Text = $"Edit JSON: {policyName}";
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(720, 560);
            MinimumSize = new Size(420, 300);
            ShowInTaskbar = false;
            MinimizeBox = false;

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(8)
            };
            buttons.Controls.Add(_cancel);
            buttons.Controls.Add(_save);
            var editor = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 0) };
            editor.Controls.Add(_json);
            Controls.Add(editor);
            Controls.Add(buttons);

            _json.Font = _mono;
            _json.Text = json;
            _save.Click += OnSave;
            CancelButton = _cancel; // Esc cancels; Enter stays a new line in the editor

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);
            ActiveControl = _json;
            _json.Select(0, 0);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            JsonCheck check = Check(_json.Text);
            if (!check.Ok)
            {
                ShowError(check.Error!);
                if (check.ErrorIndex is int index)
                {
                    _json.Select(Math.Min(index, _json.TextLength), 0);
                    _json.ScrollToCaret();
                }
                ActiveControl = _json;
                return;
            }
            ResultJson = check.Json;
            DialogResult = DialogResult.OK;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult != DialogResult.OK && _json.Text != _original && !Confirm(DiscardPrompt))
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        /// <summary>The outcome of checking edited JSON: the policy JSON to store, or what's wrong.</summary>
        internal sealed record JsonCheck(bool Ok, string? Json, string? Error, int? ErrorIndex);

        /// <summary>
        /// Checks edited policy JSON: well-formed, an object, valid for the policy schema, and loadable
        /// by the engine. On success returns the policy as the engine writes it.
        /// </summary>
        internal static JsonCheck Check(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Fail("The policy is empty. A policy is a JSON object, such as {}.");
            }

            try
            {
                using JsonDocument doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return Fail("A policy must be a JSON object, starting with { and ending with }.");
                }
            }
            catch (JsonException ex)
            {
                long line = (ex.LineNumber ?? 0) + 1;
                int index = IndexOf(text, line, ex.BytePositionInLine ?? 0);
                int column = index - LineStart(text, line) + 1;
                string reason = ex.Message.Split(" LineNumber:")[0].Trim();
                return new JsonCheck(false, null,
                    $"The JSON isn't valid at line {line}, column {column}:" + Environment.NewLine + Environment.NewLine + reason,
                    index);
            }

            PolicyValidationResult schema = PolicyValidator.Validate(text);
            if (!schema.IsValid)
            {
                return Fail("This policy does not match the redaction engine's policy schema (PhiSQL " +
                            PolicyValidator.SchemaVersion + "):" + Bullets(schema.Errors));
            }

            try
            {
                return new JsonCheck(true, PolicySerializer.SerializeToJson(PolicySerializer.DeserializeFromJson(text)), null, null);
            }
            catch (PolicyValidationException ex)
            {
                return Fail("The redaction engine can't load this policy:" + Bullets(ex.Errors));
            }
            catch (Exception ex)
            {
                return Fail("The redaction engine can't load this policy:" + Environment.NewLine + Environment.NewLine + ex.Message);
            }
        }

        private static JsonCheck Fail(string error) => new(false, null, error, null);

        private static string Bullets(IEnumerable<string> errors) =>
            Environment.NewLine + Environment.NewLine + "  • " + string.Join(Environment.NewLine + "  • ", errors.Take(15));

        // Character index of a 1-based line and a UTF-8 byte offset into it (how the JSON reader
        // reports positions), for placing the caret at a syntax error.
        internal static int IndexOf(string text, long line, long byteOffset)
        {
            int index = LineStart(text, line);
            long bytes = 0;
            while (index < text.Length && text[index] != '\n' && bytes < byteOffset)
            {
                int width = char.IsHighSurrogate(text[index]) && index + 1 < text.Length ? 2 : 1;
                bytes += System.Text.Encoding.UTF8.GetByteCount(text.AsSpan(index, width));
                index += width;
            }
            return index;
        }

        // Character index where a 1-based line starts (the end of the text past the last line).
        private static int LineStart(string text, long line)
        {
            int index = 0;
            for (long l = 1; l < line; l++)
            {
                int next = text.IndexOf('\n', index);
                if (next < 0)
                {
                    return text.Length;
                }
                index = next + 1;
            }
            return index;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _mono.Dispose();
            }
        }
    }
}
