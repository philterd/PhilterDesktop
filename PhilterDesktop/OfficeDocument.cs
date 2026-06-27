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

namespace PhilterDesktop
{
    /// <summary>The result of a quick structural check of a Word/Excel file.</summary>
    internal enum OfficeFileState
    {
        /// <summary>Looks like a normal (ZIP-based) Office Open XML file — proceed.</summary>
        Ok,

        /// <summary>An encrypted (password-protected) Office file: a compound file, not a ZIP.</summary>
        PasswordProtected,

        /// <summary>Neither a ZIP nor an encrypted Office file — corrupt or not really an Office file.</summary>
        NotReadable
    }

    /// <summary>
    /// Cheap, dependency-free inspection of <c>.docx</c>/<c>.xlsx</c> files by their first bytes. A
    /// valid Office Open XML file is a ZIP archive; a <b>password-protected</b> one is instead an OLE2
    /// compound file wrapping an encrypted package (which the Open XML SDK cannot open). Detecting these
    /// up front lets us show a clear message instead of a raw "file is corrupt" parse error.
    /// </summary>
    internal static class OfficeDocument
    {
        // OLE2 / Compound File Binary Format signature (used by encrypted OOXML, and by .msg/.doc/.xls).
        private static readonly byte[] Ole2Magic = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };

        // ZIP local-file-header signature ("PK\x03\x04") — the start of a normal .docx/.xlsx/.pptx.
        private static readonly byte[] ZipMagic = { 0x50, 0x4B, 0x03, 0x04 };

        public static OfficeFileState Inspect(string path)
        {
            byte[] head = new byte[8];
            int read;
            try
            {
                using FileStream stream = File.OpenRead(path);
                read = stream.Read(head, 0, head.Length);
            }
            catch
            {
                // Couldn't even read the bytes (locked, permission, missing); let the normal redaction
                // path raise the real I/O error so UserError can explain it.
                return OfficeFileState.Ok;
            }

            if (StartsWith(head, read, ZipMagic))
            {
                return OfficeFileState.Ok;
            }
            if (StartsWith(head, read, Ole2Magic))
            {
                return OfficeFileState.PasswordProtected;
            }
            return OfficeFileState.NotReadable;
        }

        private static bool StartsWith(byte[] buffer, int length, byte[] prefix)
        {
            if (length < prefix.Length)
            {
                return false;
            }
            for (int i = 0; i < prefix.Length; i++)
            {
                if (buffer[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// A redaction failure with an already-friendly, end-user message (e.g. a password-protected or
    /// corrupt document). <see cref="UserError.Describe"/> returns its message verbatim.
    /// </summary>
    internal sealed class DocumentLoadException : Exception
    {
        public DocumentLoadException(string message) : base(message)
        {
        }
    }
}
