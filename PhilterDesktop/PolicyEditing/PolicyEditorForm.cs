using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// Edits redaction policies using the Phileas policy model. C# replacement for the
    /// former VB PhilterPolicyEditor project; filter rows are data-driven so each filter
    /// type is a single registration.
    /// </summary>
    public sealed class PolicyEditorForm : Form
    {
        private readonly PolicyRepository _repo;
        private PhileasPolicy? _policy;
        private bool _loading;

        private readonly ComboBox _policyCombo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly Button _new = new() { Text = "New…", AutoSize = true };
        private readonly Button _save = new() { Text = "Save", AutoSize = true, Enabled = false };
        private readonly Button _saveAs = new() { Text = "Save As…", AutoSize = true, Enabled = false };
        private readonly Button _delete = new() { Text = "Delete", AutoSize = true, Enabled = false };
        private readonly Button _viewJson = new() { Text = "View JSON", AutoSize = true, Enabled = false };
        private readonly FlowLayoutPanel _filters = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, Padding = new Padding(12) };
        private readonly List<Action> _loaders = new();

        public PolicyEditorForm(PolicyRepository repo)
        {
            _repo = repo;

            Text = "Policy Editor";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 640);
            MinimumSize = new Size(460, 480);

            BuildLayout();
            RegisterFilters();

            _policyCombo.SelectedIndexChanged += (_, _) => LoadSelectedPolicy();
            _new.Click += OnNew;
            _save.Click += OnSave;
            _saveAs.Click += OnSaveAs;
            _delete.Click += OnDelete;
            _viewJson.Click += OnViewJson;

            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(_save);

            Load += (_, _) => ReloadPolicyList("default");
        }

        private void BuildLayout()
        {
            var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 44, Padding = new Padding(8), WrapContents = false, AutoSize = false };
            top.Controls.AddRange(new Control[] { _policyCombo, _new, _save, _saveAs, _delete, _viewJson });

            var header = new Label
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "Enabled filters (check to enable, Configure… to set replacement strategies):",
                Padding = new Padding(12, 6, 0, 0),
                ForeColor = ModernTheme.SubtleText
            };

            Controls.Add(_filters);
            Controls.Add(header);
            Controls.Add(top);
            SetEditingEnabled(false);
        }

        // --- Filter registration ---------------------------------------------

        private void RegisterFilters()
        {
            AddFilter<Age, AgeFilterStrategy>("Age", i => i.Age, (i, v) => i.Age = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<City, CityFilterStrategy>("City", i => i.City, (i, v) => i.City = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<County, CountyFilterStrategy>("County", i => i.County, (i, v) => i.County = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<CreditCard, CreditCardFilterStrategy>("Credit Card", i => i.CreditCard, (i, v) => i.CreditCard = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Date, DateFilterStrategy>("Date", i => i.Date, (i, v) => i.Date = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<EmailAddress, EmailAddressFilterStrategy>("Email Address", i => i.EmailAddress, (i, v) => i.EmailAddress = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<FirstName, FirstNameFilterStrategy>("First Name", i => i.FirstName, (i, v) => i.FirstName = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Hospital, HospitalFilterStrategy>("Hospital", i => i.Hospital, (i, v) => i.Hospital = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<IpAddress, IpAddressFilterStrategy>("IP Address", i => i.IpAddress, (i, v) => i.IpAddress = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<PhoneNumber, PhoneNumberFilterStrategy>("Phone Number", i => i.PhoneNumber, (i, v) => i.PhoneNumber = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<PhoneNumberExtension, PhoneNumberExtensionFilterStrategy>("Phone Number Extension", i => i.PhoneNumberExtension, (i, v) => i.PhoneNumberExtension = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Ssn, SsnFilterStrategy>("SSN", i => i.Ssn, (i, v) => i.Ssn = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<State, StateFilterStrategy>("State", i => i.State, (i, v) => i.State = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<StateAbbreviation, StateAbbreviationFilterStrategy>("State Abbreviation", i => i.StateAbbreviation, (i, v) => i.StateAbbreviation = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Surname, SurnameFilterStrategy>("Surname", i => i.Surname, (i, v) => i.Surname = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Url, UrlFilterStrategy>("URL", i => i.Url, (i, v) => i.Url = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<Vin, VinFilterStrategy>("VIN", i => i.Vin, (i, v) => i.Vin = v, f => f.Strategies, (f, s) => f.Strategies = s);
            AddFilter<ZipCode, ZipCodeFilterStrategy>("Zip Code", i => i.ZipCode, (i, v) => i.ZipCode = v, f => f.Strategies, (f, s) => f.Strategies = s);

            AddCustomIdentifiersRow();
        }

        private void AddFilter<TF, TS>(
            string name,
            Func<Identifiers, TF?> get, Action<Identifiers, TF?> set,
            Func<TF, List<TS>?> getStrategies, Action<TF, List<TS>?> setStrategies)
            where TF : AbstractPolicyFilter, new()
            where TS : AbstractFilterStrategy, new()
        {
            var checkBox = new CheckBox { Text = name, AutoSize = true, Location = new Point(3, 6) };
            var configure = new Button { Text = "Configure…", AutoSize = true, Location = new Point(260, 2), Enabled = false };
            var row = new Panel { Width = 440, Height = 32 };
            row.Controls.Add(checkBox);
            row.Controls.Add(configure);
            _filters.Controls.Add(row);

            checkBox.CheckedChanged += (_, _) =>
            {
                if (_loading || _policy is null)
                {
                    return;
                }
                if (checkBox.Checked)
                {
                    if (get(_policy.Identifiers) is null)
                    {
                        set(_policy.Identifiers, new TF { Enabled = true });
                    }
                }
                else
                {
                    set(_policy.Identifiers, null);
                }
                configure.Enabled = checkBox.Checked;
            };

            configure.Click += (_, _) =>
            {
                if (_policy is null)
                {
                    return;
                }
                TF? filter = get(_policy.Identifiers);
                if (filter is null)
                {
                    filter = new TF { Enabled = true };
                    set(_policy.Identifiers, filter);
                    checkBox.Checked = true;
                }
                List<TS> existing = getStrategies(filter) ?? new List<TS>();
                using var dlg = new FilterStrategiesForm<TS>(name, existing);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    List<TS> result = dlg.GetStrategies();
                    setStrategies(filter, result.Count > 0 ? result : null);
                }
            };

            _loaders.Add(() =>
            {
                bool enabled = _policy is not null && get(_policy.Identifiers) is not null;
                checkBox.Checked = enabled;
                configure.Enabled = enabled;
            });
        }

        private void AddCustomIdentifiersRow()
        {
            var checkBox = new CheckBox { Text = "Custom Identifiers", AutoSize = true, Location = new Point(3, 6) };
            var configure = new Button { Text = "Configure…", AutoSize = true, Location = new Point(260, 2), Enabled = false };
            var row = new Panel { Width = 440, Height = 32 };
            row.Controls.Add(checkBox);
            row.Controls.Add(configure);
            _filters.Controls.Add(row);

            checkBox.CheckedChanged += (_, _) =>
            {
                if (_loading || _policy is null)
                {
                    return;
                }
                _policy.Identifiers.CustomIdentifiers = checkBox.Checked
                    ? _policy.Identifiers.CustomIdentifiers ?? new List<Identifier>()
                    : null;
                configure.Enabled = checkBox.Checked;
            };

            configure.Click += (_, _) =>
            {
                if (_policy is null)
                {
                    return;
                }
                _policy.Identifiers.CustomIdentifiers ??= new List<Identifier>();
                using var dlg = new CustomIdentifiersForm(_policy.Identifiers.CustomIdentifiers);
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    _policy.Identifiers.CustomIdentifiers = dlg.Identifiers.Count > 0 ? dlg.Identifiers : null;
                    checkBox.Checked = _policy.Identifiers.CustomIdentifiers is not null;
                }
            };

            _loaders.Add(() =>
            {
                bool enabled = _policy?.Identifiers.CustomIdentifiers is { Count: > 0 };
                checkBox.Checked = enabled;
                configure.Enabled = enabled;
            });
        }

        // --- Policy list / load / save ---------------------------------------

        private void ReloadPolicyList(string? selectName)
        {
            _policyCombo.BeginUpdate();
            _policyCombo.Items.Clear();
            foreach (PolicyEntity entity in _repo.GetAll().OrderBy(p => p.Name))
            {
                _policyCombo.Items.Add(entity.Name);
            }
            _policyCombo.EndUpdate();

            int index = selectName is null ? -1 : _policyCombo.Items.IndexOf(selectName);
            if (index < 0 && _policyCombo.Items.Count > 0)
            {
                index = 0;
            }
            if (index >= 0)
            {
                _policyCombo.SelectedIndex = index;
            }
            else
            {
                _policy = null;
                SetEditingEnabled(false);
            }
        }

        private void LoadSelectedPolicy()
        {
            if (_policyCombo.SelectedItem is not string name)
            {
                return;
            }
            PolicyEntity? entity = _repo.FindByName(name);
            if (entity is null)
            {
                return;
            }

            _policy = PolicySerializer.DeserializeFromJson(string.IsNullOrWhiteSpace(entity.Json) ? "{}" : entity.Json);

            _loading = true;
            foreach (Action loader in _loaders)
            {
                loader();
            }
            _loading = false;

            SetEditingEnabled(true);
        }

        private void OnSave(object? sender, EventArgs e)
        {
            if (_policy is null || _policyCombo.SelectedItem is not string name)
            {
                return;
            }
            PolicyEntity? entity = _repo.FindByName(name);
            if (entity is null)
            {
                return;
            }
            entity.Json = PolicySerializer.SerializeToJson(_policy);
            _repo.Update(entity);
            MessageBox.Show($"Policy '{name}' saved.", "Policy Editor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnNew(object? sender, EventArgs e)
        {
            string? name = Prompt("Enter a name for the new policy:", "New Policy");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            if (_repo.FindByName(name) is not null)
            {
                MessageBox.Show($"A policy named '{name}' already exists.", "Duplicate Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _repo.Insert(new PolicyEntity { Name = name, Json = PolicySerializer.SerializeToJson(new PhileasPolicy { Name = name }) });
            ReloadPolicyList(name);
        }

        private void OnSaveAs(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            string? name = Prompt("Enter a name for the new policy:", "Save As");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            if (_repo.FindByName(name) is not null)
            {
                MessageBox.Show($"A policy named '{name}' already exists.", "Duplicate Policy", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            _policy.Name = name;
            _repo.Insert(new PolicyEntity { Name = name, Json = PolicySerializer.SerializeToJson(_policy) });
            ReloadPolicyList(name);
        }

        private void OnDelete(object? sender, EventArgs e)
        {
            if (_policyCombo.SelectedItem is not string name)
            {
                return;
            }
            if (name.Equals("default", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("The 'default' policy cannot be deleted.", "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (MessageBox.Show($"Delete the policy '{name}'?", "Delete Policy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }
            PolicyEntity? entity = _repo.FindByName(name);
            if (entity is not null)
            {
                _repo.Delete(entity.Id);
            }
            ReloadPolicyList("default");
        }

        private void OnViewJson(object? sender, EventArgs e)
        {
            if (_policy is null)
            {
                return;
            }
            using var dlg = new Form
            {
                Text = $"\"{_policyCombo.SelectedItem}\" Policy JSON",
                StartPosition = FormStartPosition.CenterParent,
                ClientSize = new Size(560, 480)
            };
            var box = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Text = PolicySerializer.SerializeToJson(_policy) };
            dlg.Controls.Add(box);
            ModernTheme.Apply(dlg);
            dlg.ShowDialog(this);
        }

        private void SetEditingEnabled(bool enabled)
        {
            _filters.Enabled = enabled;
            _save.Enabled = enabled;
            _saveAs.Enabled = enabled;
            _delete.Enabled = enabled;
            _viewJson.Enabled = enabled;
        }

        private static string? Prompt(string message, string title)
        {
            using var form = new Form
            {
                Text = title,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(380, 120)
            };
            var label = new Label { Text = message, AutoSize = true, Location = new Point(12, 15) };
            var textBox = new TextBox { Location = new Point(15, 40), Width = 350 };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(204, 80), Size = new Size(75, 26) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(289, 80), Size = new Size(75, 26) };
            form.Controls.AddRange(new Control[] { label, textBox, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            ModernTheme.Apply(form);
            return form.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
        }
    }
}
