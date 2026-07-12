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
using System.Text.RegularExpressions;
using Phileas.Model;
using PhilterData;
using UglyToad.PdfPig;
using UglyToad.PdfPig.AcroForms.Fields;

namespace PhilterDesktop
{
    /// <summary>
    /// PDF redaction flattens each page to an image, and it does <b>not</b> render annotations (sticky
    /// notes, free-text, highlights) or form-field values into that image — so their text is removed from
    /// the output. The redaction pass still detects and reports any PII in them, but the user should be
    /// told the content itself isn't carried over (they may need it, and it isn't re-scanned on the output).
    /// </summary>
    internal static class PdfFidelity
    {
        /// <summary>The heads-up shown when a PDF has annotation or form-field text.</summary>
        public const string Warning =
            "This PDF has annotations or form fields. Their text is not carried into the redacted image, so " +
            "it is removed from the output (any sensitive information detected in it is still reported). " +
            "Review the original if you need that content.";

        /// <summary>The caveat added to a <em>verification</em> result for such a PDF.</summary>
        public const string VerificationCaveat =
            "This PDF had annotations or form fields. Their text is removed from the redacted image and isn't " +
            "re-scanned here, so this result covers the page content only. Review the original if you need " +
            "that content.";

        /// <summary>
        /// True when <paramref name="inputPath"/> is a <c>.pdf</c> that has annotation text or a non-empty
        /// AcroForm text field (content that PDF redaction removes without rendering). Non-PDF and unreadable
        /// files return false.
        /// </summary>
        public static bool HasDroppedContent(string inputPath)
        {
            if (!string.Equals(Path.GetExtension(inputPath), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            try
            {
                using PdfDocument pdf = PdfDocument.Open(File.ReadAllBytes(inputPath));
                foreach (var page in pdf.GetPages())
                {
                    if (page.GetAnnotations().Any(a => !string.IsNullOrWhiteSpace(a.Content)))
                    {
                        return true;
                    }
                }
                return pdf.TryGetForm(out var form)
                    && form.Fields.OfType<AcroTextField>().Any(f => !string.IsNullOrWhiteSpace(f.Value));
            }
            catch
            {
                return false; // unreadable — let the normal redaction path handle/report it
            }
        }
    }

    /// <summary>
    /// The fidelity warning for a source whose content may not carry into the redacted output — an RTF's
    /// non-body parts (<see cref="RtfFidelity"/>) or a PDF's annotations/form fields
    /// (<see cref="PdfFidelity"/>) — or null when there is none. One place so every redaction path (queue,
    /// watched folders, CLI) reports the drop consistently.
    /// </summary>
    internal static class DroppedContentWarning
    {
        public static string? For(string inputPath) =>
            RtfFidelity.HasDroppedContent(inputPath) ? RtfFidelity.Warning
            : PdfFidelity.HasDroppedContent(inputPath) ? PdfFidelity.Warning
            : null;
    }
}

namespace PhilterDesktop
{
    /// <summary>
    /// RTF redaction round-trips a document through the Windows RichEdit body model, which does not carry
    /// non-body destinations — headers, footers, and footnotes — into the saved file. This detects when a
    /// source <c>.rtf</c> has such content so the user can be warned (rather than losing it silently).
    /// The wording is deliberately hedged: it does not promise what is or isn't kept, only that those
    /// parts may not carry over and the result should be reviewed.
    /// </summary>
    internal static partial class RtfFidelity
    {
        /// <summary>The heads-up shown when an RTF has header/footer/footnote content.</summary>
        public const string Warning =
            "RTF redaction works on the document body. Content in other parts — such as headers, footers, " +
            "and footnotes — may not be carried into the redacted file, so review the result. To keep those " +
            "parts, save the document as .docx and redact that instead.";

        /// <summary>
        /// The caveat added to a <em>verification</em> result for such an RTF: verification re-scans the
        /// body text only, so a clean result must not be read as confirming the non-body parts came through.
        /// </summary>
        public const string VerificationCaveat =
            "This RTF had headers, footers, or footnotes. Verification re-scans the body text only, so a " +
            "clean result does not confirm those parts were preserved — RTF redaction may not have carried " +
            "them into the output. To keep them, use .docx.";

        // Header/footer/footnote destination control words. RTF text never contains a raw backslash control
        // word (a literal backslash is escaped as "\\"), so matching the token is a reliable signal. The
        // negative lookahead stops "\footer" from also matching a longer word that merely starts the same.
        [GeneratedRegex(@"\\(?:header[lrf]?|footer[lrf]?|footnote)(?![a-zA-Z])", RegexOptions.Compiled)]
        private static partial Regex NonBodyDestinationRegex();

