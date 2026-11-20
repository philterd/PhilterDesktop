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
    internal static class PathFieldExtensions
    {
        /// <summary>
        /// Keeps the end of a long path (the file name) in view instead of the drive and folders.
        /// </summary>
        public static void KeepEndVisible(this TextBox box)
        {
            void ScrollToEnd()
            {
                if (box.IsHandleCreated)
                {
                    // Via the start, since scrolling to an unmoved caret is a no-op.
                    box.Select(0, 0);
                    box.ScrollToCaret();
                    box.Select(box.TextLength, 0);
                    box.ScrollToCaret();
                }
            }
            // Layout changes leave a user's selection (e.g. text being copied) alone.
            void Relayout()
            {
                if (box.SelectionLength == 0)
                {
                    ScrollToEnd();
                }
            }
            box.TextChanged += (_, _) => ScrollToEnd();
            box.SizeChanged += (_, _) => Relayout();
            box.HandleCreated += (_, _) => Relayout();
            box.VisibleChanged += (_, _) => Relayout();
            ScrollToEnd();
        }
    }
}
