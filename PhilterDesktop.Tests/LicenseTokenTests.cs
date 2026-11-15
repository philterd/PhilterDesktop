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

using System.Security.Cryptography;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Offline verification of account.philterd.ai license tokens (ES256 JWS, P1363 signatures).</summary>
    public sealed class LicenseTokenTests
    {
        private static LicenseInfo? Verify(string? token) => LicenseToken.Verify(token, TestLicenses.Keys);

        // --- The service's published test vectors ---------------------------------------

        [Fact]
        public void ValidVector_Verifies_WithEveryField()
        {
            LicenseInfo info = Assert.IsType<LicenseInfo>(Verify(TestLicenses.Valid));
            Assert.Equal("jane@example.com", info.Email);
            Assert.Equal("subscription", info.PurchaseType);
            Assert.Equal("123456", info.PurchaseId);
            Assert.Equal(3, info.Seats);
            Assert.Equal(new DateTime(2027, 11, 15, 0, 0, 0, DateTimeKind.Utc), info.Expires);
            Assert.Equal(new DateTime(2026, 11, 15, 18, 4, 5, DateTimeKind.Utc), info.Issued.ToUniversalTime());
            Assert.Equal("test", info.KeyId);
        }

        [Fact]
        public void TamperedPayload_IsRefused() => Assert.Null(Verify(TestLicenses.TamperedPayload));

        [Fact]
        public void BadSignature_IsRefused() => Assert.Null(Verify(TestLicenses.BadSignature));

        // License files end with a newline, and a pasted token may carry spaces.
        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        [InlineData("  \t")]
        public void SurroundingWhitespace_IsIgnored(string around) =>
            Assert.NotNull(Verify(around + TestLicenses.Valid + around));

        // --- Keys -----------------------------------------------------------------------------

        // Release builds trust only the production key: a test-key token is refused.
        [Fact]
        public void TestKeyTokens_AreRefusedByTheProductionKeys() =>
            Assert.Null(LicenseToken.Verify(TestLicenses.Valid));

        [Fact]
        public void ProductionKeys_AreExactlyTheDeployedKey()
        {
            KeyValuePair<string, string> only = Assert.Single(LicenseToken.ProductionKeys);
            Assert.Equal("2026-11", only.Key);
            using ECDsa key = ECDsa.Create();
            key.ImportFromPem(only.Value);
            Assert.Equal(256, key.KeySize);
            // The SHA-256 of the key's SPKI encoding, as given when the key was handed over.
            string fingerprint = Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo())).ToLowerInvariant();
            Assert.Equal("18daf6e3b59b0f4d55293ce4e954bc8a0ef20227afe52a6fab602a092676ede6", fingerprint);
        }

        [Fact]
        public void TheTestKey_IsNeverAProductionKey()
        {
            Assert.DoesNotContain("test", LicenseToken.ProductionKeys.Keys);
            Assert.DoesNotContain(TestLicenses.PublicPem, LicenseToken.ProductionKeys.Values);
        }

        [Fact]
        public void UnknownKid_IsRefused() => Assert.Null(Verify(TestLicenses.Make(kid: "2099-01")));

        [Fact]
        public void MissingKid_IsRefused() =>
            Assert.Null(Verify(TestLicenses.Sign(new { alg = "ES256" }, new { v = 1, product = "philter-desktop", email = "a@b.c" })));

        // --- The algorithm is pinned, not taken from the header ---------------------------------

        [Theory]
        [InlineData("none")]
        [InlineData("HS256")]
        [InlineData("ES384")]
        [InlineData("RS256")]
        [InlineData("es256")]
        public void OtherAlgorithms_AreRefused(string alg) => Assert.Null(Verify(TestLicenses.Make(alg: alg)));

        // The signature must be the 64-byte P1363 form, as JWS requires; DER is refused.
        [Fact]
        public void DerSignature_IsRefused()
        {
            string der = TestLicenses.Sign(
                new { alg = "ES256", kid = "test" },
                new { v = 1, product = "philter-desktop", email = "jane@example.com", purchase = new { type = "subscription", id = "1" }, seats = 1, expires = "2027-11-15", issued = "2026-11-15T00:00:00Z" },
                derSignature: true);
            Assert.Null(Verify(der));
        }

        [Fact]
        public void AFreshlySignedToken_Verifies() => Assert.NotNull(Verify(TestLicenses.Make()));

        // --- Malformed tokens never throw ----------------------------------------------------------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("a.b")]
        [InlineData("a.b.c.d")]
        [InlineData("not a token")]
        [InlineData("!!!.@@@.###")]
        [InlineData("e30.e30.AAAA")]                 // {} header and payload, short signature
        public void Malformed_IsRefused(string? token) => Assert.Null(Verify(token));

        // Tokens are base64url without padding; standard base64 or padding is refused.
        [Theory]
        [InlineData("=")]
        [InlineData("+")]
        [InlineData("/")]
        public void NonBase64UrlCharacters_AreRefused(string c) => Assert.Null(Verify(TestLicenses.Valid + c));

        // --- Payload rules (checked only after the signature) ------------------------------------

        [Theory]
        [InlineData(2)]
        [InlineData(0)]
        public void UnknownVersion_IsRefused(int v) => Assert.Null(Verify(TestLicenses.Make(v: v)));

        [Fact]
        public void VersionAsString_IsRefused() => Assert.Null(Verify(TestLicenses.Make(v: "1")));

        [Theory]
        [InlineData("philter")]
        [InlineData("Philter-Desktop")]
        [InlineData("")]
        public void OtherProduct_IsRefused(string product) => Assert.Null(Verify(TestLicenses.Make(product: product)));

        [Fact]
        public void EmptyEmail_IsRefused() => Assert.Null(Verify(TestLicenses.Make(email: "")));

        [Theory]
        [InlineData("2027/11/15")]
        [InlineData("15-11-2027")]
        [InlineData("2027-11-15T00:00:00Z")]
        [InlineData("")]
        public void BadExpiry_IsRefused(string expires) => Assert.Null(Verify(TestLicenses.Make(expires: expires)));

        [Fact]
        public void MissingIssued_IsRefused() => Assert.Null(Verify(TestLicenses.Make(issued: null)));

        [Fact]
        public void EmptyPurchase_IsRefused()
        {
            Assert.Null(Verify(TestLicenses.Make(purchaseType: "")));
            Assert.Null(Verify(TestLicenses.Make(purchaseId: "")));
        }

        // Seats are informational; a missing or odd count reads as one seat rather than refusing a license.
        [Theory]
        [InlineData(0, 1)]
        [InlineData(-4, 1)]
        [InlineData(1, 1)]
        [InlineData(25, 25)]
        public void Seats_AreReadLeniently(int seats, int expected) =>
            Assert.Equal(expected, Verify(TestLicenses.Make(seats: seats))!.Seats);

        [Fact]
        public void SeatsAsString_ReadsAsOne() => Assert.Equal(1, Verify(TestLicenses.Make(seats: "3"))!.Seats);

        // --- When a license counts -------------------------------------------------------------

        [Theory]
        [InlineData("2026-11-15", true)]
        [InlineData("2027-11-15", true)]   // the expiry date itself
        [InlineData("2027-12-15", true)]   // the last day of grace
        [InlineData("2027-12-16", false)]
        [InlineData("2030-01-01", false)]
        public void IsCurrent_ThroughExpiryPlusGrace(string today, bool expected)
        {
            LicenseInfo info = Verify(TestLicenses.Valid)!;
            DateTime now = DateTime.SpecifyKind(DateTime.Parse(today, System.Globalization.CultureInfo.InvariantCulture).AddHours(23), DateTimeKind.Utc);
            Assert.Equal(expected, info.IsCurrent(now));
        }

        [Fact]
        public void GraceIs30Days() => Assert.Equal(30, LicenseInfo.GraceDays);
    }
}
