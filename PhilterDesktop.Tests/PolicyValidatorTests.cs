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
    /// Verifies Philter Desktop policies validate against the PhiSQL policy schema that ships with
    /// phileas-dotnet (the engine's own contract).
    /// </summary>
    public class PolicyValidatorTests
    {
        [Fact]
        public void SchemaVersion_IsReported()
        {
            Assert.False(string.IsNullOrWhiteSpace(PolicyValidator.SchemaVersion));
        }

        [Fact]
        public void EmptyPolicy_IsValid()
        {
            Assert.True(PolicyValidator.Validate("{}").IsValid);
        }

        [Fact]
        public void DefaultPolicy_ValidatesAgainstPhiSqlSchema()
        {
            // The starter policy we generate must conform to the engine's schema.
            PolicyValidationResult result = PolicyValidator.Validate(DefaultPolicy.Json());
            Assert.True(result.IsValid, "Default policy failed schema validation: " + string.Join(" | ", result.Errors));
        }

        [Fact]
        public void Malformed_Json_IsInvalidWithMessage()
        {
            PolicyValidationResult result = PolicyValidator.Validate("{ not json");
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void WronglyTypedField_IsInvalid()
        {
            // "identifiers" must be an object, not a string — the schema should reject this.
            PolicyValidationResult result = PolicyValidator.Validate("{\"identifiers\": \"nope\"}");
            Assert.False(result.IsValid);
            Assert.NotEmpty(result.Errors);
        }
    }
}
