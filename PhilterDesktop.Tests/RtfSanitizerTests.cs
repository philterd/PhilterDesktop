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
        public void RemoveEmbeddedObjects_StripsIgnorableDestinationObjectGroup()
        {
            // The "{\*\object …}" form (ignorable destination) must be stripped just like "{\object …}".
            const string rtf = @"{\rtf1 Before {\*\object\objemb{\*\objdata 0102deadbeef}} After}";
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(rtf);

            Assert.DoesNotContain("\\object", cleaned);
            Assert.DoesNotContain("deadbeef", cleaned);
            Assert.Contains("Before", cleaned);
            Assert.Contains("After", cleaned);
        }

        [Fact]
        public void RemoveEmbeddedObjects_StripsNestedObjectGroups()
        {
            const string rtf = @"{\rtf1 {\object x {\object y}} keep}";
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(rtf);

            Assert.DoesNotContain("\\object", cleaned);
            Assert.Contains("keep", cleaned);
        }

        [Fact]
        public void RemoveEmbeddedObjects_PreservesEscapedBraces_WhileStrippingObject()
        {
            const string rtf = @"{\rtf1 lit \{x\} {\object data} end}";
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(rtf);

            Assert.DoesNotContain("\\object", cleaned);
            Assert.Contains(@"\{x\}", cleaned); // escaped literal braces are not group delimiters
            Assert.Contains("end", cleaned);
        }

        [Fact]
        public void RemoveEmbeddedObjects_MalformedUnbalanced_DoesNotThrow_AndStripsToEnd()
        {
            const string rtf = @"{\rtf1 a {\object {\*\objdata 0102"; // unterminated object group
            string cleaned = RtfSanitizer.RemoveEmbeddedObjects(rtf);

            Assert.DoesNotContain("\\objdata", cleaned);
            Assert.DoesNotContain("\\object", cleaned);
            Assert.StartsWith(@"{\rtf1 a ", cleaned); // text before the object survives
        }

        [Fact]
        public void RemoveEmbeddedObjects_Fuzz_NeverThrowsAndNeverGrows()
        {
            var rng = new Random(12345);
            char[] alphabet = @"{}\*objectdar result 0123abz".ToCharArray();
            for (int n = 0; n < 2000; n++)
            {
                var chars = new char[rng.Next(0, 200)];
                for (int k = 0; k < chars.Length; k++)
                {
                    chars[k] = alphabet[rng.Next(alphabet.Length)];
                }
                string input = new string(chars);

                string result = RtfSanitizer.RemoveEmbeddedObjects(input); // must not throw or hang
                Assert.True(result.Length <= input.Length, "sanitizer should only ever remove characters");
            }
        }

        // --- Comment / annotation stripping (#542) -----------------------------------

        [Fact]
        public void RemoveComments_StripsAnnotationGroupAndItsText()
        {
            const string rtf = @"{\rtf1\ansi Body {\annotation COMMENTBODYXYZ SSN 555-12-3456} after}";
            string cleaned = RtfSanitizer.RemoveComments(rtf);

            Assert.DoesNotContain("\\annotation", cleaned);
            Assert.DoesNotContain("COMMENTBODYXYZ", cleaned);
            Assert.DoesNotContain("555-12-3456", cleaned);
            Assert.Contains("Body", cleaned);
            Assert.Contains("after", cleaned);
        }

        [Fact]
        public void RemoveComments_StripsIgnorableAnnotationAndAuthorIdCompanions()
        {
            const string rtf = @"{\rtf1 Body{\*\atnid JZ}{\*\atnauthor Jeff}{\annotation the comment text} after}";
            string cleaned = RtfSanitizer.RemoveComments(rtf);

            Assert.DoesNotContain("\\annotation", cleaned);
            Assert.DoesNotContain("\\atnid", cleaned);
            Assert.DoesNotContain("\\atnauthor", cleaned);
            Assert.DoesNotContain("the comment text", cleaned);
            Assert.DoesNotContain("Jeff", cleaned);
            Assert.Contains("Body", cleaned);
            Assert.Contains("after", cleaned);
        }

        [Fact]
        public void RemoveComments_LeavesDocumentsWithoutCommentsUnchanged()
        {
            const string plain = @"{\rtf1\ansi\deff0 Just body text. SSN 123-45-6789.\par}";
            Assert.Equal(plain, RtfSanitizer.RemoveComments(plain));
        }

        [Fact]
        public void RemoveComments_KeepsBodyImagesAndOtherGroups()
        {
            const string rtf = @"{\rtf1\ansi{\fonttbl{\f0\fnil Arial;}} keep {\annotation COMMENTBODYXYZ} more {\pict\wmetafile8 0102}}";
            string cleaned = RtfSanitizer.RemoveComments(rtf);

            Assert.DoesNotContain("\\annotation", cleaned);
            Assert.DoesNotContain("COMMENTBODYXYZ", cleaned);
            Assert.Contains("\\fonttbl", cleaned);
            Assert.Contains("\\pict", cleaned);
            Assert.Contains("keep", cleaned);
            Assert.Contains("more", cleaned);
        }

        [Fact]
        public void RemoveComments_DoesNotStripControlWordThatMerelyStartsWithAnnotation()
        {
            const string rtf = @"{\rtf1 a {\annotationlike x} b}"; // not the \annotation control word
            Assert.Equal(rtf, RtfSanitizer.RemoveComments(rtf));
        }

        [Fact]
        public void RemoveComments_PreservesEscapedBraces_WhileStrippingComment()
        {
            const string rtf = @"{\rtf1 lit \{x\} {\annotation note} end}";
            string cleaned = RtfSanitizer.RemoveComments(rtf);

            Assert.DoesNotContain("\\annotation", cleaned);
            Assert.Contains(@"\{x\}", cleaned); // escaped literal braces are not group delimiters
            Assert.Contains("end", cleaned);
        }

        [Fact]
        public async Task RtfRedactor_Comment_IsNotFlattenedIntoTheBody()
        {
            // An RTF comment is rendered by RichEdit glued onto the surrounding prose with no boundary;
            // stripping the annotation group first keeps the redacted body clean (#542).
            string input = Path.Combine(_tempDir, "comment.rtf");
            File.WriteAllText(input,
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs22 The invoice total is due." +
                @"{\annotation Reviewer: verify client SSN 555-12-3456 before sending.} Please review.\par}");
            string output = Path.Combine(_tempDir, "comment-out.rtf");

            var policy = new PhileasPolicy { Name = "rtf", Identifiers = new Identifiers { Ssn = new Ssn() } };
            await RedactionService.RedactFileAsync(input, output, policy, "ctx");

            // The body reads cleanly — the comment text is not merged into the prose.
            string body = RtfRedactor.ReadText(output);
            Assert.Contains("The invoice total is due.", body);
            Assert.Contains("Please review.", body);
            Assert.DoesNotContain("Reviewer", body);
            Assert.DoesNotContain("verify client", body);
            Assert.DoesNotContain("due.Reviewer", body); // the exact "glued comment" corruption is gone

            // The comment (and the PII inside it) is not in the output at all.
            string raw = await File.ReadAllTextAsync(output);
            Assert.DoesNotContain("Reviewer", raw);
            Assert.DoesNotContain("555-12-3456", raw);
            Assert.DoesNotContain("\\annotation", raw);
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
