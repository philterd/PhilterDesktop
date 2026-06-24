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

using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class CommandLineOptionsTests
    {
        [Fact]
        public void Parse_PolicyContextAndFiles_AsInExample()
        {
            var o = CommandLineOptions.Parse(new[] { "/p", "mypolicy", "/c", "mycontext", "file1.pdf", "file2.pdf", "file3.pdf" });

            Assert.Equal("mypolicy", o.Policy);
            Assert.Equal("mycontext", o.Context);
            Assert.Equal(new[] { "file1.pdf", "file2.pdf", "file3.pdf" }, o.Files);
            Assert.True(o.IsCommandLine);
            Assert.False(o.ShowHelp);
        }

        [Fact]
        public void Parse_FilesOnly_PolicyAndContextDefaultToNull()
        {
            var o = CommandLineOptions.Parse(new[] { "a.txt", "b.docx" });

            Assert.Null(o.Policy);
            Assert.Null(o.Context);
            Assert.Equal(new[] { "a.txt", "b.docx" }, o.Files);
            Assert.True(o.IsCommandLine);
        }

        [Theory]
        [InlineData("-p")]
        [InlineData("--policy")]
        public void Parse_AcceptsShortAndLongPolicyFlags(string flag)
        {
            var o = CommandLineOptions.Parse(new[] { flag, "secret", "f.pdf" });
            Assert.Equal("secret", o.Policy);
            Assert.Equal(new[] { "f.pdf" }, o.Files);
        }

        [Theory]
        [InlineData("-c")]
        [InlineData("--context")]
        public void Parse_AcceptsShortAndLongContextFlags(string flag)
        {
            var o = CommandLineOptions.Parse(new[] { flag, "ctx", "f.pdf" });
            Assert.Equal("ctx", o.Context);
        }

        [Fact]
        public void Parse_NoArgs_IsNotCommandLine()
        {
            var o = CommandLineOptions.Parse(Array.Empty<string>());
            Assert.False(o.IsCommandLine);
            Assert.Empty(o.Files);
        }

        [Fact]
        public void Parse_HelpFlag_IsCommandLineEvenWithoutFiles()
        {
            var o = CommandLineOptions.Parse(new[] { "--help" });
            Assert.True(o.ShowHelp);
            Assert.True(o.IsCommandLine);
            Assert.Empty(o.Files);
        }

        [Theory]
        [InlineData("--minimized")]
        [InlineData("-m")]
        public void Parse_MinimizedSwitch_IsIgnoredNotTreatedAsFile(string flag)
        {
            var o = CommandLineOptions.Parse(new[] { flag });
            Assert.Empty(o.Files);
            Assert.False(o.IsCommandLine); // stays in GUI mode
        }

        [Fact]
        public void Parse_DanglingPolicyFlag_DoesNotThrow()
        {
            var o = CommandLineOptions.Parse(new[] { "/p" }); // no value
            Assert.Null(o.Policy);
            Assert.False(o.IsCommandLine);
        }

        [Fact]
        public void Parse_ShellSwitch_SetsShellInvokedAndIsNotAFile()
        {
            var o = CommandLineOptions.Parse(new[] { "--shell", "a.pdf", "b.pdf" });
            Assert.True(o.ShellInvoked);
            Assert.Equal(new[] { "a.pdf", "b.pdf" }, o.Files);
            Assert.True(o.IsCommandLine);
        }

        [Fact]
        public void Parse_NoShellSwitch_ShellInvokedIsFalse()
        {
            var o = CommandLineOptions.Parse(new[] { "a.pdf" });
            Assert.False(o.ShellInvoked);
        }
    }
}
