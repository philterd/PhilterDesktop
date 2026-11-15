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

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Stub = PhilterDesktop.Tests.TestLicenses.StubHandler;

namespace PhilterDesktop.Tests
{
    /// <summary>The license API client against a stubbed account.philterd.ai, matching its deployed contract.</summary>
    public sealed class LicenseClientTests
    {
        [Fact]
        public void BaseUrl_IsTheAccountSiteOverHttps() =>
            Assert.Equal("https://account.philterd.ai/api/desktop/license/", LicenseClient.BaseUrl);

        // --- request-code ----------------------------------------------------------------------

        [Fact]
        public async Task RequestCode_PostsTheNormalizedEmail_AndReturnsTheRequestId()
        {
            Stub stub = Stub.Json(HttpStatusCode.Accepted, "{\"status\":\"sent_if_eligible\",\"request\":\"req-123\"}");
            string? id = await TestLicenses.Client(stub).RequestCodeAsync("  Jane@Example.COM ");

            Assert.Equal("req-123", id);
            (HttpRequestMessage request, string body) = Assert.Single(stub.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(LicenseClient.BaseUrl + "request-code", request.RequestUri!.ToString());
            Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType);
            Assert.StartsWith("PhilterDesktop/", request.Headers.UserAgent.ToString());
            using JsonDocument json = JsonDocument.Parse(body);
            Assert.Equal("jane@example.com", json.RootElement.GetProperty("email").GetString());
            Assert.Single(json.RootElement.EnumerateObject()); // nothing but the email is sent
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "{\"request\":\"x\"}")]          // only 202 is the answer
        [InlineData(HttpStatusCode.Accepted, "{\"status\":\"sent_if_eligible\"}")]
        [InlineData(HttpStatusCode.Accepted, "{\"request\":\"\"}")]
        [InlineData(HttpStatusCode.Accepted, "<html>portal</html>")]
        [InlineData(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable\"}")]
        [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"bad_request\"}")]
        public async Task RequestCode_WithoutARequestId_IsNull(HttpStatusCode status, string body) =>
            Assert.Null(await TestLicenses.Client(Stub.Json(status, body)).RequestCodeAsync("jane@example.com"));

        [Fact]
        public async Task RequestCode_Offline_IsNull() =>
            Assert.Null(await TestLicenses.Client(Stub.Throws(new HttpRequestException("offline"))).RequestCodeAsync("jane@example.com"));

        // --- token -------------------------------------------------------------------------------

        [Fact]
        public async Task Exchange_SendsEmailCodeAndRequest()
        {
            Stub stub = Stub.Json(HttpStatusCode.OK, "{\"licensed\":true,\"token\":\"abc\"}");
            await TestLicenses.Client(stub).ExchangeCodeAsync(" Jane@Example.com", " 123456 ", "req-1");

            (HttpRequestMessage request, string body) = Assert.Single(stub.Requests);
            Assert.Equal(LicenseClient.BaseUrl + "token", request.RequestUri!.ToString());
            using JsonDocument json = JsonDocument.Parse(body);
            Assert.Equal("jane@example.com", json.RootElement.GetProperty("email").GetString());
            Assert.Equal("123456", json.RootElement.GetProperty("code").GetString());
            Assert.Equal("req-1", json.RootElement.GetProperty("request").GetString());
        }

        [Theory]
        [InlineData(HttpStatusCode.OK, "{\"licensed\":true,\"token\":\"tok\"}", LicenseOutcome.Licensed)]
        [InlineData(HttpStatusCode.OK, "{\"licensed\":false}", LicenseOutcome.NotLicensed)]
        [InlineData(HttpStatusCode.OK, "{\"licensed\":true}", LicenseOutcome.Unavailable)]       // licensed but no token
        [InlineData(HttpStatusCode.OK, "{\"licensed\":\"yes\"}", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.OK, "{}", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.OK, "<html>Sign in to Wi-Fi</html>", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"invalid_code\"}", LicenseOutcome.InvalidCode)]
        [InlineData(HttpStatusCode.BadRequest, "{\"error\":\"bad_request\"}", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.TooManyRequests, "{\"error\":\"rate_limited\"}", LicenseOutcome.RateLimited)]
        [InlineData(HttpStatusCode.ServiceUnavailable, "{\"error\":\"unavailable\"}", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.UnsupportedMediaType, "{\"error\":\"unsupported_media_type\"}", LicenseOutcome.Unavailable)]
        [InlineData(HttpStatusCode.InternalServerError, "", LicenseOutcome.Unavailable)]
        public async Task Exchange_MapsEachResponse(HttpStatusCode status, string body, LicenseOutcome expected)
        {
            LicenseResult result = await TestLicenses.Client(Stub.Json(status, body)).ExchangeCodeAsync("a@b.co", "1", "r");
            Assert.Equal(expected, result.Outcome);
            Assert.Equal(expected == LicenseOutcome.Licensed ? "tok" : null, result.Token);
        }

        [Fact]
        public async Task Exchange_Offline_IsUnavailable() =>
            Assert.Equal(LicenseOutcome.Unavailable,
                (await TestLicenses.Client(Stub.Throws(new HttpRequestException("no route"))).ExchangeCodeAsync("a@b.co", "1", "r")).Outcome);

        [Fact]
        public async Task Exchange_TimesOutQuickly()
        {
            var hang = new Stub(async (_, _, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var clock = Stopwatch.StartNew();
            LicenseResult result = await TestLicenses.Client(hang).ExchangeCodeAsync("a@b.co", "1", "r");
            Assert.Equal(LicenseOutcome.Unavailable, result.Outcome);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
        }

        [Fact]
        public async Task OversizedResponse_IsUnavailable()
        {
            string huge = "{\"licensed\":true,\"token\":\"" + new string('x', UpdateChecker.MaxManifestBytes) + "\"}";
            Assert.Equal(LicenseOutcome.Unavailable, (await TestLicenses.Client(Stub.Json(HttpStatusCode.OK, huge)).ExchangeCodeAsync("a@b.co", "1", "r")).Outcome);
        }

        // --- refresh -----------------------------------------------------------------------------

        [Fact]
        public async Task Refresh_SendsOnlyTheTrimmedToken()
        {
            Stub stub = Stub.Json(HttpStatusCode.OK, "{\"licensed\":true,\"token\":\"new\"}");
            LicenseResult result = await TestLicenses.Client(stub).RefreshAsync("old-token\n");

            Assert.Equal(LicenseOutcome.Licensed, result.Outcome);
            Assert.Equal("new", result.Token);
            (HttpRequestMessage request, string body) = Assert.Single(stub.Requests);
            Assert.Equal(LicenseClient.BaseUrl + "refresh", request.RequestUri!.ToString());
            using JsonDocument json = JsonDocument.Parse(body);
            Assert.Equal("old-token", json.RootElement.GetProperty("token").GetString());
            Assert.Single(json.RootElement.EnumerateObject());
        }

        [Fact]
        public async Task Refresh_InvalidToken_IsReported() =>
            Assert.Equal(LicenseOutcome.InvalidToken,
                (await TestLicenses.Client(Stub.Json(HttpStatusCode.BadRequest, "{\"error\":\"invalid_token\"}")).RefreshAsync("t")).Outcome);

        [Fact]
        public async Task Refresh_NotLicensed_IsReported() =>
            Assert.Equal(LicenseOutcome.NotLicensed,
                (await TestLicenses.Client(Stub.Json(HttpStatusCode.OK, "{\"licensed\":false}")).RefreshAsync("t")).Outcome);

        [Theory]
        [InlineData(" Jane@Example.COM ", "jane@example.com")]
        [InlineData("a@b.co", "a@b.co")]
        public void NormalizeEmail_TrimsAndLowercases(string input, string expected) =>
            Assert.Equal(expected, LicenseClient.NormalizeEmail(input));
    }
}
