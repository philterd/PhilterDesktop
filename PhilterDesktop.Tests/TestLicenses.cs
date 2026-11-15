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

using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// TEST ONLY: the license test vectors published by philterd-account (account/testdata/license), and a
    /// signer for custom tokens. This key pair is public and never trusted by a release build.
    /// </summary>
    internal static class TestLicenses
    {
        public const string PublicPem =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MFkwEwYHKoZIzj0CAQYIKoZIzj0DAQcDQgAEWXEoJmxaQtNbcOX+FaWp43UAPnMj\n" +
            "XPyLIkOw81ou3awit3dXpOejgm36Fi5bT7QUR5HznEaJZqsXUL7xG4FVBg==\n" +
            "-----END PUBLIC KEY-----\n";

        public const string PrivatePem =
            "-----BEGIN PRIVATE KEY-----\n" +
            "MIGHAgEAMBMGByqGSM49AgEGCCqGSM49AwEHBG0wawIBAQQgh1DQ7FNV7ekXKjKE\n" +
            "ZsF26HBo4TnFPdmnFaGvO2vJ0g6hRANCAARZcSgmbFpC01tw5f4VpanjdQA+cyNc\n" +
            "/IsiQ7DzWi7drCK3d1ek56OCbfoWLltPtBRHkfOcRolmqxdQvvEbgVUG\n" +
            "-----END PRIVATE KEY-----\n";

        // Signed by the service's test key; the payload is
        // {"v":1,"product":"philter-desktop","email":"jane@example.com","purchase":{"type":"subscription","id":"123456"},"seats":3,"expires":"2027-11-15","issued":"2026-11-15T18:04:05Z"}
        public const string Valid =
            "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRlc3QifQ." +
            "eyJ2IjoxLCJwcm9kdWN0IjoicGhpbHRlci1kZXNrdG9wIiwiZW1haWwiOiJqYW5lQGV4YW1wbGUuY29tIiwicHVyY2hhc2UiOnsidHlwZSI6InN1YnNjcmlwdGlvbiIsImlkIjoiMTIzNDU2In0sInNlYXRzIjozLCJleHBpcmVzIjoiMjAyNy0xMS0xNSIsImlzc3VlZCI6IjIwMjYtMTEtMTVUMTg6MDQ6MDVaIn0." +
            "-A5qS3saNXX2ii5xl-3XldnHZkTTItoZowbvps1JED9vUZ9PqmHr4WVU5twQyF3EJFdUkJg-KAERNGbP8gufMA";

        public const string TamperedPayload =
            "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRlc3QifQ." +
            "eyJ2IjoxLCJwcm9kdWN0IjoicGhpbHRlci1kZXNrdG9wIiwiZW1haWwiOiJqYW5lQGV4YW1wbGUuY29tIiwicHVyY2hhc2UiOnsidHlwZSI6InN1YnNjcmlwdGlvbiIsImlkIjoiMTIzNDU2In0sInNlYXRzIjozMDAsImV4cGlyZXMiOiIyMDk5LTEyLTMxIiwiaXNzdWVkIjoiMjAyNi0xMS0xNVQxODowNDowNVoifQ." +
            "-A5qS3saNXX2ii5xl-3XldnHZkTTItoZowbvps1JED9vUZ9PqmHr4WVU5twQyF3EJFdUkJg-KAERNGbP8gufMA";

        public const string BadSignature =
            "eyJhbGciOiJFUzI1NiIsInR5cCI6IkpXVCIsImtpZCI6InRlc3QifQ." +
            "eyJ2IjoxLCJwcm9kdWN0IjoicGhpbHRlci1kZXNrdG9wIiwiZW1haWwiOiJqYW5lQGV4YW1wbGUuY29tIiwicHVyY2hhc2UiOnsidHlwZSI6InN1YnNjcmlwdGlvbiIsImlkIjoiMTIzNDU2In0sInNlYXRzIjozLCJleHBpcmVzIjoiMjAyNy0xMS0xNSIsImlzc3VlZCI6IjIwMjYtMTEtMTVUMTg6MDQ6MDVaIn0." +
            "-A5qS3saNXX2ii9xl-3XldnHZkTTItoZowbvps1JED9vUZ9PqmHr4WVU5twQyF3EJFdUkJg-KAERNGbP8gufMA";

        public static readonly IReadOnlyDictionary<string, string> Keys = new Dictionary<string, string> { ["test"] = PublicPem };

        /// <summary>A token signed with the test key; every field can be overridden to build a bad one.</summary>
        public static string Make(
            string email = "jane@example.com",
            string expires = "2027-11-15",
            object? seats = null,
            string product = "philter-desktop",
            object? v = null,
            string kid = "test",
            string alg = "ES256",
            string purchaseType = "subscription",
            string purchaseId = "123456",
            string? issued = "2026-11-15T18:04:05Z")
        {
            var payload = new Dictionary<string, object?>
            {
                ["v"] = v ?? 1,
                ["product"] = product,
                ["email"] = email,
                ["purchase"] = new Dictionary<string, string> { ["type"] = purchaseType, ["id"] = purchaseId },
                ["seats"] = seats ?? 3,
                ["expires"] = expires,
            };
            if (issued is not null)
            {
                payload["issued"] = issued;
            }
            return Sign(new Dictionary<string, string> { ["alg"] = alg, ["typ"] = "JWT", ["kid"] = kid }, payload);
        }

        /// <summary>Signs arbitrary header and payload JSON with the test key (P1363 unless DER is asked for).</summary>
        public static string Sign(object header, object payload, bool derSignature = false)
        {
            string signingInput = B64(JsonSerializer.SerializeToUtf8Bytes(header)) + "." + B64(JsonSerializer.SerializeToUtf8Bytes(payload));
            using ECDsa key = ECDsa.Create();
            key.ImportFromPem(PrivatePem);
            byte[] signature = key.SignData(Encoding.ASCII.GetBytes(signingInput), HashAlgorithmName.SHA256,
                derSignature ? DSASignatureFormat.Rfc3279DerSequence : DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
            return signingInput + "." + B64(signature);
        }

        public static string B64(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        /// <summary>A stub network that records requests and answers from a function.</summary>
        public sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> _send;
            public List<(HttpRequestMessage Request, string Body)> Requests { get; } = new();

            public StubHandler(Func<HttpRequestMessage, string, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                string body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(ct);
                Requests.Add((request, body));
                return await _send(request, body, ct);
            }

            public static StubHandler Json(HttpStatusCode status, string json) =>
                new((_, _, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") }));

            public static StubHandler Throws(Exception ex) => new((_, _, _) => Task.FromException<HttpResponseMessage>(ex));
        }

        public static LicenseClient Client(HttpMessageHandler handler) => new(handler, TimeSpan.FromMilliseconds(500));
    }
}
