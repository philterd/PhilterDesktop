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
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Unit tests for the "Redact Folder" file-selection core: which files are picked up, recursion,
    /// skipping prior redaction outputs, and the per-type summary. All side-effect free over a temp dir.
    /// </summary>
    public sealed class FolderEnumeratorTests : IDisposable
    {
        private readonly string _root;

        public FolderEnumeratorTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "philter-fe-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        }

        private string Touch(string relativePath)
        {
            string full = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, "x");
            return full;
        }

        [Fact]
        public void EnumerateRedactable_ReturnsOnlySupportedTypes()
        {
            Touch("a.txt");
            Touch("b.docx");
            Touch("c.pdf");
            Touch("ignore.png");   // unsupported
            Touch("notes.xyz");    // unsupported

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: false);

            Assert.Equal(3, files.Count);
            Assert.All(files, f => Assert.True(RedactionServiceIsSupported(f)));
            Assert.DoesNotContain(files, f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void EnumerateRedactable_NonRecursive_IgnoresSubfolders()
        {
            Touch("top.txt");
            Touch("sub/nested.txt");

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: false);

            Assert.Single(files);
            Assert.EndsWith("top.txt", files[0]);
        }

        [Fact]
        public void EnumerateRedactable_Recursive_IncludesSubfolders()
        {
            Touch("top.txt");
            Touch("sub/nested.docx");
            Touch("sub/deeper/leaf.pdf");

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: true);

            Assert.Equal(3, files.Count);
            Assert.Contains(files, f => f.EndsWith("nested.docx", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(files, f => f.EndsWith("leaf.pdf", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void EnumerateRedactable_SkipsPriorRedactionOutputs()
        {
            Touch("report.txt");
            Touch("report_redacted-draft.txt"); // default suffix — a prior output

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: false);

            Assert.Single(files);
            Assert.EndsWith("report.txt", files[0]);
        }

        [Fact]
        public void EnumerateRedactable_SkipsPriorOutputs_WithCustomSuffix()
        {
            Touch("memo.txt");
            Touch("memo_clean.txt"); // a prior output under a custom suffix

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: false, redactedSuffix: "_clean");

            Assert.Single(files);
            Assert.EndsWith("memo.txt", files[0]);
        }

        [Fact]
        public void EnumerateRedactable_MissingFolder_ReturnsEmpty()
        {
            string missing = Path.Combine(_root, "does-not-exist");
            Assert.Empty(FolderEnumerator.EnumerateRedactable(missing, recursive: true));
        }

        [Fact]
        public void EnumerateRedactable_EmptyOrWhitespaceRoot_ReturnsEmpty()
        {
            Assert.Empty(FolderEnumerator.EnumerateRedactable("", recursive: true));
            Assert.Empty(FolderEnumerator.EnumerateRedactable("   ", recursive: true));
        }

        [Fact]
        public void EnumerateRedactable_ResultsAreSortedAndDeterministic()
        {
            Touch("c.txt");
            Touch("a.txt");
            Touch("b.txt");

            List<string> files = FolderEnumerator.EnumerateRedactable(_root, recursive: false);

            List<string> sorted = files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();
            Assert.Equal(sorted, files);
        }

        [Fact]
        public void SummarizeByType_CountsByLowercasedExtension()
        {
            var files = new[]
            {
                @"C:\a\one.PDF", @"C:\a\two.pdf", @"C:\a\three.txt", @"C:\a\note.docx", @"C:\a\b.docx"
            };

            IReadOnlyList<(string Extension, int Count)> summary = FolderEnumerator.SummarizeByType(files);

            Assert.Equal((".docx", 2), summary.Single(s => s.Extension == ".docx"));
            Assert.Equal((".pdf", 2), summary.Single(s => s.Extension == ".pdf"));
            Assert.Equal((".txt", 1), summary.Single(s => s.Extension == ".txt"));
        }

        [Theory]
        [InlineData("file_redacted-draft.txt", "_redacted-draft", true)]
        [InlineData("file.txt", "_redacted-draft", false)]
        [InlineData("a_redacted-draft.pdf", "_redacted-draft", true)]
        [InlineData("anything.txt", "", false)] // empty suffix never matches
        public void LooksLikeRedactionOutput_MatchesSuffixBeforeExtension(string name, string suffix, bool expected)
        {
            Assert.Equal(expected, FolderEnumerator.LooksLikeRedactionOutput(name, suffix));
        }

        [SkippableFact]
        public void Recursive_DoesNotFollowDirectoryJunctions()
        {
            Touch("a.txt");
            Touch("sub/b.txt");

            // A file reachable only by following a junction out of the tree.
            string outside = Path.Combine(Path.GetTempPath(), "philter-fe-out-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(outside);
            File.WriteAllText(Path.Combine(outside, "secret.txt"), "x");

            string link = Path.Combine(_root, "link");
            Skip.IfNot(TryCreateJunction(link, outside), "could not create a directory junction in this environment");

            try
            {
                // "Redact Folder" walk
                List<string> redactable = FolderEnumerator.EnumerateRedactable(_root, recursive: true);
                Assert.Contains(redactable, f => f.EndsWith("a.txt", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(redactable, f => f.EndsWith("b.txt", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(redactable, f => f.EndsWith("secret.txt", StringComparison.OrdinalIgnoreCase));

                // Watched-folder recursive walk (the security-cited path)
                List<string> watched = FolderWatcherService.EnumerateRecursive(_root).ToList();
                Assert.Contains(watched, f => f.EndsWith("a.txt", StringComparison.OrdinalIgnoreCase));
                Assert.Contains(watched, f => f.EndsWith("b.txt", StringComparison.OrdinalIgnoreCase));
                Assert.DoesNotContain(watched, f => f.EndsWith("secret.txt", StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                try { Directory.Delete(link); } catch { /* remove the junction itself, not its target */ }
                try { Directory.Delete(outside, recursive: true); } catch { /* best effort */ }
            }
        }

        // Creates a directory junction (mklink /J) — works without elevation, unlike symbolic links.
        // Returns false if the environment won't allow it, so the test can skip rather than fail.
        private static bool TryCreateJunction(string link, string target)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", $"/c mklink /J \"{link}\" \"{target}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using Process? p = Process.Start(psi);
                if (p is null)
                {
                    return false;
                }
                p.WaitForExit(5000);
                return p.ExitCode == 0 && Directory.Exists(link);
            }
            catch
            {
                return false;
            }
        }

        // Mirror of RedactionService.IsSupported used as a local assertion helper to avoid coupling the
        // test to its internal accessibility quirks.
        private static bool RedactionServiceIsSupported(string path) =>
            new[] { ".txt", ".docx", ".pdf", ".rtf", ".xlsx", ".csv", ".eml", ".msg" }
                .Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
    }
}
