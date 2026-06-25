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

using System.Net.Http;
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

            [JsonPropertyName("downloadUrl")]
            public string? DownloadUrl { get; set; }

            [JsonPropertyName("sha256")]
            public string? Sha256 { get; set; }
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

        /// <summary>
        /// A non-clobbering path on the user's Desktop for the installer named in <paramref name="url"/>
        /// (e.g. <c>philter_desktop_setup.exe</c>), appending " (1)", " (2)", … if the file exists.
        /// </summary>
        public static string GetDesktopDownloadPath(string url)
        {
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            string fileName;
            try { fileName = Path.GetFileName(new Uri(url).LocalPath); }
            catch { fileName = string.Empty; }
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "philter_desktop_setup.exe";
            }

            string path = Path.Combine(desktop, fileName);
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            string ext = Path.GetExtension(fileName);
            for (int n = 1; File.Exists(path); n++)
            {
                path = Path.Combine(desktop, $"{baseName} ({n}){ext}");
            }
            return path;
        }

        /// <summary>
        /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/>, streaming to disk and
        /// reporting (bytesDownloaded, totalBytes-or-null) as it goes. Honors cancellation.
        /// </summary>
        public static async Task DownloadAsync(
            string url,
            string destinationPath,
            IProgress<(long Downloaded, long? Total)>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // No overall timeout: a large installer over a slow link can take a while; cancellation
            // (the Cancel button) is the way to stop it.
            using var http = new HttpClient { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PhilterDesktop");

            using HttpResponseMessage response =
                await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            await using Stream source = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var destination = new FileStream(
                destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long downloaded = 0;
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                downloaded += read;
                progress?.Report((downloaded, total));
            }
        }

        /// <summary>Lowercase hex SHA-256 of a file's contents.</summary>
        public static string ComputeSha256(string filePath)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            using FileStream stream = File.OpenRead(filePath);
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        /// <summary>
        /// True if the file's SHA-256 equals <paramref name="expectedSha256"/> (case-insensitive).
        /// Returns false if no expected hash is supplied.
        /// </summary>
        public static bool HashMatches(string filePath, string? expectedSha256)
        {
            if (string.IsNullOrWhiteSpace(expectedSha256))
            {
                return false;
            }
            return string.Equals(ComputeSha256(filePath), expectedSha256.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
