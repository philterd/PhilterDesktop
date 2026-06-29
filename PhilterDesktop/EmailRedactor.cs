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

using System.Text;
using MimeKit;
using Phileas.Model;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts email messages. Two input formats are supported:
    /// <list type="bullet">
    ///   <item><c>.eml</c> — MIME (RFC 822), parsed and re-serialized with MimeKit.</item>
    ///   <item><c>.msg</c> — the Outlook compound-binary format, read with MsgReader and normalized
    ///   into a MIME message.</item>
    /// </list>
    /// Both are written back as <c>.eml</c> (a standard, widely-openable email file); a faithful
    /// rewrite of the proprietary <c>.msg</c> container is not attempted.
    ///
    /// Redaction runs the supplied filter over each text-bearing field — the Subject, the address
    /// headers (From / To / Cc), and every (non-attachment) text body part (plain text and HTML) — and
    /// captures the applied spans with a stable <em>field index</em> in
    /// <see cref="RedactionSpanEntity.ParagraphIndex"/>, exactly like the Word redactor uses a
    /// paragraph index, so the spans can be stored and later re-applied via <see cref="ApplySpans"/>.
    ///
    /// Attachments are not inspected or redacted. HTML bodies are redacted in their markup form, so a
    /// detected value is replaced wherever it appears in the HTML source.
    /// </summary>
    internal static class EmailRedactor
    {
        private const string DefaultReplacement = "{{{REDACTED-custom}}}";

        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its text fields with <paramref name="filter"/>,
        /// writes the result as <c>.eml</c> to <paramref name="outputPath"/>, and returns the applied
        /// spans (field-indexed). The input file is left untouched.
        /// </summary>
        public static List<RedactionSpanEntity> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, bool scrubHeaders = false, bool removeCommonHeaders = false)
        {
            MimeMessage message = Load(inputPath);

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;

            foreach (IEmailField field in EnumerateFields(message))
            {
                string original = field.Get();
                if (!string.IsNullOrEmpty(original))
                {
                    TextFilterResult result = filter(original);
                    if (!string.Equals(result.FilteredText, original, StringComparison.Ordinal))
                    {
                        field.Set(result.FilteredText);

                        foreach (Span s in result.Spans
                            .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                            .OrderBy(s => s.CharacterStart))
                        {
                            var entity = new RedactionSpanEntity
                            {
                                Order = order++,
                                ParagraphIndex = fieldIndex,
                                CharacterStart = s.CharacterStart,
                                CharacterEnd = s.CharacterEnd,
                                Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                                Replacement = s.Replacement ?? string.Empty,
                                Classification = s.Classification ?? string.Empty
                            };
                            SpanExplanation.Populate(entity, s);
                            captured.Add(entity);
                        }
                    }
                }
                fieldIndex++;
            }

            if (scrubHeaders)
            {
                RemoveTechnicalHeaders(message);
            }
            if (removeCommonHeaders)
            {
                RemoveCommonHeaders(message);
            }
            Save(message, outputPath);
            return captured;
        }

        /// <summary>
        /// Returns each redactable field's label and current text, in the canonical field order (so the
        /// index matches <see cref="RedactionSpanEntity.ParagraphIndex"/>). Read-only; for the preview.
        /// </summary>
        public static List<(string Label, string Text, bool IsBody)> ReadFields(string inputPath)
        {
            MimeMessage message = Load(inputPath);
            return EnumerateFields(message).Select(f => (f.Label, f.Get(), f.IsBody)).ToList();
        }

        /// <summary>
        /// Detects the redactions <see cref="Redact"/> would apply, without writing anything: spans are
        /// field-indexed via <see cref="RedactionSpanEntity.ParagraphIndex"/>. Used by the preview.
        /// </summary>
        public static List<RedactionSpanEntity> Detect(string inputPath, Func<string, TextFilterResult> filter)
        {
            MimeMessage message = Load(inputPath);
            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;

            foreach (IEmailField field in EnumerateFields(message))
            {
                string original = field.Get();
                if (!string.IsNullOrEmpty(original))
                {
                    foreach (Span s in filter(original).Spans
                        .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        .OrderBy(s => s.CharacterStart))
                    {
                        var entity = new RedactionSpanEntity
                        {
                            Order = order++,
                            ParagraphIndex = fieldIndex,
                            CharacterStart = s.CharacterStart,
                            CharacterEnd = s.CharacterEnd,
                            Text = original.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                            Replacement = s.Replacement ?? string.Empty,
                            Classification = s.Classification ?? string.Empty
                        };
                        SpanExplanation.Populate(entity, s);
                        captured.Add(entity);
                    }
                }
                fieldIndex++;
            }
            return captured;
        }

        /// <summary>
        /// Re-applies an explicit (edited) span set to <paramref name="inputPath"/>, writing the result
        /// as <c>.eml</c> to <paramref name="outputPath"/>. Each span is applied by <b>position</b>: its
        /// <see cref="RedactionSpanEntity.ParagraphIndex"/> (the field index) plus character start/stop
        /// offsets within that field. Used by the Modify Redaction feature.
        /// </summary>
        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans, bool scrubHeaders = false, bool removeCommonHeaders = false)
        {
            MimeMessage message = Load(inputPath);

            Dictionary<int, List<RedactionSpanEntity>> byField = spans
                .GroupBy(s => s.ParagraphIndex)
                .ToDictionary(g => g.Key, g => g.ToList());

            int fieldIndex = 0;
            foreach (IEmailField field in EnumerateFields(message))
            {
                string original = field.Get();
                if (!string.IsNullOrEmpty(original) && byField.TryGetValue(fieldIndex, out List<RedactionSpanEntity>? fieldSpans))
                {
                    var ranges = new List<ReplacementRange>();
                    foreach (RedactionSpanEntity s in fieldSpans)
                    {
                        if (s.CharacterStart >= 0 && s.CharacterEnd <= original.Length && s.CharacterEnd > s.CharacterStart)
                        {
                            string repl = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement;
                            ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, repl));
                        }
                    }

                    List<ReplacementRange> resolved = RedactionSpanMath.ResolveNonOverlapping(ranges);
                    if (resolved.Count > 0)
                    {
                        field.Set(ApplyRanges(original, resolved));
                    }
                }
                fieldIndex++;
            }

            if (scrubHeaders)
            {
                RemoveTechnicalHeaders(message);
            }
            if (removeCommonHeaders)
            {
                RemoveCommonHeaders(message);
            }
            Save(message, outputPath);
        }

        // Identifying technical headers removed when email-header scrubbing is on: the originating IP,
        // the sending mail client, and the server-hop trail. From/To/Cc/Subject/Date and the structural
        // MIME/Content headers are kept (they're needed to render, and the address fields are redacted
        // for PII separately).
        private static readonly HashSet<string> TechnicalHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Received", "Return-Path", "Message-Id", "User-Agent", "X-Mailer", "X-MimeOLE",
            "DKIM-Signature", "Authentication-Results", "Received-SPF", "Autocrypt"
        };

        private static void RemoveTechnicalHeaders(MimeMessage message)
        {
            try
            {
                HeaderList headers = message.Headers;
                for (int i = headers.Count - 1; i >= 0; i--)
                {
                    string field = headers[i].Field;
                    if (TechnicalHeaders.Contains(field)
                        || field.StartsWith("X-", StringComparison.OrdinalIgnoreCase)
                        || field.StartsWith("ARC-", StringComparison.OrdinalIgnoreCase))
                    {
                        headers.RemoveAt(i);
                    }
                }
            }
            catch
            {
                // best effort — a header quirk must never fail the redaction
            }
        }

        // Common identity/delivery headers that carry recipient or sender PII but aren't part of the
        // redacted field set (unlike From/To/Cc, which are scanned). Bcc is the most sensitive — it names
        // blind-copy recipients — and .msg input copies Bcc through, so without this it would survive into
        // the redacted .eml. We clear MimeKit's typed address properties (the reliable way to drop these)
        // and then sweep any remaining raw headers defensively.
        private static readonly HashSet<string> CommonIdentityHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Bcc", "Reply-To", "Sender"
        };

        private static void RemoveCommonHeaders(MimeMessage message)
        {
            try
            {
                message.Bcc.Clear();
                message.ReplyTo.Clear();
                message.Sender = null;
                message.ResentFrom.Clear();
                message.ResentReplyTo.Clear();
                message.ResentTo.Clear();
                message.ResentCc.Clear();
                message.ResentBcc.Clear();
                message.ResentSender = null;

                HeaderList headers = message.Headers;
                for (int i = headers.Count - 1; i >= 0; i--)
                {
                    string field = headers[i].Field;
                    if (CommonIdentityHeaders.Contains(field)
                        || field.StartsWith("Resent-", StringComparison.OrdinalIgnoreCase))
                    {
                        headers.RemoveAt(i);
                    }
                }
            }
            catch
            {
                // best effort — a header quirk must never fail the redaction
            }
        }

        // Replaces the given (non-overlapping) ranges in the source text, working back-to-front so
        // earlier offsets stay valid as later ones are spliced.
        private static string ApplyRanges(string original, IEnumerable<ReplacementRange> ranges)
        {
            var sb = new StringBuilder(original);
            foreach (ReplacementRange r in ranges.OrderByDescending(r => r.Start))
            {
                if (r.Start < 0 || r.End > original.Length || r.End <= r.Start)
                {
                    continue;
                }
                sb.Remove(r.Start, r.End - r.Start);
                sb.Insert(r.Start, r.Replacement ?? string.Empty);
            }
            return sb.ToString();
        }

        // --- Field model ------------------------------------------------------

        /// <summary>One redactable text field of a message (a header value or a body part).</summary>
        private interface IEmailField
        {
            string Label { get; }
            /// <summary>True for a body part (free text where a character position is meaningful), false for a header.</summary>
            bool IsBody { get; }
            string Get();
            void Set(string value);
        }

        private sealed class HeaderField : IEmailField
        {
            private readonly Header _header;
            public HeaderField(Header header, string label) { _header = header; Label = label; }
            public string Label { get; }
            public bool IsBody => false;
            public string Get() => _header.Value ?? string.Empty;
            public void Set(string value) => _header.Value = value;
        }

        private sealed class TextPartField : IEmailField
        {
            private readonly TextPart _part;
            public TextPartField(TextPart part) => _part = part;
            public string Label => _part.IsHtml ? "Body (HTML)" : "Body (text)";
            public bool IsBody => true;
            public string Get() => _part.Text ?? string.Empty;
            public void Set(string value) => _part.Text = value;
        }

        /// <summary>
        /// The canonical, stable order of redactable fields: the Subject, From, To and Cc headers (each
        /// only if present), then every non-attachment text body part in document order. The order must
        /// match between <see cref="Redact"/> and <see cref="ApplySpans"/> so a stored field index
        /// re-applies to the same field.
        /// </summary>
        private static List<IEmailField> EnumerateFields(MimeMessage message)
        {
            var fields = new List<IEmailField>();

            foreach ((HeaderId id, string label) in new[]
                     {
                         (HeaderId.Subject, "Subject"), (HeaderId.From, "From"),
                         (HeaderId.To, "To"), (HeaderId.Cc, "Cc")
                     })
            {
                Header? header = message.Headers.FirstOrDefault(h => h.Id == id);
                if (header is not null)
                {
                    fields.Add(new HeaderField(header, label));
                }
            }

            foreach (TextPart part in message.BodyParts.OfType<TextPart>().Where(p => !p.IsAttachment))
            {
                fields.Add(new TextPartField(part));
            }

            return fields;
        }

        // --- Load / save ------------------------------------------------------

        private static MimeMessage Load(string inputPath)
        {
            return Path.GetExtension(inputPath).Equals(".msg", StringComparison.OrdinalIgnoreCase)
                ? LoadFromMsg(inputPath)
                : MimeMessage.Load(inputPath);
        }

        // Normalizes an Outlook .msg into a MIME message we can redact and serialize as .eml.
        private static MimeMessage LoadFromMsg(string inputPath)
        {
            using var msg = new MsgReader.Outlook.Storage.Message(inputPath);
            var message = new MimeMessage();

            if (!string.IsNullOrEmpty(msg.Subject))
            {
                message.Subject = msg.Subject;
            }

            MailboxAddress? from = ToMailbox(msg.Sender?.DisplayName, msg.Sender?.Email);
            if (from is not null)
            {
                message.From.Add(from);
            }

            foreach (MsgReader.Outlook.Storage.Recipient recipient in msg.Recipients)
            {
                MailboxAddress? mailbox = ToMailbox(recipient.DisplayName, recipient.Email);
                if (mailbox is null)
                {
                    continue;
                }
                switch (recipient.Type)
                {
                    case MsgReader.Outlook.RecipientType.To:
                        message.To.Add(mailbox);
                        break;
                    case MsgReader.Outlook.RecipientType.Cc:
                        message.Cc.Add(mailbox);
                        break;
                    case MsgReader.Outlook.RecipientType.Bcc:
                        message.Bcc.Add(mailbox);
                        break;
                    default:
                        message.To.Add(mailbox);
                        break;
                }
            }

            if (msg.SentOn.HasValue)
            {
                message.Date = msg.SentOn.Value;
            }

            var builder = new BodyBuilder();
            if (!string.IsNullOrEmpty(msg.BodyHtml))
            {
                builder.HtmlBody = msg.BodyHtml;
            }
            if (!string.IsNullOrEmpty(msg.BodyText))
            {
                builder.TextBody = msg.BodyText;
            }
            message.Body = builder.ToMessageBody();

            return message;
        }

        // MimeKit's MailboxAddress requires an address; substitute a clearly-fake one when the source
        // has only a display name, and skip an address that has neither name nor email.
        private static MailboxAddress? ToMailbox(string? name, string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return null;
                }
                email = "unknown@invalid";
            }
            try
            {
                return new MailboxAddress(name ?? string.Empty, email);
            }
            catch
            {
                return null;
            }
        }

        private static void Save(MimeMessage message, string outputPath)
        {
            using FileStream stream = File.Create(outputPath);
            message.WriteTo(stream);
        }
    }
}
