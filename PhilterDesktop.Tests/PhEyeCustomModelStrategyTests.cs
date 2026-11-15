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
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Redaction strategies for custom (user-added) PhEye models, edited from the Policy Editor's PhEye
    /// tab: Strategies… opens the strategy dialog for any model, editing the model keeps its strategies,
    /// and hand-written strategies load and survive a save.
    /// </summary>
    public sealed class PhEyeCustomModelStrategyTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "pheye-strategies-" + Guid.NewGuid().ToString("N"));
        private readonly string _modelDir;

        public PhEyeCustomModelStrategyTests()
        {
            // A folder PhEyeModelForm accepts; the redaction test uses the real bundled model instead.
            _modelDir = Path.Combine(_dir, "my-model");
            Directory.CreateDirectory(_modelDir);
            foreach (string file in new[] { "model.onnx", "spm.model", "gliner_config.json" })
            {
                File.WriteAllText(Path.Combine(_modelDir, file), "");
            }
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        // ---- Buttons ----

        [Fact]
        public void StrategiesButton_IsEnabledOnlyWithASelection() => WithEditor(CustomPolicy(), editor =>
        {
            Button strategies = Field<Button>(editor, "_phEyeStrategies");
            Assert.Equal("Strategies…", strategies.Text);
            Assert.False(strategies.Enabled);

            Select(editor, 0);
            Assert.True(strategies.Enabled);
        });

        [Fact]
        public void StrategiesButton_SitsBetweenEditAndRemove() => WithEditor(CustomPolicy(), editor =>
        {
            Control.ControlCollection row = Field<Button>(editor, "_phEyeStrategies").Parent!.Controls;
            int index = row.IndexOf(Field<Button>(editor, "_phEyeStrategies"));

            Assert.Equal(row.IndexOf(Field<Button>(editor, "_editPhEyeModel")) + 1, index);
            Assert.Equal(row.IndexOf(Field<Button>(editor, "_removePhEyeModel")) - 1, index);
        });

        // ---- Hand-written strategies load ----

        [Fact]
        public void HandWrittenStrategy_ShowsInTheList() => WithEditor(CustomPolicy(Abbreviate()), editor =>
        {
            ListViewItem item = List(editor).Items[0];

            Assert.Equal("my-model", item.Text);
            Assert.Equal("Strategy", List(editor).Columns[3].Text);
            Assert.Equal(FilterStrategiesForm.Describe(Abbreviate()), item.SubItems[3].Text);
        });

        [Fact]
        public void HandWrittenStrategies_LoadInTheDialog() => WithEditor(CustomPolicy(Abbreviate(), Mask()), editor =>
        {
            List<AbstractFilterStrategy>? shown = null;
            string? title = null;
            editor.ShowPhEyeDialog = dlg =>
            {
                var form = Assert.IsType<FilterStrategiesForm>(dlg);
                title = form.Text;
                shown = Items(form).ToList();
                return DialogResult.Cancel;
            };

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.Equal(new[] { AbstractFilterStrategy.Abbreviate, AbstractFilterStrategy.Mask }, shown!.Select(st => st.Strategy));
            Assert.Contains("my-model", title);
        });

        [Fact]
        public void ModelWithoutStrategies_OpensWithTheDefaultRedact() => WithEditor(CustomPolicy(), editor =>
        {
            List<AbstractFilterStrategy>? shown = null;
            editor.ShowPhEyeDialog = dlg => { shown = Items((FilterStrategiesForm)dlg).ToList(); return DialogResult.Cancel; };

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.Equal(AbstractFilterStrategy.Redact, Assert.Single(shown!).Strategy);
        });

        // ---- Editing strategies ----

        [Fact]
        public void Strategies_OnOk_ReplaceTheModelsStrategies() => WithEditor(CustomPolicy(), editor =>
        {
            editor.ShowPhEyeDialog = dlg => SetItems((FilterStrategiesForm)dlg, Abbreviate());

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            PhEye phEye = Policy(editor).Identifiers.PhEyes![0];
            Assert.Equal(AbstractFilterStrategy.Abbreviate, Assert.Single(phEye.Strategies!).Strategy);
            Assert.True(Dirty(editor));
            Assert.Equal(FilterStrategiesForm.Describe(Abbreviate()), List(editor).Items[0].SubItems[3].Text);
        });

        [Fact]
        public void RemovingEveryStrategy_ShowsTheDefaultRedaction() => WithEditor(CustomPolicy(Abbreviate()), editor =>
        {
            editor.ShowPhEyeDialog = dlg => SetItems((FilterStrategiesForm)dlg);

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.Null(Policy(editor).Identifiers.PhEyes![0].Strategies);
            Assert.Equal(FilterStrategiesForm.Describe(new PhEyeFilterStrategy()), List(editor).Items[0].SubItems[3].Text);
        });

        [Fact]
        public void Strategies_OnCancel_ChangeNothing() => WithEditor(CustomPolicy(Mask()), editor =>
        {
            editor.ShowPhEyeDialog = dlg => { SetItems((FilterStrategiesForm)dlg, Abbreviate()); return DialogResult.Cancel; };

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.Equal(AbstractFilterStrategy.Mask, Assert.Single(Policy(editor).Identifiers.PhEyes![0].Strategies!).Strategy);
            Assert.False(Dirty(editor));
        });

        [Fact]
        public void Strategies_EditsOnlyTheSelectedModel() => WithEditor(TwoModelPolicy(), editor =>
        {
            editor.ShowPhEyeDialog = dlg => SetItems((FilterStrategiesForm)dlg, Abbreviate());

            Select(editor, 1);
            Invoke(editor, "OnPhEyeStrategies");

            List<PhEye> phEyes = Policy(editor).Identifiers.PhEyes!;
            Assert.Equal(AbstractFilterStrategy.Mask, Assert.Single(phEyes[0].Strategies!).Strategy);
            Assert.Equal(AbstractFilterStrategy.Abbreviate, Assert.Single(phEyes[1].Strategies!).Strategy);
        });

        [Fact]
        public void Strategies_WorkForTheBuiltInModelToo() => WithEditor(BuiltInPolicy(), editor =>
        {
            editor.ShowPhEyeDialog = dlg => SetItems((FilterStrategiesForm)dlg, Abbreviate());

            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.Equal(AbstractFilterStrategy.Abbreviate, Assert.Single(Policy(editor).Identifiers.PhEyes![0].Strategies!).Strategy);
        });

        [Fact]
        public void BuiltInModel_EditStillOpensStrategies() => WithEditor(BuiltInPolicy(), editor =>
        {
            Form? opened = null;
            editor.ShowPhEyeDialog = dlg => { opened = dlg; return DialogResult.Cancel; };

            Select(editor, 0);
            Invoke(editor, "OnEditPhEyeModel");

            Assert.IsType<FilterStrategiesForm>(opened);
        });

        // ---- Editing the model keeps strategies ----

        [Fact]
        public void CustomModel_EditStillOpensTheModelDialog() => WithEditor(CustomPolicy(Abbreviate()), editor =>
        {
            Form? opened = null;
            editor.ShowPhEyeDialog = dlg => { opened = dlg; return DialogResult.Cancel; };

            Select(editor, 0);
            Invoke(editor, "OnEditPhEyeModel");

            Assert.IsType<PhEyeModelForm>(opened);
        });

        [Fact]
        public void EditingFolderTypesAndThreshold_KeepsStrategies() => WithEditor(CustomPolicy(Abbreviate(), Mask()), editor =>
        {
            string otherModel = Path.Combine(_dir, "other-model");
            Directory.CreateDirectory(otherModel);
            foreach (string file in Directory.GetFiles(_modelDir))
            {
                File.Copy(file, Path.Combine(otherModel, Path.GetFileName(file)));
            }
            editor.ShowPhEyeDialog = dlg =>
            {
                var form = Assert.IsType<PhEyeModelForm>(dlg);
                Field<TextBox>(form, "_modelPath").Text = otherModel;
                Field<TextBox>(form, "_labels").Text = "organization, location";
                Field<NumericUpDown>(form, "_threshold").Value = 0.8M;
                typeof(PhEyeModelForm).GetMethod("OnOk", Flags)!.Invoke(form, new object?[] { null, EventArgs.Empty });
                return DialogResult.OK;
            };

            Select(editor, 0);
            Invoke(editor, "OnEditPhEyeModel");

            PhEye phEye = Policy(editor).Identifiers.PhEyes![0];
            Assert.Equal(otherModel, phEye.PhEyeConfiguration.ModelPath);
            Assert.Equal(new[] { "organization", "location" }, phEye.PhEyeConfiguration.Labels);
            Assert.Equal(0.8, phEye.PhEyeConfiguration.Threshold, 3);
            Assert.Equal(new[] { AbstractFilterStrategy.Abbreviate, AbstractFilterStrategy.Mask }, phEye.Strategies!.Select(st => st.Strategy));
        });

        // ---- Saving ----

        [Fact]
        public void HandWrittenStrategies_AreKeptOnSave() => WithEditor(CustomPolicy(Abbreviate(), Mask()), editor =>
        {
            Assert.True(Save(editor));

            PhEye saved = Saved(editor).Identifiers.PhEyes![0];
            Assert.Equal(new[] { AbstractFilterStrategy.Abbreviate, AbstractFilterStrategy.Mask }, saved.Strategies!.Select(st => st.Strategy));
            Assert.Equal(_modelDir, saved.PhEyeConfiguration.ModelPath);
        });

        [Fact]
        public void EditedStrategy_IsSavedAndValid() => WithEditor(CustomPolicy(), editor =>
        {
            editor.ShowPhEyeDialog = dlg => SetItems((FilterStrategiesForm)dlg, Abbreviate());
            Select(editor, 0);
            Invoke(editor, "OnPhEyeStrategies");

            Assert.True(Save(editor));

            PolicyEntity entity = Repo(editor).FindByName("default")!;
            Assert.True(PolicyValidator.Validate(entity.Json).IsValid);
            Assert.Equal(AbstractFilterStrategy.Abbreviate, Assert.Single(Saved(editor).Identifiers.PhEyes![0].Strategies!).Strategy);
        });

        // ---- Redaction ----

        [SkippableFact]
        public void CustomModel_RedactsWithItsStrategy()
        {
            Skip.IfNot(PhEyeModel.IsAvailable, ModelSkipReason);
            Assert.Equal("JS called.", RedactWithCustomModel(Abbreviate(), "John Smith called."));
        }

        // The docs quote this marker as the default for custom models.
        [SkippableFact]
        public void CustomModel_DefaultStrategy_WritesTheDocumentedMarker()
        {
            Skip.IfNot(PhEyeModel.IsAvailable, ModelSkipReason);
            Assert.Equal("{{{REDACTED-ph-eye}}} called.", RedactWithCustomModel(new PhEyeFilterStrategy(), "John Smith called."));
        }

        private const string ModelSkipReason = "PhEye model not bundled (run scripts/download-pheye-model.ps1 or set PHEYE_MODEL_DIR).";

        private static string RedactWithCustomModel(PhEyeFilterStrategy strategy, string text)
        {
            // The bundled model added as a custom one: its own path and label, so Prepare leaves it as-is.
            PhileasPolicy policy = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(new PhileasPolicy
            {
                Name = "custom",
                Identifiers = new Identifiers
                {
                    PhEyes = new List<PhEye>
                    {
                        new()
                        {
                            Enabled = true,
                            PhEyeConfiguration = new PhEyeConfiguration
                            {
                                ModelPath = PhEyeModel.ModelDirectory,
                                Labels = new List<string> { "name" },
                                Threshold = PhEyeModel.DefaultThreshold
                            },
                            Strategies = new List<PhEyeFilterStrategy> { strategy }
                        }
                    }
                }
            }));
            Assert.True(PhEyeModel.IsUserLocalModel(policy.Identifiers.PhEyes![0]));
            PhEyeModel.Prepare(policy);
            return new FilterService().Filter(policy, "ctx", 0, text).FilteredText;
        }

        // ---- Helpers ----

        private static PhEyeFilterStrategy Abbreviate() => new() { Strategy = AbstractFilterStrategy.Abbreviate };

        private static PhEyeFilterStrategy Mask() => new() { Strategy = AbstractFilterStrategy.Mask };

        private PhEye Custom(string folder, params PhEyeFilterStrategy[] strategies) => new()
        {
            Enabled = true,
            PhEyeConfiguration = new PhEyeConfiguration { ModelPath = folder, Labels = new List<string> { "person" }, Threshold = 0.5 },
            Strategies = strategies.Length > 0 ? strategies.ToList() : null
        };

        private string CustomPolicy(params PhEyeFilterStrategy[] strategies) => PolicySerializer.SerializeToJson(
            new PhileasPolicy { Name = "default", Identifiers = new Identifiers { PhEyes = new List<PhEye> { Custom(_modelDir, strategies) } } });

        private string TwoModelPolicy() => PolicySerializer.SerializeToJson(new PhileasPolicy
        {
            Name = "default",
            Identifiers = new Identifiers { PhEyes = new List<PhEye> { Custom(_modelDir, Mask()), Custom(_modelDir, Mask()) } }
        });

        private static string BuiltInPolicy() => PolicySerializer.SerializeToJson(
            new PhileasPolicy { Name = "default", Identifiers = new Identifiers { PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() } } });

        private static DialogResult SetItems(FilterStrategiesForm form, params AbstractFilterStrategy[] items)
        {
            List<AbstractFilterStrategy> list = Items(form);
            list.Clear();
            list.AddRange(items);
            return DialogResult.OK;
        }

        private static List<AbstractFilterStrategy> Items(FilterStrategiesForm form) => Field<List<AbstractFilterStrategy>>(form, "_items");

        private static T Field<T>(object owner, string name) =>
            (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        private static ListView List(PolicyEditorForm editor) => Field<ListView>(editor, "_phEyeModelList");

        private static PhileasPolicy Policy(PolicyEditorForm editor) => Field<PhileasPolicy>(editor, "_policy");

        private static PolicyRepository Repo(PolicyEditorForm editor) => Field<PolicyRepository>(editor, "_repo");

        private static PhileasPolicy Saved(PolicyEditorForm editor) =>
            PolicySerializer.DeserializeFromJson(Repo(editor).FindByName("default")!.Json);

        private static bool Dirty(PolicyEditorForm editor) => Field<bool>(editor, "_dirty");

        private static bool Save(PolicyEditorForm editor) =>
            (bool)typeof(PolicyEditorForm).GetMethod("SaveCurrent", Flags)!.Invoke(editor, null)!;

        private static void Invoke(PolicyEditorForm editor, string handler) =>
            typeof(PolicyEditorForm).GetMethod(handler, Flags)!.Invoke(editor, new object?[] { null, EventArgs.Empty });

        private static void Select(PolicyEditorForm editor, int index)
        {
            ListView list = List(editor);
            list.SelectedItems.Clear();
            list.Items[index].Selected = true;
        }

        private void WithEditor(string policyJson, Action<PolicyEditorForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var db = new LiteDatabase(Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".db"));
                    var repo = new PolicyRepository(db);
                    repo.Insert(new PolicyEntity { Name = "default", Json = policyJson });
                    using var editor = new PolicyEditorForm(repo)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(-32000, -32000),
                        ShowInTaskbar = false
                    };
                    editor.Show(); // Load selects and loads the "default" policy
                    Application.DoEvents();
                    Assert.NotNull(Policy(editor));
                    TabControl tabs = Field<TabControl>(editor, "_tabs");
                    tabs.SelectedTab = tabs.TabPages.Cast<TabPage>().Single(p => p.Text == "PhEye");
                    Application.DoEvents();
                    test(editor);
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
