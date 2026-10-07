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
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Phileas.Policy;
using Phileas.Policy.Filters.Strategies;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>
    /// What the filter-strategy dialog edits: the chosen strategy and its settings, loaded from a strategy
    /// (and the policy, for encryption keys), validated, and applied back. Settings for strategies other
    /// than the chosen one are left as they were.
    /// </summary>
    internal sealed class StrategySettings
    {
        public const string RandomRealistic = "REALISTIC";
        public const string RandomFromList = "FROM_LIST";
        public const string RandomUuid = "UUID";
        private const string EnvPrefix = "env:";

        public string Strategy { get; set; } = StrategyCatalog.Redact;
        public string RedactionFormat { get; set; } = AbstractFilterStrategy.DefaultRedaction;
        public string StaticReplacement { get; set; } = string.Empty;
        public string RandomMethod { get; set; } = RandomRealistic;
        public List<string> RandomCandidates { get; set; } = new();
        public bool Consistent { get; set; }
        public string MaskCharacter { get; set; } = "*";
        public int? MaskLength { get; set; } // null keeps the value's own length
        public bool Salt { get; set; }
        public string CryptoKeyVariable { get; set; } = string.Empty;
        public string FpeKeyVariable { get; set; } = string.Empty;
        public string FpeTweakVariable { get; set; } = string.Empty;
        public List<KeyValuePair<string, string>> Mappings { get; set; } = new();
        public string FallbackStrategy { get; set; } = StrategyCatalog.Redact;
        public bool CaseSensitive { get; set; }
        public int ShiftDays { get; set; }
        public int ShiftMonths { get; set; }
        public int ShiftYears { get; set; }
        public bool ShiftRandom { get; set; }
        public bool FutureDates { get; set; }
        public int TruncateLeaveCharacters { get; set; } = DefaultTruncateLeave;
        public bool TruncateTrailing { get; set; }
        public string TruncateCharacter { get; set; } = "*";

        /// <summary>Characters TRUNCATE keeps when none is set (as in Phileas).</summary>
        public const int DefaultTruncateLeave = 4;

        /// <summary>True when the policy already holds a key itself (not an <c>env:</c> reference), which
        /// is kept unless a variable name is entered.</summary>
        public bool HasStoredCryptoKey { get; private set; }

        public bool HasStoredFpeKey { get; private set; }

        public static StrategySettings Load(AbstractFilterStrategy strategy, PhileasPolicy? policy)
        {
            var s = new StrategySettings
            {
                Strategy = string.IsNullOrEmpty(strategy.Strategy) ? StrategyCatalog.Redact : strategy.Strategy,
                RedactionFormat = string.IsNullOrEmpty(strategy.RedactionFormat) ? AbstractFilterStrategy.DefaultRedaction : strategy.RedactionFormat,
                StaticReplacement = strategy.StaticReplacement ?? string.Empty,
                RandomMethod = NormalizeRandomMethod(strategy.AnonymizationMethod),
                RandomCandidates = strategy.AnonymizationCandidates?.ToList() ?? new List<string>(),
                Consistent = string.Equals(strategy.ReplacementScope, AbstractFilterStrategy.ReplacementScopeContext, StringComparison.OrdinalIgnoreCase),
                MaskCharacter = string.IsNullOrEmpty(strategy.MaskCharacter) ? "*" : strategy.MaskCharacter,
                MaskLength = int.TryParse(strategy.MaskLength, out int length) ? length : null,
                Salt = strategy.Salt,
                Mappings = strategy.Mappings?.ToList() ?? new List<KeyValuePair<string, string>>(),
                FallbackStrategy = string.IsNullOrEmpty(strategy.FallbackStrategy) ? StrategyCatalog.Redact : strategy.FallbackStrategy,
                CaseSensitive = strategy.CaseSensitive == true, // bool in Phileas 1.6.0, bool? from 1.7.0
                ShiftDays = GetJson<int>(strategy, "shiftDays"),
                ShiftMonths = GetJson<int>(strategy, "shiftMonths"),
                ShiftYears = GetJson<int>(strategy, "shiftYears"),
                ShiftRandom = GetJson<bool>(strategy, "shiftRandom"),
                FutureDates = GetJson<bool>(strategy, "futureDates"),
                TruncateLeaveCharacters = GetJson<int>(strategy, "truncateLeaveCharacters") is int leave and > 0 ? leave : DefaultTruncateLeave,
                TruncateTrailing = string.Equals(GetJsonString(strategy, "truncateDirection"), "TRAILING", StringComparison.OrdinalIgnoreCase),
                TruncateCharacter = GetJsonString(strategy, "truncateCharacter") is { Length: > 0 } character ? character : "*",
            };

            (s.CryptoKeyVariable, s.HasStoredCryptoKey) = SplitKey(policy?.Crypto?.Key);
            (s.FpeKeyVariable, s.HasStoredFpeKey) = SplitKey(policy?.Fpe?.Key);
            (s.FpeTweakVariable, _) = SplitKey(policy?.Fpe?.Tweak);
            return s;
        }

        /// <summary>A message describing what to fix, or null when the settings can be applied.</summary>
        public string? Validate()
        {
            switch (Strategy)
            {
                case StrategyCatalog.Redact when string.IsNullOrWhiteSpace(RedactionFormat):
                    return "Enter a redaction format, such as {{{REDACTED-%t}}}.";
                case StrategyCatalog.StaticReplace:
                    return AddFilterStrategyForm.ValidateStaticReplacement(StaticReplacement);
                case StrategyCatalog.RandomReplace when RandomMethod == RandomFromList && RandomCandidates.All(string.IsNullOrWhiteSpace):
                    return "Enter at least one value to pick from, one per line.";
                case StrategyCatalog.Mask when MaskCharacter.Length != 1 || char.IsWhiteSpace(MaskCharacter[0]):
                    return "Enter a single, visible mask character, such as *.";
                case StrategyCatalog.Mask when MaskLength is < 1 or > 1000:
                    return "Enter a mask length from 1 to 1000, or keep the value's own length.";
                case StrategyCatalog.Crypto:
                    return ValidateVariable(CryptoKeyVariable, "key", HasStoredCryptoKey);
                case StrategyCatalog.Fpe:
                    // Key and tweak are replaced together, so a new key variable needs a tweak variable too.
                    return ValidateVariable(FpeKeyVariable, "key", HasStoredFpeKey)
                           ?? ValidateVariable(FpeTweakVariable, "tweak", HasStoredFpeKey && FpeKeyVariable.Length == 0);
                case StrategyCatalog.MapReplace:
                    return ValidateMappings();
                case StrategyCatalog.Shift when !ShiftRandom && ShiftDays == 0 && ShiftMonths == 0 && ShiftYears == 0:
                    return "Enter how far to shift dates, or choose a random shift.";
                case StrategyCatalog.Truncate when StrategyCatalog.TruncateSettingsSupported && TruncateLeaveCharacters is < 1 or > 1000:
                    return "Enter how many characters to keep, from 1 to 1000.";
                case StrategyCatalog.Truncate when StrategyCatalog.TruncateSettingsSupported
                                                   && (TruncateCharacter.Length != 1 || char.IsWhiteSpace(TruncateCharacter[0])):
                    return "Enter a single, visible character to put in place of the others, such as *.";
                default:
                    return null;
            }
        }

        /// <summary>Writes the chosen strategy and its settings to <paramref name="strategy"/>, and any
        /// encryption key reference to <paramref name="policy"/>.</summary>
        public void ApplyTo(AbstractFilterStrategy strategy, PhileasPolicy? policy)
        {
            strategy.Strategy = Strategy;
            switch (Strategy)
            {
                case StrategyCatalog.Redact:
                    strategy.RedactionFormat = RedactionFormat;
                    break;
                case StrategyCatalog.StaticReplace:
                    strategy.StaticReplacement = StaticReplacement;
                    break;
                case StrategyCatalog.RandomReplace:
                    strategy.AnonymizationMethod = RandomMethod;
                    strategy.AnonymizationCandidates = RandomMethod == RandomFromList
                        ? RandomCandidates.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).ToList()
                        : null;
                    strategy.ReplacementScope = Scope();
                    break;
                case StrategyCatalog.Mask:
                    strategy.MaskCharacter = MaskCharacter;
                    strategy.MaskLength = MaskLength?.ToString() ?? "same";
                    break;
                case StrategyCatalog.HashSha256:
                    strategy.Salt = Salt;
                    break;
                case StrategyCatalog.Crypto when policy is not null && CryptoKeyVariable.Length > 0:
                    policy.Crypto = new Crypto { Key = EnvPrefix + CryptoKeyVariable };
                    break;
                case StrategyCatalog.Fpe when policy is not null && FpeKeyVariable.Length > 0:
                    policy.Fpe = new Fpe { Key = EnvPrefix + FpeKeyVariable, Tweak = EnvPrefix + FpeTweakVariable };
                    break;
                case StrategyCatalog.MapReplace:
                    strategy.Mappings = FilledMappings().ToDictionary(m => m.Key.Trim(), m => m.Value);
                    strategy.FallbackStrategy = FallbackStrategy;
                    strategy.CaseSensitive = CaseSensitive;
                    strategy.ReplacementScope = Scope();
                    break;
                case StrategyCatalog.Shift:
                    SetJson(strategy, "shiftDays", ShiftDays);
                    SetJson(strategy, "shiftMonths", ShiftMonths);
                    SetJson(strategy, "shiftYears", ShiftYears);
                    SetJson(strategy, "shiftRandom", ShiftRandom);
                    SetJson(strategy, "futureDates", FutureDates);
                    break;
                case StrategyCatalog.Truncate when StrategyCatalog.TruncateSettingsSupported:
                    SetJson<int?>(strategy, "truncateLeaveCharacters", TruncateLeaveCharacters);
                    SetJson(strategy, "truncateDirection", TruncateTrailing ? "TRAILING" : "LEADING");
                    SetJson(strategy, "truncateCharacter", TruncateCharacter);
                    break;
            }
        }

        /// <summary>True for a name usable as an environment variable: letters, digits and underscores, not
        /// starting with a digit.</summary>
        internal static bool IsVariableName(string name) => Regex.IsMatch(name, "^[A-Za-z_][A-Za-z0-9_]*$");

        private string Scope() => Consistent ? AbstractFilterStrategy.ReplacementScopeContext : AbstractFilterStrategy.ReplacementScopeDocument;

        private static string? ValidateVariable(string name, string what, bool hasStoredKey)
        {
            if (name.Length == 0)
            {
                return hasStoredKey ? null : $"Enter the name of the environment variable that holds the {what}.";
            }
            return IsVariableName(name)
                ? null
                : $"\"{name}\" isn't a valid environment variable name. Use letters, digits and underscores, not starting with a digit.";
        }

        // Table rows with anything typed in them (blank rows are ignored).
        private List<KeyValuePair<string, string>> FilledMappings() =>
            Mappings.Where(m => !string.IsNullOrWhiteSpace(m.Key) || !string.IsNullOrEmpty(m.Value)).ToList();

        private string? ValidateMappings()
        {
            List<KeyValuePair<string, string>> rows = FilledMappings();
            if (rows.Count == 0)
            {
                return "Add at least one value and its replacement.";
            }
            if (rows.Any(m => string.IsNullOrWhiteSpace(m.Key)))
            {
                return "Every replacement needs the value it replaces.";
            }
            if (rows.Any(m => string.IsNullOrWhiteSpace(m.Value)))
            {
                return "Every value needs a replacement. To remove a value instead, use a different strategy.";
            }
            StringComparer comparer = CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
            string? duplicate = rows.GroupBy(m => m.Key.Trim(), comparer).FirstOrDefault(g => g.Count() > 1)?.Key;
            return duplicate is null ? null : $"\"{duplicate}\" is listed more than once.";
        }

        private static string NormalizeRandomMethod(string? method) => method?.ToUpperInvariant() switch
        {
            RandomFromList => RandomFromList,
            RandomUuid => RandomUuid,
            _ => RandomRealistic
        };

        // An "env:NAME" reference gives the variable name; any other non-empty value is a key stored in the policy.
        private static (string Variable, bool Stored) SplitKey(string? value) =>
            value is null || value.Length == 0 ? (string.Empty, false)
            : value.StartsWith(EnvPrefix, StringComparison.Ordinal) ? (value[EnvPrefix.Length..], false)
            : (string.Empty, true);

        // Date-shift settings are found by their JSON name: their C# names differ between Phileas versions.
        private static PropertyInfo? JsonProperty(object target, string jsonName) =>
            target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == jsonName);

        private static T GetJson<T>(object target, string jsonName) where T : struct =>
            JsonProperty(target, jsonName)?.GetValue(target) is T value ? value : default;

        private static string? GetJsonString(object target, string jsonName) =>
            JsonProperty(target, jsonName)?.GetValue(target) as string;

        private static void SetJson<T>(object target, string jsonName, T value) =>
            JsonProperty(target, jsonName)?.SetValue(target, value);
    }
}
