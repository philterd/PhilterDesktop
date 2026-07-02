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

using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Sanity tests for entity defaults relied on by the app.</summary>
    public sealed class EntityDefaultsTests
    {
        [Fact]
        public void RedactionQueueEntity_DefaultsToPending_WithId()
        {
            var entity = new RedactionQueueEntity();
            Assert.Equal("Pending", entity.Status);
            Assert.NotNull(entity.Id);
            Assert.Equal(string.Empty, entity.Name);
        }

        [Fact]
        public void SettingsEntity_DefaultsToOriginalLocation_LoggingOff()
        {
            var settings = new SettingsEntity();
            Assert.True(settings.OutputToOriginalLocation);
            Assert.False(settings.LoggingEnabled);
            Assert.Equal(string.Empty, settings.CustomOutputFolder);
        }

        [Fact]
        public void SettingsEntity_Verification_DefaultsToOn_AndBroadPolicy()
        {
            var settings = new SettingsEntity();
            Assert.True(settings.VerifyAfterRedaction);
            // Broad is the default: same-policy verification can't catch the missed-PII case.
            Assert.True(settings.VerificationUseBroadPolicy);
        }

        [Fact]
        public void PolicyEntity_HasIdentifiersJsonDefault()
        {
            var policy = new PolicyEntity();
            Assert.Contains("Identifiers", policy.Json);
            Assert.NotNull(policy.Id);
        }

        [Fact]
        public void ContextEntity_HasGeneratedId()
        {
            Assert.NotNull(new ContextEntity().Id);
        }
    }
}
