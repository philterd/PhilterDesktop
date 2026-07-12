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
using Phileas.Services;
using Phileas.Policy;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The Modify Redaction "Type" column names the entity. Pattern-based filters (first-name, email, …)
    /// carry their type in FilterType (Classification is empty), so two things must hold: the docx capture
    /// path must populate FilterType, and the form's per-span Clone must preserve it — otherwise the label
    /// falls back to a bare "Detected".
    /// </summary>
    public sealed class SpanTypeLabelIntegrationTests
    {
        [Fact]
        public void DocxCapture_PopulatesFilterType_AndLabelsIt()
        {
            string path = Path.Combine(Path.GetTempPath(), "stype-" + Guid.NewGuid().ToString("N") + ".docx");
            string outPath = Path.ChangeExtension(path, ".out.docx");
            WordDocs.Create(path, "My name is John Smith and I emailed jane@example.com.");

            var policy = PolicyWith("FirstName", "Surname", "EmailAddress");
            var filterService = new FilterService();

            List<RedactionSpanEntity> spans = OfficeSpanMapping.ToEntities(
                Phileas.Services.Office.WordDocumentRedactor.Redact(
                    path, outPath, text => filterService.Filter(policy, "ctx", 0, text), false, true, true, false));

            try
            {
                Assert.NotEmpty(spans);
                // Every captured span has a filter type and no classification, so the label is the humanized type.
                Assert.All(spans, s =>
                {
                    Assert.False(string.IsNullOrEmpty(s.FilterType));
                    Assert.NotEqual("Detected", SpanTypeLabel.For(s));
                });
                Assert.Contains(spans, s => SpanTypeLabel.For(s) == "First Name");
                Assert.Contains(spans, s => SpanTypeLabel.For(s) == "Email Address");
            }
            finally
            {
                foreach (string p in new[] { path, outPath })
                {
                    try { File.Delete(p); } catch { /* best effort */ }
                }
            }
        }

        [Fact]
        public void Clone_PreservesExplanationDetail()
        {
            // The form clones each stored span before display/editing; the clone must keep the fields the
            // Type column and Export Explanation rely on — dropping FilterType made every row read "Detected".
            var source = new RedactionSpanEntity
            {
                Text = "John",
                Replacement = "{{{REDACTED-first-name}}}",
                Classification = string.Empty,
                FilterType = "FirstName",
                Confidence = 0.87,
                Pattern = "given-names",
                Window = new List<string> { "name", "is", "John" },
            };

            MethodInfo clone = typeof(ModifyRedactionForm).GetMethod("Clone", BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Clone not found");
            var copy = (RedactionSpanEntity)clone.Invoke(null, new object[] { source })!;

            Assert.Equal("FirstName", copy.FilterType);
            Assert.Equal(0.87, copy.Confidence);
            Assert.Equal("given-names", copy.Pattern);
            Assert.Equal(source.Window, copy.Window);
            Assert.NotSame(source.Window, copy.Window); // deep copy, not a shared reference
            Assert.Equal("First Name", SpanTypeLabel.For(copy));
        }

        private static PhileasPolicy PolicyWith(params string[] filterNames)
        {
            var identifiers = new Identifiers();
            Dictionary<string, PropertyInfo> props = FilterCatalog.Discover();
            foreach (string name in filterNames)
            {
                if (props.TryGetValue(name, out PropertyInfo? p) && p.CanWrite
                    && p.PropertyType.GetConstructor(Type.EmptyTypes) is not null)
                {
                    p.SetValue(identifiers, Activator.CreateInstance(p.PropertyType));
                }
            }
            return new PhileasPolicy { Name = "stype", Identifiers = identifiers };
        }
    }
}
