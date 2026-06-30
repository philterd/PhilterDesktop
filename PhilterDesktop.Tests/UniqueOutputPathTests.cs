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

using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// <see cref="RedactionService.GetUniqueOutputPath"/> prevents two same-named source files (from
    /// different folders) silently overwriting each other in a shared output folder.
    /// </summary>
    public sealed class UniqueOutputPathTests : IDisposable
    {
        private readonly string _dir;

        public UniqueOutputPathTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-uniq-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private string Path_(string name) => Path.Combine(_dir, name);

        private void Touch(string name) => File.WriteAllText(Path_(name), "x");

        [Fact]
        public void NoCollision_ReturnsTheDesiredPath()
        {
            string desired = Path_("invoice_redacted-draft.pdf");

            Assert.Equal(desired, RedactionService.GetUniqueOutputPath(desired));
        }

        [Fact]
        public void Collision_AppendsCounterTwo()
        {
            Touch("invoice_redacted-draft.pdf");

            string unique = RedactionService.GetUniqueOutputPath(Path_("invoice_redacted-draft.pdf"));

            Assert.Equal(Path_("invoice_redacted-draft (2).pdf"), unique);
        }

        [Fact]
        public void MultipleCollisions_KeepIncrementingTheCounter()
        {
            Touch("invoice_redacted-draft.pdf");
            Touch("invoice_redacted-draft (2).pdf");
            Touch("invoice_redacted-draft (3).pdf");

            string unique = RedactionService.GetUniqueOutputPath(Path_("invoice_redacted-draft.pdf"));

            Assert.Equal(Path_("invoice_redacted-draft (4).pdf"), unique);
        }

        [Fact]
        public void PreservesDirectoryAndExtension()
        {
            Touch("report_redacted-draft.docx");

            string unique = RedactionService.GetUniqueOutputPath(Path_("report_redacted-draft.docx"));

            Assert.Equal(_dir, Path.GetDirectoryName(unique));
            Assert.Equal(".docx", Path.GetExtension(unique));
        }

        [Fact]
        public void HandlesFileWithNoExtension()
        {
            Touch("notes_redacted-draft");

            string unique = RedactionService.GetUniqueOutputPath(Path_("notes_redacted-draft"));

            Assert.Equal(Path_("notes_redacted-draft (2)"), unique);
        }

        [Fact]
        public void NameContainingDots_OnlyTheExtensionIsSeparated()
        {
            Touch("2024.q1.summary_redacted-draft.csv");

            string unique = RedactionService.GetUniqueOutputPath(Path_("2024.q1.summary_redacted-draft.csv"));

            Assert.Equal(Path_("2024.q1.summary_redacted-draft (2).csv"), unique);
        }

        [Fact]
        public void GapInSequence_ReturnsFirstFreeSlot()
        {
            // base and (3) exist but (2) is free — it must reuse (2), not jump to (4).
            Touch("invoice_redacted-draft.pdf");
            Touch("invoice_redacted-draft (3).pdf");

            string unique = RedactionService.GetUniqueOutputPath(Path_("invoice_redacted-draft.pdf"));

            Assert.Equal(Path_("invoice_redacted-draft (2).pdf"), unique);
        }

        [Fact]
        public void NonexistentDirectory_ReturnsDesiredUnchanged_DoesNotThrow()
        {
            // Unhappy path: the target folder doesn't exist yet — nothing is there to collide with, and
            // the method must not throw (it only checks existence; the caller creates/writes the file).
            string desired = Path.Combine(_dir, "does-not-exist", "report_redacted-draft.pdf");

            Assert.Equal(desired, RedactionService.GetUniqueOutputPath(desired));
        }

        [Fact]
        public void NameAlreadyEndingInCounter_GetsAnotherCounter()
        {
            Touch("report (2).pdf");

            string unique = RedactionService.GetUniqueOutputPath(Path_("report (2).pdf"));

            Assert.Equal(Path_("report (2) (2).pdf"), unique);
        }

        [Fact]
        public void ReRedactingSameFile_InOriginalLocation_GetsNumberedCopy()
        {
            // Re-redacting a document whose redacted output already exists yields a new numbered copy
            // rather than overwriting the previous redaction.
            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            string source = Path_("memo.txt");
            File.WriteAllText(source, "content");

            string firstOutput = RedactionService.GetOutputPath(source, settings);
            File.WriteAllText(firstOutput, "first redaction");
            string secondOutput = RedactionService.GetUniqueOutputPath(RedactionService.GetOutputPath(source, settings));

            Assert.NotEqual(firstOutput, secondOutput);
            Assert.EndsWith("memo_redacted-draft (2).txt", secondOutput);
            Assert.Equal("first redaction", File.ReadAllText(firstOutput)); // previous output preserved
        }

        [Fact]
        public void ManySequentialCollisions_KeepProducingDistinctPaths()
        {
            // Happy-path-at-scale: simulate ten same-named sources written one after another.
            string desired = Path_("dup_redacted-draft.csv");
            var produced = new HashSet<string>();
            for (int i = 0; i < 10; i++)
            {
                string unique = RedactionService.GetUniqueOutputPath(desired);
                Assert.True(produced.Add(unique), "each output path must be distinct");
                File.WriteAllText(unique, "x"); // "write" it so the next call sees it as taken
            }

            Assert.Equal(10, produced.Count);
        }

        [Fact]
        public void TwoSameNamedSources_ProduceDistinctOutputs()
        {
            // The actual scenario: two source files named "invoice.pdf" from different folders resolve to
            // the same output name; writing the first then asking for the second's path must differ.
            var settings = new SettingsEntity { OutputToOriginalLocation = false, CustomOutputFolder = _dir };

            string first = RedactionService.GetUniqueOutputPath(RedactionService.GetOutputPath(@"C:\a\invoice.pdf", settings));
            File.WriteAllText(first, "redacted A");
            string second = RedactionService.GetUniqueOutputPath(RedactionService.GetOutputPath(@"C:\b\invoice.pdf", settings));

            Assert.NotEqual(first, second);
            Assert.False(File.Exists(second), "the second path must be free");
            Assert.EndsWith("invoice_redacted-draft.pdf", first);
            Assert.EndsWith("invoice_redacted-draft (2).pdf", second);
        }
    }
}
