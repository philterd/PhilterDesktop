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

using Microsoft.Win32;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// Verifies the Explorer context-menu registry round-trip against a throwaway HKCU subkey
    /// (never the real shell association keys).
    /// </summary>
    public sealed class ContextMenuManagerTests : IDisposable
    {
        private readonly string _baseSubKey = $@"Software\PhilterDesktop.Tests\{Guid.NewGuid():N}";
        private readonly ContextMenuManager _manager;

        public ContextMenuManagerTests()
        {
            _manager = new ContextMenuManager(@"C:\app\PhilterDesktop.exe", baseSubKey: _baseSubKey);
        }

        public void Dispose()
        {
            try { Registry.CurrentUser.DeleteSubKeyTree(_baseSubKey, throwOnMissingSubKey: false); }
            catch { /* best effort */ }
        }

        [Fact]
        public void IsEnabled_FalseBeforeEnable()
        {
            Assert.False(_manager.IsEnabled());
        }

        [Fact]
        public void Enable_WritesCommandForEveryExtension()
        {
            _manager.Enable();

            Assert.True(_manager.IsEnabled());
            foreach (string ext in ContextMenuManager.Extensions)
            {
                using RegistryKey? verb = Registry.CurrentUser.OpenSubKey(
                    $@"{_baseSubKey}\{ext}\shell\PhilterDesktop");
                Assert.NotNull(verb);
                Assert.Equal("Redact with Philter Desktop", verb!.GetValue(null));

                using RegistryKey? command = verb.OpenSubKey("command");
                Assert.NotNull(command);
                Assert.Equal("\"C:\\app\\PhilterDesktop.exe\" --shell \"%1\"", command!.GetValue(null));
            }
        }

        [Fact]
        public void Disable_RemovesEntries()
        {
            _manager.Enable();
            Assert.True(_manager.IsEnabled());

            _manager.Disable();

            Assert.False(_manager.IsEnabled());
            foreach (string ext in ContextMenuManager.Extensions)
            {
                using RegistryKey? verb = Registry.CurrentUser.OpenSubKey(
                    $@"{_baseSubKey}\{ext}\shell\PhilterDesktop");
                Assert.Null(verb);
            }
        }

        [Fact]
        public void Enable_IsIdempotent()
        {
            _manager.Enable();
            _manager.Enable(); // should not throw or change the outcome
            Assert.True(_manager.IsEnabled());
        }

        [Fact]
        public void Disable_WhenNotEnabled_DoesNotThrow()
        {
            _manager.Disable();
            Assert.False(_manager.IsEnabled());
        }
    }
}
