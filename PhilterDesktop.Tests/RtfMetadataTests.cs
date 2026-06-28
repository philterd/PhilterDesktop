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
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Guards that a redacted <c>.rtf</c> doesn't leak the <c>\info</c> metadata group (author, title,
    /// company, operator). RTF redaction round-trips through the RichTextBox engine, which re-serializes
    /// only the document body and drops <c>\info</c>, so metadata is removed inherently — no setting
    /// required. This test locks that behavior in.
    /// </summary>
    public sealed class RtfMetadataTests : IDisposable
    {
        private readonly string _dir;
        public RtfMetadataTests()
        {
            _dir = Path.Combine(Path.GetTempPath(), "philter-rtfmeta-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
        }
        public void Dispose() { try { Directory.Delete(_dir, true); } catch { /* best effort */ } }

        [Fact]
        public async Task RedactedRtf_DoesNotLeakInfoMetadata()
        {
            string input = Path.Combine(_dir, "in.rtf");
            File.WriteAllText(input,
                @"{\rtf1\ansi\deff0 {\fonttbl{\f0 Arial;}}" +
                @"{\info{\author Jane Author}{\title Secret Memo}{\company Acme Corp}{\operator Bob Editor}}" +
                @"\f0 Patient SSN 123-45-6789.\par}");
            string output = Path.Combine(_dir, "out.rtf");

            var policy = new PhileasPolicy { Name = "ssn", Identifiers = new Identifiers { Ssn = new Ssn() } };
            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            string outRtf = File.ReadAllText(output);
            Assert.DoesNotContain("Jane Author", outRtf);
            Assert.DoesNotContain("Acme Corp", outRtf);
            Assert.DoesNotContain("Secret Memo", outRtf);
            Assert.DoesNotContain("Bob Editor", outRtf);
            Assert.DoesNotContain(@"\info", outRtf);
            Assert.DoesNotContain("123-45-6789", outRtf); // the body PII is redacted too
        }
    }
}
