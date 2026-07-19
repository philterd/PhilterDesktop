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
    /// <c>--selftest</c> redacts a built-in corpus in every format and verifies it (exit 0 = all passed).
    /// Where the on-device name model is bundled it also exercises name detection through the ONNX runtime.
    /// </summary>
    public sealed class SelfTestTests
    {
        [Fact]
        public void Run_RedactsBuiltInCorpus_ReturnsZero()
        {
            // End to end: email/ssn always; +ONNX name detection wherever the model is bundled.
            Assert.Equal(0, SelfTest.Run());
        }
    }
}
