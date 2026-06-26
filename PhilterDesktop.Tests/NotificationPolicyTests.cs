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
    public class NotificationPolicyTests
    {
        [Fact]
        public void DoesNotNotify_WhenWindowVisibleAndNotMinimized()
        {
            // User is looking at the window — no notification.
            Assert.False(NotificationPolicy.ShouldNotify(windowVisible: true, FormWindowState.Normal));
            Assert.False(NotificationPolicy.ShouldNotify(windowVisible: true, FormWindowState.Maximized));
        }

        [Fact]
        public void Notifies_WhenMinimized()
        {
            Assert.True(NotificationPolicy.ShouldNotify(windowVisible: true, FormWindowState.Minimized));
        }

        [Fact]
        public void Notifies_WhenHiddenToTray()
        {
            // Not visible (running in the system tray), regardless of window state.
            Assert.True(NotificationPolicy.ShouldNotify(windowVisible: false, FormWindowState.Normal));
            Assert.True(NotificationPolicy.ShouldNotify(windowVisible: false, FormWindowState.Minimized));
        }

        [Fact]
        public void Disabled_NeverNotifies_EvenWhenHidden()
        {
            // Preference off overrides everything.
            Assert.False(NotificationPolicy.ShouldNotify(enabled: false, windowVisible: false, FormWindowState.Normal));
            Assert.False(NotificationPolicy.ShouldNotify(enabled: false, windowVisible: false, FormWindowState.Minimized));
        }

        [Fact]
        public void Enabled_BehavesLikeWindowStateRule()
        {
            // With the preference on, the window-state rule applies.
            Assert.True(NotificationPolicy.ShouldNotify(enabled: true, windowVisible: false, FormWindowState.Normal));
            Assert.False(NotificationPolicy.ShouldNotify(enabled: true, windowVisible: true, FormWindowState.Normal));
        }
    }
}
