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

using Phileas.Services;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    public class FindAndRedactTests
    {
        [Fact]
        public void BuildPolicy_RedactsTheGivenTerms_RegardlessOfType()
        {
            // No built-in filter targets "Bluebird"/"Falcon" — only the ad-hoc terms should remove them.
            var policy = FindAndRedact.BuildPolicy(new[] { "Bluebird", "Falcon" });
            var fs = new FilterService();

            string result = fs.Filter(policy, "ctx", 0, "Operation Bluebird used the Falcon route.").FilteredText;

            Assert.DoesNotContain("Bluebird", result);
            Assert.DoesNotContain("Falcon", result);
        }

        [Fact]
        public void BuildPolicy_NoTerms_LeavesTextUnchanged()
        {
            var policy = FindAndRedact.BuildPolicy(System.Array.Empty<string>());
            var fs = new FilterService();

            const string text = "Nothing here should change.";
            Assert.Equal(text, fs.Filter(policy, "ctx", 0, text).FilteredText);
        }
    }
}
