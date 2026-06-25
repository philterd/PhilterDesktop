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
    /// compares its version against this build's version. It does <b>not</b> download or install
    /// anything — it only tells the user whether a newer version exists (and where to get it).
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

            // The manifest may carry a downloadUrl, but the app intentionally does not use it:
            // subscribers download the official build from their account page (see UpdateAvailableForm).
            [JsonPropertyName("downloadUrl")]
            public string? DownloadUrl { get; set; }
        }

        /// <summary>Downloads and parses the update manifest. Throws on network/parse failure.</summary>
        public static async Task<UpdateManifest?> FetchAsync(string url = ManifestUrl, CancellationToken cancellationToken = default)
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PhilterDesktop");

            await using Stream stream = await http.GetStreamAsync(url, cancellationToken);
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
