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
    public sealed class PathUtilsTests
    {
        [Theory]
        [InlineData(@"C:\watch", @"C:\watch", true)]            // same folder
        [InlineData(@"C:\watch\sub", @"C:\watch", true)]        // nested
        [InlineData(@"C:\watch\a\b\c", @"C:\watch", true)]      // deeply nested
        [InlineData(@"C:\watch\sub\", @"C:\watch", true)]       // trailing separator
        [InlineData(@"C:\other", @"C:\watch", false)]           // sibling
        [InlineData(@"C:\watchother", @"C:\watch", false)]      // prefix but not a child
        [InlineData(@"C:\watch", @"C:\watch\sub", false)]       // ancestor is deeper
        public void IsSameOrInside_Works(string path, string ancestor, bool expected)
        {
            Assert.Equal(expected, PathUtils.IsSameOrInside(path, ancestor));
        }

        [Fact]
        public void IsSameOrInside_IsCaseInsensitive()
        {
            Assert.True(PathUtils.IsSameOrInside(@"C:\Watch\Sub", @"c:\watch"));
        }

        [Fact]
        public void IsSameOrInside_EmptyInputs_AreFalse()
        {
            Assert.False(PathUtils.IsSameOrInside("", @"C:\watch"));
            Assert.False(PathUtils.IsSameOrInside(@"C:\watch", ""));
        }
    }
}
