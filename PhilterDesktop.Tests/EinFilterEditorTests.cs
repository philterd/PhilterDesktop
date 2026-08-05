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
using PhilterDesktop;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Exercises exposing the EIN (Employer Identification Number) identifier filter in the Policy
    /// Editor: it appears under the Identifiers category labeled "EIN" with an example, its
    /// <c>OnlyValidPrefixes</c> option is surfaced as a toggle (default off), and a policy using it
    /// round-trips and validates against the engine's schema.
    /// </summary>
    public sealed class EinFilterEditorTests
    {
        // --- Category / label / example ------------------------------------------------------------

        [Fact]
        public void Ein_IsGroupedUnderIdentifiers_NotOther()
        {
            FilterCatalog.FilterInfo ein = FilterCatalog.Grouped()
                .SelectMany(g => g)
                .Single(f => f.Property == "Ein");

            Assert.Equal("Identifiers", ein.Category);
            Assert.NotEqual("Other", ein.Category);
        }

        [Fact]
        public void Ein_LabelIsAcronym()
        {
            Assert.Equal("EIN", FilterLabel.Humanize("Ein"));
        }

        [Fact]
        public void Ein_HasAnExample()
        {
            Assert.False(string.IsNullOrWhiteSpace(FilterExamples.For("Ein")));
        }

        // --- Options registry ----------------------------------------------------------------------

        [Fact]
        public void Ein_ExposesOnlyValidPrefixesOption_DefaultOff()
        {
            FilterOption option = Assert.Single(FilterOptions.For("Ein"));
            Assert.Equal("OnlyValidPrefixes", option.Property);
            Assert.False(option.Default); // default off, per the acceptance criteria
            Assert.False(string.IsNullOrWhiteSpace(option.Label));
            Assert.False(string.IsNullOrWhiteSpace(option.Description));
        }

        [Fact]
        public void RegisteredOption_MatchesARealBooleanPropertyOnTheFilter()
        {
            // Guards against the option name drifting from the engine's property (a silent no-op).
            foreach (FilterOption option in FilterOptions.For("Ein"))
            {
                var prop = typeof(Ein).GetProperty(option.Property);
                Assert.NotNull(prop);
                Assert.Equal(typeof(bool), prop!.PropertyType);
                Assert.True(prop.CanWrite);
            }
        }

        [Fact]
        public void FiltersWithoutRegisteredOptions_ReturnEmpty()
        {
            Assert.Empty(FilterOptions.For("Ssn"));
            Assert.Empty(FilterOptions.For("Nonexistent"));
        }

        // --- Engine defaults / round-trip / schema -------------------------------------------------

        [Fact]
        public void NewEinFilter_DefaultsOnlyValidPrefixesOff()
        {
            Assert.False(new Ein().OnlyValidPrefixes);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void PolicyWithEin_RoundTripsPreservingOnlyValidPrefixes(bool onlyValidPrefixes)
        {
            var policy = new PhileasPolicy
            {
                Name = "ein",
                Identifiers = new Identifiers
                {
                    Ein = new Ein
                    {
                        OnlyValidPrefixes = onlyValidPrefixes,
                        Strategies = new List<EinFilterStrategy> { new() }
                    }
                }
            };

            PhileasPolicy loaded = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(policy));

            Assert.NotNull(loaded.Identifiers.Ein);
            Assert.Equal(onlyValidPrefixes, loaded.Identifiers.Ein!.OnlyValidPrefixes);
        }

        [Fact]
        public void PolicyWithEin_ValidatesAgainstPhiSqlSchema()
        {
            var policy = new PhileasPolicy
            {
                Name = "ein",
                Identifiers = new Identifiers
                {
                    Ein = new Ein
                    {
                        Enabled = true,
                        OnlyValidPrefixes = true,
                        Strategies = new List<EinFilterStrategy> { new() }
                    }
                }
            };

            PolicyValidationResult result = PolicyValidator.Validate(PolicySerializer.SerializeToJson(policy));
            Assert.True(result.IsValid, "EIN policy failed schema validation: " + string.Join(" | ", result.Errors));
        }

        [Fact]
        public void EinFilter_EnableThenDisable_RoundTrips()
        {
            // Mirrors the editor's add (set) then remove (null) of a filter.
            var identifiers = new Identifiers { Ein = new Ein() };
            PhileasPolicy added = PolicySerializer.DeserializeFromJson(
                PolicySerializer.SerializeToJson(new PhileasPolicy { Name = "x", Identifiers = identifiers }));
            Assert.NotNull(added.Identifiers.Ein);

            added.Identifiers.Ein = null;
            PhileasPolicy removed = PolicySerializer.DeserializeFromJson(PolicySerializer.SerializeToJson(added));
            Assert.Null(removed.Identifiers.Ein);
        }

        // --- Configure dialog: the toggle writes back on OK ----------------------------------------

        [Fact]
        public void StrategiesDialog_WritesOptionToggleBackOnOk()
        {
            Sta(() =>
            {
                var toggle = new FilterOptionToggle
                {
                    Label = "Only match valid IRS prefixes",
                    Description = "desc",
                    Value = false
                };
                using var form = new FilterStrategiesForm(
                    "EIN", Array.Empty<object>(), typeof(EinFilterStrategy), new[] { toggle });
                _ = form.Handle;

                CheckBox box = FindCheckBox(form) ?? throw new Xunit.Sdk.XunitException("no option checkbox rendered");
                Assert.False(box.Checked); // seeded from toggle.Value

                box.Checked = true;
                form.ApplyOptions(); // what the OK button does; copies checkboxes into the toggles

                Assert.True(toggle.Value);
            });
        }

        [Fact]
        public void StrategiesDialog_WithoutOptions_RendersNoOptionCheckbox()
        {
            Sta(() =>
            {
                using var form = new FilterStrategiesForm("SSN", Array.Empty<object>(), typeof(SsnFilterStrategy));
                _ = form.Handle;
                Assert.Null(FindCheckBox(form));
            });
        }

        private static CheckBox? FindCheckBox(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is CheckBox cb)
                {
                    return cb;
                }
                CheckBox? nested = FindCheckBox(child);
                if (nested is not null)
                {
                    return nested;
                }
            }
            return null;
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
