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
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterDesktop.PolicyEditing;
using PhoneNumberUtil = PhoneNumbers.PhoneNumberUtil;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The phone number filter's leniency, shown as a drop-down in its Configure dialog: the default
    /// (POSSIBLE) is saved by leaving the property out, the other values are saved and kept, and the
    /// engine honors whichever is set.
    /// </summary>
    public sealed class PhoneNumberLeniencyEditorTests
    {
        private static readonly string[] Values = ["POSSIBLE", "VALID", "STRICT_GROUPING", "EXACT_GROUPING"];

        private static FilterOption Leniency => Assert.Single(FilterOptions.For("PhoneNumber"));

        // ---- Registry ----

        [Fact]
        public void PhoneNumber_ExposesLeniencyAsADropDown()
        {
            FilterOption option = Leniency;

            Assert.Equal("Leniency", option.Property);
            Assert.True(option.IsChoice);
            Assert.False(string.IsNullOrWhiteSpace(option.Label));
            Assert.Contains("own documents", option.Description);
        }

        [Fact]
        public void Choices_AreTheFourEngineValues_DefaultFirst() =>
            Assert.Equal(Values, Leniency.Choices!.Select(c => c.Value));

        [Fact]
        public void Choices_MatchLibphonenumbersNamesExactly()
        {
            // Values are case-sensitive; a drifted name would make the engine reject the policy.
            Assert.Equal(Enum.GetNames<PhoneNumberUtil.Leniency>().Order(), Values.Order());
        }

        [Fact]
        public void Choices_EachHaveALabelAndDescription()
        {
            Assert.All(Leniency.Choices!, c =>
            {
                Assert.False(string.IsNullOrWhiteSpace(c.Label));
                Assert.False(string.IsNullOrWhiteSpace(c.Description));
            });
            Assert.Equal(Leniency.Choices!.Count, Leniency.Choices!.Select(c => c.Label).Distinct().Count());
            Assert.Contains("default", Leniency.Choices![0].Label, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Option_IsARealStringPropertyOnTheFilter()
        {
            var prop = typeof(PhoneNumber).GetProperty(Leniency.Property);

            Assert.NotNull(prop);
            Assert.Equal(typeof(string), prop!.PropertyType);
            Assert.True(prop.CanWrite);
        }

        [Fact]
        public void OptionText_HasNoEmDashes()
        {
            Assert.DoesNotContain('—', Leniency.Description + Leniency.Label);
            Assert.All(Leniency.Choices!, c => Assert.DoesNotContain('—', c.Label + c.Description));
        }

        // ---- Read ----

        [Fact]
        public void Read_UnsetLeniency_ShowsTheDefault()
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber())!;

            Assert.Equal("POSSIBLE", toggle.Choice);
            Assert.Equal(Values, toggle.Choices!.Select(c => c.Value));
        }

        [Theory]
        [InlineData("POSSIBLE")]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void Read_ShowsTheValueFromThePolicy(string value) =>
            Assert.Equal(value, FilterOptions.Read(Leniency, new PhoneNumber { Leniency = value })!.Choice);

        [Fact]
        public void Read_UnknownValue_IsShownAsIs_SoSavingKeepsIt()
        {
            var filter = new PhoneNumber { Leniency = "valid" };
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;

            Assert.Equal("valid", toggle.Choice);
            Assert.Equal(5, toggle.Choices!.Count);
            Assert.Equal("valid", toggle.Choices![^1].Value);

            FilterOptions.Write(Leniency, filter, toggle);
            Assert.Equal("valid", filter.Leniency);
        }

        [Fact]
        public void Read_DoesNotChangeTheRegisteredChoices()
        {
            _ = FilterOptions.Read(Leniency, new PhoneNumber { Leniency = "odd" });
            Assert.Equal(4, Leniency.Choices!.Count);
        }

        [Fact]
        public void Read_SkipsAChoiceOptionWhosePropertyIsntAString()
        {
            var option = new FilterOption("OnlyValidPrefixes", "x", "y", false) { Choices = Leniency.Choices };
            Assert.Null(FilterOptions.Read(option, new Ein()));
        }

        [Fact]
        public void Read_StillReadsBooleanOptions()
        {
            FilterOption ein = Assert.Single(FilterOptions.For("Ein"));

            Assert.True(FilterOptions.Read(ein, new Ein { OnlyValidPrefixes = true })!.Value);
            Assert.Null(FilterOptions.Read(ein, new Ein())!.Choices);
        }

        // ---- Write ----

        [Fact]
        public void Write_Default_LeavesThePropertyOut()
        {
            var filter = new PhoneNumber { Leniency = "VALID" };
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;
            toggle.Choice = "POSSIBLE";

            FilterOptions.Write(Leniency, filter, toggle);

            Assert.Null(filter.Leniency);
        }

        [Theory]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void Write_NonDefault_SavesTheValue(string value)
        {
            var filter = new PhoneNumber();
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;
            toggle.Choice = value;

            FilterOptions.Write(Leniency, filter, toggle);

            Assert.Equal(value, filter.Leniency);
        }

        [Theory]
        [InlineData(null, "POSSIBLE", null)]
        [InlineData("POSSIBLE", "POSSIBLE", "POSSIBLE")]
        [InlineData("POSSIBLE", "VALID", "VALID")]
        [InlineData("VALID", "VALID", "VALID")]
        [InlineData("VALID", "POSSIBLE", null)]
        [InlineData("EXACT_GROUPING", "STRICT_GROUPING", "STRICT_GROUPING")]
        [InlineData(null, "EXACT_GROUPING", "EXACT_GROUPING")]
        public void Write_KeepsAnUnchangedValue_AndDropsAPickedDefault(string? original, string chosen, string? saved)
        {
            var filter = new PhoneNumber { Leniency = original };
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;
            toggle.Choice = chosen;

            FilterOptions.Write(Leniency, filter, toggle);

            Assert.Equal(saved, filter.Leniency);
        }

        [Fact]
        public void Write_ChangedAndChangedBack_CountsAsUnchanged()
        {
            var filter = new PhoneNumber { Leniency = "POSSIBLE" };
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;
            toggle.Choice = "VALID";
            toggle.Choice = "POSSIBLE";

            FilterOptions.Write(Leniency, filter, toggle);

            Assert.Equal("POSSIBLE", filter.Leniency);
        }

        [Fact]
        public void Write_StillWritesBooleanOptions()
        {
            FilterOption ein = Assert.Single(FilterOptions.For("Ein"));
            var filter = new Ein();
            FilterOptionToggle toggle = FilterOptions.Read(ein, filter)!;
            toggle.Value = true;

            FilterOptions.Write(ein, filter, toggle);

            Assert.True(filter.OnlyValidPrefixes);
        }

        // ---- Saved policy ----

        [Fact]
        public void PolicySavedWithTheDefault_HasNoLeniencyProperty()
        {
            PhileasPolicy policy = PhonePolicy(null);
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, policy.Identifiers.PhoneNumber!)!;
            FilterOptions.Write(Leniency, policy.Identifiers.PhoneNumber!, toggle);

            string json = PolicySerializer.SerializeToJson(policy);

            Assert.DoesNotContain("leniency", json, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData("POSSIBLE")]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void ExistingValue_IsShownAndKeptThroughAnUnchangedSave(string value)
        {
            // Open a saved policy, open Configure, press OK without changing anything, save.
            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(PhonePolicy(value)));
            PhoneNumber filter = loaded.Identifiers.PhoneNumber!;
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, filter)!;
            Assert.Equal(value, toggle.Choice);

            FilterOptions.Write(Leniency, filter, toggle);
            PhileasPolicy saved = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(loaded));

            Assert.Equal(value, saved.Identifiers.PhoneNumber!.Leniency);
            Assert.Equal(value, FilterOptions.Read(Leniency, saved.Identifiers.PhoneNumber!)!.Choice);
        }

        [Theory]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void SavedNonDefault_IsWrittenAsTheExactName(string value) =>
            Assert.Contains($"\"leniency\":\"{value}\"", PolicySerializer.SerializeToJson(PhonePolicy(value)).Replace(" ", ""));

        [Theory]
        [InlineData(null)]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void PolicyWithEachLeniency_ValidatesAgainstTheSchema(string? value)
        {
            PolicyValidationResult result = PolicyValidator.Validate(PolicySerializer.SerializeToJson(PhonePolicy(value)));
            Assert.True(result.IsValid, string.Join(" | ", result.Errors));
        }

        // ---- Redaction ----

        [Fact]
        public void Possible_RedactsAYearThatLooksLikeAPhoneNumber()
        {
            Assert.DoesNotContain("2026", Redact(null, "The meeting is on Oct 9, 2026 at noon."));
        }

        [Theory]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void StricterLevels_LeaveTheYearAlone(string value)
        {
            Assert.Contains("Oct 9, 2026", Redact(value, "The meeting is on Oct 9, 2026 at noon."));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void EveryLevel_StillRedactsAFormattedRealNumber(string? value)
        {
            Assert.DoesNotContain("555-0182", Redact(value, "Call (202) 555-0182 today."));
        }

        // The dialog and docs say stricter levels skip local numbers with no area code; keep that true.
        [Theory]
        [InlineData("VALID")]
        [InlineData("STRICT_GROUPING")]
        [InlineData("EXACT_GROUPING")]
        public void StricterLevels_SkipALocalNumberWithNoAreaCode(string value)
        {
            const string text = "Call 555-1234 today.";
            Assert.DoesNotContain("555-1234", Redact(null, text, regions: null));
            Assert.Contains("555-1234", Redact(value, text, regions: null));
            Assert.Contains("555-1234", Redact(value, text));
        }

        // The docs say a year is only mistaken for a number when several regions are checked.
        [Fact]
        public void WithTheDefaultRegion_TheYearIsNotRedactedEvenUnderPossible() =>
            Assert.Contains("Oct 9, 2026", Redact(null, "The meeting is on Oct 9, 2026 at noon.", regions: null));

        [Fact]
        public void BuiltInPolicies_CheckTheDefaultRegionOnly()
        {
            foreach (PolicyTemplate template in PolicyTemplates.All)
            {
                PhoneNumber? phone = PolicySerializer.DeserializeFromJson(template.BuildJson()).Identifiers.PhoneNumber;
                Assert.True(phone?.Region is null or { Count: 0 }, template.Id + " sets phone regions; revisit the leniency docs.");
                Assert.Null(phone?.Leniency);
            }
        }

        [Fact]
        public void Text_WarnsAboutLocalNumbers()
        {
            Assert.Contains("555-1234", Leniency.Description);
            Assert.Contains("555-1234", Leniency.Choices![1].Description);
        }

        [Fact]
        public void StrictGrouping_SkipsANumberGroupedTheWrongWay()
        {
            const string text = "Call 202 55 50182 today.";
            Assert.DoesNotContain("50182", Redact("VALID", text));
            Assert.Contains("202 55 50182", Redact("STRICT_GROUPING", text));
        }

        [Fact]
        public void ExactGrouping_SkipsAPartlyGroupedNumber()
        {
            const string text = "Call 202-5550182 today.";
            Assert.DoesNotContain("5550182", Redact("STRICT_GROUPING", text));
            Assert.Contains("202-5550182", Redact("EXACT_GROUPING", text));
        }

        // ---- Dialog ----

        [Theory]
        [InlineData(null, "Possible (default)")]
        [InlineData("VALID", "Valid")]
        [InlineData("EXACT_GROUPING", "Exact grouping")]
        public void Dialog_ShowsTheCurrentValue(string? value, string label) => Sta(() =>
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber { Leniency = value })!;
            using var form = PhoneDialog(toggle);

            ComboBox combo = Find<ComboBox>(form).Single();
            Assert.Equal(label, combo.SelectedItem);
            Assert.Equal(ComboBoxStyle.DropDownList, combo.DropDownStyle);
            Assert.Empty(Find<CheckBox>(form));
        });

        [Fact]
        public void Dialog_WritesTheSelectionBackOnOk() => Sta(() =>
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber())!;
            using var form = PhoneDialog(toggle);

            Find<ComboBox>(form).Single().SelectedIndex = 1;
            form.ApplyOptions();

            Assert.Equal("VALID", toggle.Choice);
        });

        [Fact]
        public void Dialog_LeavesTheValueAloneUntilOk() => Sta(() =>
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber())!;
            using var form = PhoneDialog(toggle);

            Find<ComboBox>(form).Single().SelectedIndex = 2;

            Assert.Equal("POSSIBLE", toggle.Choice);
        });

        [Fact]
        public void Dialog_DescribesTheSelectedValue_AndTheTradeOff() => Sta(() =>
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber())!;
            using var form = PhoneDialog(toggle);
            ComboBox combo = Find<ComboBox>(form).Single();
            string Texts() => string.Join("\n", Find<Label>(form).Select(l => l.Text));

            Assert.Contains(Leniency.Choices![0].Description, Texts());
            Assert.Contains(Leniency.Description, Texts());

            combo.SelectedIndex = 3;
            Assert.Contains(Leniency.Choices![3].Description, Texts());
            Assert.DoesNotContain(Leniency.Choices![0].Description, Texts());
        });

        [Fact]
        public void Dialog_UnknownValue_IsSelectedAndKept() => Sta(() =>
        {
            FilterOptionToggle toggle = FilterOptions.Read(Leniency, new PhoneNumber { Leniency = "valid" })!;
            using var form = PhoneDialog(toggle);

            Assert.Equal("valid", Find<ComboBox>(form).Single().SelectedItem);
            form.ApplyOptions();
            Assert.Equal("valid", toggle.Choice);
        });

        [Fact]
        public void Dialog_SelectedValueLine_FitsEveryDescription_SoItDoesntJump() => Sta(() =>
        {
            using var form = PhoneDialog(FilterOptions.Read(Leniency, new PhoneNumber())!);
            ComboBox combo = Find<ComboBox>(form).Single();
            Label line = Find<Label>(form).Single(l => l.Text == Leniency.Choices![0].Description);
            int reserved = line.MinimumSize.Height;

            Assert.True(reserved > 0);
            for (int i = 0; i < combo.Items.Count; i++)
            {
                combo.SelectedIndex = i;
                Assert.True(line.GetPreferredSize(new Size(line.MaximumSize.Width, 0)).Height <= reserved,
                    Leniency.Choices![i].Value + " needs more height than reserved");
            }
        });

        [Fact]
        public void Dialog_IsTallerForADropDownThanForACheckbox() => Sta(() =>
        {
            using var phone = PhoneDialog(FilterOptions.Read(Leniency, new PhoneNumber())!);
            using var ein = new FilterStrategiesForm("EIN", Array.Empty<object>(), typeof(EinFilterStrategy),
                new[] { FilterOptions.Read(Assert.Single(FilterOptions.For("Ein")), new Ein())! });

            Assert.True(phone.MinimumSize.Height > ein.MinimumSize.Height);
        });

        // ---- Helpers ----

        private static readonly List<string> SeveralRegions = ["US", "GB", "DE"];

        private static PhileasPolicy PhonePolicy(string? leniency, List<string>? regions = null) => new()
        {
            Name = "phone",
            Identifiers = new Identifiers
            {
                PhoneNumber = new PhoneNumber
                {
                    Region = regions,
                    Leniency = leniency,
                    Strategies = new List<PhoneNumberFilterStrategy> { new() }
                }
            }
        };

        // Several regions unless told otherwise, as in the issue's examples.
        private static string Redact(string? leniency, string text) => Redact(leniency, text, SeveralRegions);

        private static string Redact(string? leniency, string text, List<string>? regions) =>
            new FilterService().Filter(PhonePolicy(leniency, regions), "ctx", 0, text).FilteredText;

        private static FilterStrategiesForm PhoneDialog(FilterOptionToggle toggle)
        {
            var form = new FilterStrategiesForm("Phone Number", Array.Empty<object>(), typeof(PhoneNumberFilterStrategy), new[] { toggle });
            _ = form.Handle;
            return form;
        }

        private static IEnumerable<T> Find<T>(Control parent) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                if (child is T match)
                {
                    yield return match;
                }
                foreach (T nested in Find<T>(child))
                {
                    yield return nested;
                }
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
