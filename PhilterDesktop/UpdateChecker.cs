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
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PhilterDesktop
{
    /// <summary>
    /// A lightweight update check: downloads a small JSON manifest published at philterd.ai and
    /// compares its version against this build's version, on demand (Help menu) or automatically at
    /// startup. It does <b>not</b> download or install anything; it only tells the user whether a newer
    /// version exists (and where to get it).
    /// </summary>
    public static class UpdateChecker
    {
        /// <summary>The manifest describing the latest published release.</summary>
        public const string ManifestUrl = "https://philterd.ai/philterdesktop.json";

        public sealed class UpdateManifest
        {
            [JsonPropertyName("version")]
            public string? Version { get; set; }

            [JsonPropertyName("releaseDate")]
            public string? ReleaseDate { get; set; }

            // The manifest may carry a downloadUrl, but the app intentionally does not use it: the update
            // dialog always links to the public download page (see UpdateAvailableForm).
            [JsonPropertyName("downloadUrl")]
            public string? DownloadUrl { get; set; }
        }

        /// <summary>
        /// The User-Agent sent with the check, e.g. "PhilterDesktop/1.4.0". The app version is the only
        /// information it carries; philterd.ai's access log records it to count versions in use.
        /// </summary>
        internal static string UserAgent(Version version) =>
            $"PhilterDesktop/{version.Major}.{version.Minor}.{Math.Max(version.Build, 0)}";

        /// <summary>What started an automatic check.</summary>
        internal enum Trigger
        {
            /// <summary>The main window appeared for the first time this run.</summary>
            WindowOpened,

            /// <summary>The background timer, while the app runs (often from the tray, for watched folders).</summary>
            Background
        }

        /// <summary>How long after start the first background check waits, so a sign-in start has a network.</summary>
        internal static readonly TimeSpan BackgroundFirstDelay = TimeSpan.FromMinutes(5);

        /// <summary>How often the background timer looks again; a failed check is retried at this pace.</summary>
        internal static readonly TimeSpan BackgroundRetryInterval = TimeSpan.FromHours(1);

        /// <summary>A background check contacts philterd.ai at most this often after a successful check.</summary>
        internal static readonly TimeSpan BackgroundCheckInterval = TimeSpan.FromHours(24);

        /// <summary>
        /// Whether to check now. Opening the window checks unless a check already succeeded this run; the
        /// background timer checks once a day after a success (and keeps retrying hourly until one succeeds).
        /// A last success in the future (clock moved back) counts as due.
        /// </summary>
        internal static bool ShouldCheck(Trigger trigger, DateTime? lastSuccessUtc, DateTime nowUtc) => trigger switch
        {
            Trigger.WindowOpened => lastSuccessUtc is null,
            _ => lastSuccessUtc is not DateTime last || last > nowUtc || nowUtc - last >= BackgroundCheckInterval
        };

        /// <summary>A tray notification is shown at most once per run for a given version.</summary>
        internal static bool ShouldShowTrayNotification(string? version, string? alreadyShownThisRun) =>
            !string.IsNullOrWhiteSpace(version) &&
            !string.Equals(version.Trim(), alreadyShownThisRun?.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>The tray notification announcing a new version while the window isn't showing.</summary>
        internal static (string Title, string Text) TrayNotification(string version) =>
            ($"Philter Desktop {version.Trim()} is available",
             "Click here to see what's new and where to download it.");

        /// <summary>Whether to announce <paramref name="publishedVersion"/>: it is newer and not already announced.</summary>
        public static bool ShouldNotify(string? publishedVersion, Version current, string? lastNotifiedVersion) =>
            IsNewer(publishedVersion, current) == true &&
            !string.Equals(publishedVersion?.Trim(), lastNotifiedVersion?.Trim(), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The automatic check: if enabled, fetches the manifest and returns it when it announces a
        /// version the user hasn't been told about. Returns null otherwise, including on any failure, so an
        /// offline or blocked machine sees nothing.
        /// </summary>
        internal static async Task<UpdateManifest?> RunAutoCheckAsync(
            IUpdatePreferences prefs,
            Func<CancellationToken, Task<UpdateManifest?>> fetch,
            Version current,
            CancellationToken cancellationToken = default,
            Action<Exception>? onFailure = null,
            Action? onReached = null)
        {
            if (!UpdatePreferences.IsAutoCheckOn(prefs))
            {
                return null;
            }

            UpdateManifest? manifest;
            try
            {
                manifest = await fetch(cancellationToken);
            }
            catch (Exception ex)
            {
                // Offline, blocked, timed out, or an unexpected response: say nothing to the user.
                try { onFailure?.Invoke(ex); } catch { /* reporting must not break the check */ }
                return null;
            }

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                return null;
            }

            try { onReached?.Invoke(); } catch { /* bookkeeping must not break the check */ }
            return ShouldNotify(manifest.Version, current, prefs.LastNotifiedVersion) ? manifest : null;
        }

        /// <summary>How long a check may take before it gives up.</summary>
        internal static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(15);

        /// <summary>The manifest is a few hundred bytes; anything far larger (e.g. a captive portal's page) is refused.</summary>
        internal const int MaxManifestBytes = 64 * 1024;

        /// <summary>Downloads and parses the update manifest. Throws on network/parse failure.</summary>
        public static Task<UpdateManifest?> FetchAsync(string url = ManifestUrl, CancellationToken cancellationToken = default) =>
            FetchAsync(url, handler: null, FetchTimeout, cancellationToken);

        /// <summary>As <see cref="FetchAsync(string, CancellationToken)"/>, with a handler and timeout tests can supply.</summary>
        internal static async Task<UpdateManifest?> FetchAsync(
            string url, HttpMessageHandler? handler, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            // The manifest's integrity in transit relies entirely on TLS, so refuse anything that isn't
            // an absolute HTTPS URL. This keeps a typo or a future edit from silently downgrading to
            // plaintext HTTP, where the response could be tampered with en route.
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
                !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("The update manifest URL must be an absolute HTTPS URL.", nameof(url));
            }

            using var http = handler is null ? new HttpClient() : new HttpClient(handler, disposeHandler: false);
            http.Timeout = timeout;
            http.MaxResponseContentBufferSize = MaxManifestBytes;
            http.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent(CurrentVersion()));

            using HttpResponseMessage response = await http.GetAsync(url, cancellationToken);
            if ((int)response.StatusCode != 200)
            {
                throw new HttpRequestException($"The update server returned HTTP status {(int)response.StatusCode}.");
            }

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<UpdateManifest>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
        }

        /// <summary>This build's version, normalized to Major.Minor.Build.</summary>
        public static Version CurrentVersion()
        {
            Version v = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0);
            return Normalize(v);
        }

        /// <summary>
        /// Compares the manifest's version string against <paramref name="current"/>.
        /// Returns <c>true</c> if the published version is newer, <c>false</c> if it is the same or
        /// older, and <c>null</c> if the published version can't be understood.
        /// </summary>
        public static bool? IsNewer(string? publishedVersion, Version current)
        {
            if (!Version.TryParse(publishedVersion, out Version? published))
            {
                return null;
            }
            return Normalize(published) > Normalize(current);
        }

        // Compare on Major.Minor.Build only; ignore the (often unset) revision component so that, e.g.,
        // a manifest "1.0.0" doesn't read as older than an assembly version of 1.0.0.0.
        private static Version Normalize(Version v) =>
            new(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0));
    }
}
