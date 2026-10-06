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

// LiteDB uses a process-global BsonMapper that isn't safe to initialize from multiple
// threads at once; running tests serially avoids intermittent "Member not found on
// BsonMapper" errors. The suite is small, so this has negligible cost.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

namespace PhilterDesktop.Tests
{
    internal static class WinFormsStartup
    {
        // Start WinForms exactly as Program.Main does (PerMonitorV2, visual styles, theme font) so forms
        // measure and scale as they do for users, and so tests can simulate a move to a high-DPI monitor.
        // Must run before any window is created.
        [System.Runtime.CompilerServices.ModuleInitializer]
        internal static void Initialize()
        {
            System.Windows.Forms.Application.SetHighDpiMode(System.Windows.Forms.HighDpiMode.PerMonitorV2);
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.SetDefaultFont(ModernTheme.UiFont);
        }
    }
}
