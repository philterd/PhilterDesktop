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

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhilterDesktop
{
    /// <summary>What a verified work-use license says.</summary>
    internal sealed record LicenseInfo(
        string Email,
        string PurchaseType,
        string PurchaseId,
        int Seats,
        DateTime Expires,
        DateTime Issued,
        string KeyId)
    {
        /// <summary>Days of grace after <see cref="Expires"/>, so a renewal while offline doesn't lapse.</summary>
        public const int GraceDays = 30;

        /// <summary>Whether the license counts on <paramref name="nowUtc"/>: through the expiry date plus grace.</summary>
        public bool IsCurrent(DateTime nowUtc) => nowUtc.Date <= Expires.Date.AddDays(GraceDays);
    }

    /// <summary>
    /// Verifies the license tokens issued by account.philterd.ai: an ES256 JWS (compact, base64url without
    /// padding, P1363 signature) whose signature is checked offline against public keys built into the app.
    /// The signature is what makes a token unforgeable even though this code is open source.
    /// </summary>
    internal static class LicenseToken
    {
        internal const string Product = "philter-desktop";
        internal const int FormatVersion = 1;

        /// <summary>
        /// Public keys that release builds trust, by key id. Rotate by adding a key here before the service
        /// starts signing with it, and remove one only once no unexpired token uses it.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> ProductionKeys = new Dictionary<string, string>
        {
            ["2026-11"] =
                "-----BEGIN PUBLIC KEY-----\n" +
                "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEzTGqsaJrs5m6y+DIU2B71FMqzhVK\n" +
                "2B2NybTAeqneIwz3Ph9Yocjgu3RiHdO2xBW6yiCg50Q9yk3aPw9TjkLWxg==\n" +
                "-----END PUBLIC KEY-----\n"
        };

        /// <summary>Verifies against the production keys.</summary>
        public static LicenseInfo? Verify(string? token) => Verify(token, ProductionKeys);

        /// <summary>
        /// Returns the license a token carries, or null if it is malformed, signed by an unknown key, not
        /// ES256, badly signed, or not a version-1 Philter Desktop license. Never throws.
        /// </summary>
        public static LicenseInfo? Verify(string? token, IReadOnlyDictionary<string, string> keysByKid)
        {
            try
            {
                return VerifyCore(token, keysByKid);
            }
            catch
            {
                return null;
            }
        }

        private static LicenseInfo? VerifyCore(string? token, IReadOnlyDictionary<string, string> keysByKid)
        {
            // License files end with a newline.
            token = token?.Trim();
            if (string.IsNullOrEmpty(token) || token.IndexOfAny(['=', '+', '/']) >= 0)
            {
                return null;
            }
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            // Header: only alg and kid matter, and alg is pinned rather than trusted.
            using (JsonDocument header = JsonDocument.Parse(FromBase64Url(parts[0])))
            {
                if (String(header.RootElement, "alg") != "ES256" ||
                    String(header.RootElement, "kid") is not string kid ||
                    !keysByKid.TryGetValue(kid, out string? pem))
                {
                    return null;
                }

                byte[] signature = FromBase64Url(parts[2]);
                if (signature.Length != 64)
                {
                    return null;
                }
                using ECDsa key = ECDsa.Create();
                key.ImportFromPem(pem);
                byte[] signedBytes = Encoding.ASCII.GetBytes(parts[0] + "." + parts[1]);
                if (!key.VerifyData(signedBytes, signature, HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation))
                {
                    return null;
                }

                // Only a correctly signed payload is read.
                using JsonDocument payload = JsonDocument.Parse(FromBase64Url(parts[1]));
                JsonElement p = payload.RootElement;
                if (!p.TryGetProperty("v", out JsonElement v) || v.ValueKind != JsonValueKind.Number || v.GetInt32() != FormatVersion ||
                    String(p, "product") != Product ||
                    String(p, "email") is not { Length: > 0 } email ||
                    !p.TryGetProperty("purchase", out JsonElement purchase) || purchase.ValueKind != JsonValueKind.Object ||
                    String(purchase, "type") is not { Length: > 0 } purchaseType ||
                    String(purchase, "id") is not { Length: > 0 } purchaseId ||
                    !DateTime.TryParseExact(String(p, "expires"), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime expires) ||
                    !DateTime.TryParse(String(p, "issued"), CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime issued))
                {
                    return null;
                }
                int seats = p.TryGetProperty("seats", out JsonElement s) && s.ValueKind == JsonValueKind.Number && s.TryGetInt32(out int n) ? n : 1;

                return new LicenseInfo(email, purchaseType, purchaseId, Math.Max(seats, 1),
                    DateTime.SpecifyKind(expires.Date, DateTimeKind.Utc), issued, kid);
            }
        }

        private static string? String(JsonElement obj, string name) =>
            obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement e) && e.ValueKind == JsonValueKind.String
                ? e.GetString()
                : null;

        internal static byte[] FromBase64Url(string s)
        {
            if (s.IndexOfAny(['=', '+', '/']) >= 0)
            {
                throw new FormatException("Not base64url without padding.");
            }
            s = s.Replace('-', '+').Replace('_', '/');
            return Convert.FromBase64String(s.PadRight(s.Length + (4 - s.Length % 4) % 4, '='));
        }
    }
}
