using System.Text.Json;

namespace PhilterDesktop
{
    /// <summary>
    /// Reads third-party license keys from an untracked config file so they never
    /// live in source control. The file is plain JSON, e.g.:
    ///
    ///   { "XceedLicenseKey": "your-key-here" }
    ///
    /// It is looked for next to the executable first, then in the per-user app data
    /// folder, and finally falls back to the XCEED_LICENSE_KEY environment variable.
    /// </summary>
    internal static class LicenseConfig
    {
        private const string FileName = "xceed-license.json";

        /// <summary>Returns the Xceed license key, or null if none is configured.</summary>
        public static string? GetXceedLicenseKey()
        {
            return ResolveKey(CandidatePaths(), Environment.GetEnvironmentVariable("XCEED_LICENSE_KEY"));
        }

        /// <summary>
        /// Pure resolution logic: returns the first valid key from the candidate files
        /// (in order), otherwise the environment value, otherwise null. Keys are trimmed;
        /// missing files and malformed JSON are skipped. Exposed for unit testing.
        /// </summary>
        internal static string? ResolveKey(IEnumerable<string> candidatePaths, string? envValue)
        {
            foreach (string path in candidatePaths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                try
                {
                    using FileStream stream = File.OpenRead(path);
                    LicenseFile? file = JsonSerializer.Deserialize<LicenseFile>(stream);
                    if (!string.IsNullOrWhiteSpace(file?.XceedLicenseKey))
                    {
                        return file!.XceedLicenseKey!.Trim();
                    }
                }
                catch
                {
                    // Ignore a malformed file and keep looking.
                }
            }

            return string.IsNullOrWhiteSpace(envValue) ? null : envValue.Trim();
        }

        private static IEnumerable<string> CandidatePaths()
        {
            yield return Path.Combine(AppContext.BaseDirectory, FileName);
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PhilterDesktop", FileName);
        }

        private sealed class LicenseFile
        {
            public string? XceedLicenseKey { get; set; }
        }
    }
}
