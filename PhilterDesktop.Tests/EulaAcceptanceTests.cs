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
    /// First-run EULA acceptance: agreeing is persisted with the EULA's version, so the dialog returns only
    /// when a newer EULA ships.
    /// The previous opt-out "Don't show this again" checkbox is gone — leaving it unchecked used to
    /// re-prompt on every launch. Tests use an in-memory acceptance store so the real per-user
    /// registry is never touched.
    /// </summary>
    public sealed class EulaAcceptanceTests
    {
        private sealed class InMemoryStore : IAcceptanceStore
        {
            private readonly HashSet<string> _accepted = new();
            private readonly Dictionary<string, string> _values = new();
            public bool HasAccepted(string key) => _accepted.Contains(key);
            public void RememberAccepted(string key) => _accepted.Add(key);
            public string? GetValue(string key) => _values.TryGetValue(key, out string? v) ? v : null;
            public void SetValue(string key, string value) => _values[key] = value;
        }

        private static readonly Version V2 = new(2, 0);

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
            LicenseForm.RememberAccepted(); // the bundled EULA, with its version
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

        // --- Versioned acceptance ---------------------------------------------

        [Fact]
        public void AcceptingAVersion_StopsReprompting_ForThatVersion() => WithStore(store =>
        {
            Assert.True(LicenseForm.ShouldShow(V2));
            LicenseForm.RememberAccepted(V2);
            Assert.False(LicenseForm.ShouldShow(V2));
            Assert.Equal(new Version(2, 0, 0, 0), LicenseForm.AcceptedEulaVersion());
        });

        [Fact]
        public void RememberAccepted_StoresTheVersion() => WithStore(store =>
        {
            LicenseForm.RememberAccepted(new Version(2, 1));
            Assert.True(store.HasAccepted(Acknowledgements.LicenseKey));
            Assert.Equal(new Version(2, 1, 0, 0), LicenseForm.AcceptedEulaVersion());
        });

        [Fact]
        public void NewerEula_Reprompts() => WithStore(store =>
        {
            LicenseForm.RememberAccepted(V2);
            Assert.True(LicenseForm.ShouldShow(new Version(2, 1)));
        });

        // Acceptances recorded before versioning existed carry no version, so any versioned EULA re-prompts.
        [Fact]
        public void PreVersioningAcceptance_RepromptsForAVersionedEula() => WithStore(store =>
        {
            store.RememberAccepted(Acknowledgements.LicenseKey);
            Assert.True(LicenseForm.ShouldShow(V2));
        });

        [Theory]
        [InlineData("2.0.0")]   // same version, different spelling
        [InlineData("1.0")]     // an older EULA (e.g. reinstalling an older release)
        public void SameOrOlderEula_DoesNotReprompt(string bundled) => WithStore(store =>
        {
            LicenseForm.RememberAccepted(V2);
            Assert.False(LicenseForm.ShouldShow(Version.Parse(bundled)));
        });

        // If the bundled EULA is missing or unversioned, a prior acceptance stands rather than nagging.
        [Fact]
        public void UnversionedBundledEula_KeepsPriorAcceptance() => WithStore(store =>
        {
            LicenseForm.RememberAccepted(V2);
            Assert.False(LicenseForm.ShouldShow(null));
        });

        [Fact]
        public void UnversionedBundledEula_StillShowsOnFirstRun() =>
            WithStore(store => Assert.True(LicenseForm.ShouldShow(null)));

        [Fact]
        public void AcceptingAnUnversionedEula_RecordsOnlyTheFlag() => WithStore(store =>
        {
            LicenseForm.RememberAccepted(null);
            Assert.True(store.HasAccepted(Acknowledgements.LicenseKey));
            Assert.Null(store.GetValue(Acknowledgements.LicenseVersionKey));
            Assert.True(LicenseForm.ShouldShow(V2)); // the next versioned EULA asks again
        });

        [Fact]
        public void CorruptStoredVersion_IsTreatedAsUnaccepted() => WithStore(store =>
        {
            store.RememberAccepted(Acknowledgements.LicenseKey);
            store.SetValue(Acknowledgements.LicenseVersionKey, "garbage");
            Assert.Null(LicenseForm.AcceptedEulaVersion());
            Assert.True(LicenseForm.ShouldShow(V2));
        });

        // When nothing is owed the gate returns at once without showing a dialog (a dialog would block here).
        [Fact]
        public void RunGateIfNeeded_NothingOwed_ReturnsTrueWithoutADialog() => WithStore(store =>
        {
            LicenseForm.RememberAccepted();
            Assert.True(LicenseForm.RunGateIfNeeded());
        });

        [Fact]
        public void LicenseKeys_AreDistinct()
        {
            Assert.NotEqual(Acknowledgements.LicenseKey, Acknowledgements.LicenseVersionKey);
            Assert.NotEqual(Acknowledgements.LicenseVersionKey, Acknowledgements.RedactionNoticeKey);
        }

        // The EULA copied next to the binaries must declare a version, or upgrades can't re-prompt.
        [SkippableFact]
        public void BundledEula_DeclaresAVersion()
        {
            Skip.If(BundledEula.ReadText() is null, "EULA file not copied next to the test binaries");
            Assert.NotNull(BundledEula.Version());
        }

        // The checked-in snapshot is the fallback the installer ships; it must declare a version too.
        [Fact]
        public void CheckedInEulaSnapshot_DeclaresAVersion()
        {
            string path = RepoFile("PhilterDesktop", "Resources", "philterd-eula.txt");
            Assert.NotNull(EulaVersion.Parse(File.ReadAllText(path)));
        }

        private static string RepoFile(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts));
        }

        // The default (production) store is the registry-backed one.
        [Fact]
        public void DefaultStore_IsRegistryBacked() =>
            Assert.IsType<RegistryAcceptanceStore>(Acknowledgements.Store);
    }
}
