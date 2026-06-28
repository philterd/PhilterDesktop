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
    /// Tests for <see cref="RtfSanitizer"/> (unit) and the end-to-end guarantee that an embedded OLE
    /// object cannot ferry unredacted content through the RTF redactor (defense-in-depth P2 #3).
    /// </summary>
    public sealed class RtfSanitizerTests : IDisposable
    {
        private readonly string _tempDir;

        public RtfSanitizerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-rtfsan-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        // The embedded-object marker hex we expect to be gone after sanitizing.
        private const string ObjectMarker = "deadbeefcafef00d";

        private const string RtfWithObject =
            @"{\rtf1\ansi\deff0 Before " +
            @"{\object\objemb\objw1000\objh1000{\*\objclass Package}{\*\objdata 0105000002000000" + ObjectMarker + @"}{\result {\pict\wmetafile8 0102}}}" +
            @" After.}";

        [Fact]
        public void RemoveEmbeddedObjects_StripsTheWholeObjectGroup()
        {
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(RtfWithObject);

            Assert.DoesNotContain("\\object", cleaned);
            Assert.DoesNotContain("\\objdata", cleaned);
            Assert.DoesNotContain(ObjectMarker, cleaned);
            // Surrounding document is preserved.
            Assert.StartsWith(@"{\rtf1", cleaned);
            Assert.Contains("Before", cleaned);
            Assert.Contains("After.", cleaned);
        }

        [Fact]
        public void RemoveEmbeddedObjects_LeavesDocumentsWithoutObjectsUnchanged()
        {
            const string plain = @"{\rtf1\ansi\deff0 Just text, no objects. SSN 123-45-6789.\par}";
            Assert.Equal(plain, RtfSanitizer.RemoveEmbeddedObjects(plain));
        }

        [Fact]
        public void RemoveEmbeddedObjects_KeepsImagesAndOtherGroups()
        {
            const string withPict =
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}} text {\pict\wmetafile8 0102030405} more}";
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(withPict);

            Assert.Contains("\\pict", cleaned);     // images deliberately retained
            Assert.Contains("\\fonttbl", cleaned);  // unrelated groups retained
        }

        [Fact]
        public void RemoveEmbeddedObjects_DoesNotMatchSimilarControlWords()
        {
            // \objemb appears as a flag inside \object but should never itself trigger a removal when
            // it is the group's leading word (it isn't a real RTF group head, but guard anyway).
            const string rtf = @"{\rtf1 a {\fldinst objective} b}";
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(rtf);
            Assert.Equal(rtf, cleaned); // "objective" is not the \object control word
        }

        [Fact]
        public async Task RtfRedactor_DoesNotLeakEmbeddedObjectContent()
        {
            string input = Path.Combine(_tempDir, "embedded.rtf");
            File.WriteAllText(input, RtfWithObject);
            string output = Path.Combine(_tempDir, "embedded-out.rtf");

            var policy = new PhileasPolicy
            {
                Name = "rtf",
                Identifiers = new Identifiers { Ssn = new Ssn() }
            };
            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            string result = await File.ReadAllTextAsync(output);
            // The embedded object (and its hidden payload) must not survive into the redacted output.
            Assert.DoesNotContain(ObjectMarker, result);
            Assert.DoesNotContain("\\objdata", result);
            Assert.StartsWith(@"{\rtf", result);
        }
    }
}
