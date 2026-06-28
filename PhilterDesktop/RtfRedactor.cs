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

using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using Phileas.Model;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts Rich Text Format (<c>.rtf</c>) documents. RTF is a control-word markup format, so
    /// blindly replacing text in the raw file would corrupt it. Instead we use the Windows
    /// <see cref="RichTextBox"/> control (the same engine WordPad uses) to parse the document, redact
    /// the <em>visible</em> text by character position — which automatically covers text in tables and
    /// fields — and write valid RTF back out, preserving the surrounding formatting.
    ///
    /// <para><see cref="RichTextBox"/> requires an STA thread, and the redaction pipeline runs on
    /// background (MTA) threads, so each operation runs on its own short-lived STA thread.</para>
    /// </summary>
    internal static class RtfRedactor
    {
        private const string DefaultReplacement = "{{{REDACTED-custom}}}";

        /// <summary>
        /// Returns the document's visible text (what the redaction operates on). Read-only; used by the
        /// preview to show a before/after diff without writing anything.
        /// </summary>
        public static string ReadText(string inputPath)
        {
            return RunSta(() =>
            {
                using var box = new RichTextBox();
                LoadSanitized(box, inputPath);
                return box.Text;
            });
        }

        /// <summary>
        /// Loads <paramref name="inputPath"/>, redacts its visible text with <paramref name="filter"/>,
        /// writes the result to <paramref name="outputPath"/>, and returns the applied spans. The input
        /// file is left untouched.
        /// </summary>
        public static List<RedactionSpanEntity> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter)
        {
            return RunSta(() =>
            {
                using var box = new RichTextBox();
                LoadSanitized(box, inputPath);

                string text = box.Text;
                TextFilterResult result = filter(text);

                List<Span> spans = result.Spans
                    .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= text.Length && s.CharacterEnd > s.CharacterStart)
                    .OrderBy(s => s.CharacterStart)
                    .ToList();

                var captured = new List<RedactionSpanEntity>();
                int order = 0;
                foreach (Span s in spans)
                {
                    var entity = new RedactionSpanEntity
                    {
                        Order = order++,
                        ParagraphIndex = -1,
                        CharacterStart = s.CharacterStart,
                        CharacterEnd = s.CharacterEnd,
                        Text = text.Substring(s.CharacterStart, s.CharacterEnd - s.CharacterStart),
                        Replacement = s.Replacement ?? string.Empty,
                        Classification = s.Classification ?? string.Empty
                    };
                    SpanExplanation.Populate(entity, s);
                    captured.Add(entity);
                }

                // Apply back-to-front so earlier offsets stay valid as later ones are spliced.
                foreach (Span s in spans.OrderByDescending(s => s.CharacterStart))
                {
                    box.Select(s.CharacterStart, s.CharacterEnd - s.CharacterStart);
                    box.SelectedText = s.Replacement ?? string.Empty;
                }

                box.SaveFile(outputPath, RichTextBoxStreamType.RichText);
                return captured;
            });
        }

        /// <summary>
        /// Re-applies an explicit (edited) span set to <paramref name="inputPath"/> by character
        /// position, writing the result to <paramref name="outputPath"/>. Used by Modify Redaction.
        /// </summary>
        public static void ApplySpans(string inputPath, string outputPath, IReadOnlyList<RedactionSpanEntity> spans)
        {
            RunSta<object?>(() =>
            {
                using var box = new RichTextBox();
                LoadSanitized(box, inputPath);

                string text = box.Text;
                var ranges = new List<ReplacementRange>();
                foreach (RedactionSpanEntity s in spans)
                {
                    if (s.CharacterStart >= 0 && s.CharacterEnd <= text.Length && s.CharacterEnd > s.CharacterStart)
                    {
                        string repl = string.IsNullOrEmpty(s.Replacement) ? DefaultReplacement : s.Replacement;
                        ranges.Add(new ReplacementRange(s.CharacterStart, s.CharacterEnd, repl));
                    }
                }

                foreach (ReplacementRange r in RedactionSpanMath.ResolveNonOverlapping(ranges).OrderByDescending(r => r.Start))
                {
                    box.Select(r.Start, r.End - r.Start);
                    box.SelectedText = r.Replacement ?? string.Empty;
                }

                box.SaveFile(outputPath, RichTextBoxStreamType.RichText);
                return null;
            });
        }

        // Loads the RTF into the control after stripping embedded OLE objects. Reading the file as
        // Latin1 round-trips every byte 1:1 (RTF is a 7-bit-safe format that escapes non-ASCII as
        // \'xx), so the only change is the removed object groups. Setting box.Rtf (rather than
        // LoadFile) lets us hand RichTextBox the already-sanitized markup so it never instantiates an
        // embedded OLE server.
        private static void LoadSanitized(RichTextBox box, string inputPath)
        {
            byte[] bytes = File.ReadAllBytes(inputPath);
            string rtf = Encoding.Latin1.GetString(bytes);
            box.Rtf = RtfSanitizer.RemoveEmbeddedObjects(rtf);
        }

        // RichTextBox requires an STA thread; the pipeline runs on MTA background threads, so do the
        // work on a dedicated short-lived STA thread and surface any error with its original stack.
        private static T RunSta<T>(Func<T> func)
        {
            T result = default!;
            ExceptionDispatchInfo? error = null;
            var thread = new Thread(() =>
            {
                try { result = func(); }
                catch (Exception ex) { error = ExceptionDispatchInfo.Capture(ex); }
            })
            {
                IsBackground = true
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            error?.Throw();
            return result;
        }
    }
}
