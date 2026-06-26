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

namespace PhilterDesktop
{
    /// <summary>Decides when a redaction-completed tray notification should be shown.</summary>
    internal static class NotificationPolicy
    {
        /// <summary>
        /// True when a notification should be shown — i.e. the user isn't already looking at the
        /// window. That's the case when the window is hidden (in the tray) or minimized.
        /// </summary>
        public static bool ShouldNotify(bool windowVisible, FormWindowState windowState) =>
            !(windowVisible && windowState != FormWindowState.Minimized);
    }
}