        /// <summary>
        /// True when <paramref name="inputPath"/> is an <c>.rtf</c> whose markup contains header, footer,
        /// or footnote destinations (which RTF redaction does not carry into the output). Non-RTF files
        /// and unreadable files return false.
        /// </summary>
        public static bool HasDroppedContent(string inputPath)
        {
            if (!string.Equals(Path.GetExtension(inputPath), ".rtf", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            try
            {
                // RTF is a 7-bit-safe format; Latin1 round-trips the bytes so control words read correctly.
                string rtf = Encoding.Latin1.GetString(File.ReadAllBytes(inputPath));
                return NonBodyDestinationRegex().IsMatch(rtf);
            }
            catch
            {
                return false; // unreadable — let the normal redaction path handle/report it
            }
        }
    }

    /// <summary>
    /// Detects a text file's encoding from its byte-order mark so a redacted copy can be written back in
    /// the same encoding (otherwise a UTF-16 or UTF-8-with-BOM source is silently re-encoded to UTF-8
    /// no-BOM, changing the file's bytes and dropping the BOM). Shared by the plain-text (.txt) and CSV
    /// paths so both preserve encoding consistently. Falls back to UTF-8 without a BOM.
    /// </summary>
    internal static class TextEncodingDetector
    {
        public static Encoding Detect(string path)
        {
            try
            {
                using FileStream fs = File.OpenRead(path);
                Span<byte> bom = stackalloc byte[3];
                int read = fs.Read(bom);
                if (read >= 3 && bom[0] == 0xEF && bom[1] == 0xBB && bom[2] == 0xBF)
                {
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);    // UTF-8 with BOM
                }
                if (read >= 2 && bom[0] == 0xFF && bom[1] == 0xFE)
                {
                    return new UnicodeEncoding(bigEndian: false, byteOrderMark: true); // UTF-16 LE
                }
                if (read >= 2 && bom[0] == 0xFE && bom[1] == 0xFF)
                {
                    return new UnicodeEncoding(bigEndian: true, byteOrderMark: true);  // UTF-16 BE
                }
            }
            catch
            {
                // unreadable preamble — fall through to the default
            }
            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);           // UTF-8, no BOM
        }
    }

    /// <summary>A text replacement range used when re-applying redaction spans.</summary>
    internal readonly record struct ReplacementRange(int Start, int End, string Replacement);

    /// <summary>Resolves and applies redaction spans/ranges to text (the heart of producing redacted output).</summary>
    internal static class RedactionSpanMath
    {
        /// <summary>
        /// Sorts ranges by start and drops any that overlap one already kept (highest priority is
        /// earliest start). Returns them ordered by start ascending.
        /// </summary>
        public static List<ReplacementRange> ResolveNonOverlapping(IEnumerable<ReplacementRange> ranges)
        {
            var result = new List<ReplacementRange>();
            int lastEnd = -1;
            foreach (ReplacementRange r in ranges.Where(r => r.End > r.Start).OrderBy(r => r.Start).ThenByDescending(r => r.End))
            {
                if (r.Start >= lastEnd)
                {
                    result.Add(r);
                    lastEnd = r.End;
                }
            }
            return result;
        }

        /// <summary>
        /// Builds replacement ranges from spans that are valid for text of <paramref name="textLength"/>
        /// (in-bounds and non-empty), substituting <paramref name="defaultReplacement"/> when a span
        /// carries no replacement.
        /// </summary>
        public static List<ReplacementRange> BuildRanges(IEnumerable<RedactionSpanEntity> spans, int textLength, string defaultReplacement)
        {
            var ranges = new List<ReplacementRange>();
            foreach (RedactionSpanEntity s in spans)
            {
                if (s.CharacterStart >= 0 && s.CharacterEnd <= textLength && s.CharacterEnd > s.CharacterStart)
                {
                    string replacement = string.IsNullOrEmpty(s.Replacement) ? defaultReplacement : s.Replacement;
                    ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, replacement));
                }
            }
            return ranges;
        }

        /// <summary>
        /// Applies the ranges to <paramref name="text"/>, resolving overlaps first and applying
        /// right-to-left so earlier offsets stay valid as the string changes length.
        /// </summary>
        public static string Apply(string text, IEnumerable<ReplacementRange> ranges)
        {
            var sb = new StringBuilder(text);
            foreach (ReplacementRange r in ResolveNonOverlapping(ranges).OrderByDescending(r => r.Start))
            {
                sb.Remove(r.Start, r.End - r.Start);
                sb.Insert(r.Start, r.Replacement ?? string.Empty);
            }
            return sb.ToString();
        }

