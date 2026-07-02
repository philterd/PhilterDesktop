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

using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// High-DPI configuration. The app opts into Per-Monitor v2 DPI awareness so windows scale
    /// crisply on high-DPI and mixed-DPI multi-monitor setups; combined with AutoScaleMode.Font on every
    /// form, absolute-coordinate layouts scale proportionally. This guards against the setting being
    /// dropped (which would silently revert to the blurrier SystemAware default).
    /// </summary>
    public sealed class HighDpiConfigTests
    {
        [Fact]
        public void Project_OptsIntoPerMonitorV2HighDpi()
        {
            string csproj = LocateAppCsproj();
            string xml = File.ReadAllText(csproj);
            Assert.Contains("<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>", xml);
        }

        private static string LocateAppCsproj()
        {
            DirectoryInfo? dir = new(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName, "PhilterDesktop", "PhilterDesktop.csproj");
                if (File.Exists(candidate))
                {
                    return candidate;
                }
                dir = dir.Parent;
            }
            throw new FileNotFoundException("Could not locate PhilterDesktop\\PhilterDesktop.csproj above " + AppContext.BaseDirectory);
        }
    }
}
