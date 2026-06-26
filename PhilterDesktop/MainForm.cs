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

using LiteDB;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PhilterDesktop
{
    public partial class MainForm : Form
    {
        private readonly LiteDatabase _database;
        private readonly PolicyRepository _policyRepository;
        private readonly ContextRepository _contextRepository;
        private readonly ContextEntryRepository _contextEntryRepository;
        private readonly SettingsRepository _settingsRepository;
        private readonly RedactionQueueRepository _redactionQueueRepository;
        private readonly RedactionVersionRepository _redactionVersionRepository;
        private readonly RedactionSpanRepository _redactionSpanRepository;
        private readonly WatchedFolderRepository _watchedFolderRepository;
        private readonly WatchedFolderLogRepository _watchedFolderLogRepository;
        private readonly FolderWatcherService _folderWatcher;
        private bool _loggingEnabled;

        private NotifyIcon _trayIcon = null!;
        private ToolStripMenuItem _pauseResumeItem = null!;
        private System.Windows.Forms.Timer? _logPruneTimer;
        private bool _exiting;
        private bool _watchingPaused;
        private bool _started;
        private bool _startMinimized;

        private readonly List<WatchedFileProcessedEventArgs> _pendingNotifications = new();
        private System.Windows.Forms.Timer? _notifyCoalesceTimer;
        private string? _lastNotifyDirectory;

        private const string HelpUrl = "https://philterd.github.io/PhilterDesktop/";

        private Label _emptyStateLabel = null!;
        private TextBox _filterBox = null!;
        private ToolStripStatusLabel _queueSummaryLabel = null!;
        private ToolStripStatusLabel _reviewWarningLabel = null!;
        private Image? _documentImage;
        private readonly QueueColumnSorter _sorter = new();

        private const string DefaultEmptyText =
            "No documents queued\r\n\r\nClick \"Redact\" or drag .txt, .docx, or .pdf files here";
        private const string FilterEmptyText =
            "No documents match your filter.\r\n\r\nClear the filter box to see the whole queue.";
        private System.Windows.Forms.Timer _statusAnimTimer = null!;
        private int _statusAnimPhase;

        private const int RowHeight = 32;

        public MainForm() : this(startMinimized: false)
        {
        }

        public MainForm(bool startMinimized)
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            InitializeQueueUi();
            InitializeTray();
            _startMinimized = startMinimized;

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 2. Combine with your specific App Folder
            string folder = Path.Combine(root, "PhilterDesktop");
            string dbPath = Path.Combine(folder, "data.db");

            // 3. The Magic Step: Ensure the directory exists
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Create a single shared database instance, encrypted at rest (it stores detected PII).
            _database = EncryptedDatabase.Open(dbPath);

            // Pass the shared database to all repositories
            _policyRepository = new PolicyRepository(_database);
            _contextRepository = new ContextRepository(_database);
            _contextEntryRepository = new ContextEntryRepository(_database);
            _settingsRepository = new SettingsRepository(_database);
            _redactionQueueRepository = new RedactionQueueRepository(_database);
            _redactionVersionRepository = new RedactionVersionRepository(_database);
            _redactionSpanRepository = new RedactionSpanRepository(_database);
            _watchedFolderRepository = new WatchedFolderRepository(_database);
            _watchedFolderLogRepository = new WatchedFolderLogRepository(_database);
            _folderWatcher = new FolderWatcherService(_policyRepository, _loggingEnabled, _watchedFolderLogRepository, _settingsRepository);
            _folderWatcher.FileProcessed += OnWatchedFileProcessed;

            // Load settings and check if logging is enabled
            var settings = _settingsRepository.GetSettings();
            _loggingEnabled = settings.LoggingEnabled;

            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application started");
            }

            // Insert default policy.
            if (_policyRepository.FindByName(DefaultPolicy.Name) == null)
            {
                PolicyEntity policyEntity = new PolicyEntity
                {
                    Name = DefaultPolicy.Name,
                    Json = DefaultPolicy.Json()
                };
                _policyRepository.Insert(policyEntity);

                if (_loggingEnabled)
                {
                    Logger.LogInfo("Created default redaction policy");
                }
            }

            // Insert default context.
            if (_contextRepository.FindByName("default") == null)
            {
                ContextEntity contextEntity = new ContextEntity
                {
                    Name = "default"
                };
                _contextRepository.Insert(contextEntity);

                if (_loggingEnabled)
                {
                    Logger.LogInfo("Created default redaction context");
                }
            }

            RestoreUiState();
        }

        // Restore the remembered window size/position, sort, and column widths. Best-effort: a bad or
        // stale value must never stop the app from starting.
        private void RestoreUiState()
        {
            try
            {
                SettingsEntity s = _settingsRepository.GetSettings();

                // Sort column/direction (first LoadRedactionQueue will apply it via _sorter).
                if (s.SortColumn >= 0 && s.SortColumn < listView1.Columns.Count)
                {
                    _sorter.Column = s.SortColumn;
                    _sorter.Ascending = s.SortAscending;
                }

                // Column widths. File Name (index 0) is re-derived by AdjustColumns, so restoring it
                // is harmless; the other three are what the user actually controls.
                int[]? widths = UiState.ParseWidths(s.ColumnWidths, listView1.Columns.Count);
                if (widths is not null)
                {
                    for (int i = 0; i < widths.Length; i++)
                    {
                        listView1.Columns[i].Width = widths[i];
                    }
                }

                // Window size/position, guarded against an unplugged/changed monitor.
                if (s.WindowWidth > 0 && s.WindowHeight > 0)
                {
                    var bounds = new Rectangle(s.WindowX, s.WindowY, s.WindowWidth, s.WindowHeight);
                    if (UiState.IsBoundsVisible(bounds, Screen.AllScreens.Select(sc => sc.WorkingArea)))
                    {
                        StartPosition = FormStartPosition.Manual;
                        Bounds = bounds;
                    }

                    if (s.WindowMaximized && !_startMinimized)
                    {
                        WindowState = FormWindowState.Maximized;
                    }
                }
            }
            catch
            {
                // best effort — keep the designer defaults on any failure
            }
        }

        // Persist the current window size/position, sort, and column widths. Called when hiding to the
        // tray and when exiting, so the last-seen layout is remembered.
        private void SaveUiState()
        {
            try
            {
                SettingsEntity s = _settingsRepository.GetSettings();

                // Use RestoreBounds (not Bounds) when maximized/minimized so we store the *normal* size.
                Rectangle bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
                s.WindowX = bounds.X;
                s.WindowY = bounds.Y;
                s.WindowWidth = bounds.Width;
                s.WindowHeight = bounds.Height;
                s.WindowMaximized = WindowState == FormWindowState.Maximized;

                s.SortColumn = _sorter.Column;
                s.SortAscending = _sorter.Ascending;
                s.ColumnWidths = UiState.FormatWidths(
                    listView1.Columns.Cast<ColumnHeader>().Select(c => c.Width));

                _settingsRepository.SaveSettings(s);
            }
            catch
            {
                // best effort — never let a settings write disrupt close/hide
            }
        }

        /// <summary>
        /// Sets up the modern queue experience: owner-drawn status badges, an empty
        /// state, drag-and-drop, a live status-bar summary, and working Help buttons.
        /// </summary>
        private void InitializeQueueUi()
        {
            // Keep a copy of the document icon for owner-drawing the first column,
            // then replace the SmallImageList with a transparent spacer purely to
            // force a taller row height (Details-view rows size to ImageSize.Height).
            if (imageList1.Images.Count > 0)
            {
                _documentImage = imageList1.Images[0];
            }
            listView1.SmallImageList = new ImageList
            {
                ImageSize = new Size(1, RowHeight),
                ColorDepth = ColorDepth.Depth32Bit
            };

            // --- Owner-drawn status badges + full-row selection ---
            listView1.OwnerDraw = true;
            listView1.ShowItemToolTips = true; // failed rows carry a hover tooltip with the reason
            listView1.DrawColumnHeader += ListView1_DrawColumnHeader;
            listView1.DrawItem += ListView1_DrawItem;
            listView1.DrawSubItem += ListView1_DrawSubItem;
            listView1.Resize += (_, _) => { AdjustColumns(); PositionEmptyState(); };

            // Click a column header to sort by it; click again to reverse the direction.
            // (ModernTheme.Apply set the header Nonclickable; the queue list needs it clickable.)
            listView1.HeaderStyle = ColumnHeaderStyle.Clickable;
            listView1.ListViewItemSorter = _sorter;
            listView1.ColumnClick += ListView1_ColumnClick;

            // --- Drag-and-drop files onto the queue ---
            listView1.AllowDrop = true;
            listView1.DragEnter += QueueList_DragEnter;
            listView1.DragDrop += QueueList_DragDrop;
            listView1.DoubleClick += ListView1_DoubleClick;

            // Enable/disable context-menu items based on selection and queue state.
            contextMenuStrip1.Opening += ContextMenuStrip1_Opening;

            // --- Filter box (docked just below the toolbar, above the list) ---
            BuildFilterBar();

            // --- Empty state overlay ---
            _emptyStateLabel = new Label
            {
                Text = DefaultEmptyText,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = ModernTheme.SubtleText,
                BackColor = ModernTheme.Surface,
                Font = new Font(ModernTheme.UiFont.FontFamily, 11f, FontStyle.Regular),
                Visible = false
            };
            Controls.Add(_emptyStateLabel);

            // --- Status-bar summary ---
            _queueSummaryLabel = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernTheme.SubtleText,
                AccessibleName = "Queue summary"
            };
            statusStrip1.Items.Add(_queueSummaryLabel);

            // Persistent safety reminder: automated redaction is imperfect and output must be reviewed.
            // Sits after the spring summary label, so it stays pinned to the right of the status bar,
            // shown in bold amber so it stays visible.
            _reviewWarningLabel = new ToolStripStatusLabel
            {
                Text = "Redaction can include mistakes. Always carefully review redacted documents before sharing.",
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                ForeColor = Color.FromArgb(176, 92, 0),
                AccessibleName = "Important: redaction can include mistakes. Always carefully review redacted documents before sharing."
            };
            statusStrip1.Items.Add(_reviewWarningLabel);

            // --- Wire the previously-dead Help buttons ---
            HelpToolStripButton.Click += OpenHelp;
            helpToolStripMenuItem1.Click += OpenHelp;

            // --- Modern monochrome toolbar icons + animated processing badges ---
            ApplyToolbarIcons();

            _statusAnimTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _statusAnimTimer.Tick += StatusAnimTimer_Tick;
        }

        // A slim filter bar docked between the toolbar and the list. Built in code (rather than the
        // VS-regenerated designer) and slotted into the z-order so it docks below the toolbar and
        // above the Fill list.
        private void BuildFilterBar()
        {
            _filterBox = new TextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Filter by file name, status, policy, or context…",
                AccessibleName = "Filter the redaction queue"
            };
            _filterBox.TextChanged += (_, _) => LoadRedactionQueue();
            _filterBox.KeyDown += (_, e) =>
            {
                if (e.KeyCode == Keys.Escape && _filterBox.Text.Length > 0)
                {
                    _filterBox.Clear();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(8, 6, 8, 4),
                BackColor = ModernTheme.Surface
            };
            panel.Controls.Add(_filterBox);
            Controls.Add(panel);

            // Docking is applied back-to-front in the z-order. Put the panel at index 1 so it docks
            // just inside the toolbar/menu (higher indexes) and just outside the Fill list (index 0).
            Controls.SetChildIndex(panel, 1);
        }

        private void ApplyToolbarIcons()
        {
            const int size = 24;
            // Segoe Fluent / MDL2 glyphs. Redact is accent-colored as the primary action.
            toolStripButtonRedactDocuments.Image = ModernTheme.CreateGlyphImage("\uE72E", size, ModernTheme.Accent); // Lock
            toolStripButtonRedactPreview.Image = ModernTheme.CreateGlyphImage("\uE890", size, ModernTheme.Accent);  // View (preview)
            policiesToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE8FD", size, ModernTheme.Text);          // BulletedList
            contextsToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE8EC", size, ModernTheme.Text);         // Tag
            settingsToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE713", size, ModernTheme.Text);         // Settings
            HelpToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE897", size, ModernTheme.Text);             // Help
        }

        private void StatusAnimTimer_Tick(object? sender, EventArgs e)
        {
            if (!HasProcessingItems())
            {
                _statusAnimTimer.Stop();
                return;
            }

            _statusAnimPhase = (_statusAnimPhase + 1) % 4;
            listView1.Invalidate();
        }

        private bool HasProcessingItems()
        {
            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems.Count > 1 && item.SubItems[1].Text == "Processing")
                {
                    return true;
                }
            }
            return false;
        }

        private void EnsureStatusAnimation()
        {
            if (HasProcessingItems())
            {
                if (!_statusAnimTimer.Enabled)
                {
                    _statusAnimTimer.Start();
                }
            }
            else
            {
                _statusAnimTimer.Stop();
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            EnsureStarted();
        }

        // Keyboard shortcuts for the queue: F5 refresh, Ctrl+O add files (both global); Delete remove
        // and Enter open the redacted file when a queue item is selected.
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F5:
                    LoadRedactionQueue();
                    return true;
                case Keys.Control | Keys.O:
                    addFilesToRedactToolStripMenuItem_Click(this, EventArgs.Empty);
                    return true;
            }

            if (listView1.Focused && listView1.SelectedItems.Count > 0)
            {
                if (keyData == Keys.Delete)
                {
                    removeToolStripMenuItem_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == Keys.Enter && IsCompleted(listView1.SelectedItems[0]))
                {
                    openRedactedFileToolStripMenuItem_Click(this, EventArgs.Empty);
                    return true;
                }
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Startup work runs once, whether the window is shown normally or the app launched
        // straight to the tray (in which case the handle is created but the form stays hidden).
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnsureStarted();
        }

        // EnsureStarted() loads the queue during handle creation, before the form has done its
        // initial layout — so the empty-state overlay's size/z-order don't reliably stick on a
        // fresh launch (it would only appear after the next refresh). Re-evaluate it here, once the
        // window is laid out and shown, so an empty queue shows the placeholder immediately.
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UpdateEmptyState(listView1.Items.Count);
        }

        private void EnsureStarted()
        {
            if (_started)
            {
                return;
            }
            _started = true;

            redactionQueueTimer.Start();
            AdjustColumns();
            LoadRedactionQueue();
            StartFolderWatcher();
            StartLogPruning();
        }

        // Watched-folder activity logs are pruned by age (30 days). Prune at startup and once a day
        // while the app keeps running (it may stay open in the tray for a long time).
        private void StartLogPruning()
        {
            PruneWatchedFolderLogs();
            _logPruneTimer = new System.Windows.Forms.Timer { Interval = 24 * 60 * 60 * 1000 }; // 24h
            _logPruneTimer.Tick += (_, _) => PruneWatchedFolderLogs();
            _logPruneTimer.Start();
        }

        private void PruneWatchedFolderLogs()
        {
            try
            {
                int removed = _watchedFolderLogRepository.PruneOldEntries();
                if (removed > 0 && _loggingEnabled)
                {
                    Logger.LogInfo($"Pruned {removed} watched-folder log entr{(removed == 1 ? "y" : "ies")} older than {WatchedFolderLogRepository.RetentionDays} days.");
                }
            }
            catch
            {
                // pruning is best-effort and must never disrupt the app
            }
        }

        private void StartFolderWatcher()
        {
            if (_watchingPaused)
            {
                return;
            }
            _folderWatcher.LoggingEnabled = _loggingEnabled;
            _folderWatcher.Restart(_watchedFolderRepository.GetAll());
        }

        // --- System tray --------------------------------------------------------

        private void InitializeTray()
        {
            _pauseResumeItem = new ToolStripMenuItem("Pause watching", null, (_, _) => ToggleWatchingPaused());

            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Open Philter Desktop", null, (_, _) => ShowFromTray()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_pauseResumeItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, _) => ExitApplication()));

            _trayIcon = new NotifyIcon
            {
                Icon = Icon ?? SystemIcons.Application,
                Text = "Philter Desktop",
                Visible = true,
                ContextMenuStrip = menu
            };
            _trayIcon.DoubleClick += (_, _) => ShowFromTray();
            _trayIcon.BalloonTipClicked += (_, _) => OpenLastNotifyDirectory();

            // Coalesce a burst of redactions into a single notification.
            _notifyCoalesceTimer = new System.Windows.Forms.Timer { Interval = 1500 };
            _notifyCoalesceTimer.Tick += (_, _) => FlushNotifications();
        }

        // --- Watched-folder redaction notifications -----------------------------

        private void OnWatchedFileProcessed(object? sender, WatchedFileProcessedEventArgs e)
        {
            // Raised on a background thread; marshal to the UI thread.
            if (IsDisposed || !IsHandleCreated)
            {
                return;
            }
            try
            {
                BeginInvoke(new Action(() => QueueNotification(e)));
            }
            catch
            {
                // form may be closing
            }
        }

        private void QueueNotification(WatchedFileProcessedEventArgs e)
        {
            // Respect the user's Notifications preference, and don't notify about what they're already
            // looking at. Reading settings here keeps it current after a change in the Settings dialog.
            bool enabled = true;
            try { enabled = _settingsRepository.GetSettings().NotificationsEnabled; }
            catch { /* default to showing on a settings read failure */ }

            if (!NotificationPolicy.ShouldNotify(enabled, Visible, WindowState))
            {
                return;
            }
            _pendingNotifications.Add(e);
            _notifyCoalesceTimer!.Stop();
            _notifyCoalesceTimer!.Start();
        }

        private void FlushNotifications()
        {
            _notifyCoalesceTimer!.Stop();
            if (_pendingNotifications.Count == 0)
            {
                return;
            }

            List<WatchedFileProcessedEventArgs> items = _pendingNotifications.ToList();
            _pendingNotifications.Clear();

            int succeeded = items.Count(i => i.Success);
            int failed = items.Count - succeeded;

            // The most recent successful output drives click-to-open.
            WatchedFileProcessedEventArgs? lastSuccess =
                items.LastOrDefault(i => i.Success && !string.IsNullOrEmpty(i.OutputPath));
            _lastNotifyDirectory = lastSuccess is null ? null : Path.GetDirectoryName(lastSuccess.OutputPath);

            string title;
            string text;
            if (items.Count == 1)
            {
                WatchedFileProcessedEventArgs only = items[0];
                if (only.Success)
                {
                    title = "File redacted";
                    text = $"{Path.GetFileName(only.InputPath)} → {Path.GetDirectoryName(only.OutputPath)}";
                }
                else
                {
                    title = "Redaction failed";
                    text = $"{Path.GetFileName(only.InputPath)}: {only.ErrorMessage}";
                }
            }
            else
            {
                title = "Philter Desktop";
                var parts = new List<string>();
                if (succeeded > 0)
                {
                    parts.Add($"Redacted {succeeded} file{(succeeded == 1 ? "" : "s")}");
                }
                if (failed > 0)
                {
                    parts.Add($"{failed} failed");
                }
                text = string.Join(", ", parts) + ".";
            }

            _trayIcon.BalloonTipIcon = failed > 0 && succeeded == 0 ? ToolTipIcon.Warning : ToolTipIcon.Info;
            _trayIcon.BalloonTipTitle = title;
            _trayIcon.BalloonTipText = text;
            _trayIcon.ShowBalloonTip(5000);
        }

        private void OpenLastNotifyDirectory()
        {
            if (string.IsNullOrEmpty(_lastNotifyDirectory) || !Directory.Exists(_lastNotifyDirectory))
            {
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo { FileName = _lastNotifyDirectory, UseShellExecute = true });
            }
            catch
            {
                // best effort
            }
        }

        private void ShowFromTray()
        {
            Show();
            ShowInTaskbar = true;
            if (WindowState == FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Normal;
            }
            Activate();
            BringToFront();
        }

        private void HideToTray()
        {
            SaveUiState(); // remember the layout the user last had open
            Hide();
            ShowInTaskbar = false;
            ShowTrayHintIfFirstTime();
        }

        private void ShowTrayHintIfFirstTime()
        {
            SettingsEntity settings = _settingsRepository.GetSettings();
            if (settings.TrayHintShown)
            {
                return;
            }
            _trayIcon.BalloonTipTitle = "Philter Desktop is still running";
            _trayIcon.BalloonTipText =
                "It keeps watching your folders in the background. Right-click the tray icon to open it again or exit.";
            _trayIcon.ShowBalloonTip(5000);

            settings.TrayHintShown = true;
            _settingsRepository.SaveSettings(settings);
        }

        private void ToggleWatchingPaused()
        {
            _watchingPaused = !_watchingPaused;
            if (_watchingPaused)
            {
                _folderWatcher.Stop();
            }
            else
            {
                StartFolderWatcher();
            }
            UpdateTrayWatchState();

            if (_loggingEnabled)
            {
                Logger.LogInfo(_watchingPaused ? "Folder watching paused" : "Folder watching resumed");
            }
        }

        private void UpdateTrayWatchState()
        {
            _pauseResumeItem.Text = _watchingPaused ? "Resume watching" : "Pause watching";
            _trayIcon.Text = _watchingPaused ? "Philter Desktop (watching paused)" : "Philter Desktop";
        }

        private void ExitApplication()
        {
            _exiting = true;
            Close();
        }

        // Closing the window (the X button) hides to the tray instead of exiting, so watching
        // continues. The tray menu's Exit (or a non-user close) performs a real shutdown.
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_exiting && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideToTray();
                return;
            }

            // A real exit chosen from the tray menu: if redaction is still queued or running, give
            // the user a chance to stay so they don't unknowingly abandon in-progress work. (Skipped
            // for OS shutdown/logoff, where blocking with a prompt would be inappropriate.)
            if (_exiting && !ConfirmExitWithActiveWork())
            {
                e.Cancel = true;
                _exiting = false; // back out of the exit; keep running in the tray
                return;
            }

            SaveUiState(); // committed to closing — remember the layout
            base.OnFormClosing(e);
        }

        // Returns true if it's OK to exit: either no active work, or the user confirmed.
        private bool ConfirmExitWithActiveWork()
        {
            int active;
            try
            {
                active = ExitGuard.CountActive(_redactionQueueRepository.GetAll().Select(x => x.Status));
            }
            catch
            {
                return true; // never block exit on a counting failure
            }

            if (active == 0)
            {
                return true;
            }

            DialogResult choice = MessageBox.Show(
                this,
                ExitGuard.Message(active),
                "Redaction still in progress",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            return choice == DialogResult.Yes;
        }

        // Start hidden in the tray when launched with --minimized (e.g., at sign-in).
        protected override void SetVisibleCore(bool value)
        {
            if (_startMinimized && !_started)
            {
                if (!IsHandleCreated)
                {
                    CreateHandle();
                }
                _startMinimized = false;
                value = false;
            }
            base.SetVisibleCore(value);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _logPruneTimer?.Dispose();
            _notifyCoalesceTimer?.Dispose();
            _folderWatcher.Dispose();
            _trayIcon.Visible = false;
            _trayIcon.Dispose();
            base.OnFormClosed(e);
        }

        // --- Owner-draw handlers ---------------------------------------------

        private void ListView1_ColumnClick(object? sender, ColumnClickEventArgs e)
        {
            // Toggle direction when re-clicking the active column; otherwise sort the new one ascending.
            if (e.Column == _sorter.Column)
            {
                _sorter.Ascending = !_sorter.Ascending;
            }
            else
            {
                _sorter.Column = e.Column;
                _sorter.Ascending = true;
            }

            listView1.Sort();
            InvalidateListViewHeader(); // repaint the header window so the sort arrow moves columns
        }

        // The ListView's column header is a separate native child window, so listView1.Invalidate()
        // doesn't repaint it — the old column would keep its sort arrow. Invalidate the header window
        // directly to force every column's DrawColumnHeader to run again.
        private const int LVM_FIRST = 0x1000;
        private const int LVM_GETHEADER = LVM_FIRST + 31;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InvalidateRect(IntPtr hWnd, IntPtr lpRect, bool bErase);

        private void InvalidateListViewHeader()
        {
            if (!listView1.IsHandleCreated)
            {
                return;
            }
            IntPtr header = SendMessage(listView1.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (header != IntPtr.Zero)
            {
                InvalidateRect(header, IntPtr.Zero, true);
            }
        }

        private void ListView1_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using (var bg = new SolidBrush(ModernTheme.Surface))
            {
                e.Graphics.FillRectangle(bg, e.Bounds);
            }
            using (var pen = new Pen(ModernTheme.Border))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            Rectangle textBounds = Rectangle.Inflate(e.Bounds, -8, 0);

            // Show a sort-direction arrow on the active column, drawn at the right edge of the header.
            if (e.ColumnIndex == _sorter.Column)
            {
                string arrow = _sorter.Ascending ? "▲" : "▼"; // ▲ / ▼
                Size arrowSize = TextRenderer.MeasureText(e.Graphics, arrow, ModernTheme.UiFont);
                var arrowBounds = new Rectangle(
                    textBounds.Right - arrowSize.Width, textBounds.Top, arrowSize.Width, textBounds.Height);
                TextRenderer.DrawText(
                    e.Graphics, arrow, ModernTheme.UiFont, arrowBounds, ModernTheme.SubtleText,
                    TextFormatFlags.Right | TextFormatFlags.VerticalCenter);
                textBounds.Width -= arrowSize.Width + 4;
            }

            TextRenderer.DrawText(
                e.Graphics, e.Header?.Text, ModernTheme.UiFont,
                textBounds, ModernTheme.SubtleText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // Row background/selection is painted per-cell in DrawSubItem; nothing to do here.
        private void ListView1_DrawItem(object? sender, DrawListViewItemEventArgs e) { }

        private void ListView1_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            if (e.Item is null)
            {
                return;
            }
            bool selected = e.Item.Selected;
            Color back = selected ? ModernTheme.SelectionBack : ModernTheme.Surface;
            using (var b = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            // Status column -> colored pill.
            if (e.ColumnIndex == 1)
            {
                DrawStatusBadge(e.Graphics, e.Bounds, e.SubItem?.Text ?? string.Empty);
                return;
            }

            Rectangle textBounds;

            // First column -> document icon followed by the file name.
            if (e.ColumnIndex == 0 && _documentImage != null)
            {
                const int iconSize = 16;
                int iconY = e.Bounds.Top + (e.Bounds.Height - iconSize) / 2;
                e.Graphics.DrawImage(_documentImage, e.Bounds.Left + 6, iconY, iconSize, iconSize);
                textBounds = new Rectangle(
                    e.Bounds.Left + 6 + iconSize + 6, e.Bounds.Top,
                    e.Bounds.Width - (iconSize + 18), e.Bounds.Height);
            }
            else
            {
                textBounds = Rectangle.Inflate(e.Bounds, -8, 0);
            }

            TextRenderer.DrawText(
                e.Graphics, e.SubItem?.Text, ModernTheme.UiFont, textBounds, ModernTheme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawStatusBadge(Graphics g, Rectangle cell, string status)
        {
            (Color fill, Color fg) = StatusColors(status);

            // While processing, animate trailing dots; size the badge to the widest
            // form ("Processing...") so it doesn't jitter as the dots change.
            bool processing = status == "Processing";
            string displayText = processing ? "Processing" + new string('.', _statusAnimPhase) : status;
            string measureText = processing ? "Processing..." : status;

            Size textSize = TextRenderer.MeasureText(g, measureText, ModernTheme.UiFont);
            int badgeHeight = Math.Min(cell.Height - 8, textSize.Height + 6);
            int badgeWidth = Math.Min(cell.Width - 12, textSize.Width + 20);
            var rect = new Rectangle(
                cell.Left + 8,
                cell.Top + (cell.Height - badgeHeight) / 2,
                Math.Max(badgeWidth, 16),
                badgeHeight);

            SmoothingMode previous = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            using (var path = RoundedRectangle(rect, badgeHeight / 2))
            using (var brush = new SolidBrush(fill))
            {
                g.FillPath(brush, path);
            }
            g.SmoothingMode = previous;

            TextRenderer.DrawText(
                g, displayText, ModernTheme.UiFont, rect, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private static (Color fill, Color fg) StatusColors(string status) => status switch
        {
            "Completed" => (Color.FromArgb(16, 124, 16), Color.White),
            "Processing" => (ModernTheme.Accent, Color.White),
            "Pending" => (Color.FromArgb(240, 173, 0), Color.FromArgb(45, 30, 0)),
            "Failed" => (Color.FromArgb(197, 15, 31), Color.White),
            _ => (Color.FromArgb(120, 120, 120), Color.White)
        };

        private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
        {
            int d = Math.Max(1, radius * 2);
            var path = new GraphicsPath();
            path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void AdjustColumns()
        {
            if (listView1.Columns.Count < 4)
            {
                return;
            }

            int fixedWidth = columnHeader2.Width + columnHeader4.Width + columnHeader3.Width;
            int available = listView1.ClientSize.Width - fixedWidth - 4;
            columnHeader1.Width = Math.Max(220, available);
        }

        // --- Empty state -----------------------------------------------------

        private void PositionEmptyState()
        {
            if (_emptyStateLabel == null)
            {
                return;
            }

            // Cover the list body (below the column header strip).
            const int headerHeight = 28;
            _emptyStateLabel.Bounds = new Rectangle(
                listView1.Left,
                listView1.Top + headerHeight,
                listView1.Width,
                listView1.Height - headerHeight);
        }

        private void UpdateEmptyState(int itemCount)
        {
            PositionEmptyState();
            _emptyStateLabel.Visible = itemCount == 0;
            if (_emptyStateLabel.Visible)
            {
                _emptyStateLabel.BringToFront();
            }
        }

        // --- Drag-and-drop ---------------------------------------------------

        private void QueueList_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;
        }

        private void QueueList_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
            {
                return;
            }

            var valid = new List<string>();
            var skipped = new List<string>();

            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }
                if (RedactionService.IsSupported(path))
                {
                    valid.Add(path);
                }
                else
                {
                    skipped.Add(Path.GetFileName(path));
                }
            }

            // Ask which policy/context to apply to the dropped files (rather than silently
            // using the default policy, which may redact nothing).
            if (valid.Count > 0 && PromptPolicyContext(out string policy, out string context))
            {
                foreach (string path in valid)
                {
                    _redactionQueueRepository.Insert(new RedactionQueueEntity { Name = path, Policy = policy, Context = context });

                    if (_loggingEnabled)
                    {
                        Logger.LogInfo($"File queued via drag-drop: {path} (policy={policy}, context={context})");
                    }
                }
                LoadRedactionQueue();
            }

            if (skipped.Count > 0)
            {
                MessageBox.Show(
                    $"These files were skipped (supported types: {string.Join(", ", RedactionService.SupportedExtensions)}):\n\n{string.Join("\n", skipped)}",
                    "Unsupported File Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Prompts for the policy and context to apply to dropped files. Returns false if cancelled.
        /// </summary>
        private bool PromptPolicyContext(out string policy, out string context)
        {
            policy = "default";
            context = "default";

            using var form = new Form
            {
                Text = "Redact Dropped Files",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false,
                ClientSize = new Size(370, 165)
            };
            var policyLabel = new Label { Text = "Policy:", AutoSize = true, Location = new Point(14, 20) };
            var policyCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(95, 17), Width = 255 };
            var contextLabel = new Label { Text = "Context:", AutoSize = true, Location = new Point(14, 56) };
            var contextCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(95, 53), Width = 255 };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = ModernTheme.StandardButtonSize, Location = new Point(130, 115) };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Size = ModernTheme.StandardButtonSize, Location = new Point(246, 115) };

            foreach (PolicyEntity p in _policyRepository.GetAll().OrderBy(p => p.Name))
            {
                policyCombo.Items.Add(p.Name);
            }
            foreach (ContextEntity c in _contextRepository.GetAll().OrderBy(c => c.Name))
            {
                contextCombo.Items.Add(c.Name);
            }
            SelectDefault(policyCombo);
            SelectDefault(contextCombo);

            form.Controls.AddRange(new Control[] { policyLabel, policyCombo, contextLabel, contextCombo, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;
            ModernTheme.Apply(form);
            ModernTheme.MakePrimary(ok);

            if (form.ShowDialog(this) != DialogResult.OK ||
                policyCombo.SelectedItem is null || contextCombo.SelectedItem is null)
            {
                return false;
            }

            policy = policyCombo.SelectedItem.ToString()!;
            context = contextCombo.SelectedItem.ToString()!;
            return true;

            static void SelectDefault(ComboBox combo)
            {
                int i = combo.Items.IndexOf("default");
                combo.SelectedIndex = i >= 0 ? i : (combo.Items.Count > 0 ? 0 : -1);
            }
        }

        // --- Help ------------------------------------------------------------

        private void OpenHelp(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Help");
            }

            try
            {
                Process.Start(new ProcessStartInfo { FileName = HelpUrl, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                if (_loggingEnabled)
                {
                    Logger.LogError("Failed to open Help URL", ex);
                }

                MessageBox.Show(
                    $"Unable to open the help page.\n\n{HelpUrl}",
                    "Help",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application closing");
            }

            Application.Exit();
        }

        private async void checkForUpdatesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Checking for updates");
            }

            const string checkFailedMessage =
                "Philter Desktop was unable to check for updates.\n\n" +
                "Please visit https://www.philterd.ai/philter-desktop/ to check for updates.";

            UpdateChecker.UpdateManifest? manifest;
            try
            {
                UseWaitCursor = true;
                manifest = await UpdateChecker.FetchAsync();
            }
            catch
            {
                MessageBox.Show(this, checkFailedMessage, "Check for Updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (manifest is null || string.IsNullOrWhiteSpace(manifest.Version))
            {
                MessageBox.Show(this, checkFailedMessage, "Check for Updates",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Version current = UpdateChecker.CurrentVersion();
            bool? newer = UpdateChecker.IsNewer(manifest.Version, current);

            if (newer == true)
            {
                using var dialog = new UpdateAvailableForm(
                    current.ToString(3), manifest.Version!, manifest.ReleaseDate);
                dialog.ShowDialog(this);
            }
            else if (newer == false)
            {
                MessageBox.Show(this,
                    $"You have the latest version of Philter Desktop ({current.ToString(3)}).",
                    "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Couldn't parse the published version; show both and let the user decide.
                MessageBox.Show(this,
                    $"The latest published version is {manifest.Version}.\nYour installed version is {current.ToString(3)}.",
                    "Check for Updates", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening About dialog");
            }

            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }


        private void policiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Policy Editor");
            }

            var f = new PolicyEditorForm(_policyRepository);
            f.ShowDialog();
        }

        private void redactionContextsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) => OpenSettings();

        private void toolStripButtonRedactDocuments_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled, _settingsRepository);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        private void policiesToolStripButton_Click(object sender, EventArgs e)
        {
            var f = new PolicyEditorForm(_policyRepository);
            f.ShowDialog();
        }

        private void contextsToolStripButton_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void settingsToolStripButton_Click(object sender, EventArgs e) => OpenSettings();

        private void OpenSettings()
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Settings dialog");
            }

            using var settingsForm = new SettingsForm(
                _settingsRepository, _policyRepository, _contextRepository, _watchedFolderRepository,
                _watchedFolderLogRepository, EncryptedDatabase.CurrentKeyStore);
            var result = settingsForm.ShowDialog();

            // Reload logging setting in case it changed.
            if (result == DialogResult.OK)
            {
                var settings = _settingsRepository.GetSettings();
                bool previousLoggingState = _loggingEnabled;
                _loggingEnabled = settings.LoggingEnabled;

                if (_loggingEnabled && !previousLoggingState)
                {
                    Logger.LogInfo("Logging enabled via settings");
                }
                else if (!_loggingEnabled && previousLoggingState)
                {
                    Logger.LogInfo("Logging disabled via settings");
                }
            }

            // Watched folders are persisted immediately (independent of Save/Cancel); restart the
            // watcher if the list changed so monitoring reflects the new configuration.
            if (settingsForm.WatchedFoldersChanged)
            {
                StartFolderWatcher();
            }
            else
            {
                _folderWatcher.LoggingEnabled = _loggingEnabled;
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRedactionQueue();
        }

        private void modifyRedactionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }

            using var form = new ModifyRedactionForm(
                id, _redactionVersionRepository, _redactionSpanRepository, _policyRepository,
                _settingsRepository.GetSettings().RedactedSuffix);
            form.ShowDialog(this);
        }

        // Stores the initial redaction as version 1 (with its spans) so it can be reviewed/modified.
        private void SaveRedactionVersion(RedactionQueueEntity entity, string outputPath, List<RedactionSpanEntity> spans)
        {
            try
            {
                var version = new RedactionVersionEntity
                {
                    DocumentId = entity.Id,
                    Version = _redactionVersionRepository.NextVersionNumber(entity.Id),
                    SourcePath = entity.Name,
                    OutputPath = outputPath,
                    FileType = Path.GetExtension(entity.Name).ToLowerInvariant(),
                    Policy = entity.Policy,
                    Context = entity.Context,
                    Highlight = entity.Highlight
                };
                _redactionVersionRepository.Insert(version);

                int order = 0;
                foreach (RedactionSpanEntity s in spans)
                {
                    s.VersionId = version.Id;
                    s.Order = order++;
                }
                if (spans.Count > 0)
                {
                    _redactionSpanRepository.InsertBulk(spans);
                }
            }
            catch (Exception ex)
            {
                if (_loggingEnabled)
                {
                    Logger.LogWarning($"Failed to store redaction spans: {ex.Message}");
                }
            }
        }

        private void clearRedactionHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int versionCount = _redactionVersionRepository.Count();
            int completed = listView1.Items.Cast<ListViewItem>()
                .Count(i => i.SubItems.Count > 1 && i.SubItems[1].Text == "Completed");

            if (versionCount == 0 && completed == 0)
            {
                MessageBox.Show("There is no redaction history to clear.", "Clear Redaction History",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Permanently delete all saved redaction history (every version and its stored spans, " +
                "including the detected text) and remove completed documents from the list?\n\n" +
                "This does not delete any redacted output files already on disk.",
                "Clear Redaction History",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            RedactionHistory.ClearAll(_redactionSpanRepository, _redactionVersionRepository, _redactionQueueRepository);
            LoadRedactionQueue();

            if (_loggingEnabled)
            {
                Logger.LogInfo("Cleared all stored redaction history and completed queue items.");
            }

            MessageBox.Show("Redaction history cleared.", "Clear Redaction History",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Removes all stored versions and spans for a document (used when the queue item is removed).
        private void DeleteRedactionHistory(ObjectId documentId)
        {
            foreach (RedactionVersionEntity v in _redactionVersionRepository.GetForDocument(documentId))
            {
                _redactionSpanRepository.DeleteForVersion(v.Id);
            }
            _redactionVersionRepository.DeleteForDocument(documentId);
        }

        private void removeCompletedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int completed = listView1.Items.Cast<ListViewItem>()
                .Count(i => i.SubItems.Count > 1 && i.SubItems[1].Text == "Completed");

            if (completed == 0)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Remove {completed} completed item{(completed == 1 ? "" : "s")} from the queue?",
                "Remove Completed",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (RedactionQueueEntity done in _redactionQueueRepository.Find(x => x.Status == "Completed").ToList())
            {
                DeleteRedactionHistory(done.Id);
            }
            _redactionQueueRepository.DeleteWhere(x => x.Status == "Completed");
            LoadRedactionQueue();
        }

        private void ContextMenuStrip1_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = listView1.SelectedItems.Count > 0;
            bool hasItems = listView1.Items.Count > 0;
            bool selectedCompleted = hasSelection && IsCompleted(listView1.SelectedItems[0]);

            bool anyCompleted = false;
            foreach (ListViewItem item in listView1.Items)
            {
                if (IsCompleted(item))
                {
                    anyCompleted = true;
                    break;
                }
            }

            // "Add Files…" and "Refresh" don't depend on the queue and stay enabled.
            removeToolStripMenuItem.Enabled = hasSelection;
            removeAllToolStripMenuItem.Enabled = hasItems;
            removeCompletedToolStripMenuItem.Enabled = anyCompleted;
            openRedactedFileToolStripMenuItem.Enabled = selectedCompleted; // exists only once redacted
            openOriginalFileToolStripMenuItem.Enabled = hasSelection;
            openContainingFolderToolStripMenuItem.Enabled = selectedCompleted; // redacted file exists once completed
            modifyRedactionToolStripMenuItem.Enabled = selectedCompleted; // versions exist once redacted

            // Enable for a completed .txt / .docx (text diff) or .pdf (side-by-side page comparison).
            viewDiffToolStripMenuItem.Enabled = selectedCompleted &&
                SelectedSourcePath() is { } src &&
                (src.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                 src.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                 src.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

            viewDetailsToolStripMenuItem.Enabled = hasSelection;
        }

        /// <summary>The source file path of the selected queue item, or null.</summary>
        private string? SelectedSourcePath()
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return null;
            }
            return _redactionQueueRepository.GetById(id)?.Name;
        }

        /// <summary>
        /// The actual redacted-output path for a document: the latest stored version's output that
        /// exists on disk (so a custom Save-As location or a later Modify-Redaction output is honored),
        /// falling back to the computed default location.
        /// </summary>
        private string ResolveRedactedOutputPath(ObjectId documentId, string sourceName) =>
            RedactionService.ResolveOutputPath(
                _redactionVersionRepository.GetForDocument(documentId), // ordered by version ascending
                RedactionService.GetOutputPath(sourceName, _settingsRepository.GetSettings()));

        private static bool IsCompleted(ListViewItem item) =>
            item.SubItems.Count > 1 && string.Equals(item.SubItems[1].Text, "Completed", StringComparison.OrdinalIgnoreCase);

        private void viewDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity is null)
            {
                return;
            }

            string source = entity.Name;
            string output = ResolveRedactedOutputPath(id, source);

            if (!File.Exists(source))
            {
                MessageBox.Show($"The original file could not be found:\n\n{source}", "View Diff",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(output))
            {
                MessageBox.Show($"The redacted file could not be found:\n\n{output}", "View Diff",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                if (source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    byte[] before = File.ReadAllBytes(source);
                    byte[] after = File.ReadAllBytes(output);
                    using var compare = new PdfCompareForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    compare.ShowDialog(this);
                }
                else if (source.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    // Word documents have no plain-text form: compare the extracted body paragraphs
                    // (one per line), which is what the redactor operates on.
                    string before = string.Join("\n", WordDocumentRedactor.ReadParagraphs(source));
                    string after = string.Join("\n", WordDocumentRedactor.ReadParagraphs(output));
                    using var diff = new DiffViewerForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    diff.ShowDialog(this);
                }
                else
                {
                    string before = File.ReadAllText(source);
                    string after = File.ReadAllText(output);
                    using var diff = new DiffViewerForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    diff.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not show the diff: {ex.Message}", "View Diff",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void viewDetailsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity is null)
            {
                return;
            }

            string source = entity.Name;
            RedactionVersionEntity? latest = _redactionVersionRepository.GetForDocument(id).LastOrDefault();

            string redactedFile = "—";
            string redactionCount = "—";
            string timestamp = "—";
            if (latest is not null)
            {
                redactedFile = ResolveRedactedOutputPath(id, source);
                redactionCount = _redactionSpanRepository.GetForVersion(latest.Id).Count.ToString();
                timestamp = latest.CreatedAt.ToLocalTime().ToString("f");
            }

            var rows = new List<(string, string)>
            {
                ("Source file", Path.GetFileName(source)),
                ("Source path", source),
                ("Redacted file", string.IsNullOrEmpty(redactedFile) || redactedFile == "—" ? "—" : Path.GetFileName(redactedFile)),
                ("Redacted path", redactedFile),
                ("Status", entity.Status),
                ("Policy", string.IsNullOrEmpty(entity.Policy) ? "—" : entity.Policy),
                ("Context", string.IsNullOrEmpty(entity.Context) ? "—" : entity.Context),
                ("Redactions", redactionCount),
                ("Redacted", timestamp)
            };

            // For a failed document, show why so the user isn't left guessing.
            if (!string.IsNullOrEmpty(entity.ErrorMessage))
            {
                rows.Add(("Why it failed", entity.ErrorMessage));
            }

            using var details = new RedactionDetailsForm($"Details — {Path.GetFileName(source)}", rows);
            details.ShowDialog(this);
        }

        private void addFilesToRedactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled, _settingsRepository);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        // Prototype preview-first flow for a single .txt or .pdf file: pick the file, preview the
        // redaction, and only write the output on Save. The result is recorded as a Completed queue
        // item (with version history) so View Diff / Modify Redaction work on it afterward.
        private void redactPreviewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var picker = new OpenFileDialog
            {
                Title = "Select a file to redact",
                Filter = "Supported (*.txt;*.docx;*.pdf)|*.txt;*.docx;*.pdf|Text (*.txt)|*.txt|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf"
            };
            if (picker.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            string source = picker.FileName;
            SettingsEntity settings = _settingsRepository.GetSettings();

            if (source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var preview = new PdfRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList());
                }
            }
            else if (source.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                using var preview = new WordRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList());
                }
            }
            else
            {
                using var preview = new TextRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList());
                }
            }
        }

        private void RecordCompletedRedaction(string source, string outputPath, string policy, string context, List<RedactionSpanEntity> spans)
        {
            var entity = new RedactionQueueEntity
            {
                Name = source,
                Policy = policy,
                Context = context,
                Status = "Completed"
            };
            _redactionQueueRepository.Insert(entity);
            SaveRedactionVersion(entity, outputPath, spans);
            RememberLastUsed(policy, context, Path.GetDirectoryName(outputPath));
            LoadRedactionQueue();
        }

        // Persists the most recently used policy/context and save folder so they're pre-selected next
        // time. Best-effort: a settings write failure must not disrupt the redaction that just succeeded.
        private void RememberLastUsed(string? policy, string? context, string? saveFolder)
        {
            try
            {
                SettingsEntity settings = _settingsRepository.GetSettings();
                if (!string.IsNullOrEmpty(policy)) settings.LastPolicy = policy;
                if (!string.IsNullOrEmpty(context)) settings.LastContext = context;
                if (!string.IsNullOrEmpty(saveFolder)) settings.LastSaveFolder = saveFolder;
                _settingsRepository.SaveSettings(settings);
            }
            catch
            {
                // best effort
            }
        }

        private async void RedactionQueueTimer_Tick(object sender, EventArgs e)
        {
            redactionQueueTimer.Stop();

            try
            {
                var pendingEntities = _redactionQueueRepository
                    .Find(x => x.Status == "Pending")
                    .ToList();

                if (pendingEntities.Count == 0)
                {
                    return;
                }

                if (_loggingEnabled)
                {
                    Logger.LogInfo($"Redaction queue timer fired: {pendingEntities.Count} item(s) pending redaction.");
                }

                var settings = _settingsRepository.GetSettings();
                var filterService = new FilterService();

                foreach (var entity in pendingEntities)
                {
                    UpdateEntityStatus(entity, "Processing");

                    QueueRedactionResult result = await QueueProcessor.ProcessAsync(
                        entity, _policyRepository, settings, filterService);

                    if (result.Success)
                    {
                        SaveRedactionVersion(entity, result.OutputPath!, result.Spans.ToList());
                        entity.ErrorMessage = string.Empty; // clear any prior failure reason
                        UpdateEntityStatus(entity, "Completed");
                        QueueNotification(new WatchedFileProcessedEventArgs
                        {
                            InputPath = entity.Name,
                            OutputPath = result.OutputPath,
                            Success = true
                        });

                        if (_loggingEnabled)
                        {
                            Logger.LogInfo($"Redaction completed: {entity.Name} -> {result.OutputPath}");
                        }
                    }
                    else
                    {
                        string reason = QueueProcessor.DescribeFailure(result, entity.Name);
                        entity.ErrorMessage = reason; // persisted so it survives past the toast
                        UpdateEntityStatus(entity, "Failed");
                        QueueNotification(new WatchedFileProcessedEventArgs
                        {
                            InputPath = entity.Name,
                            Success = false,
                            ErrorMessage = reason
                        });

                        if (_loggingEnabled)
                        {
                            if (result.Exception is not null)
                            {
                                Logger.LogError($"Redaction failed for {entity.Name}", result.Exception);
                            }
                            else
                            {
                                Logger.LogError($"Redaction failed for {entity.Name}: {result.ErrorMessage}");
                            }
                        }
                    }
                }

                LoadRedactionQueue();
            }
            finally
            {
                redactionQueueTimer.Start();
            }
        }

        private void UpdateEntityStatus(RedactionQueueEntity entity, string status)
        {
            entity.Status = status;
            _redactionQueueRepository.Update(entity);

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.Tag is ObjectId id && id == entity.Id)
                {
                    item.SubItems[1].Text = status;
                    break;
                }
            }

            EnsureStatusAnimation();
        }

        private void LoadRedactionQueue()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int total = 0;
            int shown = 0;
            string query = _filterBox?.Text ?? string.Empty;

            foreach (RedactionQueueEntity entity in _redactionQueueRepository.GetAll())
            {
                // Counts reflect the whole queue; the list shows only rows matching the filter.
                counts[entity.Status] = counts.GetValueOrDefault(entity.Status) + 1;
                total++;

                if (!QueueFilter.Matches(query, entity.Name, entity.Status, entity.Policy, entity.Context))
                {
                    continue;
                }

                var item = new ListViewItem(entity.Name);
                item.SubItems.Add(entity.Status);
                item.SubItems.Add(entity.Policy);
                item.SubItems.Add(entity.Context);
                item.Tag = entity.Id;
                item.ImageIndex = 0;
                if (!string.IsNullOrEmpty(entity.ErrorMessage))
                {
                    item.ToolTipText = entity.ErrorMessage; // hover a failed row to see why
                }
                listView1.Items.Add(item);
                shown++;
            }

            listView1.Sort(); // keep the user's chosen column/direction across reloads
            listView1.EndUpdate();

            bool filtering = !string.IsNullOrWhiteSpace(query);
            _emptyStateLabel.Text = filtering ? FilterEmptyText : DefaultEmptyText;
            UpdateEmptyState(shown);
            UpdateQueueSummary(total, shown, counts, filtering);
            EnsureStatusAnimation();
        }

        private void UpdateQueueSummary(int total, int shown, IReadOnlyDictionary<string, int> counts, bool filtering)
        {
            _queueSummaryLabel.Text = filtering
                ? QueueSummary.DescribeFilter(shown, total)
                : QueueSummary.Describe(total, counts);
        }

        private void ListView1_DoubleClick(object? sender, EventArgs e)
        {
            // Double-clicking a completed item opens its redacted file.
            if (listView1.SelectedItems.Count > 0 &&
                listView1.SelectedItems[0].SubItems.Count > 1 &&
                listView1.SelectedItems[0].SubItems[1].Text == "Completed")
            {
                openRedactedFileToolStripMenuItem_Click(sender!, e);
            }
        }

        private void openRedactedFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            string outputPath = ResolveRedactedOutputPath(id, entity.Name);

            if (!File.Exists(outputPath))
            {
                MessageBox.Show(
                    $"The redacted file could not be found:\n\n{outputPath}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = outputPath,
                UseShellExecute = true
            });
        }

        private void openOriginalFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            if (!File.Exists(entity.Name))
            {
                MessageBox.Show(
                    $"The original file could not be found:\n\n{entity.Name}",
                    "File Not Found",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = entity.Name,
                UseShellExecute = true
            });
        }

        // Opens the redacted file's folder in Explorer with the file selected.
        private void openContainingFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity is null)
            {
                return;
            }

            string outputPath = ResolveRedactedOutputPath(id, entity.Name);
            if (!File.Exists(outputPath))
            {
                MessageBox.Show(
                    $"The redacted file could not be found:\n\n{outputPath}",
                    "Open Containing Folder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{outputPath}\""
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Could not open the folder:\n\n{ex.Message}", "Open Containing Folder",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void removeAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.Items.Count == 0)
            {
                return;
            }

            var result = MessageBox.Show(
                "Remove all items from the queue? Items currently being processed will be kept.",
                "Remove All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result != DialogResult.Yes)
            {
                return;
            }

            foreach (RedactionQueueEntity removable in _redactionQueueRepository.Find(x => x.Status != "Processing").ToList())
            {
                DeleteRedactionHistory(removable.Id);
            }
            _redactionQueueRepository.DeleteWhere(x => x.Status != "Processing");
            LoadRedactionQueue();
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
            {
                return;
            }

            ListViewItem selected = listView1.SelectedItems[0];

            if (selected.Tag is not ObjectId id)
            {
                return;
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity == null)
            {
                return;
            }

            if (entity.Status == "Processing")
            {
                MessageBox.Show(
                    $"'{Path.GetFileName(entity.Name)}' cannot be removed because it is currently being processed.",
                    "Cannot Remove",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            _redactionQueueRepository.Delete(id);
            DeleteRedactionHistory(id);
            listView1.Items.Remove(selected);
        }
    }
}
