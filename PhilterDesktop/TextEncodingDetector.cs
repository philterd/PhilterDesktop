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

namespace PhilterDesktop
{
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
}
