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
}
