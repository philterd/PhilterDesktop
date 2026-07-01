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

using System.Threading;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// The single-instance guard (#544/#549): only the first GUI in a session "creates new"; a second
    /// launch detects the running one and can signal it to come to the front. Tested with unique names so
    /// it's deterministic and never touches the real session-scoped objects.
    /// </summary>
    public sealed class AppInstanceTests
    {
        private static string UniqueName() => $@"Local\PhilterDesktopTest.{Guid.NewGuid():N}";

        [Fact]
        public void CreateLifetime_FirstInstance_IsCreatedNew_SecondIsNot()
        {
            string name = UniqueName();

            using Mutex first = AppInstance.CreateLifetime(name, out bool firstNew);
            Assert.True(firstNew); // the first GUI owns the session

            using Mutex second = AppInstance.CreateLifetime(name, out bool secondNew);
            Assert.False(secondNew); // a second launch must see it already exists (and refuse to start)
        }

        [Fact]
        public void CreateLifetime_AfterFirstReleased_IsCreatedNewAgain()
        {
            string name = UniqueName();

            using (Mutex first = AppInstance.CreateLifetime(name, out bool firstNew))
            {
                Assert.True(firstNew);
            }

            using Mutex again = AppInstance.CreateLifetime(name, out bool againNew);
            Assert.True(againNew); // once the first exits, a fresh launch is the first instance again
        }

        [Fact]
        public void SignalExisting_NoRunningInstance_ReturnsFalse()
        {
            Assert.False(AppInstance.SignalExisting(UniqueName()));
        }

        [Fact]
        public void SignalExisting_RunningInstance_SignalsItsActivationListener()
        {
            string name = UniqueName();
            using EventWaitHandle listener = AppInstance.CreateActivationSignal(name);

            Assert.False(listener.WaitOne(0));                 // not signaled yet
            Assert.True(AppInstance.SignalExisting(name));     // a second launch asks it to come forward
            Assert.True(listener.WaitOne(1000));               // the running GUI's listener observes it
        }
    }
}
