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

using System.Text.RegularExpressions;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Policy.Filters.Strategies;
using Phileas.Services;
using PhilterDesktop.PolicyEditing;
using Xunit;
using static PhilterDesktop.Tests.DpiLayoutTests;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The filter-strategy dialog offers every strategy Phileas supports for a filter, edits each one's
    /// settings, and saves policies that validate and redact as described.
    /// </summary>
    public sealed class FilterStrategyEditingTests
    {
        private static readonly Version Phileas16 = new(1, 6, 0);
        private static readonly Version Phileas17 = new(1, 7, 0);

        private static IEnumerable<string> Names(IEnumerable<StrategyInfo> strategies) => strategies.Select(s => s.Name);

        // --- StrategyCatalog -----------------------------------------------------------------

        [Fact]
        public void Catalog_OffersTheCommonStrategies_ForANonDateFilter()
        {
            var offered = Names(StrategyCatalog.For(typeof(SsnFilterStrategy), Phileas16)).ToList();
            Assert.Equal(new[]
            {
                StrategyCatalog.Redact, StrategyCatalog.StaticReplace, StrategyCatalog.RandomReplace, StrategyCatalog.Mask,
                StrategyCatalog.Last4, StrategyCatalog.Truncate, StrategyCatalog.Abbreviate, StrategyCatalog.HashSha256,
                StrategyCatalog.Crypto, StrategyCatalog.Fpe
            }, offered);
        }

        [Fact]
        public void Catalog_AddsLookupTable_FromPhileas17()
        {
            Assert.DoesNotContain(StrategyCatalog.MapReplace, Names(StrategyCatalog.For(typeof(CreditCardFilterStrategy), Phileas16)));
            Assert.Contains(StrategyCatalog.MapReplace, Names(StrategyCatalog.For(typeof(CreditCardFilterStrategy), Phileas17)));
        }

        [Fact]
        public void Catalog_DateStrategies_OnlyForDates_AndOnlyFromPhileas17()
        {
            string[] dateOnly = { StrategyCatalog.Shift, StrategyCatalog.Relative, StrategyCatalog.TruncateToYear };

            List<string> date17 = Names(StrategyCatalog.For(typeof(DateFilterStrategy), Phileas17)).ToList();
            Assert.All(dateOnly, s => Assert.Contains(s, date17));
            Assert.DoesNotContain(StrategyCatalog.Abbreviate, date17);  // the schema leaves these out for dates
            Assert.DoesNotContain(StrategyCatalog.MapReplace, date17);

            Assert.All(dateOnly, s => Assert.DoesNotContain(s, Names(StrategyCatalog.For(typeof(DateFilterStrategy), Phileas16))));
            Assert.All(dateOnly, s => Assert.DoesNotContain(s, Names(StrategyCatalog.For(typeof(SsnFilterStrategy), Phileas17))));
        }

        [Fact]
        public void Catalog_NeverOffersAStrategyTheSchemaRejects()
        {
            foreach (Type type in new[] { typeof(SsnFilterStrategy), typeof(DateFilterStrategy), typeof(PhEyeFilterStrategy) })
            {
                ISet<string> allowed = StrategyCatalog.AllowedBySchema(type);
                Assert.All(StrategyCatalog.For(type, Phileas17), s => Assert.Contains(s.Name, allowed));
            }
            Assert.DoesNotContain("SAME", StrategyCatalog.AllowedBySchema(typeof(SsnFilterStrategy)));
        }

        [Fact]
        public void Catalog_EveryStrategyIsAllowedSomewhere()
        {
            var anywhere = StrategyCatalog.AllowedBySchema(typeof(SsnFilterStrategy))
                .Union(StrategyCatalog.AllowedBySchema(typeof(DateFilterStrategy))).ToHashSet();
            Assert.All(StrategyCatalog.All, s => Assert.Contains(s.Name, anywhere));
        }

        [Fact]
        public void Catalog_FallbacksExcludeTheLookupTableItself() =>
            Assert.DoesNotContain(StrategyCatalog.MapReplace, Names(StrategyCatalog.Fallbacks(Phileas17)));

        [Fact]
        public void Catalog_TextHasNoEmDashes() =>
            Assert.All(StrategyCatalog.All, s => Assert.DoesNotContain("—", s.Label + s.Description));

        [Fact]
        public void Catalog_FindIsCaseInsensitive()
        {
            Assert.Equal(StrategyCatalog.Last4, StrategyCatalog.Find("last_4")!.Name);
            Assert.Null(StrategyCatalog.Find("NOT_A_STRATEGY"));
        }

        // --- StrategySettings: round trip through a real redaction ---------------------------

        private const string Card = "Card 4111-1111-1111-1111 on file.";

        private static (string Output, PhileasPolicy Policy) RedactCard(StrategySettings settings)
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };
            var strategy = new CreditCardFilterStrategy();
            settings.ApplyTo(strategy, policy);
            policy.Identifiers.CreditCard = new CreditCard { Strategies = new List<CreditCardFilterStrategy> { strategy } };
            return (Run(policy, Card), policy);
        }

        // Saves and reloads the policy the way the editor does, checks it against the schema, then redacts.
        private static string Run(PhileasPolicy policy, string text)
        {
            string json = PolicySerializer.SerializeToJson(policy);
            PolicyValidationResult valid = PolicyValidator.Validate(json);
            Assert.True(valid.IsValid, string.Join(" | ", valid.Errors));
            return new FilterService().Filter(PolicySerializer.DeserializeFromJson(json), "ctx", 0, text).FilteredText;
        }

        [Theory]
        [InlineData(StrategyCatalog.Last4, "Card 1111 on file.")]
        [InlineData(StrategyCatalog.Abbreviate, "Card 4 on file.")]
        public void SettingFreeStrategies_RedactAsDescribed(string strategy, string expected) =>
            Assert.Equal(expected, RedactCard(new StrategySettings { Strategy = strategy }).Output);

        [Fact]
        public void Redact_UsesTheFormat() =>
            Assert.Equal("Card [CARD] on file.", RedactCard(new StrategySettings { Strategy = StrategyCatalog.Redact, RedactionFormat = "[CARD]" }).Output);

        [Fact]
        public void Static_UsesTheValue() =>
            Assert.Equal("Card XXXX on file.", RedactCard(new StrategySettings { Strategy = StrategyCatalog.StaticReplace, StaticReplacement = "XXXX" }).Output);

        [Fact]
        public void Mask_KeepsTheLength_ByDefault() =>
            Assert.Equal("Card ################### on file.", RedactCard(new StrategySettings { Strategy = StrategyCatalog.Mask, MaskCharacter = "#" }).Output);

        [Fact]
        public void Mask_UsesAFixedLength_WhenGiven()
        {
            string output = RedactCard(new StrategySettings { Strategy = StrategyCatalog.Mask, MaskCharacter = "*", MaskLength = 5 }).Output;
            Assert.Equal("Card ***** on file.", output);
        }

        [Fact]
        public void Hash_IsTheSameEachTime_WithoutSalt()
        {
            var settings = new StrategySettings { Strategy = StrategyCatalog.HashSha256 };
            string first = RedactCard(settings).Output;
            Assert.Matches("^Card [0-9a-f]{64} on file\\.$", first);
            Assert.Equal(first, RedactCard(settings).Output);
        }

        [Fact]
        public void Hash_SaltSetting_IsSaved()
        {
            var strategy = new CreditCardFilterStrategy();
            new StrategySettings { Strategy = StrategyCatalog.HashSha256, Salt = true }.ApplyTo(strategy, null);
            Assert.True(strategy.Salt);
        }

        [Fact]
        public void Random_FromAList_PicksOneOfTheValues()
        {
            var settings = new StrategySettings
            {
                Strategy = StrategyCatalog.RandomReplace,
                RandomMethod = StrategySettings.RandomFromList,
                RandomCandidates = new List<string> { " ALPHA ", "", "BETA" }
            };
            Assert.Matches("^Card (ALPHA|BETA) on file\\.$", RedactCard(settings).Output);
        }

        [Fact]
        public void Random_Uuid_ProducesAnId() =>
            Assert.Matches("^Card [0-9a-f-]{36} on file\\.$",
                RedactCard(new StrategySettings { Strategy = StrategyCatalog.RandomReplace, RandomMethod = StrategySettings.RandomUuid }).Output);

        [Fact]
        public void Random_Consistent_SetsContextScope()
        {
            var strategy = new CreditCardFilterStrategy();
            new StrategySettings { Strategy = StrategyCatalog.RandomReplace, Consistent = true }.ApplyTo(strategy, null);
            Assert.Equal(AbstractFilterStrategy.ReplacementScopeContext, strategy.ReplacementScope);
        }

        [Fact]
        public void Crypto_StoresOnlyAnEnvironmentReference_AndEncrypts()
        {
            Environment.SetEnvironmentVariable("PD_TEST_CRYPTO_KEY", "00112233445566778899aabbccddeeff00112233445566778899aabbccddeeff");
            (string output, PhileasPolicy policy) = RedactCard(new StrategySettings { Strategy = StrategyCatalog.Crypto, CryptoKeyVariable = "PD_TEST_CRYPTO_KEY" });

            Assert.Equal("env:PD_TEST_CRYPTO_KEY", policy.Crypto!.Key);
            Assert.Matches("^Card \\{\\{[A-Za-z0-9+/=]+\\}\\} on file\\.$", output);
            Assert.DoesNotContain("00112233", PolicySerializer.SerializeToJson(policy)); // no key material saved
        }

        [Fact]
        public void Fpe_StoresOnlyEnvironmentReferences_AndKeepsTheFormat()
        {
            Environment.SetEnvironmentVariable("PD_TEST_FPE_KEY", "00112233445566778899aabbccddeeff00112233445566778899aabbccddeeff");
            Environment.SetEnvironmentVariable("PD_TEST_FPE_TWEAK", "00112233445566");
            (string output, PhileasPolicy policy) = RedactCard(new StrategySettings
            {
                Strategy = StrategyCatalog.Fpe, FpeKeyVariable = "PD_TEST_FPE_KEY", FpeTweakVariable = "PD_TEST_FPE_TWEAK"
            });

            Assert.Equal(("env:PD_TEST_FPE_KEY", "env:PD_TEST_FPE_TWEAK"), (policy.Fpe!.Key, policy.Fpe.Tweak));
            Assert.Matches("^Card \\d{4}-\\d{4}-\\d{4}-\\d{4} on file\\.$", output);
            Assert.NotEqual(Card, output);
        }

        [Fact]
        public void LookupTable_ReplacesListedValues_AndFallsBackForOthers()
        {
            var settings = new StrategySettings
            {
                Strategy = StrategyCatalog.MapReplace,
                Mappings = new List<KeyValuePair<string, string>> { new("John", "Jack"), new("", "") },
                FallbackStrategy = StrategyCatalog.Mask
            };
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };
            var strategy = new FirstNameFilterStrategy();
            settings.ApplyTo(strategy, policy);
            policy.Identifiers.FirstName = new FirstName { Strategies = new List<FirstNameFilterStrategy> { strategy } };

            Assert.Equal("Contact Jack and ****.", Run(policy, "Contact John and Mary."));
            Assert.Single(strategy.Mappings!); // the blank row isn't saved
        }

        [Fact]
        public void Shift_MovesTheDate()
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };
            var strategy = new DateFilterStrategy();
            new StrategySettings { Strategy = StrategyCatalog.Shift, ShiftYears = 1, ShiftDays = 10 }.ApplyTo(strategy, policy);
            policy.Identifiers.Date = new Date { Strategies = new List<DateFilterStrategy> { strategy } };

            Assert.Equal("DOB: 1/25/1991.", Run(policy, "DOB: 01/15/1990."));
            StrategySettings reloaded = StrategySettings.Load(strategy, policy);
            Assert.Equal((1, 0, 10), (reloaded.ShiftYears, reloaded.ShiftMonths, reloaded.ShiftDays));
        }

        [Theory]
        [InlineData(StrategyCatalog.Relative, "DOB: \\d+ years( \\d+ months?)? ago\\.")]
        [InlineData(StrategyCatalog.TruncateToYear, "DOB: 1990\\.")]
        public void DateStrategies_RedactAsDescribed(string name, string pattern)
        {
            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers() };
            var strategy = new DateFilterStrategy();
            new StrategySettings { Strategy = name }.ApplyTo(strategy, policy);
            policy.Identifiers.Date = new Date { Strategies = new List<DateFilterStrategy> { strategy } };

            Assert.Matches("^" + pattern + "$", Run(policy, "DOB: 01/15/1990."));
        }

        // --- StrategySettings: load, validate, apply -------------------------------------------

        [Fact]
        public void Load_NewStrategy_IsRedactWithTheDefaultFormat()
        {
            StrategySettings s = StrategySettings.Load(new SsnFilterStrategy(), null);
            Assert.Equal((StrategyCatalog.Redact, AbstractFilterStrategy.DefaultRedaction), (s.Strategy, s.RedactionFormat));
            Assert.Null(s.Validate());
        }

        [Fact]
        public void Apply_LeavesOtherStrategiesSettingsAlone()
        {
            var strategy = new SsnFilterStrategy { RedactionFormat = "[SSN]", StaticReplacement = "keep me" };
            new StrategySettings { Strategy = StrategyCatalog.Last4 }.ApplyTo(strategy, null);
            Assert.Equal((StrategyCatalog.Last4, "[SSN]", "keep me"), (strategy.Strategy, strategy.RedactionFormat, strategy.StaticReplacement));
        }

        private static readonly Dictionary<string, (StrategySettings Settings, string Message)> Invalid = new()
        {
            ["case 1"] = (new StrategySettings { Strategy = StrategyCatalog.Redact, RedactionFormat = " " }, "redaction format"),
            ["case 2"] = (new StrategySettings { Strategy = StrategyCatalog.StaticReplace, StaticReplacement = "" }, "value to replace"),
            ["case 3"] = (new StrategySettings { Strategy = StrategyCatalog.RandomReplace, RandomMethod = StrategySettings.RandomFromList, RandomCandidates = new() { " ", "" } }, "at least one value"),
            ["case 4"] = (new StrategySettings { Strategy = StrategyCatalog.Mask, MaskCharacter = "" }, "mask character"),
            ["case 5"] = (new StrategySettings { Strategy = StrategyCatalog.Mask, MaskCharacter = " " }, "mask character"),
            ["case 6"] = (new StrategySettings { Strategy = StrategyCatalog.Mask, MaskCharacter = "*", MaskLength = 0 }, "mask length"),
            ["case 7"] = (new StrategySettings { Strategy = StrategyCatalog.Crypto }, "environment variable"),
            ["case 8"] = (new StrategySettings { Strategy = StrategyCatalog.Crypto, CryptoKeyVariable = "1BAD" }, "valid environment variable"),
            ["case 9"] = (new StrategySettings { Strategy = StrategyCatalog.Fpe, FpeKeyVariable = "KEY" }, "tweak"),
            ["case 10"] = (new StrategySettings { Strategy = StrategyCatalog.MapReplace }, "at least one value"),
            ["case 11"] = (new StrategySettings { Strategy = StrategyCatalog.MapReplace, Mappings = new() { new("", "x") } }, "the value it replaces"),
            ["case 12"] = (new StrategySettings { Strategy = StrategyCatalog.MapReplace, Mappings = new() { new("x", " ") } }, "needs a replacement"),
            ["case 13"] = (new StrategySettings { Strategy = StrategyCatalog.MapReplace, Mappings = new() { new("John", "a"), new("john", "b") } }, "more than once"),
            ["case 14"] = (new StrategySettings { Strategy = StrategyCatalog.Shift }, "how far to shift"),
        };

        public static TheoryData<string> InvalidCases => new(Invalid.Keys);

        [Theory]
        [MemberData(nameof(InvalidCases))]
        public void Validate_RejectsIncompleteSettings(string name) =>
            Assert.Contains(Invalid[name].Message, Invalid[name].Settings.Validate(), StringComparison.OrdinalIgnoreCase);

        [Fact]
        public void Validate_CaseSensitiveTable_AllowsValuesThatDifferOnlyByCase() =>
            Assert.Null(new StrategySettings
            {
                Strategy = StrategyCatalog.MapReplace, CaseSensitive = true, Mappings = new() { new("John", "a"), new("john", "b") }
            }.Validate());

        [Fact]
        public void Validate_RandomShift_NeedsNoAmounts() =>
            Assert.Null(new StrategySettings { Strategy = StrategyCatalog.Shift, ShiftRandom = true }.Validate());

        [Theory]
        [InlineData("CRYPTO_KEY", true)]
        [InlineData("_key2", true)]
        [InlineData("2KEY", false)]
        [InlineData("MY-KEY", false)]
        [InlineData("MY KEY", false)]
        [InlineData("", false)]
        public void IsVariableName(string name, bool expected) => Assert.Equal(expected, StrategySettings.IsVariableName(name));

        [Fact]
        public void KeyStoredInThePolicy_IsKept_UntilAVariableIsEntered()
        {
            var policy = new PhileasPolicy { Name = "p", Crypto = new Crypto { Key = "aabbcc" } };
            var strategy = new SsnFilterStrategy { Strategy = StrategyCatalog.Crypto };

            StrategySettings s = StrategySettings.Load(strategy, policy);
            Assert.True(s.HasStoredCryptoKey);
            Assert.Equal(string.Empty, s.CryptoKeyVariable);
            Assert.Null(s.Validate());
            s.ApplyTo(strategy, policy);
            Assert.Equal("aabbcc", policy.Crypto!.Key); // not dropped or replaced

            s.CryptoKeyVariable = "CRYPTO_KEY";
            s.ApplyTo(strategy, policy);
            Assert.Equal("env:CRYPTO_KEY", policy.Crypto!.Key);
        }

        [Fact]
        public void EnvironmentReference_LoadsAsItsVariableName()
        {
            var policy = new PhileasPolicy { Name = "p", Fpe = new Fpe { Key = "env:FPE_KEY", Tweak = "env:FPE_TWEAK" } };
            StrategySettings s = StrategySettings.Load(new SsnFilterStrategy { Strategy = StrategyCatalog.Fpe }, policy);
            Assert.Equal(("FPE_KEY", "FPE_TWEAK", false), (s.FpeKeyVariable, s.FpeTweakVariable, s.HasStoredFpeKey));
        }

        [Fact]
        public void StoredFpeKey_WithANewKeyVariable_NeedsATweakVariableToo()
        {
            var policy = new PhileasPolicy { Name = "p", Fpe = new Fpe { Key = "aabb", Tweak = "ccdd" } };
            StrategySettings s = StrategySettings.Load(new SsnFilterStrategy { Strategy = StrategyCatalog.Fpe }, policy);
            Assert.Null(s.Validate()); // keeps the stored key and tweak
            s.FpeKeyVariable = "FPE_KEY";
            Assert.Contains("tweak", s.Validate());
        }

        // --- The dialog ----------------------------------------------------------------------

        private static List<string> Offered(AddFilterStrategyForm form) =>
            form.StrategyChoice.Items.Cast<StrategyInfo>().Select(s => s.Name).ToList();

        [Fact]
        public void Dialog_WithoutAPolicy_DoesNotOfferEncryption() => Sta(() =>
        {
            using var form = new AddFilterStrategyForm(new SsnFilterStrategy(), "SSN");
            Assert.DoesNotContain(StrategyCatalog.Crypto, Offered(form));
            Assert.Contains(StrategyCatalog.Last4, Offered(form));
        });

        [Fact]
        public void Dialog_WithAPolicy_OffersEncryption() => Sta(() =>
        {
            using var form = new AddFilterStrategyForm(new SsnFilterStrategy(), "SSN", new PhileasPolicy());
            Assert.Contains(StrategyCatalog.Crypto, Offered(form));
            Assert.Contains(StrategyCatalog.Fpe, Offered(form));
        });

        [Fact]
        public void Dialog_ChoosingAStrategy_AndAccepting_SavesIt() => Sta(() =>
        {
            var strategy = new CreditCardFilterStrategy();
            using var form = new AddFilterStrategyForm(strategy, "Credit Card");
            form.StrategyChoice.SelectedItem = form.StrategyChoice.Items.Cast<StrategyInfo>().Single(s => s.Name == StrategyCatalog.Last4);

            Assert.Null(form.Accept());
            Assert.Equal(StrategyCatalog.Last4, strategy.Strategy);
        });

        [Fact]
        public void Dialog_InvalidSettings_ReturnAnError_AndChangeNothing() => Sta(() =>
        {
            var strategy = new CreditCardFilterStrategy { Strategy = StrategyCatalog.Redact };
            using var form = new AddFilterStrategyForm(strategy, "Credit Card");
            form.StrategyChoice.SelectedItem = form.StrategyChoice.Items.Cast<StrategyInfo>().Single(s => s.Name == StrategyCatalog.StaticReplace);

            Assert.NotNull(form.Accept()); // no fixed value entered
            Assert.Equal(StrategyCatalog.Redact, strategy.Strategy);
        });

        [Fact]
        public void Dialog_DoesNotOfferDetectedType_ButKeepsAnExistingTypeCondition() => Sta(() =>
        {
            var strategy = new IdentifierFilterStrategy { Strategy = StrategyCatalog.Redact, Condition = "type == \"acct\"" };
            using var form = new AddFilterStrategyForm(strategy, "Custom Identifier");

            Assert.DoesNotContain(form.ConditionFieldChoice.Items.Cast<ConditionBuilder.ConditionField>(), f => f.Keyword == "type");
            Assert.Null(form.Accept());
            Assert.Equal("type == \"acct\"", strategy.Condition);
        });

        [Fact]
        public void Dialog_AStrategyItCantEdit_IsShownAsIs_AndKept() => Sta(() =>
        {
            // A strategy not offered here (from a newer Phileas, or not working in this one) is preserved.
            var strategy = new CreditCardFilterStrategy
            {
                Strategy = "FUTURE_STRATEGY",
                Mappings = new Dictionary<string, string> { ["a"] = "b" },
                Condition = "token startswith \"4\""
            };
            using var form = new AddFilterStrategyForm(strategy, "Credit Card");
            var first = (StrategyInfo)form.StrategyChoice.Items[0]!;

            Assert.Equal("FUTURE_STRATEGY", first.Name);
            Assert.Contains("kept as is", first.Label);
            Assert.Same(first, form.StrategyChoice.SelectedItem);
            Assert.Null(form.Accept());
            Assert.Equal("FUTURE_STRATEGY", strategy.Strategy);
            Assert.Equal("b", strategy.Mappings!["a"]);
            Assert.Equal("token startswith \"4\"", strategy.Condition);
        });

        [Fact]
        public void Dialog_EncryptionWithoutAPolicy_IsKeptAsIs() => Sta(() =>
        {
            var strategy = new SsnFilterStrategy { Strategy = StrategyCatalog.Crypto };
            using var form = new AddFilterStrategyForm(strategy, "SSN");
            Assert.Contains("kept as is", ((StrategyInfo)form.StrategyChoice.SelectedItem!).Label);
            Assert.Null(form.Accept());
            Assert.Equal(StrategyCatalog.Crypto, strategy.Strategy);
        });

        [Fact]
        public void Dialog_LaysOutCleanly_WithEachStrategySelected() => Sta(() =>
        {
            using var form = new AddFilterStrategyForm(new CreditCardFilterStrategy(), "Credit Card", new PhileasPolicy())
            {
                StartPosition = FormStartPosition.Manual,
                ShowInTaskbar = false
            };
            form.Show();
            Application.DoEvents();
            foreach (StrategyInfo strategy in form.StrategyChoice.Items.Cast<StrategyInfo>().ToList())
            {
                form.StrategyChoice.SelectedItem = strategy;
                Application.DoEvents();
                List<string> problems = LayoutAudit.Audit(form);
                Assert.True(problems.Count == 0, strategy.Name + ": " + string.Join(Environment.NewLine, problems));
            }
            form.Close();
        });

        [Fact]
        public void VariableStatus_SaysWhetherTheVariableIsSet()
        {
            Environment.SetEnvironmentVariable("PD_TEST_STATUS_SET", "x");
            Assert.Contains("is set", AddFilterStrategyForm.VariableStatus("PD_TEST_STATUS_SET", false));
            Assert.Contains("isn't set", AddFilterStrategyForm.VariableStatus("PD_TEST_STATUS_MISSING_" + Guid.NewGuid().ToString("N"), false));
            Assert.Contains("Not a valid", AddFilterStrategyForm.VariableStatus("2BAD", false));
            Assert.Contains("stores the key itself", AddFilterStrategyForm.VariableStatus("", true));
            Assert.Equal(string.Empty, AddFilterStrategyForm.VariableStatus(" ", false));
        }

        // --- The strategy list ---------------------------------------------------------------

        public static TheoryData<AbstractFilterStrategy, string> Descriptions => new()
        {
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Redact, RedactionFormat = "[X]" }, "Redact with \"[X]\"" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Last4 }, "Keep the last 4 characters" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Mask, MaskCharacter = "#" }, "Mask with #" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Mask, MaskCharacter = "*", MaskLength = "5" }, "Mask with *, 5 characters" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.HashSha256, Salt = true }, "Replace with a SHA-256 hash, salted" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.RandomReplace, AnonymizationMethod = "UUID" }, "Replace with a random ID" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Crypto }, "Encrypt (no key set)" },
            { new SsnFilterStrategy { Strategy = StrategyCatalog.Abbreviate, Condition = "confidence > 0.9" }, "Abbreviate to initials  [when confidence > 0.9]" },
            { new SsnFilterStrategy { Strategy = "FUTURE_STRATEGY" }, "FUTURE_STRATEGY" },
            {
                new SsnFilterStrategy { Strategy = StrategyCatalog.MapReplace, Mappings = new() { ["a"] = "b", ["c"] = "d" }, FallbackStrategy = StrategyCatalog.Mask },
                "Replace from a lookup table (2 entries, otherwise mask)"
            },
        };

        [Theory]
        [MemberData(nameof(Descriptions))]
        public void Describe_SummarizesEachStrategy(AbstractFilterStrategy strategy, string expected) =>
            Assert.Equal(expected, FilterStrategiesForm.Describe(strategy));

        [Fact]
        public void Describe_ShowsTheKeyVariable() =>
            Assert.Equal("Encrypt (key from %CRYPTO_KEY%)",
                FilterStrategiesForm.Describe(new SsnFilterStrategy { Strategy = StrategyCatalog.Crypto },
                    new PhileasPolicy { Crypto = new Crypto { Key = "env:CRYPTO_KEY" } }));

        // --- TRUNCATE: keeps some characters at either end and masks the rest -------------------------------

        // A credit-card strategy loaded from JSON, so these tests compile against versions without the properties.
        private static CreditCardFilterStrategy CardStrategy(string json) =>
            PolicySerializer.DeserializeFromJson("{\"identifiers\":{\"creditCard\":{\"creditCardFilterStrategies\":[" + json + "]}}}")
                .Identifiers.CreditCard!.Strategies!.Single();

        [Fact]
        public void Truncate_LabelMatchesWhatTheInstalledPhileasDoes()
        {
            Assert.True(StrategyCatalog.TruncateSettingsSupported);
            Assert.Equal(StrategyCatalog.TruncateInfo(true), StrategyCatalog.Find(StrategyCatalog.Truncate));
            Assert.Equal("Keep the first character", StrategyCatalog.TruncateInfo(false).Label);
            Assert.Equal("Keep some characters, mask the rest", StrategyCatalog.TruncateInfo(true).Label);
            Assert.DoesNotContain("—", StrategyCatalog.TruncateInfo(true).Description + StrategyCatalog.TruncateInfo(false).Description);
        }

        [Theory]
        [InlineData(4, false, "*", "Card 4111*************** on file.")]
        [InlineData(4, true, "*", "Card ***************1111 on file.")]
        [InlineData(4, true, "#", "Card ###############1111 on file.")]
        [InlineData(1, false, "X", "Card 4XXXXXXXXXXXXXXXXXX on file.")]
        public void Truncate_KeepsCharactersAtTheChosenEnd_AndMasksTheRest(int leave, bool trailing, string character, string expected)
        {
            var settings = new StrategySettings
            {
                Strategy = StrategyCatalog.Truncate, TruncateLeaveCharacters = leave, TruncateTrailing = trailing, TruncateCharacter = character
            };
            Assert.Null(settings.Validate());
            Assert.Equal(expected, RedactCard(settings).Output);
        }

        [Fact]
        public void Truncate_SettingsRoundTripThroughThePolicy()
        {
            (_, PhileasPolicy policy) = RedactCard(new StrategySettings
            {
                Strategy = StrategyCatalog.Truncate, TruncateLeaveCharacters = 4, TruncateTrailing = true, TruncateCharacter = "#"
            });
            string json = PolicySerializer.SerializeToJson(policy);
            Assert.Contains("\"truncateLeaveCharacters\":4", json);
            Assert.Contains("\"truncateDirection\":\"TRAILING\"", json);
            Assert.Contains("\"truncateCharacter\":\"#\"", json);

            StrategySettings reloaded = StrategySettings.Load(PolicySerializer.DeserializeFromJson(json).Identifiers.CreditCard!.Strategies!.Single(), null);
            Assert.Equal((4, true, "#"), (reloaded.TruncateLeaveCharacters, reloaded.TruncateTrailing, reloaded.TruncateCharacter));
        }

        [Fact]
        public void Truncate_LoadsPhileasDefaults_WhenNothingIsSet()
        {
            StrategySettings s = StrategySettings.Load(CardStrategy("{\"strategy\":\"TRUNCATE\"}"), null);
            Assert.Equal((StrategySettings.DefaultTruncateLeave, false, "*"), (s.TruncateLeaveCharacters, s.TruncateTrailing, s.TruncateCharacter));
        }

        [Theory]
        [InlineData(0, "*", "how many characters")]
        [InlineData(1001, "*", "how many characters")]
        [InlineData(4, "", "single, visible character")]
        [InlineData(4, " ", "single, visible character")]
        public void Truncate_RejectsInvalidSettings(int leave, string character, string message)
        {
            var settings = new StrategySettings { Strategy = StrategyCatalog.Truncate, TruncateLeaveCharacters = leave, TruncateCharacter = character };
            Assert.Contains(message, settings.Validate());
        }

        [Fact]
        public void Truncate_DescribesItsSettings()
        {
            Assert.Equal("Keep the last 4 characters, mask with #",
                FilterStrategiesForm.Describe(CardStrategy("{\"strategy\":\"TRUNCATE\",\"truncateLeaveCharacters\":4,\"truncateDirection\":\"TRAILING\",\"truncateCharacter\":\"#\"}")));
            Assert.Equal("Keep the first 1 character, mask with *",
                FilterStrategiesForm.Describe(CardStrategy("{\"strategy\":\"TRUNCATE\",\"truncateLeaveCharacters\":1}")));
        }

        [Fact]
        public void Truncate_DialogEditsAndKeepsTheSettings() => Sta(() =>
        {
            CreditCardFilterStrategy strategy = CardStrategy(
                "{\"strategy\":\"TRUNCATE\",\"truncateLeaveCharacters\":2,\"truncateDirection\":\"TRAILING\",\"truncateCharacter\":\"#\"}");
            using var form = new AddFilterStrategyForm(strategy, "Credit Card");

            StrategySettings shown = form.ReadSettings();
            Assert.Equal((StrategyCatalog.Truncate, 2, true, "#"), (shown.Strategy, shown.TruncateLeaveCharacters, shown.TruncateTrailing, shown.TruncateCharacter));
            Assert.Null(form.Accept());
            Assert.Equal((2, true, "#"), (StrategySettings.Load(strategy, null).TruncateLeaveCharacters,
                StrategySettings.Load(strategy, null).TruncateTrailing, StrategySettings.Load(strategy, null).TruncateCharacter));
        });
    }
}
