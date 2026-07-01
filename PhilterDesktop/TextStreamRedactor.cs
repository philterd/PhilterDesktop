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
using Phileas.Model;

namespace PhilterDesktop
{
    /// <summary>
    /// Redacts a very large plain-text file without loading it whole into memory. It slides an
    /// overlapping window over the file: each window is filtered as one string (so detectors see full
    /// local context), and an entity that straddles a window boundary is still caught because the
    /// overlap is wider than any entity we can detect. Output is written incrementally to a temp file,
    /// then committed. Normal-sized files use the simpler whole-string path in RedactionService; this is
    /// only engaged above <see cref="StreamAboveBytes"/>.
    ///
    /// Caveat: context-sensitive detectors (the on-device name model, confidence/disambiguation that look
    /// at surrounding text) can differ very slightly from the whole-string pass near a window seam. Regex
    /// and dictionary detectors are unaffected. The overlap bounds the longest entity that can span a
    /// seam — an entity longer than <see cref="OverlapChars"/> chars is the only theoretical gap.
    /// </summary>
    internal static class TextStreamRedactor
    {
        /// <summary>Plain-text files larger than this stream; at or below it, the whole-string path is used.</summary>
        public const long StreamAboveBytes = 50L * 1024 * 1024; // 50 MB

        private const int ChunkChars = 8 * 1024 * 1024;   // ~8M-char core committed per window
        private const int OverlapChars = 1 * 1024 * 1024;  // 1M-char overlap = longest entity that can span a seam
        private const int ReadBufferChars = 64 * 1024;
        private const int MaxNudgeChars = 4096;            // how far back to look for a whitespace split point

        public static List<Span> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter, Encoding encoding) =>
            Redact(inputPath, outputPath, filter, encoding, ChunkChars, OverlapChars);

        // Chunk/overlap are parameters so tests can force many small windows over a small file.
        internal static List<Span> Redact(string inputPath, string outputPath, Func<string, TextFilterResult> filter,
            Encoding encoding, int chunkChars, int overlapChars)
        {
            var globalSpans = new List<Span>();
            string tempPath = outputPath + ".redacting-" + Guid.NewGuid().ToString("N") + ".tmp";
            bool committed = false;
            try
            {
                using (var reader = new StreamReader(inputPath, encoding, detectEncodingFromByteOrderMarks: true))
                using (var writer = new StreamWriter(tempPath, append: false, encoding))
                {
                    var buffer = new char[ReadBufferChars];
                    var window = new StringBuilder();
                    long windowStart = 0; // global char index of window[0]

                    while (true)
                    {
                        bool exhausted = FillWindow(reader, window, chunkChars + overlapChars, buffer);
                        if (window.Length == 0)
                        {
                            break; // nothing left to process
                        }

                        string text = window.ToString();
                        bool lastWindow = exhausted; // the window holds the remaining tail of the file

                        List<Span> spans = filter(text).Spans
                            .Where(s => s.CharacterStart >= 0 && s.CharacterEnd <= text.Length && s.CharacterEnd > s.CharacterStart)
                            .OrderBy(s => s.CharacterStart)
                            .ToList();

                        // Commit entities that start before the horizon; the overlap guarantees they're
                        // fully contained. Entities starting in the overlap tail are deferred to the next
                        // window, where they'll sit inside the core with full local context.
                        int horizon = lastWindow ? text.Length : Math.Min(chunkChars, text.Length);
                        List<Span> committedSpans = spans.Where(s => s.CharacterStart < horizon).ToList();

                        // Because engine spans never overlap, the first deferred entity starts at or after
                        // the last committed entity's end, so this write boundary never bisects an entity.
                        int committedEnd = committedSpans.Count > 0 ? committedSpans[^1].CharacterEnd : 0;
                        int writeEnd = lastWindow ? text.Length : Math.Max(horizon, committedEnd);

                        // Prefer to split just after a whitespace/newline so the next window begins on a
                        // clean word/line boundary — steadier context for context-sensitive detectors at a
                        // seam. Only ever moves the boundary back (never past a deferred entity) and never
                        // into a committed entity, and falls back to the raw split for a long unbroken run.
                        if (!lastWindow)
                        {
                            writeEnd = NudgeBackToBoundary(text, writeEnd, committedEnd);
                        }

                        int last = 0;
                        foreach (Span s in committedSpans)
                        {
                            int localStart = s.CharacterStart;
                            int localEnd = s.CharacterEnd;
                            if (localStart > last)
                            {
                                writer.Write(text.AsSpan(last, localStart - last));
                            }
                            writer.Write(s.Replacement ?? string.Empty);
                            last = localEnd;

                            // Re-home the span to the whole-file coordinate space for the returned history.
                            s.CharacterStart = checked((int)(windowStart + localStart));
                            s.CharacterEnd = checked((int)(windowStart + localEnd));
                            globalSpans.Add(s);
                        }
                        if (writeEnd > last)
                        {
                            writer.Write(text.AsSpan(last, writeEnd - last));
                        }

                        if (lastWindow)
                        {
                            break;
                        }

                        window.Remove(0, writeEnd);
                        windowStart += writeEnd;
                    }
                }

                File.Move(tempPath, outputPath, overwrite: true);
                committed = true;
            }
            finally
            {
                if (!committed)
                {
                    try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* best effort */ }
                }
            }

            return globalSpans;
        }

        // Moves the split back to just after the nearest whitespace/newline within a bounded distance, so
        // the next window starts on a clean word/line boundary. Never goes at or before <paramref
        // name="committedEnd"/> (would re-open a committed entity); returns the original split when a long
        // unbroken run has no nearby whitespace. Because it only ever moves the split earlier, a deferred
        // entity (which starts at or after the split) is never pulled into this window.
        private static int NudgeBackToBoundary(string text, int writeEnd, int committedEnd)
        {
            int floor = Math.Max(committedEnd, writeEnd - MaxNudgeChars);
            for (int p = writeEnd - 1; p >= floor; p--)
            {
                if (char.IsWhiteSpace(text[p]))
                {
                    return p + 1; // include the whitespace; the next window begins at the following char
                }
            }
            return writeEnd;
        }

        // Reads into <paramref name="window"/> until it holds <paramref name="target"/> chars or the
        // reader is exhausted. Never reads past <paramref name="target"/>. Returns true when the reader
        // has no more characters (so the caller knows this window is the file's tail).
        private static bool FillWindow(StreamReader reader, StringBuilder window, int target, char[] buffer)
        {
            while (window.Length < target)
            {
                int want = Math.Min(buffer.Length, target - window.Length);
                int n = reader.Read(buffer, 0, want);
                if (n == 0)
                {
                    return true; // reader exhausted while filling
                }
                window.Append(buffer, 0, n);
            }
            return false; // filled to target; the next FillWindow reports EOF via a zero-length read
        }
    }
}
