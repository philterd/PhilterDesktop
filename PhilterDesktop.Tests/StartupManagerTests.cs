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
using Xunit;

namespace PhilterDesktop.Tests
{
    public sealed class StartupManagerTests
    {
        [Fact]
        public void EnableDisable_TogglesRegistryRunEntry()
        {
            // Use a throwaway HKCU subkey so the real Run key is never touched.
            string keyPath = @"Software\PhilterDesktopTests\" + Guid.NewGuid().ToString("N");
            const string appName = "PhilterDesktopTest";
            const string command = "\"C:\\app\\PhilterDesktop.exe\" --minimized";
            var manager = new StartupManager(appName, command, keyPath);

            try
            {
                Assert.False(manager.IsEnabled());

                manager.Enable();
                Assert.True(manager.IsEnabled());

                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    Assert.Equal(command, key!.GetValue(appName));
                }

                manager.Disable();
                Assert.False(manager.IsEnabled());

                // Disabling again is a no-op (no throw).
                manager.Disable();
            }
            finally
            {
                Registry.CurrentUser.DeleteSubKeyTree(keyPath, throwOnMissingSubKey: false);
            }
        }

        [Fact]
        public void MinimizedSwitch_IsStable()
        {
            Assert.Equal("--minimized", StartupManager.MinimizedSwitch);
        }
    }
}
