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

using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Tests the exception → friendly-message mapping used for file open/save failures.
    /// </summary>
    public class UserErrorTests
    {
        // Win32 HResults (0x8007____ = facility-Win32 + error code in the low word).
        private const int SharingViolation = unchecked((int)0x80070020); // 32
        private const int LockViolation = unchecked((int)0x80070021);    // 33
        private const int DiskFull = unchecked((int)0x80070070);         // 112
        private const int HandleDiskFull = unchecked((int)0x80070027);   // 39

        [Theory]
        [InlineData(SharingViolation)]
        [InlineData(LockViolation)]
        public void Describe_FileInUse_SaysOpenInAnotherProgram(int hResult)
        {
            string msg = UserError.Describe(new IOException("locked", hResult), @"C:\out\report.pdf", writing: true);

            Assert.Contains("open in another program", msg);
            Assert.Contains("report.pdf", msg);   // shows the file name
            Assert.Contains("save", msg);          // write verb
        }

        [Theory]
        [InlineData(DiskFull)]
        [InlineData(HandleDiskFull)]
        public void Describe_DiskFull_MentionsDiskSpace(int hResult)
        {
            string msg = UserError.Describe(new IOException("full", hResult), @"C:\out\a.txt", writing: true);

            Assert.Contains("disk space", msg);
        }

        [Fact]
        public void Describe_Unauthorized_MentionsPermission()
        {
            string msg = UserError.Describe(new UnauthorizedAccessException(), @"C:\out\a.txt", writing: true);

            Assert.Contains("permission", msg);
        }

        [Fact]
        public void Describe_DirectoryNotFound_MentionsFolder()
        {
            string msg = UserError.Describe(new DirectoryNotFoundException(), @"C:\gone\a.txt", writing: true);

            Assert.Contains("folder no longer exists", msg);
        }

        [Fact]
        public void Describe_FileNotFound_MentionsMovedOrDeleted()
        {
            string msg = UserError.Describe(new FileNotFoundException(), @"C:\a.txt", writing: false);

            Assert.Contains("moved or deleted", msg);
        }

        [Fact]
        public void Describe_UsesOpenVerb_WhenReading()
        {
            // A generic IOException (no special HResult) falls back to the default branch with the verb.
            string msg = UserError.Describe(new IOException("boom"), @"C:\a.pdf", writing: false);

            Assert.Contains("open", msg);
            Assert.Contains("boom", msg);   // default branch includes the raw message
        }

        [Fact]
        public void Describe_UnknownException_FallsBackToRawMessage()
        {
            string msg = UserError.Describe(new InvalidOperationException("weird thing"), @"C:\a.txt", writing: true);

            Assert.Contains("weird thing", msg);
        }
    }
}
