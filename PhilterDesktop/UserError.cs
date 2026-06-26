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
    /// <summary>
    /// Turns low-level file-operation exceptions into short, plain-language guidance for end users,
    /// so a redaction tool aimed at non-technical professionals doesn't surface raw .NET errors.
    /// </summary>
    internal static class UserError
    {
        // Win32 error codes (low word of HResult) for the common, explainable file failures.
        private const int ErrorSharingViolation = 32;
        private const int ErrorLockViolation = 33;
        private const int ErrorHandleDiskFull = 39;
        private const int ErrorDiskFull = 112;

        /// <summary>
        /// A complete, friendly message for a failed file open/save, tailored to the likely cause.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <param name="path">The file involved (its name is shown to the user).</param>
        /// <param name="writing">True for a save/write operation; false for an open/read.</param>
        public static string Describe(Exception ex, string path, bool writing)
        {
            string verb = writing ? "save" : "open";
            string name = string.IsNullOrEmpty(path) ? "the file" : $"\"{Path.GetFileName(path)}\"";

            return ex switch
            {
                UnauthorizedAccessException =>
                    $"Philter Desktop could not {verb} {name}. You may not have permission to use that " +
                    "location, or the file may be read-only. Try a different folder.",

                IOException io when IsCode(io, ErrorSharingViolation, ErrorLockViolation) =>
                    $"Philter Desktop could not {verb} {name} because it is open in another program " +
                    "(such as Microsoft Word or a PDF viewer). Please close it and try again.",

                IOException io when IsCode(io, ErrorHandleDiskFull, ErrorDiskFull) =>
                    $"Philter Desktop could not {verb} {name} because there is not enough free disk space. " +
                    "Free up some space and try again.",

                DirectoryNotFoundException =>
                    $"Philter Desktop could not {verb} {name} because the folder no longer exists. " +
                    "Choose a different location and try again.",

                FileNotFoundException =>
                    $"Philter Desktop could not find {name}. It may have been moved or deleted.",

                _ => $"Philter Desktop could not {verb} {name}." + Environment.NewLine + Environment.NewLine + ex.Message,
            };
        }

        private static bool IsCode(IOException io, params int[] codes)
        {
            int code = io.HResult & 0xFFFF;
            return Array.IndexOf(codes, code) >= 0;
        }
    }
}
