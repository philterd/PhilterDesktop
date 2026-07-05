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
        private sealed class InMemoryStore : IAcceptanceStore
        {
            private readonly HashSet<string> _accepted = new();
            public bool HasAccepted(string key) => _accepted.Contains(key);
            public void RememberAccepted(string key) => _accepted.Add(key);
        }

        private static void WithStore(Action<InMemoryStore> test)
        {
            IAcceptanceStore original = Acknowledgements.Store;
            try
            {
                var store = new InMemoryStore();
                Acknowledgements.Store = store;
                test(store);
            }
            finally
            {
                Acknowledgements.Store = original;
            }
        }

        [Fact]
        public void ShouldShow_IsTrue_BeforeAcceptance() =>
            WithStore(store => Assert.True(LicenseForm.ShouldShow()));

        [Fact]
        public void RememberAccepted_PersistsAcceptance_AndStopsReprompting() => WithStore(store =>
        {
            Assert.True(LicenseForm.ShouldShow());

            LicenseForm.RememberAccepted();

            Assert.True(store.HasAccepted(Acknowledgements.LicenseKey));
            Assert.False(LicenseForm.ShouldShow()); // never shown again once accepted
        });

        [Fact]
        public void AlreadyAccepted_DoesNotShowOnLaunch() => WithStore(store =>
        {
            store.RememberAccepted(Acknowledgements.LicenseKey);
            Assert.False(LicenseForm.ShouldShow());
        });

        // The redaction-review notice is a second, independent gate.
        [Fact]
        public void RedactionNotice_HasItsOwnAcceptance() => WithStore(store =>
        {
            Assert.True(RedactionNoticeForm.ShouldShow());

            RedactionNoticeForm.RememberAccepted();

            Assert.True(store.HasAccepted(Acknowledgements.RedactionNoticeKey));
            Assert.False(RedactionNoticeForm.ShouldShow());
        });

        // The two gates are tracked by separate flags — accepting one must not satisfy the other.
        [Fact]
        public void LicenseAndNotice_AreIndependent() => WithStore(store =>
        {
            LicenseForm.RememberAccepted();
            Assert.False(LicenseForm.ShouldShow());
            Assert.True(RedactionNoticeForm.ShouldShow()); // still owed the notice

            RedactionNoticeForm.RememberAccepted();
            Assert.False(RedactionNoticeForm.ShouldShow());
        });

        [Fact]
        public void LicenseAndNotice_UseDistinctKeys() =>
            Assert.NotEqual(Acknowledgements.LicenseKey, Acknowledgements.RedactionNoticeKey);

        // Regression guard for the actual bug: there must be no opt-out checkbox/property whose unchecked
        // state would skip persisting acceptance.
        [Fact]
        public void LicenseForm_HasNoOptOutControl()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            Assert.Null(typeof(LicenseForm).GetField("_doNotShowAgain", flags));
            Assert.Null(typeof(LicenseForm).GetProperty("DoNotShowAgain", flags));
        }

        // The default (production) store is the registry-backed one.
        [Fact]
        public void DefaultStore_IsRegistryBacked() =>
            Assert.IsType<RegistryAcceptanceStore>(Acknowledgements.Store);
    }
}
