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

using System.Text.Json;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests for the redaction-explanation JSON projection (<see cref="RedactionExplanation"/>).</summary>
    public sealed class RedactionExplanationTests
    {
        private static readonly DateTimeOffset Generated = new(2025, 5, 1, 12, 0, 0, TimeSpan.Zero);

        private static RedactionVersionEntity TextVersion() => new()
        {
            Version = 1,
            SourcePath = @"C:\cases\memo.txt",
            OutputPath = @"C:\cases\memo_redacted-draft.txt",
            FileType = ".txt",
            Policy = "default",
            Context = "default",
            CreatedAt = new DateTime(2025, 4, 1, 10, 0, 0, DateTimeKind.Utc)
        };

        [Fact]
        public void ToJson_IncludesDocumentMetadataAndDetectionDetail()
        {
            var spans = new List<RedactionSpanEntity>
            {
                new()
                {
                    Order = 0,
                    FilterType = "EmailAddress",
                    Classification = "email-address",
                    Confidence = 0.97,
                    Text = "secret@example.com",
                    Replacement = "{{{REDACTED-email-address}}}",
                    Pattern = "(some-regex)",
                    Window = new List<string> { "email", "me", "at" },
                    CharacterStart = 13,
                    CharacterEnd = 31
                }
            };

            string json = RedactionExplanation.ToJson(TextVersion(), spans, "1.2.3", Generated);

            using var doc = JsonDocument.Parse(json);
            JsonElement root = doc.RootElement;
            Assert.Equal("Philter Desktop", root.GetProperty("tool").GetString());
            Assert.Equal("1.2.3", root.GetProperty("toolVersion").GetString());
            Assert.Equal(@"C:\cases\memo.txt", root.GetProperty("source").GetString());
            Assert.Equal("default", root.GetProperty("policy").GetString());
            Assert.Equal(1, root.GetProperty("detectionCount").GetInt32());

            JsonElement d = root.GetProperty("detections")[0];
            Assert.Equal("EmailAddress", d.GetProperty("filterType").GetString());
            Assert.Equal("email-address", d.GetProperty("classification").GetString());
            Assert.Equal(0.97, d.GetProperty("confidence").GetDouble(), 3);
            Assert.Equal("secret@example.com", d.GetProperty("text").GetString());
            Assert.Equal("{{{REDACTED-email-address}}}", d.GetProperty("replacement").GetString());
            Assert.Equal("(some-regex)", d.GetProperty("pattern").GetString());
            Assert.Equal(3, d.GetProperty("window").GetArrayLength());

            JsonElement loc = d.GetProperty("location");
            Assert.Equal(13, loc.GetProperty("characterStart").GetInt32());
            Assert.Equal(31, loc.GetProperty("characterEnd").GetInt32());
            Assert.False(loc.TryGetProperty("pageNumber", out _)); // text span: no PDF coordinates
        }

        [Fact]
        public void ToJson_UserAddedSpan_OmitsEngineOnlyFields()
        {
            var spans = new List<RedactionSpanEntity>
            {
                new()
                {
                    Order = 0,
                    UserAdded = true,
                    Text = "Bluebird",
                    Replacement = "{{{REDACTED-custom}}}",
                    CharacterStart = 5,
                    CharacterEnd = 13
                }
            };

            string json = RedactionExplanation.ToJson(TextVersion(), spans, "1.0.0", Generated);

            using var doc = JsonDocument.Parse(json);
            JsonElement d = doc.RootElement.GetProperty("detections")[0];
            Assert.True(d.GetProperty("userAdded").GetBoolean());
            // No engine detail for a hand-added term.
            Assert.False(d.TryGetProperty("confidence", out _));
            Assert.False(d.TryGetProperty("filterType", out _));
            Assert.False(d.TryGetProperty("pattern", out _));
            Assert.False(d.TryGetProperty("window", out _));
        }

        [Fact]
        public void ToJson_PdfSpan_LocatesByPageAndBoundingBox()
        {
            var version = TextVersion();
            version.FileType = ".pdf";
            var spans = new List<RedactionSpanEntity>
            {
                new()
                {
                    Order = 0,
                    FilterType = "SSN",
                    Text = "123-45-6789",
                    Replacement = "*",
                    PageNumber = 2,
                    LowerLeftX = 10, LowerLeftY = 20, UpperRightX = 110, UpperRightY = 32
                }
            };

            string json = RedactionExplanation.ToJson(version, spans, "1.0.0", Generated);

            using var doc = JsonDocument.Parse(json);
            JsonElement loc = doc.RootElement.GetProperty("detections")[0].GetProperty("location");
            Assert.Equal(2, loc.GetProperty("pageNumber").GetInt32());
            Assert.Equal(10, loc.GetProperty("lowerLeftX").GetDouble());
            Assert.False(loc.TryGetProperty("characterStart", out _)); // PDF span: no character offsets
        }

        [Fact]
        public void ToJson_OrdersDetectionsByOrder()
        {
            var spans = new List<RedactionSpanEntity>
            {
                new() { Order = 2, Text = "third", Replacement = "x" },
                new() { Order = 0, Text = "first", Replacement = "x" },
                new() { Order = 1, Text = "second", Replacement = "x" }
            };

            string json = RedactionExplanation.ToJson(TextVersion(), spans, "1.0.0", Generated);

            using var doc = JsonDocument.Parse(json);
            JsonElement dets = doc.RootElement.GetProperty("detections");
            Assert.Equal("first", dets[0].GetProperty("text").GetString());
            Assert.Equal("second", dets[1].GetProperty("text").GetString());
            Assert.Equal("third", dets[2].GetProperty("text").GetString());
        }
    }
}
