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

using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterDesktop;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class RedactionConcurrencyGateTests
    {
        private static async Task<bool> CompletesWithin(Task task, int ms) =>
            await Task.WhenAny(task, Task.Delay(ms)) == task;

        [Theory]
        [InlineData(0, 1)]   // clamped up to 1
        [InlineData(1, 1)]
        [InlineData(4, 4)]
        [InlineData(100, 16)] // clamped down to 16
        public void MaxConcurrency_IsClamped(int requested, int expected)
        {
            using var gate = new RedactionConcurrencyGate(requested);
            Assert.Equal(expected, gate.MaxConcurrency);
        }

        [Fact]
        public async Task AllowsUpToMaxConcurrentSlots_ThenBlocks()
        {
            using var gate = new RedactionConcurrencyGate(3);

            IDisposable a = await gate.EnterAsync(solo: false);
            IDisposable b = await gate.EnterAsync(solo: false);
            IDisposable c = await gate.EnterAsync(solo: false); // 3 in flight

            Task<IDisposable> fourth = gate.EnterAsync(solo: false);
            Assert.False(await CompletesWithin(fourth, 200), "a 4th slot must wait");

            a.Dispose(); // free one
            Assert.True(await CompletesWithin(fourth, 2000), "the 4th slot should open up");

            (await fourth).Dispose();
            b.Dispose();
            c.Dispose();
        }

        [Fact]
        public async Task Solo_WaitsForAll_AndBlocksOthersWhileHeld()
        {
            using var gate = new RedactionConcurrencyGate(2);

            IDisposable small = await gate.EnterAsync(solo: false); // 1 of 2

            Task<IDisposable> soloTask = gate.EnterAsync(solo: true);
            Assert.False(await CompletesWithin(soloTask, 200), "solo must wait for all slots");

            small.Dispose();
            Assert.True(await CompletesWithin(soloTask, 2000), "solo runs once the gate is clear");
            IDisposable solo = await soloTask;

            Task<IDisposable> blocked = gate.EnterAsync(solo: false);
            Assert.False(await CompletesWithin(blocked, 200), "nothing runs while a solo holds the gate");

            solo.Dispose();
            Assert.True(await CompletesWithin(blocked, 2000));
            (await blocked).Dispose();
        }

        [Fact]
        public async Task TwoSoloEntries_Serialize_WithoutDeadlock()
        {
            using var gate = new RedactionConcurrencyGate(3);

            IDisposable solo1 = await gate.EnterAsync(solo: true);
            Task<IDisposable> solo2 = gate.EnterAsync(solo: true);
            Assert.False(await CompletesWithin(solo2, 200), "the second solo waits for the first");

            solo1.Dispose();
            Assert.True(await CompletesWithin(solo2, 2000), "the second solo proceeds — no deadlock");
            (await solo2).Dispose();
        }

        [Fact]
        public async Task SharedFilterService_ConcurrentFilter_MatchesSerialResults()
        {
            // The watched-folder service shares one FilterService across concurrent redactions, so this
            // guards the assumption that FilterService.Filter is safe to call concurrently.
            var fs = new FilterService();
            var policy = new PhileasPolicy
            {
                Name = "p",
                Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() }
            };

            string[] inputs = Enumerable.Range(0, 64)
                .Select(i => $"Row {i}: email user{i}@example.com and ssn 123-45-6789 end.")
                .ToArray();

            string[] serial = inputs.Select(t => fs.Filter(policy, "ctx", 0, t).FilteredText).ToArray();

            string[] concurrent = await Task.WhenAll(
                inputs.Select(t => Task.Run(() => fs.Filter(policy, "ctx", 0, t).FilteredText)));

            Assert.Equal(serial, concurrent); // identical results regardless of concurrency
            Assert.All(concurrent, r =>
            {
                Assert.DoesNotContain("@example.com", r);
                Assert.DoesNotContain("123-45-6789", r);
            });
        }
    }
}
