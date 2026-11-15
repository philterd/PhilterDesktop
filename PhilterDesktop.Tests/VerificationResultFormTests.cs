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
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The leftover check result window: a headline per result, the caveat shown in full, a findings list
    /// only when something was found, and nothing else (no product links). Layout at each display scaling
    /// is covered by <see cref="DpiLayoutTests"/>.
    /// </summary>
    public sealed class VerificationResultFormTests
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly RedactionSpanEntity[] Findings =
        {
            new() { FilterType = "ssn", Text = "123-45-6789", CharacterStart = 1, CharacterEnd = 12 },
            new() { FilterType = "email-address", Text = "jane.doe@example.com", CharacterStart = 20, CharacterEnd = 40 }
        };

        // ---- Each result ----

        [Fact]
        public void Clean_IsCompact_WithAGreenHeadline() => Show(new VerificationOutcome { Status = VerificationStatus.Clean }, form =>
        {
            Assert.Equal("No leftover PII found", Headline(form).Text);
            Assert.Equal(Color.FromArgb(0x1B, 0x5E, 0x20), Headline(form).ForeColor);
            Assert.Equal("No leftover PII was detected in a.docx.", Summary(form).Text);
            Assert.False(List(form).Visible);
            Assert.False(Field<Label>(form, "_note").Visible);
            Assert.Equal(FormBorderStyle.FixedDialog, form.FormBorderStyle);
        });

        [Fact]
        public void CleanWithCaveat_ShowsTheWholeCaveat() => Show(new VerificationOutcome
        {
            Status = VerificationStatus.Clean,
            FidelityNote = "Headers and footers weren't re-scanned. Open the output and check them before sharing it."
        }, form =>
        {
            Label note = Field<Label>(form, "_note");

            Assert.Equal("Review needed", Headline(form).Text);
            Assert.True(note.Visible);
            Assert.Equal("Headers and footers weren't re-scanned. Open the output and check them before sharing it.", note.Text);
            // It used to be cut off below the first line; the label must be as tall as its text.
            Assert.True(note.Height >= note.GetPreferredSize(new Size(note.Width, 0)).Height);
        });

        [Fact]
        public void Found_ListsEachItem_AndCanBeResized() => Show(new VerificationOutcome
        {
            Status = VerificationStatus.ResidualsFound,
            Residuals = Findings
        }, form =>
        {
            ListView list = List(form);

            Assert.Equal("2 possible items of leftover PII", Headline(form).Text);
            Assert.StartsWith("These may still be present in a.docx.", Summary(form).Text);
            Assert.True(list.Visible);
            Assert.Equal(new[] { "SSN", "Email Address" }, list.Items.Cast<ListViewItem>().Select(i => i.Text));
            Assert.Equal("123-45-6789", list.Items[0].SubItems[1].Text);
            Assert.Equal(FormBorderStyle.Sizable, form.FormBorderStyle);
        });

        [Fact]
        public void Found_One_IsSingular() => Show(new VerificationOutcome
        {
            Status = VerificationStatus.ResidualsFound,
            Residuals = Findings.Take(1).ToArray()
        }, form =>
        {
            Assert.Equal("1 possible item of leftover PII", Headline(form).Text);
            Assert.StartsWith("This may still be present", Summary(form).Text);
        });

        [Fact]
        public void Found_TextColumn_FillsTheList() => Show(new VerificationOutcome
        {
            Status = VerificationStatus.ResidualsFound,
            Residuals = Findings
        }, form =>
        {
            ListView list = List(form);
            int columns = list.Columns.Cast<ColumnHeader>().Sum(c => c.Width);

            // No empty strip down the right of the list.
            Assert.InRange(list.ClientSize.Width - columns, 0, 2);
        });

        [Fact]
        public void Error_SaysWhyOnce() => Show(new VerificationOutcome { Status = VerificationStatus.Error, Error = "The redacted output file was not found." }, form =>
        {
            Assert.Equal("The check could not be completed", Headline(form).Text);
            Assert.Equal("a.docx wasn't checked. The redacted output file was not found.", Summary(form).Text);
            Assert.False(List(form).Visible);
        });

        // ---- Every result ----

        public static TheoryData<string> Results => new() { "clean", "found", "error" };

        [Theory]
        [MemberData(nameof(Results))]
        public void HasNoPhilterScopeLink(string result) => Show(Outcome(result), form =>
        {
            Assert.Empty(All<LinkLabel>(form));
            Assert.DoesNotContain(All<Control>(form), c => c.Text.Contains("Philter Scope", StringComparison.OrdinalIgnoreCase));
        });

        [Theory]
        [MemberData(nameof(Results))]
        public void IsADialog(string result) => Show(Outcome(result), form =>
        {
            Assert.Equal("Leftover Check", form.Text);
            Assert.False(form.MaximizeBox);
            Assert.False(form.MinimizeBox);
            Assert.Same(form.AcceptButton, form.CancelButton);
            Assert.Contains("not a guarantee", Field<Label>(form, "_disclaimer").Text);
        });

        [Theory]
        [MemberData(nameof(Results))]
        public void TextHasNoEmDashes(string result) => Show(Outcome(result), form =>
            Assert.DoesNotContain(All<Control>(form), c => c.Text.Contains('—')));

        [Fact]
        public void CleanAndError_AreTheSameWidth()
        {
            int clean = 0, error = 0;
            Show(Outcome("clean"), form => clean = form.Width);
            Show(Outcome("error"), form => error = form.Width);

            Assert.Equal(clean, error);
        }

        // ---- Finding type names ----

        // Spans store the engine's enum name; it must read as words, the way the Policy Editor names filters.
        [Theory]
        [InlineData("LocationCity", "City")]
        [InlineData("LocationState", "State")]
        [InlineData("LocationCounty", "County")]
        [InlineData("StateAbbreviation", "State Abbreviation")]
        [InlineData("EmailAddress", "Email Address")]
        [InlineData("IpAddress", "IP Address")]
        [InlineData("IbanCode", "IBAN Code")]
        [InlineData("Ssn", "SSN")]
        [InlineData("CanadaSin", "Canada SIN")]
        [InlineData("DriversLicenseNumber", "Drivers License Number")]
        [InlineData("PhoneNumberExtension", "Phone Number Extension")]
        public void FindingTypes_StoredAsEnumNames_ReadAsWords(string filterType, string shown) =>
            Assert.Equal(shown, RedactionReport.FriendlyType(new RedactionSpanEntity { FilterType = filterType }));

        // Older or hand-made records may hold other spellings.
        [Theory]
        [InlineData("ssn", "SSN")]
        [InlineData("ip-address", "IP Address")]
        [InlineData("email-address", "Email Address")]
        [InlineData("PHONE_NUMBER", "Phone Number")]
        [InlineData("SomethingNew", "Something New")]
        public void FindingTypes_OtherSpellings_ReadAsWords(string filterType, string shown) =>
            Assert.Equal(shown, RedactionReport.FriendlyType(new RedactionSpanEntity { FilterType = filterType }));

        // A generic type names the detector, not what it found; the classification says what.
        [Theory]
        [InlineData("Other", "name", "Name")]                 // on-device name detection
        [InlineData("Identifier", "case-number", "Case Number")] // a custom identifier's own label
        [InlineData("PhEye", "organization", "Organization")]
        [InlineData("Person", "person", "Person")]
        [InlineData("CustomDictionary", "client-names", "Client Names")]
        [InlineData("Other", "", "Other")]
        [InlineData("", "", "Other")]
        [InlineData("", "account-id", "Account Id")]
        public void GenericTypes_ShowTheirClassification(string filterType, string classification, string shown) =>
            Assert.Equal(shown, RedactionReport.FriendlyType(new RedactionSpanEntity { FilterType = filterType, Classification = classification }));

        [Fact]
        public void SpecificTypes_IgnoreTheClassification() =>
            Assert.Equal("City", RedactionReport.FriendlyType(new RedactionSpanEntity { FilterType = "LocationCity", Classification = "anything" }));

        [Fact]
        public void UserAdded_IsGroupedOnItsOwn() =>
            Assert.Equal("User-added", RedactionReport.FriendlyType(new RedactionSpanEntity { UserAdded = true, FilterType = "LocationCity" }));

        [Fact]
        public void FindingTypes_FromRealDetections_ReadAsWords()
        {
            // Through the engine and the desktop's own recording, as a real redaction stores them.
            var policy = new Phileas.Policy.Policy
            {
                Name = "p",
                Identifiers = new Phileas.Policy.Identifiers
                {
                    City = new Phileas.Policy.Filters.City(),
                    State = new Phileas.Policy.Filters.State(),
                    StateAbbreviation = new Phileas.Policy.Filters.StateAbbreviation(),
                    CustomIdentifiers = new List<Phileas.Policy.Filters.Identifier> { new() { Classification = "case-number", Pattern = @"CASE-\d+" } }
                }
            };
            var result = new Phileas.Services.FilterService().Filter(policy, "c", 0, "Lives in Boston, Massachusetts, MA. See CASE-123.");

            var shown = result.Spans.ToDictionary(sp => sp.Text, sp =>
            {
                var entity = new RedactionSpanEntity { Classification = sp.Classification ?? string.Empty };
                SpanExplanation.Populate(entity, sp);
                return RedactionReport.FriendlyType(entity);
            });

            Assert.Equal("City", shown["Boston"]);
            Assert.Equal("State", shown["Massachusetts"]);
            Assert.Equal("State Abbreviation", shown["MA"]);
            Assert.Equal("Case Number", shown["CASE-123"]);
        }

        // ---- Helpers ----

        private static VerificationOutcome Outcome(string result) => result switch
        {
            "clean" => new VerificationOutcome { Status = VerificationStatus.Clean },
            "found" => new VerificationOutcome { Status = VerificationStatus.ResidualsFound, Residuals = Findings },
            _ => new VerificationOutcome { Status = VerificationStatus.Error, Error = "boom" }
        };

        private static Label Headline(Form form) => Field<Label>(form, "_headline");

        private static Label Summary(Form form) => Field<Label>(form, "_summary");

        private static ListView List(Form form) => Field<ListView>(form, "_list");

        private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

        private static IEnumerable<T> All<T>(Control parent) where T : Control
        {
            foreach (Control child in parent.Controls)
            {
                if (child is T match)
                {
                    yield return match;
                }
                foreach (T nested in All<T>(child))
                {
                    yield return nested;
                }
            }
        }

        private static void Show(VerificationOutcome outcome, Action<VerificationResultForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var form = new VerificationResultForm("a.docx", outcome)
                    {
                        StartPosition = FormStartPosition.Manual,
                        Location = new Point(-32000, -32000)
                    };
                    form.Show(); // lay out at the real size
                    Application.DoEvents();
                    test(form);
                    form.Close();
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
