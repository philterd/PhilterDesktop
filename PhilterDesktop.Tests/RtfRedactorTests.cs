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
    /// Tests for <see cref="RtfRedactor"/> and the RTF path through <see cref="RedactionService"/>:
    /// the visible text is redacted, the output is still valid RTF, and an edited span set re-applies.
    /// </summary>
    public sealed class RtfRedactorTests : IDisposable
    {
        private readonly string _tempDir;

        public RtfRedactorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-rtf-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        private const string SampleRtf =
            @"{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fnil Arial;}}" +
            @"\f0\fs24 Email john@example.com and SSN 123-45-6789. Thanks.\par}";

        private string WriteRtf(string name = "sample.rtf")
        {
            string path = Path.Combine(_tempDir, name);
            File.WriteAllText(path, SampleRtf);
            return path;
        }

        private static PhileasPolicy EmailAndSsnPolicy() => new()
        {
            Name = "rtf",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
        };

        [Fact]
        public async Task RedactFileAsync_Rtf_RedactsVisibleTextAndKeepsRtf()
        {
            string input = WriteRtf();
            string output = Path.Combine(_tempDir, "out.rtf");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("john@example.com", result);
            Assert.DoesNotContain("123-45-6789", result);
            // Still a valid RTF document, and non-sensitive text survives.
            Assert.StartsWith(@"{\rtf", result);
            Assert.Contains("Thanks", result);
        }

        [Fact]
        public async Task RedactFileAsync_Rtf_CapturesExplanationSpans()
        {
            string input = WriteRtf();
            string output = Path.Combine(_tempDir, "out.rtf");

            List<RedactionSpanEntity> spans =
                await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            Assert.NotEmpty(spans);
            Assert.Contains(spans, s => s.FilterType == "EmailAddress");
            Assert.All(spans, s => Assert.True(s.Confidence > 0));
        }

        [Fact]
        public async Task RedactFileAsync_Rtf_DisabledFilters_LeaveDocumentUnchangedText()
        {
            string input = WriteRtf();
            string output = Path.Combine(_tempDir, "out.rtf");

            await RedactionService.RedactFileAsync(input, output, new PhileasPolicy { Name = "noop" }, "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.Contains("john@example.com", result);
            Assert.Contains("123-45-6789", result);
        }

        [Fact]
        public async Task RedactFileAsync_Rtf_RedactsTextInsideTables()
        {
            // SSN lives inside a table cell — the visible-text approach must still catch it.
            string rtf =
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs24 " +
                @"\pard\trowd\cellx3000\cellx6000 Name\cell SSN\cell\row" +
                @"\trowd\cellx3000\cellx6000 John Smith\cell 123-45-6789\cell\row\pard\par}";
            string input = Path.Combine(_tempDir, "table.rtf");
            File.WriteAllText(input, rtf);
            string output = Path.Combine(_tempDir, "table-out.rtf");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("123-45-6789", result);
            Assert.StartsWith(@"{\rtf", result);
        }

        [Fact]
        public async Task RedactFileAsync_Rtf_PreservesFormattingAndNonSensitiveText()
        {
            // A bold heading plus a body line; only the SSN should change, formatting should remain.
            string rtf =
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs24 " +
                @"{\b Patient Summary}\par John Smith SSN 123-45-6789.\par}";
            string input = Path.Combine(_tempDir, "fmt.rtf");
            File.WriteAllText(input, rtf);
            string output = Path.Combine(_tempDir, "fmt-out.rtf");

            await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("123-45-6789", result);
            Assert.Contains("Patient Summary", result); // heading text survives
            Assert.Contains(@"\b", result);             // bold control word survives
        }

        [Fact]
        public async Task RedactFileAsync_Rtf_RedactsEveryOccurrence()
        {
            string rtf =
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs24 " +
                @"Primary a@example.com, secondary b@example.com, billing c@example.com.\par}";
            string input = Path.Combine(_tempDir, "multi.rtf");
            File.WriteAllText(input, rtf);
            string output = Path.Combine(_tempDir, "multi-out.rtf");

            List<RedactionSpanEntity> spans =
                await RedactionService.RedactFileAsync(input, output, EmailAndSsnPolicy(), "ctx");

            string result = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("a@example.com", result);
            Assert.DoesNotContain("b@example.com", result);
            Assert.DoesNotContain("c@example.com", result);
            Assert.True(spans.Count >= 3);
        }

        [Fact]
        public void ApplySpans_Rtf_ReappliesEditedSpanSet()
        {
            string input = WriteRtf();
            string firstPass = Path.Combine(_tempDir, "first.rtf");

            var fs = new Phileas.Services.FilterService();
            var policy = EmailAndSsnPolicy();
            List<RedactionSpanEntity> spans = RtfRedactor.Redact(
                input, firstPass, text => fs.Filter(policy, "ctx", 0, text));

            Assert.NotEmpty(spans);

            string reapplied = Path.Combine(_tempDir, "reapplied.rtf");
            RtfRedactor.ApplySpans(input, reapplied, spans);

            string result = File.ReadAllText(reapplied);
            Assert.DoesNotContain("john@example.com", result);
            Assert.DoesNotContain("123-45-6789", result);
            Assert.StartsWith(@"{\rtf", result);
        }
    }
}
