using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests for <see cref="LicenseConfig.ResolveKey"/> — the pure precedence/parse
    /// logic behind license-key resolution.
    /// </summary>
    public sealed class LicenseConfigTests : IDisposable
    {
        private readonly string _tempDir;

        public LicenseConfigTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-license-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private string WriteFile(string name, string contents)
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, contents);
            return path;
        }

        [Fact]
        public void ResolveKey_ReadsKeyFromFile()
        {
            string path = WriteFile("a.json", """{ "XceedLicenseKey": "ABC-123" }""");

            string? key = LicenseConfig.ResolveKey(new[] { path }, envValue: null);

            Assert.Equal("ABC-123", key);
        }

        [Fact]
        public void ResolveKey_TrimsWhitespaceInKey()
        {
            string path = WriteFile("a.json", """{ "XceedLicenseKey": "  ABC-123  " }""");

            string? key = LicenseConfig.ResolveKey(new[] { path }, envValue: null);

            Assert.Equal("ABC-123", key);
        }

        [Fact]
        public void ResolveKey_FileTakesPrecedenceOverEnv()
        {
            string path = WriteFile("a.json", """{ "XceedLicenseKey": "FROM-FILE" }""");

            string? key = LicenseConfig.ResolveKey(new[] { path }, envValue: "FROM-ENV");

            Assert.Equal("FROM-FILE", key);
        }

        [Fact]
        public void ResolveKey_FirstExistingValidFileWins()
        {
            string first = WriteFile("first.json", """{ "XceedLicenseKey": "FIRST" }""");
            string second = WriteFile("second.json", """{ "XceedLicenseKey": "SECOND" }""");

            string? key = LicenseConfig.ResolveKey(new[] { first, second }, envValue: null);

            Assert.Equal("FIRST", key);
        }

        [Fact]
        public void ResolveKey_SkipsMissingFiles()
        {
            string missing = Path.Combine(_tempDir, "does-not-exist.json");
            string present = WriteFile("present.json", """{ "XceedLicenseKey": "PRESENT" }""");

            string? key = LicenseConfig.ResolveKey(new[] { missing, present }, envValue: null);

            Assert.Equal("PRESENT", key);
        }

        [Fact]
        public void ResolveKey_SkipsMalformedJsonAndFallsThrough()
        {
            string bad = WriteFile("bad.json", "{ this is not valid json ");
            string good = WriteFile("good.json", """{ "XceedLicenseKey": "GOOD" }""");

            string? key = LicenseConfig.ResolveKey(new[] { bad, good }, envValue: null);

            Assert.Equal("GOOD", key);
        }

        [Fact]
        public void ResolveKey_EmptyKeyInFileFallsThroughToEnv()
        {
            string path = WriteFile("a.json", """{ "XceedLicenseKey": "" }""");

            string? key = LicenseConfig.ResolveKey(new[] { path }, envValue: "FROM-ENV");

            Assert.Equal("FROM-ENV", key);
        }

        [Fact]
        public void ResolveKey_UsesEnvWhenNoFiles()
        {
            string? key = LicenseConfig.ResolveKey(Array.Empty<string>(), envValue: "  ENV-KEY  ");

            Assert.Equal("ENV-KEY", key);
        }

        [Fact]
        public void ResolveKey_ReturnsNullWhenNothingConfigured()
        {
            string? key = LicenseConfig.ResolveKey(Array.Empty<string>(), envValue: null);

            Assert.Null(key);
        }

        [Fact]
        public void ResolveKey_ReturnsNullWhenEnvIsWhitespace()
        {
            string? key = LicenseConfig.ResolveKey(Array.Empty<string>(), envValue: "   ");

            Assert.Null(key);
        }
    }
}
