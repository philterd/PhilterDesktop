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
using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterDesktop.PolicyEditing;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The Policy Editor must not offer Phileas's deprecated <c>Identifiers</c> aliases (such as
    /// <c>Person</c>, which accepts a value but never reads it back) as filters.
    /// </summary>
    public sealed class FilterAliasTests
    {
        // Stand-ins for the shapes a property on Identifiers can take.
        private sealed class Shapes
        {
            private Ssn? _folded;
            public Ssn? Real { get; set; }
            public Ssn? WriteOnlyAlias { get => null; set => _folded = value; }
            public Ssn? ReadOnly => _folded;
            public List<CustomDictionary>? RealList { get; set; }
            public List<CustomDictionary>? ListAlias { get => null; set { } }
            public NoDefaultConstructor? Uncreatable { get; set; }
        }

        private sealed class NoDefaultConstructor
        {
            public NoDefaultConstructor(int unused) { }
        }

        private static PropertyInfo Prop(string name) => typeof(Shapes).GetProperty(name)!;

        [Theory]
        [InlineData("Real", false)]
        [InlineData("RealList", false)]
        [InlineData("WriteOnlyAlias", true)]
        [InlineData("ListAlias", true)]
        [InlineData("ReadOnly", true)]
        [InlineData("Uncreatable", true)]
        public void IsDeprecatedAlias_RecognizesPropertiesThatDontHoldAValue(string property, bool expected) =>
            Assert.Equal(expected, FilterCatalog.IsDeprecatedAlias(Prop(property)));

        // The invariant behind the check, on whatever Phileas version is referenced: everything the
        // Policy Editor offers keeps a value once set (an alias would show a box that never stays ticked).
        [Fact]
        public void EveryOfferedFilter_ReadsBackWhatWasSet()
        {
            foreach (PropertyInfo property in FilterCatalog.Discover().Values)
            {
                var identifiers = new Identifiers();
                object filter = Activator.CreateInstance(property.PropertyType)!;
                property.SetValue(identifiers, filter);
                Assert.Same(filter, property.GetValue(identifiers));
            }
        }

        [Fact]
        public void Discover_ExcludesExactlyTheAliases()
        {
            List<PropertyInfo> all = typeof(Identifiers)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => typeof(AbstractPolicyFilter).IsAssignableFrom(p.PropertyType))
                .ToList();

            Dictionary<string, PropertyInfo> offered = FilterCatalog.Discover();

            Assert.Equal(all.Where(p => !FilterCatalog.IsDeprecatedAlias(p)).Select(p => p.Name).OrderBy(n => n),
                offered.Keys.OrderBy(n => n));
            Assert.True(offered.Count >= 15, "the real filters must still be offered");
            Assert.DoesNotContain("Person", offered.Keys); // Phileas's deprecated PhEye alias, where it exists
        }

        [Fact]
        public void Grouped_NeverPlacesAnAliasInACategory()
        {
            var aliases = typeof(Identifiers).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(FilterCatalog.IsDeprecatedAlias)
                .Select(p => p.Name)
                .ToHashSet();

            Assert.DoesNotContain(FilterCatalog.Grouped().SelectMany(g => g), f => aliases.Contains(f.Property));
        }
    }
}