        /// <summary>Builds ranges from <paramref name="spans"/> and applies them to <paramref name="text"/>.</summary>
        public static string ApplySpans(string text, IEnumerable<RedactionSpanEntity> spans, string defaultReplacement)
            => Apply(text, BuildRanges(spans, text.Length, defaultReplacement));
    }

    /// <summary>
    /// Guards against runaway (catastrophic-backtracking) regular expressions. Detection patterns can
    /// come from user-supplied or imported policies (custom identifiers), so a pathological pattern such
    /// as <c>(a+)+$</c> over a large document could otherwise hang the redaction thread forever.
    /// </summary>
    internal static class RegexSafety
    {
        /// <summary>Smallest selectable match timeout, in seconds — also the safe default floor.</summary>
        public const int MinTimeoutSeconds = 5;

        /// <summary>Largest selectable match timeout, in seconds.</summary>
        public const int MaxTimeoutSeconds = 15;

        /// <summary>
        /// Per-process cap on how long a single regex match may run. .NET applies this as the default for
        /// every <see cref="Regex"/> created without its own timeout — including those built inside the
        /// redaction engine — turning an unbounded hang into a bounded, surfaced error.
        /// </summary>
        public static readonly TimeSpan DefaultMatchTimeout = TimeSpan.FromSeconds(MinTimeoutSeconds);

        // The AppDomain data key the .NET regex engine reads its default match timeout from. The value
        // must be a TimeSpan despite the "_MS" suffix.
        private const string RegexTimeoutKey = "REGEX_DEFAULT_MATCH_TIMEOUT_MS";

        /// <summary>Clamps a configured timeout to the supported <see cref="MinTimeoutSeconds"/>–<see cref="MaxTimeoutSeconds"/> range.</summary>
        public static int ClampSeconds(int seconds) => Math.Clamp(seconds, MinTimeoutSeconds, MaxTimeoutSeconds);

        /// <summary>
        /// Installs the safe default (floor) regex match timeout. Call once at process start, before the
        /// settings database is available, so a runaway pattern is bounded even during startup.
        /// </summary>
        public static void InstallDefaultMatchTimeout() => Install(DefaultMatchTimeout);

        /// <summary>
        /// Installs the user-configured regex match timeout (clamped to the supported range). Applied once
        /// settings are loaded and again whenever they change; it governs every regex compiled afterward,
        /// including the per-redaction custom-identifier patterns.
        /// </summary>
        public static void InstallMatchTimeout(int seconds) => Install(TimeSpan.FromSeconds(ClampSeconds(seconds)));

        private static void Install(TimeSpan timeout) => AppDomain.CurrentDomain.SetData(RegexTimeoutKey, timeout);

        /// <summary>
        /// True if <paramref name="pattern"/> is a syntactically valid regular expression. This checks
        /// <b>syntax only</b>; catastrophic-backtracking patterns still compile and are contained at match
        /// time by <see cref="DefaultMatchTimeout"/>, not here.
        /// </summary>
        public static bool IsValidPattern(string? pattern, out string? error)
        {
            error = null;
            if (string.IsNullOrEmpty(pattern))
            {
                return true; // no pattern; treated as a no-op by the engine
            }
            try
            {
                _ = new Regex(pattern);
                return true;
            }
            catch (ArgumentException ex) // includes RegexParseException
            {
                error = ex.Message;
                return false;
            }
        }
    }

    /// <summary>
    /// Copies the engine's "why was this flagged" detail from a Phileas <see cref="Span"/> onto a
    /// stored <see cref="RedactionSpanEntity"/>. Phileas has no separate <c>explain</c> call — every
    /// detection it returns already carries this detail — so we persist it at capture time for the
    /// "Export Explanation (JSON)" feature.
    /// </summary>
    internal static class SpanExplanation
    {
        public static void Populate(RedactionSpanEntity entity, Span span)
        {
            entity.FilterType = span.FilterType.ToString();
            entity.Confidence = span.Confidence;
            entity.Pattern = span.Pattern ?? string.Empty;
            entity.Window = span.Window is { Length: > 0 } ? new List<string>(span.Window) : new List<string>();
        }
    }

    /// <summary>
    /// The "Type" label shown for a captured span. Prefers the engine's classification (e.g. "name"
    /// from the on-device model), then the humanized filter that matched (e.g. "First Name"), and only
    /// falls back to a generic "Detected" when neither is known (older records missing both).
    /// </summary>
    internal static class SpanTypeLabel
    {
        public static string For(RedactionSpanEntity span)
        {
            if (!string.IsNullOrEmpty(span.Classification))
            {
                return span.Classification;
            }
            if (!string.IsNullOrEmpty(span.FilterType))
            {
                return PolicyEditing.FilterLabel.Humanize(span.FilterType);
            }
            return "Detected";
        }
    }
}
