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

using System.Diagnostics;
using System.Drawing;

namespace PhilterDesktop
{
    /// <summary>
    /// Shared rule for choosing which policy/context entry a combo box should pre-select:
    /// the last-used value if it's present, otherwise "default", otherwise the first item.
    /// </summary>
    internal static class ComboSelection
    {
        /// <summary>
        /// Returns the index to select in <paramref name="items"/>: <paramref name="preferred"/> if
        /// present, then "default", then 0. Returns -1 when there are no items.
        /// </summary>
        public static int ResolveIndex(IReadOnlyList<string> items, string? preferred)
        {
            if (items.Count == 0)
            {
                return -1;
            }
            if (!string.IsNullOrEmpty(preferred))
            {
                int index = IndexOf(items, preferred);
                if (index >= 0)
                {
                    return index;
                }
            }
            int defaultIndex = IndexOf(items, "default");
            return defaultIndex >= 0 ? defaultIndex : 0;
        }

        /// <summary>Selects the preferred entry (per <see cref="ResolveIndex"/>) in the combo box.</summary>
        public static void Select(ComboBox combo, string? preferred)
        {
            var items = new List<string>(combo.Items.Count);
            foreach (object? item in combo.Items)
            {
                items.Add(item?.ToString() ?? string.Empty);
            }
            int index = ResolveIndex(items, preferred);
            if (index >= 0)
            {
                combo.SelectedIndex = index;
            }
        }

