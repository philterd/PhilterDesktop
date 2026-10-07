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

using System.Text.Json.Nodes;
using Philterd.PhiSql;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.PolicyEditing
{
    /// <summary>A replacement strategy the filter-strategy dialog can offer.</summary>
    /// <param name="Name">The policy token, e.g. <c>LAST_4</c>.</param>
    /// <param name="Label">Short name shown in the dropdown.</param>
    /// <param name="Description">One line shown under the dropdown.</param>
    /// <param name="MinimumPhileas">The first Phileas version where the strategy works, or null.</param>
    internal sealed record StrategyInfo(string Name, string Label, string Description, Version? MinimumPhileas = null)
    {
        public override string ToString() => Label;
    }

    /// <summary>
    /// The replacement strategies Phileas supports, and which of them to offer for a filter: those its
    /// policy schema allows for that filter's strategy type, and that work in the installed Phileas.
    /// </summary>
    internal static class StrategyCatalog
    {
        public const string Redact = "REDACT";
        public const string StaticReplace = "STATIC_REPLACE";
        public const string RandomReplace = "RANDOM_REPLACE";
        public const string Mask = "MASK";
        public const string Last4 = "LAST_4";
        public const string Truncate = "TRUNCATE";
        public const string Abbreviate = "ABBREVIATE";
        public const string HashSha256 = "HASH_SHA256_REPLACE";
        public const string Crypto = "CRYPTO_REPLACE";
        public const string Fpe = "FPE_ENCRYPT_REPLACE";
        public const string MapReplace = "MAP_REPLACE";
        public const string Shift = "SHIFT";
        public const string Relative = "RELATIVE";
        public const string TruncateToYear = "TRUNCATE_TO_YEAR";

        // MAP_REPLACE and the date strategies are accepted by Phileas 1.6.0 but redact instead.
        private static readonly Version Phileas17 = new(1, 7, 0);

        public static readonly IReadOnlyList<StrategyInfo> All = new StrategyInfo[]
        {
            new(Redact, "Redact", "Replace with a redaction marker, such as {{{REDACTED-credit-card}}}."),
            new(StaticReplace, "Replace with a fixed value", "Replace with text you choose."),
            new(RandomReplace, "Replace with a random value", "Replace with a made-up value of the same kind."),
            new(Mask, "Mask", "Replace each character, for example with ****."),
            new(Last4, "Keep the last 4 characters", "For example, 4111-1111-1111-1111 becomes 1111."),
            new(Truncate, "Keep the first character", "For example, John becomes J."),
            new(Abbreviate, "Abbreviate to initials", "For example, John Smith becomes JS."),
            new(HashSha256, "Replace with a SHA-256 hash", "The same value always gets the same hash, unless salted."),
            new(Crypto, "Encrypt", "AES-GCM encryption. The original can be recovered with the key."),
            new(Fpe, "Encrypt, keeping the format", "Format-preserving encryption: a card number stays shaped like a card number."),
            new(MapReplace, "Replace from a lookup table", "Replace listed values with the replacement you give each.", Phileas17),
            new(Shift, "Shift the date", "Move each date by a fixed or random amount.", Phileas17),
            new(Relative, "Describe relative to today", "For example, 36 years 9 months ago.", Phileas17),
            new(TruncateToYear, "Keep only the year", "For example, 01/15/1990 becomes 1990.", Phileas17),
        };

        public static Version InstalledPhileas => typeof(PhileasPolicy).Assembly.GetName().Version ?? new Version(0, 0);

        public static StrategyInfo? Find(string? name) =>
            All.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>The strategies to offer for a filter whose strategies are of <paramref name="strategyType"/>.</summary>
        public static IReadOnlyList<StrategyInfo> For(Type strategyType) => For(strategyType, InstalledPhileas);

        internal static IReadOnlyList<StrategyInfo> For(Type strategyType, Version phileas)
        {
            ISet<string> allowed = AllowedBySchema(strategyType);
            return All.Where(s => allowed.Contains(s.Name) && Works(s, phileas)).ToList();
        }

        /// <summary>The strategies a lookup table can fall back to for a value not in the table.</summary>
        internal static IReadOnlyList<StrategyInfo> Fallbacks(Version phileas)
        {
            ISet<string> allowed = SchemaEnum("baseFilterStrategy", "fallbackStrategy");
            return All.Where(s => allowed.Contains(s.Name) && Works(s, phileas)).ToList();
        }

        internal static bool Works(StrategyInfo strategy, Version phileas) =>
            strategy.MinimumPhileas is null || phileas >= strategy.MinimumPhileas;

        /// <summary>The <c>strategy</c> values the bundled policy schema accepts for this strategy type.</summary>
        internal static ISet<string> AllowedBySchema(Type strategyType)
        {
            // Date strategies have their own schema definition (dateFilterStrategy); every other filter's
            // strategies use the shared baseFilterStrategy.
            string camel = char.ToLowerInvariant(strategyType.Name[0]) + strategyType.Name[1..];
            string definition = Definitions.Value.ContainsKey(camel) ? camel : "baseFilterStrategy";
            return SchemaEnum(definition, "strategy");
        }

        private static ISet<string> SchemaEnum(string definition, string property) =>
            Definitions.Value[definition]?["properties"]?[property]?["enum"] is JsonArray values
                ? values.Select(v => v!.GetValue<string>()).ToHashSet(StringComparer.Ordinal)
                : new HashSet<string>();

        private static readonly Lazy<JsonObject> Definitions =
            new(() => (JsonObject)JsonNode.Parse(PolicySchema.GetSchema())!["$defs"]!);
    }
}
