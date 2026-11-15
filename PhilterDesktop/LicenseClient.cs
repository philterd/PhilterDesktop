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
using System.Net.Http.Json;
using System.Text.Json;

namespace PhilterDesktop
{
    /// <summary>How a license call to account.philterd.ai ended.</summary>
    public enum LicenseOutcome
    {
        /// <summary>Licensed: <see cref="LicenseResult.Token"/> holds the signed token.</summary>
        Licensed,

        /// <summary>The server answered definitely: no current license.</summary>
        NotLicensed,

        /// <summary>The code was wrong, expired, used, out of tries, or for an unknown request.</summary>
        InvalidCode,

        /// <summary>The server refused the token sent to refresh.</summary>
        InvalidToken,

        RateLimited,

        /// <summary>Couldn't get an answer: offline, timed out, server trouble, or an unexpected reply.</summary>
        Unavailable
    }

    internal sealed record LicenseResult(LicenseOutcome Outcome, string? Token = null);

    /// <summary>
    /// The license API on account.philterd.ai: ask for an emailed code, exchange it for a signed token, and
    /// refresh a token. Sends only what each call needs (the email, the code, or the token) with the same
    /// User-Agent as the update check. Never throws; every failure is <see cref="LicenseOutcome.Unavailable"/>.
    /// </summary>
    internal sealed class LicenseClient
    {
        internal const string BaseUrl = "https://account.philterd.ai/api/desktop/license/";

        private readonly HttpMessageHandler? _handler;
        private readonly TimeSpan _timeout;

        public LicenseClient() : this(null, UpdateChecker.FetchTimeout)
        {
        }

        /// <summary>With a handler and timeout tests can supply.</summary>
        internal LicenseClient(HttpMessageHandler? handler, TimeSpan timeout)
        {
            _handler = handler;
            _timeout = timeout;
        }

        /// <summary>
        /// Asks for a code to be emailed. Returns the request id to send back with the code, or null if the
        /// server couldn't be reached. The server answers the same for every address, so a non-null id says
        /// nothing about whether a code was sent.
        /// </summary>
        public async Task<string?> RequestCodeAsync(string email, CancellationToken ct = default)
        {
            using JsonDocument? body = await PostAsync("request-code", new { email = NormalizeEmail(email) }, ct, HttpStatusCode.Accepted);
            return body is not null && Str(body.RootElement, "request") is { Length: > 0 } id ? id : null;
        }

        public Task<LicenseResult> ExchangeCodeAsync(string email, string code, string requestId, CancellationToken ct = default) =>
            LicenseCallAsync("token", new { email = NormalizeEmail(email), code = code.Trim(), request = requestId }, ct);

        public Task<LicenseResult> RefreshAsync(string token, CancellationToken ct = default) =>
            LicenseCallAsync("refresh", new { token = token.Trim() }, ct);

        internal static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private async Task<LicenseResult> LicenseCallAsync(string endpoint, object request, CancellationToken ct)
        {
            HttpStatusCode status;
            JsonDocument? body;
            try
            {
                (status, body) = await SendAsync(endpoint, request, ct);
            }
            catch
            {
                return new LicenseResult(LicenseOutcome.Unavailable);
            }

            using (body)
            {
                JsonElement root = body?.RootElement ?? default;
                switch (status)
                {
                    case HttpStatusCode.OK when body is not null && root.ValueKind == JsonValueKind.Object &&
                                                root.TryGetProperty("licensed", out JsonElement licensed):
                        if (licensed.ValueKind == JsonValueKind.True && Str(root, "token") is { Length: > 0 } token)
                        {
                            return new LicenseResult(LicenseOutcome.Licensed, token);
                        }
                        return licensed.ValueKind == JsonValueKind.False
                            ? new LicenseResult(LicenseOutcome.NotLicensed)
                            : new LicenseResult(LicenseOutcome.Unavailable);
                    case HttpStatusCode.BadRequest when Str(root, "error") == "invalid_code":
                        return new LicenseResult(LicenseOutcome.InvalidCode);
                    case HttpStatusCode.BadRequest when Str(root, "error") == "invalid_token":
                        return new LicenseResult(LicenseOutcome.InvalidToken);
                    case HttpStatusCode.TooManyRequests:
                        return new LicenseResult(LicenseOutcome.RateLimited);
                    default:
                        return new LicenseResult(LicenseOutcome.Unavailable);
                }
            }
        }

        private async Task<JsonDocument?> PostAsync(string endpoint, object request, CancellationToken ct, HttpStatusCode expected)
        {
            try
            {
                (HttpStatusCode status, JsonDocument? body) = await SendAsync(endpoint, request, ct);
                if (status == expected)
                {
                    return body;
                }
                body?.Dispose();
            }
            catch
            {
            }
            return null;
        }

        private async Task<(HttpStatusCode Status, JsonDocument? Body)> SendAsync(string endpoint, object request, CancellationToken ct)
        {
            using var http = _handler is null ? new HttpClient() : new HttpClient(_handler, disposeHandler: false);
            http.Timeout = _timeout;
            http.MaxResponseContentBufferSize = UpdateChecker.MaxManifestBytes;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UpdateChecker.UserAgent(UpdateChecker.CurrentVersion()));

            using HttpResponseMessage response = await http.PostAsJsonAsync(BaseUrl + endpoint, request, ct);
            string text = await response.Content.ReadAsStringAsync(ct);
            JsonDocument? body = null;
            try
            {
                body = string.IsNullOrWhiteSpace(text) ? null : JsonDocument.Parse(text);
            }
            catch (JsonException)
            {
                // Not JSON (a proxy's or captive portal's page): treated by status alone.
            }
            return (response.StatusCode, body);
        }

        private static string? Str(JsonElement obj, string name) =>
            obj.ValueKind == JsonValueKind.Object && obj.TryGetProperty(name, out JsonElement e) && e.ValueKind == JsonValueKind.String
                ? e.GetString()
                : null;
    }
}
