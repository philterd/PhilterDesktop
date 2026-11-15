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
using System.Security.Authentication;
using System.Text;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The startup update check must fail quietly whenever philterd.ai can't be reached or answers with
    /// something unexpected: it returns nothing to announce, never throws, and doesn't hang. These drive the
    /// real download code (<see cref="UpdateChecker.FetchAsync(string, HttpMessageHandler?, TimeSpan, CancellationToken)"/>)
    /// through the startup path, with a stubbed network.
    /// </summary>
    public sealed class UpdateCheckFailureTests
    {
        private static readonly Version Current = new(1, 4, 0);
        private static readonly TimeSpan ShortTimeout = TimeSpan.FromMilliseconds(300);

        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _send;
            public HttpRequestMessage? LastRequest { get; private set; }
            public StubHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) => _send = send;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            {
                LastRequest = request;
                return _send(request, ct);
            }
        }

        private static StubHandler Responds(HttpStatusCode status, string body, string mediaType = "application/json") =>
            new((_, _) => Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) }));

        private static StubHandler Throws(Exception ex) => new((_, _) => Task.FromException<HttpResponseMessage>(ex));

        // The startup check, wired to the real fetch code over the given handler.
        private static async Task<(UpdateChecker.UpdateManifest? Result, List<Exception> Failures)> RunStartupCheck(
            HttpMessageHandler handler, TimeSpan? timeout = null, string url = UpdateChecker.ManifestUrl)
        {
            var failures = new List<Exception>();
            UpdateChecker.UpdateManifest? result = await UpdateChecker.RunAutoCheckAsync(
                new FakeUpdatePreferences(),
                ct => UpdateChecker.FetchAsync(url, handler, timeout ?? ShortTimeout, ct),
                Current,
                onFailure: failures.Add);
            return (result, failures);
        }

        // --- Can't reach philterd.ai ----------------------------------------------------

        [Fact]
        public async Task NoConnection_IsQuiet()
        {
            var (result, failures) = await RunStartupCheck(Throws(new HttpRequestException("No such host is known.")));
            Assert.Null(result);
            Assert.IsType<HttpRequestException>(Assert.Single(failures));
        }

        [Fact]
        public async Task TlsFailure_IsQuiet()
        {
            // E.g. a proxy that intercepts HTTPS with an untrusted certificate.
            var tls = new HttpRequestException("SSL connection could not be established.", new AuthenticationException("bad cert"));
            var (result, failures) = await RunStartupCheck(Throws(tls));
            Assert.Null(result);
            Assert.Single(failures);
        }

        // A server that never answers: the check gives up at its timeout instead of hanging.
        [Fact]
        public async Task UnresponsiveServer_TimesOutQuietly()
        {
            var hang = new StubHandler(async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(30), ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            });
            var clock = Stopwatch.StartNew();
            var (result, failures) = await RunStartupCheck(hang);
            Assert.Null(result);
            Assert.Single(failures);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
        }

        // A real socket: nothing listens on loopback port 1, so the connection is refused. No external traffic.
        [Fact]
        public async Task ConnectionRefused_OnARealSocket_IsQuiet()
        {
            var clock = Stopwatch.StartNew();
            var failures = new List<Exception>();
            UpdateChecker.UpdateManifest? result = await UpdateChecker.RunAutoCheckAsync(
                new FakeUpdatePreferences(),
                ct => UpdateChecker.FetchAsync("https://127.0.0.1:1/philterdesktop.json", handler: null, TimeSpan.FromSeconds(5), ct),
                Current,
                onFailure: failures.Add);
            Assert.Null(result);
            Assert.Single(failures);
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"took {clock.Elapsed}");
        }

        // --- Reached something, but not the manifest ---------------------------------------

        [Theory]
        [InlineData(HttpStatusCode.NotFound)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.ServiceUnavailable)]
        [InlineData(HttpStatusCode.Forbidden)]
        [InlineData(HttpStatusCode.NoContent)]
        public async Task ErrorStatus_IsQuiet(HttpStatusCode status)
        {
            var (result, failures) = await RunStartupCheck(Responds(status, "{\"version\":\"9.9.9\"}"));
            Assert.Null(result); // never announced, even if the body looks like a newer version
            Assert.Single(failures);
        }

        // A hotel or airport Wi-Fi login page answers every request with HTML.
        [Fact]
        public async Task CaptivePortalPage_IsQuiet()
        {
            var (result, failures) = await RunStartupCheck(
                Responds(HttpStatusCode.OK, "<!doctype html><html><body>Sign in to Wi-Fi</body></html>", "text/html"));
            Assert.Null(result);
            Assert.Single(failures);
        }

        [Theory]
        [InlineData("")]                          // empty body
        [InlineData("{")]                         // truncated
        [InlineData("{\"version\": 140}")]        // wrong type
        [InlineData("[1, 2, 3]")]                 // wrong shape
        public async Task MalformedManifest_IsQuiet(string body)
        {
            var (result, _) = await RunStartupCheck(Responds(HttpStatusCode.OK, body));
            Assert.Null(result);
        }

        // Valid JSON without a usable version is ignored, not announced.
        [Theory]
        [InlineData("{}")]
        [InlineData("null")]
        [InlineData("{\"version\": \"\"}")]
        [InlineData("{\"version\": \"not-a-version\"}")]
        public async Task ManifestWithoutAUsableVersion_IsIgnored(string body)
        {
            var (result, _) = await RunStartupCheck(Responds(HttpStatusCode.OK, body));
            Assert.Null(result);
        }

        // Something far larger than the manifest (a big portal page, a misrouted download) is refused.
        [Fact]
        public async Task OversizedResponse_IsRefusedQuietly()
        {
            string huge = "{\"version\":\"9.9.9\",\"pad\":\"" + new string('x', UpdateChecker.MaxManifestBytes) + "\"}";
            var (result, failures) = await RunStartupCheck(Responds(HttpStatusCode.OK, huge));
            Assert.Null(result);
            Assert.Single(failures);
        }

        // --- Reporting ------------------------------------------------------------------------

        // The failure is reported (for the log) but a faulty reporter can't turn a quiet failure into a crash.
        [Fact]
        public async Task ThrowingFailureReporter_IsContained()
        {
            UpdateChecker.UpdateManifest? result = await UpdateChecker.RunAutoCheckAsync(
                new FakeUpdatePreferences(),
                ct => UpdateChecker.FetchAsync(UpdateChecker.ManifestUrl, Throws(new HttpRequestException("offline")), ShortTimeout, ct),
                Current,
                onFailure: _ => throw new InvalidOperationException("log file locked"));
            Assert.Null(result);
        }

        [Fact]
        public async Task Success_ReportsNoFailure_AndSendsTheVersion()
        {
            var handler = Responds(HttpStatusCode.OK, "{\"version\":\"1.5.0\",\"eulaVersion\":\"2.0\"}");
            var (result, failures) = await RunStartupCheck(handler);
            Assert.Equal("1.5.0", result?.Version);
            Assert.Empty(failures);
            Assert.Equal(UpdateChecker.ManifestUrl, handler.LastRequest!.RequestUri!.ToString());
            Assert.StartsWith("PhilterDesktop/", handler.LastRequest.Headers.UserAgent.ToString());
        }

        // Turned off: nothing is attempted, so nothing can fail.
        [Fact]
        public async Task TurnedOff_NeverTouchesTheNetwork()
        {
            var handler = Throws(new HttpRequestException("should not be called"));
            var failures = new List<Exception>();
            UpdateChecker.UpdateManifest? result = await UpdateChecker.RunAutoCheckAsync(
                new FakeUpdatePreferences { AutoCheckEnabled = false },
                ct => UpdateChecker.FetchAsync(UpdateChecker.ManifestUrl, handler, ShortTimeout, ct),
                Current,
                onFailure: failures.Add);
            Assert.Null(result);
            Assert.Empty(failures);
            Assert.Null(handler.LastRequest);
        }

        [Fact]
        public void Timeout_IsShort()
        {
            Assert.True(UpdateChecker.FetchTimeout <= TimeSpan.FromSeconds(30));
            Assert.True(UpdateChecker.FetchTimeout > TimeSpan.Zero);
        }
    }
}
