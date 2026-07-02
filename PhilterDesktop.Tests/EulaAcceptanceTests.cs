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

using System.Reflection;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// First-run EULA acceptance: agreeing once is persisted so the dialog is never re-shown.
    /// The previous opt-out "Don't show this again" checkbox is gone — leaving it unchecked used to
    /// re-prompt on every launch. Tests use an in-memory acceptance store so the real per-user
    /// registry is never touched.
    /// </summary>
    public sealed class EulaAcceptanceTests
    {
        private sealed class InMemoryStore : IEulaAcceptanceStore
        {
            public int RememberCalls { get; private set; }
            public bool Accepted { get; set; }
            public bool HasAccepted() => Accepted;
            public void RememberAccepted() { Accepted = true; RememberCalls++; }
        }

        private static void WithStore(Action<InMemoryStore> test)
        {
            IEulaAcceptanceStore original = WelcomeForm.AcceptanceStore;
            try
            {
                var store = new InMemoryStore();
                WelcomeForm.AcceptanceStore = store;
                test(store);
            }
            finally
            {
                WelcomeForm.AcceptanceStore = original;
            }
        }

        [Fact]
        public void ShouldShow_IsTrue_BeforeAcceptance() =>
            WithStore(store => Assert.True(WelcomeForm.ShouldShow()));

        [Fact]
        public void RememberAccepted_PersistsAcceptance_AndStopsReprompting() => WithStore(store =>
        {
            Assert.True(WelcomeForm.ShouldShow());

            WelcomeForm.RememberAccepted();

            Assert.True(store.Accepted);
            Assert.False(WelcomeForm.ShouldShow()); // never shown again once accepted
        });

        [Fact]
        public void AlreadyAccepted_DoesNotShowOnLaunch() => WithStore(store =>
        {
            store.Accepted = true;
            Assert.False(WelcomeForm.ShouldShow());
        });

        // Regression guard for the actual bug: there must be no opt-out checkbox/property whose unchecked
        // state would skip persisting acceptance.
        [Fact]
        public void WelcomeForm_HasNoOptOutControl()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            Assert.Null(typeof(WelcomeForm).GetField("_doNotShowAgain", flags));
            Assert.Null(typeof(WelcomeForm).GetProperty("DoNotShowAgain", flags));
        }

        // The default (production) store is the registry-backed one.
        [Fact]
        public void DefaultStore_IsRegistryBacked() =>
            Assert.IsType<RegistryEulaAcceptanceStore>(WelcomeForm.AcceptanceStore);
    }
}
