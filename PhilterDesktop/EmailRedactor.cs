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
        /// The caveat added to a <em>verification</em> result when the redacted email still carries
        /// attachments: their content is never inspected or redacted, so a clean body result doesn't cover
        /// them. Shown when the "Remove attachments" option is off and the message has attachments.
        /// </summary>
        public const string AttachmentVerificationCaveat =
            "This email has one or more attachments. Philter Desktop does not inspect or redact attachment " +
            "content, so a clean result does not cover them — review each attachment before sharing, or turn " +
            "on \"Remove attachments from redacted email\" in Settings → Email.";

        /// <summary>True if the email at <paramref name="path"/> has at least one attachment.</summary>
        public static bool HasAttachments(string path)
        {
            try
            {
                return Load(path).Attachments.Any();
            }
            catch
            {
                return false; // best effort — a parse quirk must never fail verification
            }
        }

        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its text fields with <paramref name="filter"/>,
        /// writes the result as <c>.eml</c> to <paramref name="outputPath"/>, and returns the applied
        /// spans (field-indexed). The input file is left untouched.
        /// </summary>
        public static List<RedactionSpanEntity> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, bool scrubHeaders = false, bool removeCommonHeaders = false, bool removeDateHeader = false, bool removeAttachments = false)
        {
            MimeMessage message = Load(inputPath);

            var captured = new List<RedactionSpanEntity>();
            int order = 0;
            int fieldIndex = 0;

            foreach (IEmailField field in EnumerateFields(message))
            {
                (string? newContent, List<RedactionSpanEntity> spans) = RedactField(field, filter, fieldIndex, ref order);
                if (newContent is not null)
                {
                    field.Set(newContent);
                }
                captured.AddRange(spans);
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
            if (removeDateHeader)
            {
                RemoveDateHeader(message);
            }
            if (removeAttachments)
            {
                RemoveAttachments(message);
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
                (_, List<RedactionSpanEntity> spans) = RedactField(field, filter, fieldIndex, ref order);
                captured.AddRange(spans);
                fieldIndex++;
            }
            return captured;
        }

        // Redacts one field. Returns the new content to write (null when unchanged) and the spans captured
        // (field-indexed by <paramref name="fieldIndex"/>). An HTML body is redacted by its visible text —
        // entity-aware and tag-aware — with the spans mapped to raw-HTML offsets so position-based re-apply
        // (Modify/ApplySpans) still works; every other field is a straight text filter.
        private static (string? NewContent, List<RedactionSpanEntity> Spans) RedactField(
            IEmailField field, Func<string, TextFilterResult> filter, int fieldIndex, ref int order)
        {
            var spans = new List<RedactionSpanEntity>();
            string original = field.Get();
            if (string.IsNullOrEmpty(original))
            {
                return (null, spans);
            }

            if (field.IsHtmlBody)
            {
                (string redacted, List<HtmlRedactor.HtmlRedaction> redactions) = HtmlRedactor.Redact(original, filter);
                foreach (HtmlRedactor.HtmlRedaction r in redactions)
                {
                    var entity = new RedactionSpanEntity
                    {
                        Order = order++,
                        ParagraphIndex = fieldIndex,
                        CharacterStart = r.RawStart,
                        CharacterEnd = r.RawEnd,
                        Text = r.Span.Text ?? string.Empty,
                        Replacement = r.Span.Replacement ?? string.Empty,
                        Classification = r.Span.Classification ?? string.Empty
                    };
                    SpanExplanation.Populate(entity, r.Span);
                    spans.Add(entity);
                }
                return (string.Equals(redacted, original, StringComparison.Ordinal) ? null : redacted, spans);
            }

            TextFilterResult result = filter(original);
            if (string.Equals(result.FilteredText, original, StringComparison.Ordinal))
            {
                return (null, spans);
            }
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
                spans.Add(entity);
            }
            return (result.FilteredText, spans);
        }

        /// <summary>
        /// Re-applies an explicit (edited) span set to <paramref name="inputPath"/>, writing the result
        /// as <c>.eml</c> to <paramref name="outputPath"/>. Each span is applied by <b>position</b>: its
        /// <see cref="RedactionSpanEntity.ParagraphIndex"/> (the field index) plus character start/stop
        /// offsets within that field. Used by the Modify Redaction feature.
        /// </summary>
        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans, bool scrubHeaders = false, bool removeCommonHeaders = false, bool removeDateHeader = false, bool removeAttachments = false)
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
            if (removeDateHeader)
            {
                RemoveDateHeader(message);
            }
            if (removeAttachments)
            {
                RemoveAttachments(message);
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
            // Also strip these from any nested/forwarded message, whose Received trail etc. would otherwise
            // survive in the embedded headers.
            foreach (MimeMessage m in AllMessages(message))
            {
                try
                {
                    HeaderList headers = m.Headers;
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
        }

        // The top-level message plus every nested/forwarded message (message/rfc822), at any depth, so the
        // header scrubs cover embedded messages too.
        private static List<MimeMessage> AllMessages(MimeMessage message)
        {
            var messages = new List<MimeMessage> { message };
            try
            {
                var iterator = new MimeIterator(message);
                while (iterator.MoveNext())
                {
                    if (iterator.Current is MessagePart { Message: not null } part)
                    {
                        messages.Add(part.Message);
                    }
                }
            }
            catch
            {
                // best effort — a traversal quirk shouldn't lose the top-level scrub
            }
            return messages;
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
            // Nested/forwarded messages carry their own Bcc/Reply-To/Sender/Resent-* too.
            foreach (MimeMessage m in AllMessages(message))
            {
                try
                {
                    m.Bcc.Clear();
                    m.ReplyTo.Clear();
                    m.Sender = null;
                    m.ResentFrom.Clear();
                    m.ResentReplyTo.Clear();
                    m.ResentTo.Clear();
                    m.ResentCc.Clear();
                    m.ResentBcc.Clear();
                    m.ResentSender = null;

                    HeaderList headers = m.Headers;
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
        }

        // Drops the Date header outright when date removal is enabled. The send time is removed regardless
        // of its format, without relying on the detector to recognize a date string. Resent-Date is left to
        // RemoveCommonHeaders (it goes with the Resent-* set).
        private static void RemoveDateHeader(MimeMessage message)
        {
            // A forwarded message's own Date is removed too.
            foreach (MimeMessage m in AllMessages(message))
            {
                try
                {
                    HeaderList headers = m.Headers;
                    for (int i = headers.Count - 1; i >= 0; i--)
                    {
                        if (headers[i].Id == HeaderId.Date)
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
        }

        // Deletes attachments outright when attachment removal is enabled. Attachment content is never
        // inspected or redacted — the whole part (and its filename) is removed from the message tree. Each
        // attachment is detached from its parent multipart (the documented MimeKit approach).
        private static void RemoveAttachments(MimeMessage message)
        {
            try
            {
                var attachments = new List<MimeEntity>();
                var parents = new List<Multipart>();
                var iterator = new MimeIterator(message);
                while (iterator.MoveNext())
                {
                    if (iterator.Parent is Multipart parent && iterator.Current is MimeEntity entity && entity.IsAttachment)
                    {
                        parents.Add(parent);
                        attachments.Add(entity);
                    }
                }
                for (int i = 0; i < attachments.Count; i++)
                {
                    parents[i].Remove(attachments[i]);
                }
            }
            catch
            {
                // best effort — an attachment quirk must never fail the redaction
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
            /// <summary>True for an HTML body part, which is redacted by its visible text (entity/tag aware).</summary>
            bool IsHtmlBody { get; }
            string Get();
            void Set(string value);
        }

        private sealed class HeaderField : IEmailField
        {
            private readonly Header _header;
            public HeaderField(Header header, string label) { _header = header; Label = label; }
            public string Label { get; }
            public bool IsBody => false;
            public bool IsHtmlBody => false;
            public string Get() => _header.Value ?? string.Empty;
            public void Set(string value) => _header.Value = value;
        }

        private sealed class TextPartField : IEmailField
        {
            private readonly TextPart _part;
            private readonly string _prefix;
            public TextPartField(TextPart part, string prefix = "") { _part = part; _prefix = prefix; }
            public string Label => _prefix + (_part.IsHtml ? "Body (HTML)" : "Body (text)");
            public bool IsBody => true;
            public bool IsHtmlBody => _part.IsHtml;
            public string Get() => _part.Text ?? string.Empty;
            public void Set(string value) => _part.Text = value;
        }

        /// <summary>
        /// The canonical, stable order of redactable fields: the Subject, From, To and Cc headers (each
        /// only if present), then every non-attachment text body part in document order — <b>recursing
        /// into any nested/forwarded message</b> (<c>message/rfc822</c>) so a forwarded email's own
        /// headers and body are redacted too. The order must match between <see cref="Redact"/> and
        /// <see cref="ApplySpans"/> so a stored field index re-applies to the same field.
        /// </summary>
        private static List<IEmailField> EnumerateFields(MimeMessage message)
        {
            var fields = new List<IEmailField>();
            AddMessageFields(message, string.Empty, fields);
            return fields;
        }

        // Adds one message level's header fields (Subject/From/To/Cc), then walks its body. A forwarded or
        // attached message (message/rfc822) is recursed into so its From/To/Cc/Subject and body are covered
        // — otherwise the embedded message's PII (and its Received/Bcc, via the header scrubs) would ship
        // unredacted. <paramref name="labelPrefix"/> marks nested fields in the preview.
        private static void AddMessageFields(MimeMessage message, string labelPrefix, List<IEmailField> fields)
        {
            foreach ((HeaderId id, string label) in new[]
                     {
                         (HeaderId.Subject, "Subject"), (HeaderId.From, "From"),
                         (HeaderId.To, "To"), (HeaderId.Cc, "Cc")
                     })
            {
                Header? header = message.Headers.FirstOrDefault(h => h.Id == id);
                if (header is not null)
                {
                    fields.Add(new HeaderField(header, labelPrefix + label));
                }
            }

            AddBodyFields(message.Body, labelPrefix, fields);
        }

        // Walks a body entity in document order, collecting redactable text parts and recursing into any
        // nested message. Attachments (including a forwarded message's own attachments) are skipped.
        private static void AddBodyFields(MimeEntity? entity, string labelPrefix, List<IEmailField> fields)
        {
            switch (entity)
            {
                case Multipart multipart:
                    foreach (MimeEntity child in multipart)
                    {
                        AddBodyFields(child, labelPrefix, fields);
                    }
                    break;
                case MessagePart { Message: not null } messagePart:
                    AddMessageFields(messagePart.Message, labelPrefix + "Forwarded: ", fields);
                    break;
                case TextPart textPart when !textPart.IsAttachment:
                    fields.Add(new TextPartField(textPart, labelPrefix));
                    break;
            }
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
            // RTF-only message: no HTML or plain-text body, but there is an RTF body. Recover its text so
            // the body — and any PII in it — isn't silently dropped from the redacted output.
            if (string.IsNullOrEmpty(builder.HtmlBody) && string.IsNullOrEmpty(builder.TextBody)
                && !string.IsNullOrEmpty(msg.BodyRtf))
            {
                string rtfText = RtfRedactor.ExtractText(msg.BodyRtf);
                if (!string.IsNullOrEmpty(rtfText))
                {
                    builder.TextBody = rtfText;
                }
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
            // Serialize in memory, then write once so a failure never leaves the original or a partial file.
            using var buffer = new MemoryStream();
            message.WriteTo(buffer);
            SafeOutput.Write(outputPath, buffer.ToArray());
        }
    }
}
