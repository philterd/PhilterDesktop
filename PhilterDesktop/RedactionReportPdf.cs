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

using SkiaSharp;

namespace PhilterDesktop
{
    /// <summary>
    /// Renders a <see cref="RedactionReportModel"/> to a PDF using SkiaSharp's PDF backend. A simple
    /// top-to-bottom flow layout with automatic page breaks — enough for a one-or-two page certificate.
    /// </summary>
    internal static class RedactionReportPdf
    {
        private const float PageWidth = 612f;   // US Letter at 72 dpi
        private const float PageHeight = 792f;
        private const float Margin = 54f;
        private const float ContentWidth = PageWidth - 2 * Margin;
        private const float KeyColumn = 150f;

        /// <summary>Renders the report to PDF bytes.</summary>
        public static byte[] ToPdfBytes(RedactionReportModel m)
        {
            using var stream = new MemoryStream();
            using (var document = SKDocument.CreatePdf(stream))
            using (var w = new PageWriter(document))
            {
                w.Heading("Redaction Report", 22f);
                w.Muted($"Philter Desktop {m.ToolVersion}  -  generated {RedactionReport.FormatUtc(m.GeneratedUtc)}");
                w.Gap(8f);

                w.Section("Document");
                w.Field("Source file", m.SourcePath);
                w.Field("Redacted output", m.OutputPath);
                w.Field("File type", m.FileType);
                w.Field("Redacted on", RedactionReport.FormatUtc(m.RedactedUtc));
                w.Field("Policy", m.Policy);
                w.Field("Context", m.Context);
                if (m.Version > 0) w.Field("Version", m.Version.ToString());
                if (m.MachineName is not null) w.Field("Machine", m.MachineName);
                if (m.UserName is not null) w.Field("User", m.UserName);

                w.Section("File integrity (SHA-256)");
                w.Field("Source", m.SourceSha256, mono: true);
                w.Field("Redacted output", m.OutputSha256, mono: true);

                if (m.VerificationSummary is not null)
                {
                    w.Section("Verification");
                    w.Muted(m.VerificationSummary);
                }

                w.Section("What was removed");
                w.Heading($"{m.TotalRedactions} redaction{(m.TotalRedactions == 1 ? "" : "s")}", 16f);
                w.Gap(4f);
                foreach ((string type, int count) in m.CountsByType)
                {
                    w.Field(type, count.ToString());
                }

                if (m.Details.Count > 0)
                {
                    w.Section("Redaction detail");
                    w.Muted("Location and replacement only - the original text is not included.");
                    w.Gap(4f);
                    w.DetailHeader();
                    foreach (RedactionReportRow r in m.Details)
                    {
                        w.DetailRow(r);
                    }
                }

                w.Gap(12f);
                w.Note(RedactionReport.DraftReminder);

                w.Finish();
            }
            return stream.ToArray();
        }

        /// <summary>Renders the report to a PDF file.</summary>
        public static void Write(RedactionReportModel m, string path) =>
            File.WriteAllBytes(path, ToPdfBytes(m));

        // A font + ink pair (SkiaSharp 3.x splits sizing/typeface into SKFont and colour into SKPaint).
        private sealed class Style : IDisposable
        {
            public SKFont Font { get; }
            public SKPaint Paint { get; }
            public float LineHeight => Font.Spacing;

            public Style(float size, SKColor color, bool bold = false, bool mono = false)
            {
                SKTypeface face = SKTypeface.FromFamilyName(
                    mono ? "Consolas" : "Segoe UI",
                    bold ? SKFontStyle.Bold : SKFontStyle.Normal);
                Font = new SKFont(face, size);
                Paint = new SKPaint { Color = color, IsAntialias = true };
            }

            public float Measure(string s) => Font.MeasureText(s);

            public void Dispose() { Font.Dispose(); Paint.Dispose(); }
        }

        // A tiny flow-layout writer: tracks the cursor, starts pages, wraps text, and breaks pages.
        private sealed class PageWriter : IDisposable
        {
            private readonly SKDocument _document;
            private readonly Style _text;
            private readonly Style _muted;
            private readonly Style _key;
            private readonly Style _mono;
            private readonly Style _section;
            private readonly SKPaint _rule;

            private SKCanvas _canvas = null!;
            private float _y;

            public PageWriter(SKDocument document)
            {
                _document = document;
                _text = new Style(11f, SKColors.Black);
                _muted = new Style(10f, new SKColor(0x66, 0x66, 0x66));
                _key = new Style(11f, new SKColor(0x66, 0x66, 0x66));
                _mono = new Style(9.5f, SKColors.Black, mono: true);
                _section = new Style(13f, SKColors.Black, bold: true);
                _rule = new SKPaint { Color = new SKColor(0xDD, 0xDD, 0xDD), StrokeWidth = 0.75f, IsAntialias = true };
                NewPage();
            }

            private void NewPage()
            {
                _canvas = _document.BeginPage(PageWidth, PageHeight);
                _y = Margin;
            }

            private void EnsureSpace(float needed)
            {
                if (_y + needed > PageHeight - Margin)
                {
                    _document.EndPage();
                    NewPage();
                }
            }

            public void Gap(float h) => _y += h;