        private static int IndexOf(IReadOnlyList<string> items, string value)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i], value, StringComparison.Ordinal))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    /// <summary>
    /// Pure helpers for persisting and restoring the main-window layout (size/position, sort, column
    /// widths). Kept free of WinForms control state so the tricky bits — the off-screen guard and the
    /// column-width round-trip — are unit-testable.
    /// </summary>
    internal static class UiState
    {
        // A restored window must show at least this much of itself on some screen to be considered
        // usable (so the title bar can be grabbed); otherwise we fall back to the default position.
        private const int MinVisibleWidth = 120;
        private const int MinVisibleHeight = 40;

        /// <summary>
        /// True if <paramref name="bounds"/> overlaps any screen by a grabbable amount. Guards against
        /// restoring a window onto a monitor that was unplugged or whose resolution changed.
        /// </summary>
        public static bool IsBoundsVisible(Rectangle bounds, IEnumerable<Rectangle> screenWorkingAreas)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return false;
            }

            foreach (Rectangle screen in screenWorkingAreas)
            {
                Rectangle overlap = Rectangle.Intersect(screen, bounds);
                if (overlap.Width >= MinVisibleWidth && overlap.Height >= MinVisibleHeight)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Formats column widths as a comma-separated string, e.g. "350,180,120,120".</summary>
        public static string FormatWidths(IEnumerable<int> widths) => string.Join(",", widths);

        /// <summary>
        /// Parses a comma-separated width string back into an array, but only if it has exactly
        /// <paramref name="expectedCount"/> positive integers; otherwise returns null (use defaults).
        /// </summary>
        public static int[]? ParseWidths(string? text, int expectedCount)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            string[] parts = text.Split(',');
            if (parts.Length != expectedCount)
            {
                return null;
            }

            var widths = new int[expectedCount];
            for (int i = 0; i < expectedCount; i++)
            {
                if (!int.TryParse(parts[i].Trim(), out int w) || w <= 0)
                {
                    return null;
                }
                widths[i] = w;
            }
            return widths;
        }
    }

    /// <summary>
    /// Turns low-level file-operation exceptions into short, plain-language guidance for end users,
    /// so a redaction tool aimed at non-technical professionals doesn't surface raw .NET errors.
    /// </summary>
    internal static class UserError
    {
        // Win32 error codes (low word of HResult) for the common, explainable file failures.
        private const int ErrorSharingViolation = 32;
        private const int ErrorLockViolation = 33;
        private const int ErrorHandleDiskFull = 39;
        private const int ErrorDiskFull = 112;

        /// <summary>
        /// A complete, friendly message for a failed file open/save, tailored to the likely cause.
        /// </summary>
        /// <param name="ex">The exception that was thrown.</param>
        /// <param name="path">The file involved (its name is shown to the user).</param>
        /// <param name="writing">True for a save/write operation; false for an open/read.</param>
        public static string Describe(Exception ex, string path, bool writing)
        {
            // Problems we already explained with a ready-made, friendly message — use it as-is rather
            // than wrapping it in generic open/save guidance. Covers password-protected/corrupt documents
            // and the OCR page-cap limit.
            if (ex is DocumentLoadException or OcrPageLimitExceededException)
            {
                return ex.Message;
            }

            string verb = writing ? "save" : "open";
            string name = string.IsNullOrEmpty(path) ? "the file" : $"\"{Path.GetFileName(path)}\"";

            return ex switch
            {
                System.Text.RegularExpressions.RegexMatchTimeoutException =>
                    $"A detection pattern took too long to run, so Philter Desktop could not finish {(verb == "save" ? "redacting" : "opening")} {name}. " +
                    "A custom identifier pattern may be inefficient (it can backtrack badly on some text) — simplify the pattern and try again.",

                UnauthorizedAccessException =>
                    $"Philter Desktop could not {verb} {name}. You may not have permission to use that " +
                    "location, or the file may be read-only. Try a different folder.",

                IOException io when IsCode(io, ErrorSharingViolation, ErrorLockViolation) =>
                    $"Philter Desktop could not {verb} {name} because it is open in another program " +
                    "(such as Microsoft Word or a PDF viewer). Please close it and try again.",

                IOException io when IsCode(io, ErrorHandleDiskFull, ErrorDiskFull) =>
                    $"Philter Desktop could not {verb} {name} because there is not enough free disk space. " +
                    "Free up some space and try again.",

                DirectoryNotFoundException =>
                    $"Philter Desktop could not {verb} {name} because the folder no longer exists. " +
                    "Choose a different location and try again.",

                FileNotFoundException =>
                    $"Philter Desktop could not find {name}. It may have been moved or deleted.",

                _ => $"Philter Desktop could not {verb} {name}." + Environment.NewLine + Environment.NewLine + ex.Message,
            };
        }

        private static bool IsCode(IOException io, params int[] codes)
        {
            int code = io.HResult & 0xFFFF;
            return Array.IndexOf(codes, code) >= 0;
        }
    }

    /// <summary>
    /// A full-width amber warning strip docked at the top of a form — used to surface a safety-relevant
    /// condition (such as the on-device name model being unavailable) prominently and persistently,
    /// rather than letting it pass silently.
    /// </summary>
    internal static class WarningBanner
    {
        private static readonly Color BackColor = Color.FromArgb(255, 244, 206);  // soft amber
        private static readonly Color ForeColor = Color.FromArgb(124, 77, 0);     // dark amber text

        /// <summary>Creates a top-docked warning strip (not yet added to any form).</summary>
        public static Panel Create(string text)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                BackColor = BackColor,
                Padding = new Padding(10, 8, 10, 8),
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink
            };

            var label = new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Text = "⚠  " + text, // ⚠ + message
                ForeColor = ForeColor,
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 38
            };

            panel.Controls.Add(label);
            return panel;
        }

        /// <summary>
        /// Adds a top-docked warning strip to <paramref name="form"/>. Added last so it docks at the very
        /// top, above the form's other content.
        /// </summary>
        public static void AddTo(Form form, string text)
        {
            Panel panel = Create(text);
            form.Controls.Add(panel);
            panel.BringToFront();
        }
    }

    /// <summary>
    /// Builds links to Philterd's other offerings — <b>Philter</b> (server/API, for pipeline/at-scale
    /// redaction), <b>Philter Scope</b> (policy scoring), <b>Philter Diffuse</b> (differential privacy),
    /// and <b>policy consulting</b> — tagged with the in-app surface that produced the click
    /// (<c>utm_medium</c>), so the team can see which touchpoints are used.
    /// </summary>
    internal static class Links
    {
        private const string PhilterBase = "https://www.philterd.ai/philter";
        private const string ScopeBase = "https://www.philterd.ai/philter-scope";
        private const string DiffuseBase = "https://www.philterd.ai/philter-diffuse";
        private const string ConsultingBase = "https://www.philterd.ai/consulting";
        private const string SupportBase = "https://www.philterd.ai/philter-desktop";
        private const string AllProductsBase = "https://philterd.ai/open-source-software";

        /// <summary>Philter (server/API) landing page, tagged with the calling surface.</summary>
        public static string PhilterUrl(string medium) => Tag(PhilterBase, medium);

        /// <summary>
        ///     Philter Desktop product page (the official signed build + support subscription), tagged
        ///     with the calling surface. Distinct from <see cref="ConsultingUrl" /> (a services offering).
        /// </summary>
        public static string SupportUrl(string medium) => Tag(SupportBase, medium);

        /// <summary>Philter Scope (policy scoring) landing page, tagged with the calling surface.</summary>
        public static string ScopeUrl(string medium) => Tag(ScopeBase, medium);

        /// <summary>Philter Diffuse (differential privacy) landing page, tagged with the calling surface.</summary>
        public static string DiffuseUrl(string medium) => Tag(DiffuseBase, medium);

        /// <summary>Policy-consulting landing page, tagged with the calling surface.</summary>
        public static string ConsultingUrl(string medium) => Tag(ConsultingBase, medium);

        /// <summary>Philterd's full open-source product listing, tagged with the calling surface.</summary>
        public static string AllProductsUrl(string medium) => Tag(AllProductsBase, medium);

        private static string Tag(string url, string medium) =>
            $"{url}?utm_source=desktop&utm_medium={Uri.EscapeDataString(medium)}";

        /// <summary>Opens a URL in the default browser (best-effort).</summary>
        public static void Open(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch { /* best effort — never disrupt the app over a link */ }
        }

        /// <summary>A consistent one-line link that opens the given (already-tagged) URL when clicked.</summary>
        public static LinkLabel CreateLink(string text, string url)
        {
            var link = new LinkLabel
            {
                Text = text,
                AutoSize = true,
                LinkColor = ModernTheme.Accent,
                ActiveLinkColor = ModernTheme.Accent
            };
            link.LinkClicked += (_, _) => Open(url);
            return link;
        }
    }

    /// <summary>
    /// Helpers for warning the user before they exit while redaction work is still in flight.
    /// "Active" work is anything queued or running (Pending or Processing); closing the window with
    /// the X button merely hides to the tray and keeps processing, so only a real Exit needs this.
    /// </summary>
    internal static class ExitGuard
    {
        public static bool IsActiveStatus(string? status) =>
            string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Processing", StringComparison.OrdinalIgnoreCase);

        public static int CountActive(IEnumerable<string?> statuses) =>
            statuses.Count(IsActiveStatus);

        /// <summary>The confirmation prompt shown when exiting with <paramref name="active"/> items in flight.</summary>
        public static string Message(int active)
        {
            string subject = active == 1 ? "document is" : "documents are";
            return $"{active} {subject} still being redacted or waiting in the queue. " +
                   "If you exit now, that work will stop and those documents won't be finished." +
                   Environment.NewLine + Environment.NewLine +
                   "Exit anyway?";
        }
    }

    /// <summary>
    /// Decides whether "View Diff" should be offered for a document. The text diff loads both files
    /// into memory and renders one row per line, so it's limited to comparable file types and to files
    /// under a size cap (above which a huge input — e.g. a large CSV export — could freeze or run out
    /// of memory).
    /// </summary>
    internal static class DiffViewGate
    {
        /// <summary>Maximum size (per file) View Diff will compare.</summary>
        public const long MaxFileBytes = 10L * 1024 * 1024; // 10 MB

        /// <summary>Human-readable size limit (e.g. "10 MB") for messages.</summary>
        public static string MaxFileSizeText => $"{MaxFileBytes / (1024 * 1024)} MB";

        /// <summary>
        /// File types View Diff can compare: text-based types use the line diff (.txt/.docx/.csv/.eml),
        /// and .pdf uses the side-by-side page comparison.
        /// </summary>
        public static bool IsDiffableType(string path)
        {
            string ext = Path.GetExtension(path);
            return ext.Equals(".txt", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".docx", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".csv", StringComparison.OrdinalIgnoreCase)
                || ext.Equals(".eml", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True if a file of the given size is within the diff size cap.</summary>
        public static bool IsWithinSizeLimit(long fileBytes) => fileBytes <= MaxFileBytes;
    }
}
