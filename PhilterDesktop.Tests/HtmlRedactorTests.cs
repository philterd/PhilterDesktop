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

using Phileas.Model;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The HTML body is redacted by its visible text so that entity-encoded (<c>&amp;#64;</c>) or
    /// tag-split (<c>john&lt;span&gt;@&lt;/span&gt;example.com</c>) PII can't survive in the HTML
    /// alternative most clients render, while PII in attribute values (a <c>mailto:</c> href) is still
    /// caught (#540).
    /// </summary>
    public sealed class HtmlRedactorTests
    {
        private static readonly PhileasPolicy EmailPolicy = new()
        {
            Name = "email",
            Identifiers = new Identifiers { EmailAddress = new EmailAddress(), PhoneNumber = new PhoneNumber() }
        };

        private readonly FilterService _fs = new();
        private Func<string, TextFilterResult> Filter => t => _fs.Filter(EmailPolicy, "ctx", 0, t);

        private string Redact(string html) => HtmlRedactor.Redact(html, Filter).Html;

        [Fact]
        public void EntityEncodedEmail_IsRedacted()
        {
            string html = "<p>Reach john&#64;example.com now</p>";
            string result = Redact(html);

            Assert.DoesNotContain("john@example.com", result); // decoded form gone
            Assert.DoesNotContain("john&#64;example.com", result); // entity form gone
            Assert.DoesNotContain("example.com", result);
            Assert.Contains("<p", result);                       // markup preserved
            Assert.Contains("Reach", result);
        }

        [Fact]
        public void TagSplitEmail_IsRedacted()
        {
            string html = "<p>john<span>@</span>example.com sent it</p>";
            string result = Redact(html);

            Assert.DoesNotContain("@example.com", result); // the split address is caught via the visible text
            Assert.Contains("<p", result);
            Assert.Contains("sent it", result);
        }

        [Fact]
        public void AttributeEmail_IsStillRedacted()
        {
            // PII in an attribute value (not visible text) must still be removed, as before.
            string html = "<a href=\"mailto:x@y.com\">click here</a>";
            string result = Redact(html);

            Assert.DoesNotContain("x@y.com", result);
            Assert.Contains("click here", result);
        }

        [Fact]
        public void LinkTextAndHref_BothRedacted()
        {
            string html = "<a href=\"mailto:secret@example.com\">secret@example.com</a>";
            string result = Redact(html);

            Assert.DoesNotContain("secret@example.com", result); // in the href AND the link text
        }

        [Fact]
        public void PlainVisibleEmail_IsRedacted_MarkupPreserved()
        {
            string html = "<div><b>Contact</b> plain@example.com today</div>";
            string result = Redact(html);

            Assert.DoesNotContain("plain@example.com", result);
            Assert.Contains("<b>Contact</b>", result); // surrounding markup untouched
            Assert.Contains("today", result);
        }

        [Fact]
        public void NoPii_ReturnsUnchanged()
        {
            string html = "<p>Just a friendly note with no personal data.</p>";
            Assert.Equal(html, Redact(html));
        }

        [Fact]
        public void Detect_MapsToRawRange_ThatBracketsTheEntityEncodedPii()
        {
            string html = "<p>a john&#64;example.com b</p>";
            List<HtmlRedactor.HtmlRedaction> reds = HtmlRedactor.Detect(html, Filter);

            HtmlRedactor.HtmlRedaction r = Assert.Single(reds);
            Assert.Equal("john&#64;example.com", html.Substring(r.RawStart, r.RawEnd - r.RawStart));
        }

        [Fact]
        public void EmptyOrPlain_DoesNotThrow()
        {
            Assert.Equal(string.Empty, Redact(string.Empty));
            Assert.Equal("no tags here at all", Redact("no tags here at all"));
        }
    }
}