            public void Heading(string s, float size)
            {
                using var p = new Style(size, SKColors.Black, bold: true);
                EnsureSpace(p.LineHeight);
                _y += p.LineHeight;
                _canvas.DrawText(s, Margin, _y, p.Font, p.Paint);
                _y += 2f;
            }

            public void Muted(string s) => Wrapped(s, _muted, Margin, ContentWidth);

            public void Note(string s)
            {
                Gap(2f);
                Wrapped(s, _muted, Margin, ContentWidth);
            }

            public void Section(string title)
            {
                Gap(14f);
                EnsureSpace(_section.LineHeight + 8f);
                _y += _section.LineHeight;
                _canvas.DrawText(title, Margin, _y, _section.Font, _section.Paint);
                _y += 4f;
                _canvas.DrawLine(Margin, _y, Margin + ContentWidth, _y, _rule);
                _y += 6f;
            }

            public void Field(string key, string value, bool mono = false)
            {
                Style valueStyle = mono ? _mono : _text;
                float valueX = Margin + KeyColumn;
                float valueWidth = ContentWidth - KeyColumn;

                // Pre-wrap the value to know how tall the row is, then draw the key beside the first line.
                List<string> lines = WrapLines(value, valueStyle, valueWidth);
                float rowHeight = Math.Max(lines.Count, 1) * valueStyle.LineHeight;
                EnsureSpace(rowHeight);

                float startY = _y + valueStyle.LineHeight;
                _canvas.DrawText(key, Margin, startY, _key.Font, _key.Paint);
                float ly = startY;
                foreach (string line in lines)
                {
                    _canvas.DrawText(line, valueX, ly, valueStyle.Font, valueStyle.Paint);
                    ly += valueStyle.LineHeight;
                }
                _y += rowHeight + 2f;
            }

            public void DetailHeader()
            {
                EnsureSpace(_text.LineHeight + 4f);
                _y += _text.LineHeight;
                _canvas.DrawText("#", Margin, _y, _key.Font, _key.Paint);
                _canvas.DrawText("Type", Margin + 30f, _y, _key.Font, _key.Paint);
                _canvas.DrawText("Location", Margin + 180f, _y, _key.Font, _key.Paint);
                _canvas.DrawText("Replacement", Margin + 320f, _y, _key.Font, _key.Paint);
                _y += 3f;
                _canvas.DrawLine(Margin, _y, Margin + ContentWidth, _y, _rule);
                _y += 4f;
            }

            public void DetailRow(RedactionReportRow r)
            {
                EnsureSpace(_text.LineHeight);
                _y += _text.LineHeight;
                _canvas.DrawText(r.Index.ToString(), Margin, _y, _text.Font, _text.Paint);
                _canvas.DrawText(Clip(r.Type, _text, 145f), Margin + 30f, _y, _text.Font, _text.Paint);
                _canvas.DrawText(Clip(r.Location, _text, 135f), Margin + 180f, _y, _text.Font, _text.Paint);
                _canvas.DrawText(Clip(r.Replacement, _mono, ContentWidth - 320f), Margin + 320f, _y, _mono.Font, _mono.Paint);
                _y += 1f;
            }

            private void Wrapped(string s, Style style, float x, float width)
            {
                foreach (string line in WrapLines(s, style, width))
                {
                    EnsureSpace(style.LineHeight);
                    _y += style.LineHeight;
                    _canvas.DrawText(line, x, _y, style.Font, style.Paint);
                }
            }

            // Greedy word-wrap, with a hard break for single tokens longer than the column (e.g. a hash).
            private static List<string> WrapLines(string s, Style style, float width)
            {
                var lines = new List<string>();
                if (string.IsNullOrEmpty(s))
                {
                    lines.Add(string.Empty);
                    return lines;
                }

                foreach (string rawLine in s.Replace("\r", "").Split('\n'))
                {
                    string current = string.Empty;
                    foreach (string word in rawLine.Split(' '))
                    {
                        string candidate = current.Length == 0 ? word : current + " " + word;
                        if (style.Measure(candidate) <= width)
                        {
                            current = candidate;
                            continue;
                        }
                        if (current.Length > 0)
                        {
                            lines.Add(current);
                            current = string.Empty;
                        }
                        // The word itself is too wide: break it character by character.
                        string chunk = string.Empty;
                        foreach (char c in word)
                        {
                            if (style.Measure(chunk + c) > width && chunk.Length > 0)
                            {
                                lines.Add(chunk);
                                chunk = string.Empty;
                            }
                            chunk += c;
                        }
                        current = chunk;
                    }
                    lines.Add(current);
                }
                return lines;
            }

            private static string Clip(string s, Style style, float width)
            {
                if (style.Measure(s) <= width)
                {
                    return s;
                }
                const string ellipsis = "...";
                string result = s;
                while (result.Length > 0 && style.Measure(result + ellipsis) > width)
                {
                    result = result.Substring(0, result.Length - 1);
                }
                return result + ellipsis;
            }

            public void Finish() => _document.EndPage();

            public void Dispose()
            {
                _text.Dispose(); _muted.Dispose(); _key.Dispose(); _mono.Dispose();
                _section.Dispose(); _rule.Dispose();
            }
        }
    }
}
