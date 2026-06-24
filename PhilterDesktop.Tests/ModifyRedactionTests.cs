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

using Phileas.Policy;
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Covers span capture and re-application (the engine behind Modify Redaction), using plain text
    /// so the assertions are deterministic and don't need Xceed/PDF.
    /// </summary>
    public sealed class ModifyRedactionTests : IDisposable
    {
        private readonly string _dir;

        public ModifyRedactionTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-modify-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { /* best effort */ }
        }

        private static PhileasPolicy EmailSsnPolicy() => new()
        {
            Name = "p",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        private string Write(string name, string content)
        {
            string path = Path.Combine(_dir, name);
            File.WriteAllText(path, content);
            return path;
        }

        [Fact]
        public async Task RedactFile_Text_CapturesSpans()
        {
            string input = Write("in.txt", "Email a@b.com and ssn 123-45-6789.");
            string output = Path.Combine(_dir, "out.txt");

            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, output, EmailSsnPolicy(), "ctx");

            Assert.True(spans.Count >= 2);
            Assert.Contains(spans, s => s.Text == "a@b.com");
            Assert.Contains(spans, s => s.Text == "123-45-6789");
            Assert.DoesNotContain("a@b.com", await File.ReadAllTextAsync(output));
        }

        [Fact]
        public async Task ApplySpans_Text_RemovingSpan_LeavesThatTextIntact()
        {
            string input = Write("in.txt", "Email a@b.com and ssn 123-45-6789.");
            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, Path.Combine(_dir, "v1.txt"), EmailSsnPolicy(), "ctx");

            // Drop the email span; keep the SSN.
            List<RedactionSpanEntity> kept = spans.Where(s => !s.Text.Contains('@')).ToList();
            string output = Path.Combine(_dir, "v2.txt");
            await RedactionService.ApplySpansAsync(input, output, ".txt", highlight: false, kept);

            string text = await File.ReadAllTextAsync(output);
            Assert.Contains("a@b.com", text);          // excluded -> left intact
            Assert.DoesNotContain("123-45-6789", text); // still redacted
        }

        [Fact]
        public async Task ApplySpans_Text_EditedReplacement_IsUsed()
        {
            string input = Write("in.txt", "Email a@b.com here.");
            List<RedactionSpanEntity> spans = await RedactionService.RedactFileAsync(input, Path.Combine(_dir, "v1.txt"), EmailSsnPolicy(), "ctx");

            foreach (RedactionSpanEntity s in spans)
            {
                s.Replacement = "[EMAIL]";
            }
            string output = Path.Combine(_dir, "v2.txt");
            await RedactionService.ApplySpansAsync(input, output, ".txt", highlight: false, spans);

            string text = await File.ReadAllTextAsync(output);
            Assert.Contains("[EMAIL]", text);
            Assert.DoesNotContain("a@b.com", text);
        }

        [Fact]
        public async Task ApplySpans_Text_AddedSpan_IsRedactedByPosition()
        {
            const string content = "Project Apollo is secret.";
            string input = Write("in.txt", content);
            string output = Path.Combine(_dir, "v2.txt");

            // "Apollo" occupies characters [8, 14) — added spans are applied by position, not text.
            int start = content.IndexOf("Apollo", StringComparison.Ordinal);
            var added = new List<RedactionSpanEntity>
            {
                new() { UserAdded = true, CharacterStart = start, CharacterEnd = start + "Apollo".Length, Replacement = "[X]" }
            };
            await RedactionService.ApplySpansAsync(input, output, ".txt", highlight: false, added);

            string text = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("Apollo", text);
            Assert.Contains("[X]", text);
        }
    }
}
