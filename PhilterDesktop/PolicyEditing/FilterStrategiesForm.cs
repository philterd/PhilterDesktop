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

using System.Collections;
using Phileas.Policy.Filters.Strategies;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Lists and edits the strategies for one filter type. Driven by a runtime strategy
    /// <see cref="Type"/> so the reflection-based editor can use it for any filter.
    /// </summary>
    internal sealed class FilterStrategiesForm : Form
    {
        private readonly string _display;
        private readonly Type _strategyType;
        private readonly List<AbstractFilterStrategy> _items;
        private readonly IReadOnlyList<FilterOptionToggle> _options;
        private readonly PhileasPolicy? _policy; // holds the encryption key references
        private readonly List<(CheckBox Box, FilterOptionToggle Toggle)> _optionChecks = new();
        private static readonly Size ButtonSize = new(90, 34);

        private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly Button _new = new() { Text = "New…", Size = ButtonSize };
        private readonly Button _edit = new() { Text = "Edit…", Size = ButtonSize, Enabled = false };
        private readonly Button _remove = new() { Text = "Remove", Size = ButtonSize, Enabled = false };
        private readonly Button _ok = new() { Text = "OK", DialogResult = DialogResult.OK, Size = ButtonSize };
        private readonly Button _cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ButtonSize };

        public FilterStrategiesForm(string filterTypeDisplay, IEnumerable existing, Type strategyType,
            IReadOnlyList<FilterOptionToggle>? options = null, PhileasPolicy? policy = null)
        {
            _display = filterTypeDisplay;
            _policy = policy;
            _strategyType = strategyType;
            _items = existing.Cast<AbstractFilterStrategy>().ToList();
            _options = options ?? Array.Empty<FilterOptionToggle>();

            Text = $"{filterTypeDisplay} Filter Strategies";
            StartPosition = FormStartPosition.CenterParent;
            // Grow the dialog so the Options section (if any) sits above the strategies list without
            // squeezing it.
            int optionsHeight = _options.Count == 0 ? 0 : 12 + _options.Count * 46;
            ClientSize = new Size(460, 340 + optionsHeight);
            MinimumSize = new Size(380, 300 + optionsHeight);
            AcceptButton = _ok;
            CancelButton = _cancel;

            BuildLayout();
            RefreshList();

            _list.SelectedIndexChanged += (_, _) => UpdateButtons();
            _new.Click += OnNew;
            _edit.Click += OnEdit;
            _remove.Click += OnRemove;
            // Copy the option checkboxes back into the toggles when the user accepts. The button's
            // DialogResult closes the form after this handler runs, so the caller sees the new values.
            _ok.Click += (_, _) => ApplyOptions();

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_ok);
        }

        /// <summary>Copies the current option checkbox states into their toggles. Invoked when the user
        /// accepts the dialog (OK), so the caller reads the updated <see cref="FilterOptionToggle.Value"/>.</summary>
        internal void ApplyOptions()
        {
            foreach ((CheckBox box, FilterOptionToggle toggle) in _optionChecks)
            {
                toggle.Value = box.Checked;
            }
        }

        /// <summary>Builds a strongly-typed <c>List&lt;TStrategy&gt;</c> of the edited items.</summary>
        public IList BuildResultList()
        {
            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_strategyType))!;
            foreach (AbstractFilterStrategy item in _items)
            {
                list.Add(item);
            }
            return list;
        }

        private void BuildLayout()
        {
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Right, FlowDirection = FlowDirection.TopDown, Width = 112, Padding = new Padding(8) };
            buttons.Controls.AddRange(new Control[] { _new, _edit, _remove });

            var bottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 58, Padding = new Padding(8) };
            bottom.Controls.AddRange(new Control[] { _cancel, _ok });

            var listPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };
            listPanel.Controls.Add(_list);

            Controls.Add(listPanel);
            Controls.Add(buttons);
            Controls.Add(bottom);

            // Options span the full width at the very top (added last so it docks first). Only shown for
            // filters that registered options (e.g. EIN's "only valid IRS prefixes").
            if (_options.Count > 0)
            {
                Controls.Add(BuildOptionsPanel());
            }
        }

        private Control BuildOptionsPanel()
        {
            var panel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Height = 12 + _options.Count * 46,
                Padding = new Padding(10, 8, 10, 4)
            };
            panel.Controls.Add(new Label
            {
                Text = "Options",
                AutoSize = true,
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 4)
            });
            foreach (FilterOptionToggle toggle in _options)
            {
                var box = new CheckBox { Text = toggle.Label, Checked = toggle.Value, AutoSize = true, Margin = new Padding(0, 0, 0, 0) };
                panel.Controls.Add(box);
                panel.Controls.Add(new Label
                {
                    Text = toggle.Description,
                    AutoSize = false,
                    Width = 400,
                    Height = 28,
                    ForeColor = ModernTheme.SubtleText,
                    Font = new Font(ModernTheme.UiFont.FontFamily, 8f),
                    Margin = new Padding(20, 0, 0, 6)
                });
                _optionChecks.Add((box, toggle));
            }
            return panel;
        }

        private void RefreshList()
        {
            int selected = _list.SelectedIndex;
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (AbstractFilterStrategy s in _items)
            {
                _list.Items.Add(Describe(s, _policy));
            }
            _list.EndUpdate();
            if (selected >= 0 && selected < _list.Items.Count)
            {
                _list.SelectedIndex = selected;
            }
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            _edit.Enabled = _list.SelectedIndex >= 0;
            _remove.Enabled = _list.SelectedIndex >= 0;
        }

        private void OnNew(object? sender, EventArgs e)
        {
            var strategy = (AbstractFilterStrategy)Activator.CreateInstance(_strategyType)!;
            using var dlg = new AddFilterStrategyForm(strategy, _display, _policy);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                _items.Add(strategy);
                RefreshList();
            }
        }

        private void OnEdit(object? sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i < 0)
            {
                return;
            }
            using var dlg = new AddFilterStrategyForm(_items[i], _display, _policy);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshList();
            }
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i >= 0)
            {
                _items.RemoveAt(i);
                RefreshList();
            }
        }

        /// <summary>One line describing a strategy and its key settings, for the list.</summary>
        internal static string Describe(AbstractFilterStrategy s, PhileasPolicy? policy = null)
        {
            StrategySettings settings = StrategySettings.Load(s, policy);
            string consistent = settings.Consistent ? ", consistent across contexts" : string.Empty;
            string text = settings.Strategy switch
            {
                StrategyCatalog.Redact => $"Redact with \"{settings.RedactionFormat}\"",
                StrategyCatalog.StaticReplace => $"Replace with \"{settings.StaticReplacement}\"",
                StrategyCatalog.RandomReplace => settings.RandomMethod switch
                {
                    StrategySettings.RandomFromList => $"Replace with a value from a list of {settings.RandomCandidates.Count}",
                    StrategySettings.RandomUuid => "Replace with a random ID",
                    _ => "Replace with a realistic random value"
                } + consistent,
                StrategyCatalog.Mask => $"Mask with {settings.MaskCharacter}" +
                    (settings.MaskLength is int length ? $", {length} characters" : string.Empty),
                StrategyCatalog.HashSha256 => "Replace with a SHA-256 hash" + (settings.Salt ? ", salted" : string.Empty),
                StrategyCatalog.Crypto => "Encrypt" + KeySource(settings.CryptoKeyVariable, settings.HasStoredCryptoKey),
                StrategyCatalog.Fpe => "Encrypt, keeping the format" + KeySource(settings.FpeKeyVariable, settings.HasStoredFpeKey),
                StrategyCatalog.MapReplace =>
                    $"Replace from a lookup table ({settings.Mappings.Count} {(settings.Mappings.Count == 1 ? "entry" : "entries")}, " +
                    $"otherwise {StrategyCatalog.Find(settings.FallbackStrategy)?.Label.ToLowerInvariant() ?? settings.FallbackStrategy})" + consistent,
                StrategyCatalog.Shift => settings.ShiftRandom
                    ? "Shift the date by a random amount"
                    : $"Shift the date by {Amount(settings.ShiftYears, "year")}{Amount(settings.ShiftMonths, "month")}{Amount(settings.ShiftDays, "day")}".TrimEnd(',', ' '),
                _ => StrategyCatalog.Find(settings.Strategy)?.Label ?? settings.Strategy
            };
            if (!string.IsNullOrWhiteSpace(s.Condition))
            {
                text += $"  [when {s.Condition}]";
            }
            return text;
        }

        private static string KeySource(string variable, bool stored) =>
            variable.Length > 0 ? $" (key from %{variable}%)" : stored ? " (key stored in the policy)" : " (no key set)";

        private static string Amount(int value, string unit) =>
            value == 0 ? string.Empty : $"{value} {unit}{(Math.Abs(value) == 1 ? string.Empty : "s")}, ";
    }
}
