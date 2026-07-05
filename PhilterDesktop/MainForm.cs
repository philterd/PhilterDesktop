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
        private bool _queueBusy; // guards the async Verify / View Diff background operations

        private NotifyIcon _trayIcon = null!;
        private ToolStripMenuItem _pauseResumeItem = null!;
        private System.Windows.Forms.Timer? _logPruneTimer;
        private bool _exiting;
        private bool _watchingPaused;
        private bool _started;
        private bool _startMinimized;

        private readonly List<WatchedFileProcessedEventArgs> _pendingNotifications = new();
        private System.Windows.Forms.Timer? _notifyCoalesceTimer;

        // A second app launch signals this so the running window comes to the front (single-instance).
        private EventWaitHandle? _activationSignal;
        private RegisteredWaitHandle? _activationRegistration;
        private string? _lastNotifyDirectory;

        private const string HelpUrl = "https://philterd.github.io/PhilterDesktop/";

        private Label _emptyStateLabel = null!;
        private TextBox _filterBox = null!;
        private ToolStripStatusLabel _queueSummaryLabel = null!;
        private ToolStripStatusLabel _reviewWarningLabel = null!;
        private Image? _documentImage;
        private readonly QueueColumnSorter _sorter = new();

        private const string DefaultEmptyText =
            "No documents queued\r\n\r\nClick \"Redact\" or drag documents here";
        private const string FilterEmptyText =
            "No documents match your filter.\r\n\r\nClear the filter box to see the whole queue.";
        private System.Windows.Forms.Timer _statusAnimTimer = null!;
        private int _statusAnimPhase;

        private const int RowHeight = 32;

        public MainForm() : this(startMinimized: false)
        {
        }

        // Opens the encrypted user database under LocalAppData\PhilterDesktop (creating the folder).
        private static LiteDatabase OpenDefaultDatabase()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhilterDesktop");
            Directory.CreateDirectory(folder);
            return EncryptedDatabase.Open(Path.Combine(folder, "data.db"));
        }

        public MainForm(bool startMinimized) : this(OpenDefaultDatabase(), startMinimized)
        {
        }

        // Test/screenshot hook: builds the main form against a supplied (already-open) database instead
        // of the encrypted user database, so it can be exercised/rendered with seeded data without
        // touching the real one. The running app always uses the parameterless/bool constructors.
        internal MainForm(LiteDatabase database, bool startMinimized)
        {
            BuildUi();
            ModernTheme.Apply(this);
            InitializeQueueUi();
            InitializeTray();
            _startMinimized = startMinimized;

            // Single shared database instance (the default path is encrypted at rest — it stores PII).
            _database = database;

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

            // Back the shared redaction engine with a durable, database-backed context service so
            // consistent (RANDOM_REPLACE) replacements persist across documents/restarts and don't
            // accumulate in memory. Must run before any redaction (and before the watcher below, which
            // captures the shared instance).
            SharedFilterService.UseContextService(new LiteDbContextService(_contextEntryRepository));

            _folderWatcher = new FolderWatcherService(_policyRepository, _loggingEnabled, _watchedFolderLogRepository, _settingsRepository);
            _folderWatcher.FileProcessed += OnWatchedFileProcessed;

            // Load settings and check if logging is enabled
            var settings = _settingsRepository.GetSettings();
            _loggingEnabled = settings.LoggingEnabled;

            // Apply the user's configured regex match timeout (over the startup default) before the engine
            // warms up, so it governs every detection pattern, including custom-identifier regexes.
            RegexSafety.InstallMatchTimeout(settings.RegexMatchTimeoutSeconds);

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

            // Load the on-device name model in the background now, so the first redaction/preview that
            // uses it doesn't stall for tens of seconds loading it on demand.
            SharedFilterService.WarmUp();
        }

        // --- UI construction (hand-written; MainForm has no Windows Forms designer) -------------------
        //
        // The whole form is built in code, split into focused builders so each region is easy to find.
        // BuildUi is the single entry point the constructor calls; it mirrors what a designer-generated
        // InitializeComponent would do (suspend layout, create controls, add them, resume), but stays in
        // one consistent style with the other Build* helpers (BuildFilterBar, BuildTrayMenu, etc.).
        private void BuildUi()
        {
            components = new System.ComponentModel.Container();
            SuspendLayout();

            BuildSharedComponents();
            BuildMenuBar();
            BuildToolbar();
            BuildQueueContextMenu();
            BuildQueueList();
            BuildStatusBar();
            BuildFormShell();

            // The form icon is applied by ModernTheme.Apply(this) right after construction, so it is not
            // set here.
            ResumeLayout(false);
            PerformLayout();
        }

        // Container-owned, non-visual components (the auto-refresh timer and the list view's image list).
        private void BuildSharedComponents()
        {
            imageList1 = new ImageList(components)
            {
                ColorDepth = ColorDepth.Depth32Bit,
                ImageSize = new Size(16, 16),
                TransparentColor = Color.Transparent
            };

            redactionQueueTimer = new System.Windows.Forms.Timer(components) { Interval = 15000 };
            redactionQueueTimer.Tick += RedactionQueueTimer_Tick;
        }

        // The top menu bar: File (clear history, exit) and Help (help, updates, about). The "More from
        // Philterd" submenu and the Help-item handler are wired separately in the constructor flow.
        private void BuildMenuBar()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            clearRedactionHistoryToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparatorFile = new ToolStripSeparator();
            exitToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            checkForUpdatesToolStripMenuItem = new ToolStripMenuItem();
            viewLicenseToolStripMenuItem = new ToolStripMenuItem();
            aboutToolStripMenuItem = new ToolStripMenuItem();

            menuStrip1.SuspendLayout();

            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(4, 1, 0, 1);
            menuStrip1.Size = new Size(866, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";

            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { clearRedactionHistoryToolStripMenuItem, toolStripSeparatorFile, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 22);
            fileToolStripMenuItem.Text = "File";

            clearRedactionHistoryToolStripMenuItem.Name = "clearRedactionHistoryToolStripMenuItem";
            clearRedactionHistoryToolStripMenuItem.Size = new Size(207, 22);
            clearRedactionHistoryToolStripMenuItem.Text = "Clear Redaction History...";
            clearRedactionHistoryToolStripMenuItem.Click += clearRedactionHistoryToolStripMenuItem_Click;

            toolStripSeparatorFile.Name = "toolStripSeparatorFile";
            toolStripSeparatorFile.Size = new Size(204, 6);

            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(207, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;

            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripMenuItem1, toolStripSeparator1, checkForUpdatesToolStripMenuItem, viewLicenseToolStripMenuItem, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 22);
            helpToolStripMenuItem.Text = "Help";

            helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            helpToolStripMenuItem1.Size = new Size(180, 22);
            helpToolStripMenuItem1.Text = "Help";

            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(177, 6);

            checkForUpdatesToolStripMenuItem.Name = "checkForUpdatesToolStripMenuItem";
            checkForUpdatesToolStripMenuItem.Size = new Size(180, 22);
            checkForUpdatesToolStripMenuItem.Text = "Check for Updates...";
            checkForUpdatesToolStripMenuItem.Click += checkForUpdatesToolStripMenuItem_Click;

            viewLicenseToolStripMenuItem.Name = "viewLicenseToolStripMenuItem";
            viewLicenseToolStripMenuItem.Size = new Size(180, 22);
            viewLicenseToolStripMenuItem.Text = "View License...";
            viewLicenseToolStripMenuItem.Click += viewLicenseToolStripMenuItem_Click;

            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(180, 22);
            aboutToolStripMenuItem.Text = "About...";
            aboutToolStripMenuItem.Click += aboutToolStripMenuItem_Click;

            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
        }

        // The main toolbar: the Redact split button (with its dropdown), and the Policies/Contexts/Lists,
        // Settings, Refresh, and Help buttons. Button glyphs are assigned later by ApplyToolbarIcons.
        private void BuildToolbar()
        {
            toolStrip1 = new ToolStrip();
            toolStripButtonRedact = new ToolStripSplitButton();
            redactDropDownItem = new ToolStripMenuItem();
            previewDropDownItem = new ToolStripMenuItem();
            findRedactDropDownItem = new ToolStripMenuItem();
            spreadsheetDropDownItem = new ToolStripMenuItem();
            folderDropDownItem = new ToolStripMenuItem();
            toolStripSeparator3 = new ToolStripSeparator();
            policiesToolStripButton = new ToolStripButton();
            contextsToolStripButton = new ToolStripButton();
            listsToolStripButton = new ToolStripButton();
            toolStripSeparator9 = new ToolStripSeparator();
            settingsToolStripButton = new ToolStripButton();
            toolStripSeparator6 = new ToolStripSeparator();
            refreshToolStripButton = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            HelpToolStripButton = new ToolStripButton();

            toolStrip1.SuspendLayout();

            toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip1.ImageScalingSize = new Size(24, 24);
            toolStrip1.Items.AddRange(new ToolStripItem[] { toolStripButtonRedact, toolStripSeparator3, policiesToolStripButton, contextsToolStripButton, listsToolStripButton, toolStripSeparator9, settingsToolStripButton, toolStripSeparator6, refreshToolStripButton, toolStripSeparator4, HelpToolStripButton });
            toolStrip1.Location = new Point(0, 24);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Padding = new Padding(0, 0, 2, 0);
            toolStrip1.Size = new Size(866, 25);
            toolStrip1.TabIndex = 4;
            toolStrip1.Text = "toolStrip1";

            toolStripButtonRedact.AutoToolTip = false;
            toolStripButtonRedact.DropDownItems.AddRange(new ToolStripItem[] { redactDropDownItem, previewDropDownItem, findRedactDropDownItem, spreadsheetDropDownItem, folderDropDownItem });
            toolStripButtonRedact.ImageTransparentColor = Color.Magenta;
            toolStripButtonRedact.Name = "toolStripButtonRedact";
            toolStripButtonRedact.Size = new Size(59, 22);
            toolStripButtonRedact.Text = "Redact";
            toolStripButtonRedact.TextImageRelation = TextImageRelation.ImageAboveText;
            toolStripButtonRedact.ToolTipText = "Add documents to the redaction queue (click the arrow for Preview and Find & Redact)";
            toolStripButtonRedact.ButtonClick += toolStripButtonRedactDocuments_Click;

            redactDropDownItem.Name = "redactDropDownItem";
            redactDropDownItem.Size = new Size(189, 22);
            redactDropDownItem.Text = "&Redact...";
            redactDropDownItem.ToolTipText = "Add documents to the redaction queue";
            redactDropDownItem.Click += toolStripButtonRedactDocuments_Click;

            previewDropDownItem.Name = "previewDropDownItem";
            previewDropDownItem.Size = new Size(189, 22);
            previewDropDownItem.Text = "Redact with &Preview...";
            previewDropDownItem.Click += redactPreviewToolStripMenuItem_Click;

            findRedactDropDownItem.Name = "findRedactDropDownItem";
            findRedactDropDownItem.Size = new Size(189, 22);
            findRedactDropDownItem.Text = "&Find && Redact...";
            findRedactDropDownItem.Click += findAndRedactToolStripMenuItem_Click;

            spreadsheetDropDownItem.Name = "spreadsheetDropDownItem";
            spreadsheetDropDownItem.Size = new Size(189, 22);
            spreadsheetDropDownItem.Text = "Redact &Spreadsheet...";
            spreadsheetDropDownItem.Click += redactSpreadsheetToolStripMenuItem_Click;

            folderDropDownItem.Name = "folderDropDownItem";
            folderDropDownItem.Size = new Size(189, 22);
            folderDropDownItem.Text = "Redact Fol&der...";
            folderDropDownItem.ToolTipText = "Add every supported file in a folder to the redaction queue";
            folderDropDownItem.Click += redactFolderToolStripMenuItem_Click;

            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);

            policiesToolStripButton.AutoToolTip = false;
            policiesToolStripButton.ImageTransparentColor = Color.Magenta;
            policiesToolStripButton.Name = "policiesToolStripButton";
            policiesToolStripButton.Size = new Size(51, 22);
            policiesToolStripButton.Text = "Policies";
            policiesToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            policiesToolStripButton.ToolTipText = "Create and edit redaction policies";
            policiesToolStripButton.Click += policiesToolStripButton_Click;

            contextsToolStripButton.AutoToolTip = false;
            contextsToolStripButton.ImageTransparentColor = Color.Magenta;
            contextsToolStripButton.Name = "contextsToolStripButton";
            contextsToolStripButton.Size = new Size(57, 22);
            contextsToolStripButton.Text = "Contexts";
            contextsToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            contextsToolStripButton.ToolTipText = "Manage contexts for consistent replacements";
            contextsToolStripButton.Click += contextsToolStripButton_Click;

            listsToolStripButton.AutoToolTip = false;
            listsToolStripButton.ImageTransparentColor = Color.Magenta;
            listsToolStripButton.Name = "listsToolStripButton";
            listsToolStripButton.Size = new Size(34, 22);
            listsToolStripButton.Text = "Lists";
            listsToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            listsToolStripButton.ToolTipText = "Edit global Always Redact / Always Ignore lists (apply to every policy)";
            listsToolStripButton.Click += listsToolStripButton_Click;

            toolStripSeparator9.Name = "toolStripSeparator9";
            toolStripSeparator9.Size = new Size(6, 25);

            settingsToolStripButton.AutoToolTip = false;
            settingsToolStripButton.ImageTransparentColor = Color.Magenta;
            settingsToolStripButton.Name = "settingsToolStripButton";
            settingsToolStripButton.Size = new Size(53, 22);
            settingsToolStripButton.Text = "Settings";
            settingsToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            settingsToolStripButton.ToolTipText = "Open settings (output location, logging, watched folders, security)";
            settingsToolStripButton.Click += settingsToolStripButton_Click;

            toolStripSeparator6.Name = "toolStripSeparator6";
            toolStripSeparator6.Size = new Size(6, 25);

            refreshToolStripButton.AutoToolTip = false;
            refreshToolStripButton.ImageTransparentColor = Color.Magenta;
            refreshToolStripButton.Name = "refreshToolStripButton";
            refreshToolStripButton.Size = new Size(50, 22);
            refreshToolStripButton.Text = "Refresh";
            refreshToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            refreshToolStripButton.ToolTipText = "Refresh the queue (F5)";
            refreshToolStripButton.Click += refreshToolStripMenuItem_Click;

            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);

            HelpToolStripButton.AutoToolTip = false;
            HelpToolStripButton.ImageTransparentColor = Color.Magenta;
            HelpToolStripButton.Name = "HelpToolStripButton";
            HelpToolStripButton.Size = new Size(36, 22);
            HelpToolStripButton.Text = "Help";
            HelpToolStripButton.TextImageRelation = TextImageRelation.ImageAboveText;
            HelpToolStripButton.ToolTipText = "Open the help documentation";

            // A quiet, right-aligned CTA: the official signed build comes with support (a paid per-user
            // subscription). Kept distinct from policy consulting, which lives in its own contextual spots.
            // Tagged utm_medium=toolbar so its click-through can be measured against the other links.
            var support = new ToolStripLabel("Support")
            {
                IsLink = true,
                Alignment = ToolStripItemAlignment.Right,
                LinkColor = ModernTheme.Accent,
                ActiveLinkColor = ModernTheme.Accent,
                ToolTipText = "The official, signed build comes with support — a per-user subscription from Philterd"
            };
            support.Click += (_, _) => Links.Open(Links.SupportUrl("toolbar"));
            toolStrip1.Items.Add(support);

            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
        }

        // The right-click menu for the queue list (add/redact, remove, open, modify/verify/report,
        // refresh). Item enable/disable is handled by ContextMenuStrip1_Opening, wired in InitializeQueueUi.
        private void BuildQueueContextMenu()
        {
            contextMenuStrip1 = new ContextMenuStrip(components);
            addFilesToRedactToolStripMenuItem = new ToolStripMenuItem();
            redactPreviewToolStripMenuItem = new ToolStripMenuItem();
            findAndRedactToolStripMenuItem = new ToolStripMenuItem();
            redactSpreadsheetToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator2 = new ToolStripSeparator();
            removeToolStripMenuItem = new ToolStripMenuItem();
            removeAllToolStripMenuItem = new ToolStripMenuItem();
            removeCompletedToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparatorRetry = new ToolStripSeparator();
            retryToolStripMenuItem = new ToolStripMenuItem();
            retryAllFailedToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator5 = new ToolStripSeparator();
            openRedactedFileToolStripMenuItem = new ToolStripMenuItem();
            openOriginalFileToolStripMenuItem = new ToolStripMenuItem();
            openContainingFolderToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator8 = new ToolStripSeparator();
            modifyRedactionToolStripMenuItem = new ToolStripMenuItem();
            viewDiffToolStripMenuItem = new ToolStripMenuItem();
            viewDetailsToolStripMenuItem = new ToolStripMenuItem();
            exportExplanationToolStripMenuItem = new ToolStripMenuItem();
            verifyRedactionToolStripMenuItem = new ToolStripMenuItem();
            verifyWithSamePolicyToolStripMenuItem = new ToolStripMenuItem();
            verifyWithBroadPolicyToolStripMenuItem = new ToolStripMenuItem();
            generateReportToolStripMenuItem = new ToolStripMenuItem();
            toolStripSeparator7 = new ToolStripSeparator();
            refreshToolStripMenuItem = new ToolStripMenuItem();

            contextMenuStrip1.SuspendLayout();

            contextMenuStrip1.ImageScalingSize = new Size(24, 24);
            contextMenuStrip1.Items.AddRange(new ToolStripItem[] { addFilesToRedactToolStripMenuItem, redactPreviewToolStripMenuItem, findAndRedactToolStripMenuItem, redactSpreadsheetToolStripMenuItem, toolStripSeparator2, removeToolStripMenuItem, removeAllToolStripMenuItem, removeCompletedToolStripMenuItem, toolStripSeparatorRetry, retryToolStripMenuItem, retryAllFailedToolStripMenuItem, toolStripSeparator5, openRedactedFileToolStripMenuItem, openOriginalFileToolStripMenuItem, openContainingFolderToolStripMenuItem, toolStripSeparator8, modifyRedactionToolStripMenuItem, viewDiffToolStripMenuItem, viewDetailsToolStripMenuItem, exportExplanationToolStripMenuItem, verifyRedactionToolStripMenuItem, generateReportToolStripMenuItem, toolStripSeparator7, refreshToolStripMenuItem });
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(278, 314);

            addFilesToRedactToolStripMenuItem.Name = "addFilesToRedactToolStripMenuItem";
            addFilesToRedactToolStripMenuItem.ShortcutKeyDisplayString = "Ctrl+O";
            addFilesToRedactToolStripMenuItem.Size = new Size(277, 22);
            addFilesToRedactToolStripMenuItem.Text = "&Add Files to Redact...";
            addFilesToRedactToolStripMenuItem.Click += addFilesToRedactToolStripMenuItem_Click;

            redactPreviewToolStripMenuItem.Name = "redactPreviewToolStripMenuItem";
            redactPreviewToolStripMenuItem.Size = new Size(277, 22);
            redactPreviewToolStripMenuItem.Text = "Redact with &Preview...";
            redactPreviewToolStripMenuItem.Click += redactPreviewToolStripMenuItem_Click;

            findAndRedactToolStripMenuItem.Name = "findAndRedactToolStripMenuItem";
            findAndRedactToolStripMenuItem.Size = new Size(277, 22);
            findAndRedactToolStripMenuItem.Text = "&Find && Redact...";
            findAndRedactToolStripMenuItem.Click += findAndRedactToolStripMenuItem_Click;

            redactSpreadsheetToolStripMenuItem.Name = "redactSpreadsheetToolStripMenuItem";
            redactSpreadsheetToolStripMenuItem.Size = new Size(277, 22);
            redactSpreadsheetToolStripMenuItem.Text = "Redact &Spreadsheet...";
            redactSpreadsheetToolStripMenuItem.Click += redactSpreadsheetToolStripMenuItem_Click;

            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(274, 6);

            removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            removeToolStripMenuItem.ShortcutKeyDisplayString = "Del";
            removeToolStripMenuItem.Size = new Size(277, 22);
            removeToolStripMenuItem.Text = "Re&move...";
            removeToolStripMenuItem.Click += removeToolStripMenuItem_Click;

            removeAllToolStripMenuItem.Name = "removeAllToolStripMenuItem";
            removeAllToolStripMenuItem.Size = new Size(277, 22);
            removeAllToolStripMenuItem.Text = "Remove A&ll...";
            removeAllToolStripMenuItem.Click += removeAllToolStripMenuItem_Click;

            removeCompletedToolStripMenuItem.Name = "removeCompletedToolStripMenuItem";
            removeCompletedToolStripMenuItem.Size = new Size(277, 22);
            removeCompletedToolStripMenuItem.Text = "Remove &Completed...";
            removeCompletedToolStripMenuItem.Click += removeCompletedToolStripMenuItem_Click;

            toolStripSeparatorRetry.Name = "toolStripSeparatorRetry";
            toolStripSeparatorRetry.Size = new Size(274, 6);

            retryToolStripMenuItem.Name = "retryToolStripMenuItem";
            retryToolStripMenuItem.Size = new Size(277, 22);
            retryToolStripMenuItem.Text = "Re&try";
            retryToolStripMenuItem.Click += retryToolStripMenuItem_Click;

            retryAllFailedToolStripMenuItem.Name = "retryAllFailedToolStripMenuItem";
            retryAllFailedToolStripMenuItem.Size = new Size(277, 22);
            retryAllFailedToolStripMenuItem.Text = "Retry All Faile&d";
            retryAllFailedToolStripMenuItem.Click += retryAllFailedToolStripMenuItem_Click;

            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(274, 6);

            openRedactedFileToolStripMenuItem.Name = "openRedactedFileToolStripMenuItem";
            openRedactedFileToolStripMenuItem.ShortcutKeyDisplayString = "Enter";
            openRedactedFileToolStripMenuItem.Size = new Size(277, 22);
            openRedactedFileToolStripMenuItem.Text = "Open &Redacted File...";
            openRedactedFileToolStripMenuItem.Click += openRedactedFileToolStripMenuItem_Click;

            openOriginalFileToolStripMenuItem.Name = "openOriginalFileToolStripMenuItem";
            openOriginalFileToolStripMenuItem.Size = new Size(277, 22);
            openOriginalFileToolStripMenuItem.Text = "Open &Original File...";
            openOriginalFileToolStripMenuItem.Click += openOriginalFileToolStripMenuItem_Click;

            openContainingFolderToolStripMenuItem.Name = "openContainingFolderToolStripMenuItem";
            openContainingFolderToolStripMenuItem.Size = new Size(277, 22);
            openContainingFolderToolStripMenuItem.Text = "Open Containin&g Folder";
            openContainingFolderToolStripMenuItem.Click += openContainingFolderToolStripMenuItem_Click;

            toolStripSeparator8.Name = "toolStripSeparator8";
            toolStripSeparator8.Size = new Size(274, 6);

            modifyRedactionToolStripMenuItem.Name = "modifyRedactionToolStripMenuItem";
            modifyRedactionToolStripMenuItem.Size = new Size(277, 22);
            modifyRedactionToolStripMenuItem.Text = "Modif&y Redaction...";
            modifyRedactionToolStripMenuItem.Click += modifyRedactionToolStripMenuItem_Click;

            viewDiffToolStripMenuItem.Name = "viewDiffToolStripMenuItem";
            viewDiffToolStripMenuItem.Size = new Size(277, 22);
            viewDiffToolStripMenuItem.Text = "Vie&w Diff...";
            viewDiffToolStripMenuItem.Click += viewDiffToolStripMenuItem_Click;

            viewDetailsToolStripMenuItem.Name = "viewDetailsToolStripMenuItem";
            viewDetailsToolStripMenuItem.Size = new Size(277, 22);
            viewDetailsToolStripMenuItem.Text = "View D&etails...";
            viewDetailsToolStripMenuItem.Click += viewDetailsToolStripMenuItem_Click;

            exportExplanationToolStripMenuItem.Name = "exportExplanationToolStripMenuItem";
            exportExplanationToolStripMenuItem.Size = new Size(277, 22);
            exportExplanationToolStripMenuItem.Text = "E&xport Explanation...";
            exportExplanationToolStripMenuItem.Click += exportExplanationToolStripMenuItem_Click;

            verifyRedactionToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { verifyWithSamePolicyToolStripMenuItem, verifyWithBroadPolicyToolStripMenuItem });
            verifyRedactionToolStripMenuItem.Name = "verifyRedactionToolStripMenuItem";
            verifyRedactionToolStripMenuItem.Size = new Size(277, 22);
            verifyRedactionToolStripMenuItem.Text = "&Verify Redaction";
            verifyRedactionToolStripMenuItem.ToolTipText = "Re-scan the redacted output for any PII that may remain";

            verifyWithSamePolicyToolStripMenuItem.Name = "verifyWithSamePolicyToolStripMenuItem";
            verifyWithSamePolicyToolStripMenuItem.Size = new Size(220, 22);
            verifyWithSamePolicyToolStripMenuItem.Text = "With &same policy";
            verifyWithSamePolicyToolStripMenuItem.ToolTipText = "Re-scan using the same policy that redacted this document";
            verifyWithSamePolicyToolStripMenuItem.Click += verifyWithSamePolicyToolStripMenuItem_Click;

            verifyWithBroadPolicyToolStripMenuItem.Name = "verifyWithBroadPolicyToolStripMenuItem";
            verifyWithBroadPolicyToolStripMenuItem.Size = new Size(220, 22);
            verifyWithBroadPolicyToolStripMenuItem.Text = "With &broad policy";
            verifyWithBroadPolicyToolStripMenuItem.ToolTipText = "Re-scan with every detector on (may flag items you chose not to redact)";
            verifyWithBroadPolicyToolStripMenuItem.Click += verifyWithBroadPolicyToolStripMenuItem_Click;

            generateReportToolStripMenuItem.Name = "generateReportToolStripMenuItem";
            generateReportToolStripMenuItem.Size = new Size(277, 22);
            generateReportToolStripMenuItem.Text = "Ge&nerate Report...";
            generateReportToolStripMenuItem.ToolTipText = "Create a shareable PDF/HTML summary of this redaction (no original text)";
            generateReportToolStripMenuItem.Click += generateReportToolStripMenuItem_Click;

            toolStripSeparator7.Name = "toolStripSeparator7";
            toolStripSeparator7.Size = new Size(274, 6);

            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.ShortcutKeyDisplayString = "F5";
            refreshToolStripMenuItem.Size = new Size(277, 22);
            refreshToolStripMenuItem.Text = "Refres&h";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;

            contextMenuStrip1.ResumeLayout(false);
        }

        // The redaction queue list (Details view, five columns). Owner-drawing, sorting, drag-and-drop,
        // and the taller row height are configured later in InitializeQueueUi.
        private void BuildQueueList()
        {
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader5 = new ColumnHeader();

            listView1.AccessibleDescription = "Documents to redact, with their status, policy, and context";
            listView1.AccessibleName = "Redaction queue";
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader4, columnHeader3, columnHeader5 });
            listView1.ContextMenuStrip = contextMenuStrip1;
            listView1.Dock = DockStyle.Fill;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.LargeImageList = imageList1;
            listView1.Location = new Point(0, 49);
            listView1.Margin = new Padding(2);
            listView1.Name = "listView1";
            listView1.Size = new Size(866, 268);
            listView1.SmallImageList = imageList1;
            listView1.TabIndex = 3;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;

            columnHeader1.Text = "File Name";
            columnHeader1.Width = 350;
            columnHeader2.Text = "Status";
            columnHeader2.Width = 180;
            columnHeader4.Text = "Policy";
            columnHeader4.Width = 120;
            columnHeader3.Text = "Context";
            columnHeader3.Width = 120;
            columnHeader5.Text = "Verification";
            columnHeader5.Width = 130;
        }

        // The bottom status strip. Its live labels (queue summary, review reminder) are added in the
        // constructor flow.
        private void BuildStatusBar()
        {
            statusStrip1 = new StatusStrip();
            statusStrip1.ImageScalingSize = new Size(24, 24);
            statusStrip1.Location = new Point(0, 317);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(866, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
        }

        // Form-level properties and the top-level control z-order (added list-first so it fills inside
        // the docked status/tool/menu strips).
        private void BuildFormShell()
        {
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(866, 339);
            Controls.Add(listView1);
            Controls.Add(statusStrip1);
            Controls.Add(toolStrip1);
            Controls.Add(menuStrip1);

            // If the on-device name model is missing, name detection silently does nothing — surface
            // that loudly with a persistent banner just above the queue (mirrors the filter bar's
            // placement) so it's never mistaken for a working install.
            if (!PhEyeModel.IsAvailable)
            {
                Panel modelWarning = WarningBanner.Create(PhEyeModel.UnavailableWarning);
                Controls.Add(modelWarning);
                Controls.SetChildIndex(modelWarning, 1); // just above the list, below the toolbar
            }

            MainMenuStrip = menuStrip1;
            MinimumSize = new Size(722, 364);
            Name = "MainForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            Load += Form1_Load;
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
            // Document icon for owner-drawing the first column (a system glyph, drawn at 16px).
            // The SmallImageList is then replaced with a transparent spacer purely to force a taller
            // row height (Details-view rows size to ImageSize.Height).
            _documentImage = ModernTheme.CreateGlyphImage("", 16, ModernTheme.Text); // Document
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
                Visible = false,
                // The overlay sits on top of the (empty) list, so dropping onto the hint must work too —
                // forward to the same handlers as the list.
                AllowDrop = true
            };
            _emptyStateLabel.DragEnter += QueueList_DragEnter;
            _emptyStateLabel.DragDrop += QueueList_DragDrop;
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
            // Clickable so it opens the full redaction-review notice; keep the amber warning colour as the
            // link colour (only underline on hover) so it still reads as a warning, not a plain hyperlink.
            Color reviewAmber = Color.FromArgb(176, 92, 0);
            _reviewWarningLabel = new ToolStripStatusLabel
            {
                Text = "Redaction can include mistakes. Always carefully review redacted documents before sharing.",
                Font = new Font(ModernTheme.UiFont, FontStyle.Bold),
                ForeColor = reviewAmber,
                IsLink = true,
                LinkColor = reviewAmber,
                ActiveLinkColor = reviewAmber,
                LinkBehavior = LinkBehavior.HoverUnderline,
                ToolTipText = "Click to read about reviewing redacted documents",
                AccessibleName = "Important: redaction can include mistakes. Always carefully review redacted documents before sharing. Click to read more."
            };
            _reviewWarningLabel.Click += (_, _) => ShowRedactionNotice();
            statusStrip1.Items.Add(_reviewWarningLabel);

            // --- Wire the previously-dead Help buttons ---
            HelpToolStripButton.Click += OpenHelp;
            helpToolStripMenuItem1.Click += OpenHelp;

            // --- "More from Philterd" submenu under Help (other Philterd offerings) ---
            BuildMoreFromPhilterdMenu();

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

            var searchLabel = new Label
            {
                Dock = DockStyle.Left,
                Text = "Search for",
                AutoSize = false,
                Width = 72,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = ModernTheme.Text
            };

            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 34,
                Padding = new Padding(8, 6, 8, 4),
                BackColor = ModernTheme.Surface
            };
            // Add the Fill text box first (lowest z-order) so the Left label docks beside it, not over it.
            panel.Controls.Add(_filterBox);
            panel.Controls.Add(searchLabel);
            Controls.Add(panel);

            // Docking is applied back-to-front in the z-order. Put the panel at index 1 so it docks
            // just inside the toolbar/menu (higher indexes) and just outside the Fill list (index 0).
            Controls.SetChildIndex(panel, 1);
        }

        private void ApplyToolbarIcons()
        {
            const int size = 24;
            // Segoe Fluent / MDL2 glyphs. Redact is accent-colored as the primary action.
            toolStripButtonRedact.Image = ModernTheme.CreateGlyphImage("\uE72E", size, ModernTheme.Accent);          // Lock
            policiesToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE8FD", size, ModernTheme.Text);          // BulletedList
            contextsToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE8EC", size, ModernTheme.Text);         // Tag
            listsToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE71C", size, ModernTheme.Text);            // Filter (global lists)
            settingsToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE713", size, ModernTheme.Text);         // Settings
            refreshToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE72C", size, ModernTheme.Text);          // Refresh
            HelpToolStripButton.Image = ModernTheme.CreateGlyphImage("\uE897", size, ModernTheme.Text);             // Help

            // Context-menu item icons (16px glyphs) \u2014 standardized to match the toolbar, no bitmaps.
            const int menuSize = 16;
            addFilesToRedactToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE710", menuSize, ModernTheme.Text);       // Add
            openRedactedFileToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE8E5", menuSize, ModernTheme.Text);       // OpenFile
            openOriginalFileToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE8E5", menuSize, ModernTheme.Text);       // OpenFile
            openContainingFolderToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE838", menuSize, ModernTheme.Text);   // FolderOpen
            refreshToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE72C", menuSize, ModernTheme.Text);                // Refresh
            removeToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE74D", menuSize, ModernTheme.Text);                 // Delete
            redactPreviewToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE890", menuSize, ModernTheme.Text);          // View (preview)
            modifyRedactionToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE70F", menuSize, ModernTheme.Text);        // Edit
            viewDiffToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE7C4", menuSize, ModernTheme.Text);               // TwoPage (compare)
            viewDetailsToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE946", menuSize, ModernTheme.Text);            // Info (details)
            exportExplanationToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE898", menuSize, ModernTheme.Text);      // Save/Export (explanation)
            verifyRedactionToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uEA18", menuSize, ModernTheme.Text);       // Shield (verify)
            generateReportToolStripMenuItem.Image = ModernTheme.CreateGlyphImage("\uE7C3", menuSize, ModernTheme.Text);        // Page (report)

            // Redact split-button dropdown items.
            redactDropDownItem.Image = ModernTheme.CreateGlyphImage("\uE72E", menuSize, ModernTheme.Accent);                    // Lock (redact)
            previewDropDownItem.Image = ModernTheme.CreateGlyphImage("\uE890", menuSize, ModernTheme.Text);                     // View (preview)
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

        private void Form1_Load(object? sender, EventArgs e)
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
                if (keyData == Keys.Enter && listView1.SelectedItems.Count == 1 && IsCompleted(listView1.SelectedItems[0]))
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
            RegisterActivationListener();
        }

        // Listen for a second launch asking us to come forward (single-instance activation). Best-effort;
        // never blocks startup. Cleaned up in OnHandleDestroyed (which also runs on Dispose).
        private void RegisterActivationListener()
        {
            if (_activationSignal is not null)
            {
                return;
            }
            try
            {
                _activationSignal = AppInstance.CreateActivationSignal();
                _activationRegistration = ThreadPool.RegisterWaitForSingleObject(
                    _activationSignal,
                    (_, _) =>
                    {
                        try { BeginInvoke(new Action(ShowFromTray)); }
                        catch { /* form closing/handle gone */ }
                    },
                    state: null,
                    millisecondsTimeOutInterval: Timeout.Infinite,
                    executeOnlyOnce: false);
            }
            catch
            {
                // Activation is a convenience; a failure here must not stop the app from running.
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            _activationRegistration?.Unregister(null);
            _activationRegistration = null;
            _activationSignal?.Dispose();
            _activationSignal = null;
            base.OnHandleDestroyed(e);
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

            // Recover from a crash/abrupt exit: any item still marked "Processing" was interrupted (the
            // process that owned it is gone), so reset it to "Pending" to be reprocessed. The GUI is
            // single-instance and is the only processor of the queue, so this can't disturb live work.
            RecoverInterruptedQueueItems();

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
            int needCheck = items.Count(i => i.VerificationWarning is not null);

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
                    title = only.VerificationWarning is null ? "File redacted" : "File redacted — check needed";
                    text = $"{Path.GetFileName(only.InputPath)} → {Path.GetDirectoryName(only.OutputPath)}";
                    if (only.VerificationWarning is not null)
                    {
                        text += $"\n{only.VerificationWarning}";
                    }
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
                if (needCheck > 0)
                {
                    parts.Add($"{needCheck} may need review");
                }
                text = string.Join(", ", parts) + ".";
            }

            _trayIcon.BalloonTipIcon = (failed > 0 && succeeded == 0) || needCheck > 0 ? ToolTipIcon.Warning : ToolTipIcon.Info;
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

            int fixedWidth = columnHeader2.Width + columnHeader4.Width + columnHeader3.Width + columnHeader5.Width;
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
            // using the default policy, which may redact nothing). Warn first if any are very large.
            if (valid.Count > 0 && LargeFileWarning.ConfirmIfLarge(this, valid) &&
                PromptPolicyContext(out string policy, out string context))
            {
                foreach (string path in valid)
                {
                    // Skip files already queued for this policy/context so a re-drop doesn't double-redact.
                    if (QueueBulkActions.TryEnqueue(_redactionQueueRepository,
                            new RedactionQueueEntity { Name = path, Policy = policy, Context = context })
                        && _loggingEnabled)
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

        private void exitToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application closing");
            }

            Application.Exit();
        }

        private async void checkForUpdatesToolStripMenuItem_Click(object? sender, EventArgs e)
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

        private void aboutToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening About dialog");
            }

            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void viewLicenseToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var licenseForm = new LicenseForm(viewOnly: true);
            licenseForm.ShowDialog(this);
        }

        // Opens the redaction-review notice for reference (from the status-bar reminder link).
        private void ShowRedactionNotice()
        {
            using var noticeForm = new RedactionNoticeForm(viewOnly: true);
            noticeForm.ShowDialog(this);
        }

        // Adds a "More from Philterd" submenu to the Help menu, linking to the other Philterd offerings.
        // Built in code (rather than the VS-regenerated designer) so the link set is easy to maintain.
        // Each item opens a UTM-tagged URL (utm_medium=help-menu) so touchpoint use can be measured.
        private void BuildMoreFromPhilterdMenu()
        {
            var more = new ToolStripMenuItem("More from Philterd");
            more.DropDownItems.Add("Philter — redaction for servers, APIs & data pipelines",
                null, (_, _) => Links.Open(Links.PhilterUrl("help-menu")));
            more.DropDownItems.Add("Philter Scope — measure how well a policy performs",
                null, (_, _) => Links.Open(Links.ScopeUrl("help-menu")));
            more.DropDownItems.Add("Philter Diffuse — add differential privacy to data and statistics",
                null, (_, _) => Links.Open(Links.DiffuseUrl("help-menu")));
            more.DropDownItems.Add("Policy consulting — help building & validating policies",
                null, (_, _) => Links.Open(Links.ConsultingUrl("help-menu")));
            more.DropDownItems.Add(new ToolStripSeparator());
            more.DropDownItems.Add("See all products",
                null, (_, _) => Links.Open(Links.AllProductsUrl("help-menu")));

            // Insert just before the separator that precedes "Check for Updates..." / "About...".
            int index = helpToolStripMenuItem.DropDownItems.IndexOf(toolStripSeparator1);
            if (index < 0)
            {
                index = helpToolStripMenuItem.DropDownItems.Count;
            }
            helpToolStripMenuItem.DropDownItems.Insert(index, more);
        }


        private void policiesToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Policy Editor");
            }

            var f = new PolicyEditorForm(_policyRepository, _watchedFolderRepository);
            f.ShowDialog();
        }

        private void redactionContextsToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void settingsToolStripMenuItem_Click(object? sender, EventArgs e) => OpenSettings();

        private void toolStripButtonRedactDocuments_Click(object? sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled, _settingsRepository);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        private void policiesToolStripButton_Click(object? sender, EventArgs e)
        {
            var f = new PolicyEditorForm(_policyRepository, _watchedFolderRepository);
            f.ShowDialog();
        }

        private void contextsToolStripButton_Click(object? sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Redaction Contexts dialog");
            }

            var redactionContextsForm = new Contexts(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }

        private void listsToolStripButton_Click(object? sender, EventArgs e)
        {
            SettingsEntity settings = _settingsRepository.GetSettings();
            using var form = new GlobalListsForm(settings.GlobalAlwaysRedact, settings.GlobalAlwaysIgnore);
            if (form.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }
            settings.GlobalAlwaysRedact = form.AlwaysRedactText;
            settings.GlobalAlwaysIgnore = form.AlwaysIgnoreText;
            _settingsRepository.SaveSettings(settings);
        }

        private void settingsToolStripButton_Click(object? sender, EventArgs e) => OpenSettings();

        private void OpenSettings()
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Settings dialog");
            }

            // The max-concurrency setting is read by the watcher only when it (re)starts, so note its
            // value beforehand to detect a change and restart the watcher when it does.
            int previousConcurrency = _settingsRepository.GetSettings().WatchedFolderMaxConcurrency;

            using var settingsForm = new SettingsForm(
                _settingsRepository, _policyRepository, _contextRepository, _watchedFolderRepository,
                _watchedFolderLogRepository, EncryptedDatabase.CurrentKeyStore);
            var result = settingsForm.ShowDialog();

            // Reload settings that changed (saved only on OK).
            bool concurrencyChanged = false;
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

                concurrencyChanged = settings.WatchedFolderMaxConcurrency != previousConcurrency;
            }

            // Restart the watcher if the folder list changed (persisted immediately, independent of
            // Save/Cancel) or the max-concurrency setting changed, so monitoring reflects the new
            // configuration without an app restart. Re-scanning on restart is safe: already-redacted
            // output is skipped.
            if (settingsForm.WatchedFoldersChanged || concurrencyChanged)
            {
                StartFolderWatcher();
            }
            else
            {
                _folderWatcher.LoggingEnabled = _loggingEnabled;
            }
        }

        private void refreshToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            LoadRedactionQueue();
        }

        private void modifyRedactionToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }

            SettingsEntity modifySettings = _settingsRepository.GetSettings();
            using var form = new ModifyRedactionForm(
                id, _redactionVersionRepository, _redactionSpanRepository, _policyRepository,
                modifySettings.RedactedSuffix, DocumentMetadata.OptionsFor(modifySettings),
                modifySettings.ScrubEmailHeaders, modifySettings.RemoveCommonEmailHeaders,
                modifySettings.RemoveEmailDateHeader, modifySettings.RemoveEmailAttachments,
                modifySettings.RemoveEmailAttachments && modifySettings.RemoveEmailInlineImages,
                modifySettings.RedactOfficeHeadersFooters, modifySettings.RedactOfficeCharts,
                modifySettings.RedactCachedFormulaValues, modifySettings.RedactPivotCaches,
                modifySettings.RemoveUninspectableEmbeddedObjects,
                modifySettings.OutputToOriginalLocation, modifySettings.CustomOutputFolder,
                modifySettings.GlobalAlwaysRedact, modifySettings.GlobalAlwaysIgnore);
            form.ShowDialog(this);

            // A modify re-redaction produces a new, unverified output. Clear the document's stored
            // verification verdict so the report (and queue column / details / highlight) don't carry the
            // previous version's "verified" result onto the new output.
            if (form.RedactionChanged && _redactionQueueRepository.GetById(id) is { } entity)
            {
                ResetVerification(entity);
                _redactionQueueRepository.Update(entity);
                LoadRedactionQueue();
            }
        }

        // Stores the initial redaction as version 1 (with its spans) so it can be reviewed/modified.
        private void SaveRedactionVersion(RedactionQueueEntity entity, string outputPath, List<RedactionSpanEntity> spans, long durationMs = 0)
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
                    Highlight = entity.Highlight,
                    Worksheet = entity.Worksheet,
                    DurationMs = durationMs
                };
                int order = 0;
                foreach (RedactionSpanEntity s in spans)
                {
                    s.VersionId = version.Id; // version.Id is assigned at construction, so this is valid pre-insert
                    s.Order = order++;
                }

                // Atomic: a span-write failure rolls back the version too, so we never leave a span-less version.
                RedactionHistory.SaveVersionWithSpans(
                    _database, _redactionVersionRepository, _redactionSpanRepository, version, spans);
            }
            catch (Exception ex)
            {
                if (_loggingEnabled)
                {
                    Logger.LogWarning($"Failed to store redaction spans: {ex.Message}");
                }
            }
        }

        private async void clearRedactionHistoryToolStripMenuItem_Click(object? sender, EventArgs e)
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

            // Physically reclaim the freed pages so the deleted detected text can't linger in the
            // database file. Runs off the UI thread; the queue timer is paused so it doesn't issue a DB
            // operation that would block on LiteDB's mutex while the rebuild holds it.
            string? password = EncryptedDatabase.CurrentKeyStore?.DatabasePassword;
            if (!string.IsNullOrEmpty(password))
            {
                redactionQueueTimer.Stop();
                UseWaitCursor = true;
                try
                {
                    await Task.Run(() => RedactionHistory.Compact(_database, password));
                }
                catch (Exception ex)
                {
                    // The rows are already deleted; compaction is the bonus physical reclaim, so a failure
                    // here must not fail the clear.
                    if (_loggingEnabled)
                    {
                        Logger.LogError("Database compaction after clearing redaction history failed", ex);
                    }
                }
                finally
                {
                    UseWaitCursor = false;
                    redactionQueueTimer.Start();
                }
            }

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

        private void removeCompletedToolStripMenuItem_Click(object? sender, EventArgs e)
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

        // Requeues a single failed item: the background queue timer picks up anything set back to
        // "Pending" on its next tick and runs it through redaction again.
        private void retryToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            List<ObjectId> ids = listView1.SelectedItems.Cast<ListViewItem>()
                .Select(i => i.Tag).OfType<ObjectId>().ToList();
            if (QueueBulkActions.RetryManyFailed(_redactionQueueRepository, ids) > 0)
            {
                LoadRedactionQueue();
            }
        }

        // Requeues every failed item at once (e.g. after fixing a shared cause such as a full disk).
        private void retryAllFailedToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            List<RedactionQueueEntity> failed = _redactionQueueRepository.Find(x => x.Status == "Failed").ToList();
            if (failed.Count == 0)
            {
                return;
            }
            foreach (RedactionQueueEntity entity in failed)
            {
                RequeueForRetry(entity);
            }
            LoadRedactionQueue();
        }

        // Resets a failed item to Pending and clears its recorded error so the timer reprocesses it.
        private void RequeueForRetry(RedactionQueueEntity entity)
        {
            entity.Status = "Pending";
            entity.ErrorMessage = string.Empty;
            _redactionQueueRepository.Update(entity);
        }

        // On startup, requeue items left "Processing" by a previous crash/abrupt exit (see EnsureStarted).
        private void RecoverInterruptedQueueItems()
        {
            List<RedactionQueueEntity> interrupted =
                _redactionQueueRepository.Find(x => x.Status == "Processing").ToList();
            foreach (RedactionQueueEntity stuck in interrupted)
            {
                stuck.Status = "Pending";
                stuck.ErrorMessage = string.Empty;
                _redactionQueueRepository.Update(stuck);
            }
            if (interrupted.Count > 0 && _loggingEnabled)
            {
                Logger.LogInfo($"Reset {interrupted.Count} interrupted queue item(s) from Processing to Pending after restart.");
            }
        }

        private void ContextMenuStrip1_Opening(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            bool hasSelection = listView1.SelectedItems.Count > 0;
            bool single = listView1.SelectedItems.Count == 1; // single-document actions need exactly one row
            bool hasItems = listView1.Items.Count > 0;
            bool singleCompleted = single && IsCompleted(listView1.SelectedItems[0]);

            bool anyCompleted = false;
            bool anyFailed = false;
            foreach (ListViewItem item in listView1.Items)
            {
                if (IsCompleted(item))
                {
                    anyCompleted = true;
                }
                else if (IsFailed(item))
                {
                    anyFailed = true;
                }
            }
            bool anySelectedFailed = listView1.SelectedItems.Cast<ListViewItem>().Any(IsFailed);

            // Bulk actions operate on every selected row. ("Add Files…"/"Refresh" stay always enabled.)
            removeToolStripMenuItem.Enabled = hasSelection;
            removeAllToolStripMenuItem.Enabled = hasItems;
            removeCompletedToolStripMenuItem.Enabled = anyCompleted;
            retryToolStripMenuItem.Enabled = anySelectedFailed;    // requeue the selected failed item(s)
            retryAllFailedToolStripMenuItem.Enabled = anyFailed;   // requeue every failed item

            // Single-document actions open a viewer/file/dialog for one item, so they're enabled only
            // when exactly one row is selected — never silently acting on just the first of several.
            openRedactedFileToolStripMenuItem.Enabled = singleCompleted; // exists only once redacted
            openOriginalFileToolStripMenuItem.Enabled = single;
            openContainingFolderToolStripMenuItem.Enabled = singleCompleted; // redacted file exists once completed
            modifyRedactionToolStripMenuItem.Enabled = singleCompleted; // versions exist once redacted

            // Enable for a single completed, diffable type. Text-based types (.txt/.docx/.csv/.eml) use the
            // text diff; .pdf uses the side-by-side page comparison. Very large files are excluded
            // because the text diff loads both files and renders a row per line — see MaxDiffFileBytes.
            ObjectId? selectedId = single && listView1.SelectedItems[0].Tag is ObjectId oid ? oid : null;
            bool diffableType = singleCompleted && SelectedSourcePath() is { } src && IsDiffableType(src);
            bool diffTooLarge = diffableType && selectedId is { } did && !DiffWithinSizeLimit(did, SelectedSourcePath()!);
            viewDiffToolStripMenuItem.Enabled = diffableType && !diffTooLarge;
            viewDiffToolStripMenuItem.Text = diffTooLarge ? "View Diff... (file too large)" : "View Diff...";

            viewDetailsToolStripMenuItem.Enabled = single;
            exportExplanationToolStripMenuItem.Enabled = singleCompleted; // needs captured spans
            verifyRedactionToolStripMenuItem.Enabled = singleCompleted;   // re-scans a finished output
            generateReportToolStripMenuItem.Enabled = singleCompleted;    // summarizes a finished redaction
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

        private static bool IsDiffableType(string path) => DiffViewGate.IsDiffableType(path);

        /// <summary>True when both the source and the redacted output are within the diff size limit.</summary>
        private bool DiffWithinSizeLimit(ObjectId id, string source) =>
            FileWithinLimit(source) && FileWithinLimit(ResolveRedactedOutputPath(id, source));

        private static bool FileWithinLimit(string path)
        {
            try
            {
                var info = new FileInfo(path);
                return !info.Exists || DiffViewGate.IsWithinSizeLimit(info.Length);
            }
            catch
            {
                return true; // don't block the diff on a stat error
            }
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

        private static bool IsFailed(ListViewItem item) =>
            item.SubItems.Count > 1 && string.Equals(item.SubItems[1].Text, "Failed", StringComparison.OrdinalIgnoreCase);

        private async void viewDiffToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (_queueBusy || listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
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
            if (!FileWithinLimit(source) || !FileWithinLimit(output))
            {
                MessageBox.Show(
                    $"This file is too large to compare (the limit is {DiffViewGate.MaxFileSizeText}).\n\n" +
                    "Open the original and redacted files directly to review them.",
                    "View Diff", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Reading the files and (for .docx) extracting paragraphs can take a moment on a large file,
            // so do it off the UI thread, then build and show the viewer on the UI thread.
            _queueBusy = true;
            UseWaitCursor = true;
            try
            {
                if (source.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    (byte[] before, byte[] after) = await Task.Run(
                        () => (File.ReadAllBytes(source), File.ReadAllBytes(output)));
                    UseWaitCursor = false;
                    using var compare = new PdfCompareForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    compare.ShowDialog(this);
                }
                else if (source.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                {
                    // Word documents have no plain-text form: compare the extracted review lines (one per
                    // line) — the body/headers/footers/notes paragraphs plus shape/SmartArt/chart text —
                    // so the diff reflects every place the redactor changed, not just the body.
                    (string before, string after) = await Task.Run(() => (
                        string.Join("\n", WordDocumentRedactor.ReadReviewLines(source)),
                        string.Join("\n", WordDocumentRedactor.ReadReviewLines(output))));
                    UseWaitCursor = false;
                    using var diff = new DiffViewerForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    diff.ShowDialog(this);
                }
                else
                {
                    (string before, string after) = await Task.Run(
                        () => (File.ReadAllText(source), File.ReadAllText(output)));
                    UseWaitCursor = false;
                    using var diff = new DiffViewerForm(before, after, Path.GetFileName(source), Path.GetFileName(output));
                    diff.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not show the diff: {ex.Message}", "View Diff",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                UseWaitCursor = false;
                _queueBusy = false;
            }
        }

        private void viewDetailsToolStripMenuItem_Click(object? sender, EventArgs e)
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
            string duration = "—";
            if (latest is not null)
            {
                redactedFile = ResolveRedactedOutputPath(id, source);
                redactionCount = _redactionSpanRepository.GetForVersion(latest.Id).Count.ToString();
                timestamp = latest.CreatedAt.ToLocalTime().ToString("f");
                duration = DurationFormat.Humanize(latest.DurationMs);
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
                ("Redacted", timestamp),
                ("Time to redact", duration),
                ("Verification", entity.VerificationCheckedAt is { } vc
                    ? $"{VerificationDisplay(entity)} ({vc.ToLocalTime():f})"
                    : VerificationDisplay(entity))
            };

            // For a failed document, show why so the user isn't left guessing.
            if (!string.IsNullOrEmpty(entity.ErrorMessage))
            {
                rows.Add(("Why it failed", entity.ErrorMessage));
            }

            // Offer the explanation export from the Details dialog too, but only when there's a
            // completed redaction to explain.
            Action<IWin32Window>? export = latest is not null ? owner => ExportExplanationFor(owner, id) : null;

            // When a redaction actually ran, the dialog shows a count of removed items. That is the
            // natural moment to mention Philter Diffuse (differential privacy for counts/statistics).
            (string Text, string Url)? diffuse = latest is not null
                ? ("Sharing counts or statistics about sensitive data? Philter Diffuse adds differential privacy →",
                    Links.DiffuseUrl("details"))
                : null;

            using var details = new RedactionDetailsForm($"Details — {Path.GetFileName(source)}", rows, export, diffuse);
            details.ShowDialog(this);
        }

        private void exportExplanationToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            ExportExplanationFor(this, id);
        }

        // Writes a redaction-explanation JSON for the document's latest version. Shared by the queue's
        // context menu and the View Details dialog (which passes itself as the owner). The explanation
        // contains the original sensitive text, so the user is warned before it's written.
        private void ExportExplanationFor(IWin32Window owner, ObjectId id)
        {
            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity is null)
            {
                return;
            }

            RedactionVersionEntity? latest = _redactionVersionRepository.GetForDocument(id).LastOrDefault();
            if (latest is null)
            {
                MessageBox.Show(owner,
                    "There is no redaction detail to explain for this document yet.",
                    "Export Explanation", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            IReadOnlyList<RedactionSpanEntity> spans = _redactionSpanRepository.GetForVersion(latest.Id);

            // The explanation lists the ORIGINAL detected text (and its context), so the file is as
            // sensitive as the source. Make sure the user understands before writing it.
            DialogResult ack = MessageBox.Show(owner,
                "The explanation file lists every item that was found — including the original, " +
                "un-redacted text and the words around it.\n\n" +
                "That makes this file as sensitive as the original document. Save it somewhere secure, " +
                "and don't share it in place of the redacted copy.\n\nDo you want to continue?",
                "Export Explanation", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (ack != DialogResult.OK)
            {
                return;
            }

            string source = entity.Name;
            using var save = new SaveFileDialog
            {
                Title = "Save redaction explanation",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = $"{Path.GetFileNameWithoutExtension(source)}_redaction-explanation.json",
                InitialDirectory = RedactionService.InitialSaveDirectory(
                    _settingsRepository.GetSettings(), ResolveRedactedOutputPath(id, source), source)
            };
            if (save.ShowDialog(owner) != DialogResult.OK)
            {
                return;
            }

            try
            {
                Version? v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string toolVersion = v is null ? string.Empty : $"{v.Major}.{v.Minor}.{v.Build}";
                string json = RedactionExplanation.ToJson(latest, spans, toolVersion, DateTimeOffset.UtcNow);
                File.WriteAllText(save.FileName, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, UserError.Describe(ex, save.FileName, writing: true),
                    "Export Explanation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(owner,
                    $"Saved the explanation for {spans.Count} item{(spans.Count == 1 ? "" : "s")} to:\n{save.FileName}\n\nOpen the containing folder?",
                    "Export Explanation", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
            {
                try { System.Diagnostics.Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{save.FileName}\"") { UseShellExecute = true }); }
                catch { /* best effort */ }
            }
        }

        private async void verifyWithSamePolicyToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            await RunAndShowVerification(this, id, quietWhenClean: false, broadPolicy: false);
        }

        private async void verifyWithBroadPolicyToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            await RunAndShowVerification(this, id, quietWhenClean: false, broadPolicy: true);
        }

        // Clears the stored verification verdict (does not persist; the caller saves). Used when the
        // output changes (e.g. a Modify-Redaction re-redact) so a stale verdict isn't shown for it.
        internal static void ResetVerification(RedactionQueueEntity entity)
        {
            entity.VerificationStatus = "NotRun";
            entity.VerificationFindingCount = 0;
            entity.VerificationCheckedAt = null;
        }

        // Copies a verification outcome onto the queue entity (does not persist; the caller saves).
        private static void ApplyVerificationFields(RedactionQueueEntity entity, VerificationOutcome? outcome)
        {
            if (outcome is null)
            {
                return;
            }
            entity.VerificationStatus = outcome.Status.ToString();
            entity.VerificationFindingCount = outcome.Count;
            entity.VerificationCheckedAt = DateTime.UtcNow;
        }

        // Verification status shown when name detection was requested but the model was missing, so names
        // were silently skipped — kept distinct from a real "Clean" so the row doesn't read as fully checked.
        internal const string NamesNotCheckedStatus = "NamesNotChecked";

        // Verification status shown when the source RTF had header/footer/footnote content that RTF
        // redaction doesn't carry into the output — kept distinct from "Clean" so the row invites review.
        internal const string ContentDroppedStatus = "ContentDropped";

        // A short, human display of an item's stored verification status (queue column + View Details).
        private static string VerificationDisplay(RedactionQueueEntity e) => e.VerificationStatus switch
        {
            "Clean" => "Clean",
            "ResidualsFound" => $"{e.VerificationFindingCount} may remain",
            "Error" => "Check failed",
            NamesNotCheckedStatus => "Names not checked",
            ContentDroppedStatus => "Some parts not carried over",
            _ => "Not checked"
        };

        // The verification status to store/show, accounting for name detection having been silently skipped
        // and for RTF non-body content that wasn't carried over. "Clean"/"NotRun" downgrade to a review
        // status; more severe states (residuals/error) are left as-is (the caveats still ride along in the
        // notification and report). Names-not-checked takes precedence over content-dropped when both apply.
        internal static string EffectiveVerificationStatus(string verificationStatus, bool nameDetectionUnavailable, bool contentDropped = false)
        {
            if (verificationStatus is not ("Clean" or "NotRun" or NamesNotCheckedStatus))
            {
                return verificationStatus;
            }
            if (nameDetectionUnavailable)
            {
                return NamesNotCheckedStatus;
            }
            if (contentDropped)
            {
                return ContentDroppedStatus;
            }
            return verificationStatus;
        }

        // A verification result that warrants review (drives the amber/red cue on the Status cell). The
        // stored Status stays "Completed"; only its colour changes, so sorting/filtering are unaffected.
        internal static bool IsVerificationWarning(string verificationStatus) =>
            verificationStatus is "ResidualsFound" or NamesNotCheckedStatus or ContentDroppedStatus;

        private static string? ResidualWarning(VerificationOutcome? outcome) =>
            outcome is { Status: VerificationStatus.ResidualsFound }
                ? $"Verification: {outcome.Count} possible item{(outcome.Count == 1 ? "" : "s")} may remain"
                : null;

        // The combined "check needed" notification text: the residual-PII warning, the name-model warning,
        // and/or the RTF non-body-content warning (null when none applies).
        internal static string? CombinedRedactionWarning(VerificationOutcome? verification, bool nameDetectionUnavailable, string? contentDroppedWarning = null)
        {
            string?[] parts =
            {
                ResidualWarning(verification),
                nameDetectionUnavailable ? PhEyeModel.UnavailableWarning : null,
                contentDroppedWarning
            };
            string combined = string.Join("\n", parts.Where(s => !string.IsNullOrEmpty(s)));
            return combined.Length > 0 ? combined : null;
        }

        // Builds the effective policy for a completed item and verifies its redacted output, persisting
        // the result and (unless clean and asked to stay quiet) showing it. Used on demand and after a
        // preview save. With broadPolicy, scans with every detector on (catches types the redaction
        // policy didn't cover); otherwise re-uses the redaction's own policy.
        private async Task<VerificationOutcome?> RunAndShowVerification(IWin32Window owner, ObjectId id, bool quietWhenClean, bool broadPolicy)
        {
            if (_queueBusy)
            {
                return null; // a background queue operation (verify/diff) is already running
            }

            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            RedactionVersionEntity? latest = _redactionVersionRepository.GetForDocument(id).LastOrDefault();
            if (entity is null || latest is null)
            {
                return null;
            }

            string output = ResolveRedactedOutputPath(id, entity.Name);
            Policy policy;
            if (broadPolicy)
            {
                policy = VerificationPolicy.Broad();
            }
            else
            {
                PolicyEntity? policyEntity = _policyRepository.FindByName(latest.Policy);
                policy = PolicySerializer.DeserializeFromJson(
                    string.IsNullOrWhiteSpace(policyEntity?.Json) ? "{}" : policyEntity!.Json);
            }
            GlobalLists.Apply(policy, _settingsRepository.GetSettings());

            // Re-scanning a large output can take a moment; run it off the UI thread so the window stays
            // responsive. string/policy locals are captured so the worker touches no UI state.
            string context = latest.Context;
            // Don't re-flag this version's own inserted replacements as residual PII.
            IReadOnlySet<string> knownReplacements =
                RedactionVerifier.ReplacementsOf(_redactionSpanRepository.GetForVersion(latest.Id));
            VerificationOutcome outcome;
            _queueBusy = true;
            UseWaitCursor = true;
            try
            {
                outcome = await Task.Run(() => RedactionVerifier.Verify(output, policy, context, SharedFilterService.Instance, knownReplacements, sourcePath: entity.Name));
            }
            finally
            {
                UseWaitCursor = false;
                _queueBusy = false;
            }

            ApplyVerificationFields(entity, outcome);
            // A clean re-scan of an RTF that dropped non-body content must not read as fully "Clean" — keep
            // the review cue, so the report and queue row stay honest after a re-verify.
            entity.VerificationStatus = EffectiveVerificationStatus(
                entity.VerificationStatus, nameDetectionUnavailable: false, contentDropped: DroppedContentWarning.For(entity.Name) is not null);
            _redactionQueueRepository.Update(entity);
            LoadRedactionQueue();

            if (!(quietWhenClean && outcome.Status == VerificationStatus.Clean))
            {
                using var form = new VerificationResultForm(Path.GetFileName(output), outcome);
                form.ShowDialog(owner);
            }
            return outcome;
        }

        private void generateReportToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0 || listView1.SelectedItems[0].Tag is not ObjectId id)
            {
                return;
            }
            GenerateReportFor(this, id);
        }

        // Writes a shareable redaction report (PDF or HTML) for the document's latest version. Unlike
        // the explanation JSON, the report contains NO original text, so it's safe to file alongside the
        // redacted copy. Shared by the queue context menu and the post-preview offer.
        private void GenerateReportFor(IWin32Window owner, ObjectId id)
        {
            RedactionQueueEntity? entity = _redactionQueueRepository.GetById(id);
            if (entity is null)
            {
                return;
            }

            RedactionVersionEntity? latest = _redactionVersionRepository.GetForDocument(id).LastOrDefault();
            if (latest is null)
            {
                MessageBox.Show(owner, "There is no redaction to report on for this document yet.",
                    "Generate Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            IReadOnlyList<RedactionSpanEntity> spans = _redactionSpanRepository.GetForVersion(latest.Id);

            // Offer the optional per-redaction detail table (still contains no original text).
            DialogResult detail = MessageBox.Show(owner,
                "Include a detailed per-redaction table?\n\n" +
                "It lists each redaction's type, location, and replacement. It does not include the " +
                "original text, so the report stays safe to share.",
                "Generate Report", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (detail == DialogResult.Cancel)
            {
                return;
            }

            string source = entity.Name;
            string outputPath = ResolveRedactedOutputPath(id, source);
            using var save = new SaveFileDialog
            {
                Title = "Save redaction report",
                Filter = "PDF report (*.pdf)|*.pdf",
                DefaultExt = "pdf",
                AddExtension = true,
                FileName = $"{Path.GetFileNameWithoutExtension(source)}_redaction-report.pdf",
                InitialDirectory = RedactionService.InitialSaveDirectory(_settingsRepository.GetSettings(), outputPath, source)
            };
            if (save.ShowDialog(owner) != DialogResult.OK)
            {
                return;
            }

            try
            {
                Version? v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                string toolVersion = v is null ? string.Empty : $"{v.Major}.{v.Minor}.{v.Build}";

                var options = new RedactionReportOptions
                {
                    IncludeDetailTable = detail == DialogResult.Yes,
                    IncludeMachineInfo = _loggingEnabled
                };
                RedactionReportModel model = RedactionReport.Build(
                    latest, spans, toolVersion, DateTimeOffset.UtcNow,
                    FileHash.Sha256OrUnavailable(source),
                    FileHash.Sha256OrUnavailable(outputPath),
                    options,
                    entity.VerificationStatus, entity.VerificationFindingCount, entity.VerificationCheckedAt);

                RedactionReportPdf.Write(model, save.FileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(owner, UserError.Describe(ex, save.FileName, writing: true),
                    "Generate Report", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Open the saved report directly in the default PDF viewer.
            try { System.Diagnostics.Process.Start(new ProcessStartInfo(save.FileName) { UseShellExecute = true }); }
            catch (Exception ex)
            {
                MessageBox.Show(owner, $"The report was saved to:\n{save.FileName}\n\nBut it could not be opened: {ex.Message}",
                    "Generate Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void addFilesToRedactToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled, _settingsRepository);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
        }

        // Preview-first flow for a single .txt or .pdf file: pick the file, preview the
        // redaction, and only write the output on Save. The result is recorded as a Completed queue
        // item (with version history) so View Diff / Modify Redaction work on it afterward.
        private void findAndRedactToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // Pre-fill the source from the selected queue item, if any; otherwise the dialog's Browse.
            using var form = new FindAndRedactForm(_settingsRepository.GetSettings(), SelectedSourcePath());
            form.ShowDialog(this);
        }

        private void redactSpreadsheetToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            // Spreadsheets/CSV with optional whole-column redaction; pre-fill a selected .xlsx/.csv.
            string? selected = SelectedSourcePath();
            string? initial = selected is not null &&
                (selected.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                 selected.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                ? selected
                : null;

            using var form = new SpreadsheetRedactionForm(
                _policyRepository, _contextRepository, _redactionQueueRepository, _settingsRepository.GetSettings(), initial);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadRedactionQueue(); // show the newly queued spreadsheet
            }
        }

        // One-shot "Redact Folder": enqueue every supported file in a chosen folder (optionally
        // recursing) with one policy/context. The queue then redacts them like any other documents, so
        // per-file success/failure shows up in the queue. No persistent watcher is created.
        private void redactFolderToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var form = new FolderRedactForm(
                _policyRepository, _contextRepository, _redactionQueueRepository, _settingsRepository);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                LoadRedactionQueue(); // show the newly queued files
                if (_loggingEnabled)
                {
                    Logger.LogInfo($"Redact Folder queued {form.EnqueuedCount} file(s).");
                }
            }
        }

        private void redactPreviewToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            using var picker = new OpenFileDialog
            {
                Title = "Select a file to redact",
                Filter = "Supported (*.txt;*.docx;*.pdf;*.rtf;*.eml;*.msg)|*.txt;*.docx;*.pdf;*.rtf;*.eml;*.msg|Text (*.txt)|*.txt|Rich Text (*.rtf)|*.rtf|Word (*.docx)|*.docx|PDF (*.pdf)|*.pdf|Email (*.eml;*.msg)|*.eml;*.msg"
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
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList(), preview.RedactionDurationMs);
                }
            }
            else if (source.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            {
                using var preview = new WordRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList(), preview.RedactionDurationMs);
                }
            }
            else if (source.EndsWith(".eml", StringComparison.OrdinalIgnoreCase) ||
                     source.EndsWith(".msg", StringComparison.OrdinalIgnoreCase))
            {
                using var preview = new EmailRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList(), preview.RedactionDurationMs);
                }
            }
            else
            {
                using var preview = new TextRedactionPreviewForm(source, _policyRepository, _contextRepository, settings);
                if (preview.ShowDialog(this) == DialogResult.OK)
                {
                    RecordCompletedRedaction(source, preview.OutputPath, preview.SelectedPolicy, preview.SelectedContext, preview.CapturedSpans.ToList(), preview.RedactionDurationMs);
                }
            }
        }

        private void RecordCompletedRedaction(string source, string outputPath, string policy, string context, List<RedactionSpanEntity> spans, long durationMs = 0)
        {
            var entity = new RedactionQueueEntity
            {
                Name = source,
                Policy = policy,
                Context = context,
                Status = "Completed"
            };
            _redactionQueueRepository.Insert(entity);
            SaveRedactionVersion(entity, outputPath, spans, durationMs);
            RememberLastUsed(policy, context, Path.GetDirectoryName(outputPath));
            LoadRedactionQueue();

            // Self-check the output for residual PII when the setting is on. Show the result only when
            // something remains (a clean pass shouldn't add a click to the careful preview workflow).
            SettingsEntity verifySettings = _settingsRepository.GetSettings();
            if (verifySettings.VerifyAfterRedaction)
            {
                _ = RunAndShowVerification(this, entity.Id, quietWhenClean: true, broadPolicy: verifySettings.VerificationUseBroadPolicy);
            }

            // A report is available on demand by right-clicking the completed item; don't prompt here.
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

        private async void RedactionQueueTimer_Tick(object? sender, EventArgs e)
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
                FilterService filterService = SharedFilterService.Instance; // shared so the name model loads once

                foreach (var entity in pendingEntities)
                {
                    // Re-check: a still-Pending item later in the snapshot can be removed while we await
                    // an earlier one. Skip it so we don't redact a row that's gone (orphan output/history).
                    if (!QueueBulkActions.IsStillPending(_redactionQueueRepository, entity.Id))
                    {
                        continue;
                    }

                    UpdateEntityStatus(entity, "Processing");

                    QueueRedactionResult result = await QueueProcessor.ProcessAsync(
                        entity, _policyRepository, settings, filterService);

                    if (result.Success)
                    {
                        SaveRedactionVersion(entity, result.OutputPath!, result.Spans.ToList(), result.DurationMs);
                        entity.ErrorMessage = string.Empty; // clear any prior failure reason
                        ApplyVerificationFields(entity, result.Verification);
                        // Names requested but model missing, or RTF non-body content not carried over:
                        // don't let the row read "Clean" — it needs review.
                        entity.VerificationStatus = EffectiveVerificationStatus(entity.VerificationStatus, result.NameDetectionUnavailable, result.ContentDroppedWarning is not null);
                        UpdateEntityStatus(entity, "Completed");
                        QueueNotification(new WatchedFileProcessedEventArgs
                        {
                            InputPath = entity.Name,
                            OutputPath = result.OutputPath,
                            Success = true,
                            VerificationWarning = CombinedRedactionWarning(result.Verification, result.NameDetectionUnavailable, result.ContentDroppedWarning)
                        });

                        if (_loggingEnabled)
                        {
                            Logger.LogInfo($"Redaction completed: {entity.Name} -> {result.OutputPath}");
                            if (result.NameDetectionUnavailable)
                            {
                                Logger.LogWarning($"Name detection was unavailable (model not installed); names may remain in {result.OutputPath}.");
                            }
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
                ListViewItem.ListViewSubItem statusCell = item.SubItems.Add(entity.Status);
                item.SubItems.Add(entity.Policy);
                item.SubItems.Add(entity.Context);
                ListViewItem.ListViewSubItem verification = item.SubItems.Add(VerificationDisplay(entity));
                item.Tag = entity.Id;
                item.ImageIndex = 0;
                if (entity.VerificationStatus == "ResidualsFound")
                {
                    // Make a "needs review" result stand out without affecting the rest of the row, and
                    // cue the Status cell (its text stays "Completed", only the colour changes).
                    item.UseItemStyleForSubItems = false;
                    verification.ForeColor = Color.FromArgb(0x8A, 0x1C, 0x1C);
                    statusCell.ForeColor = Color.FromArgb(0x8A, 0x1C, 0x1C);
                }
                else if (entity.VerificationStatus is NamesNotCheckedStatus or ContentDroppedStatus)
                {
                    item.UseItemStyleForSubItems = false;
                    verification.ForeColor = Color.DarkOrange;
                    statusCell.ForeColor = Color.DarkOrange;
                }
                if (!string.IsNullOrEmpty(entity.ErrorMessage))
                {
                    item.ToolTipText = entity.ErrorMessage; // hover a failed row to see why
                }
                else if (IsVerificationWarning(entity.VerificationStatus))
                {
                    // Non-colour cue (hover) so the warning isn't missed, e.g. by color-blind users.
                    item.ToolTipText = $"Completed with a warning — {VerificationDisplay(entity)}. See the Verification column.";
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

        private void openRedactedFileToolStripMenuItem_Click(object? sender, EventArgs e)
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

        private void openOriginalFileToolStripMenuItem_Click(object? sender, EventArgs e)
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
        private void openContainingFolderToolStripMenuItem_Click(object? sender, EventArgs e)
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

        private void removeAllToolStripMenuItem_Click(object? sender, EventArgs e)
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

        private void removeToolStripMenuItem_Click(object? sender, EventArgs e)
        {
            List<ObjectId> ids = listView1.SelectedItems.Cast<ListViewItem>()
                .Select(i => i.Tag).OfType<ObjectId>().ToList();
            if (ids.Count == 0)
            {
                return;
            }

            // Confirm first — removal is easy to trigger (context menu or Delete key) and discards the
            // item's redaction history.
            string? singleName = ids.Count == 1 ? Path.GetFileName(_redactionQueueRepository.GetById(ids[0])?.Name ?? string.Empty) : null;
            if (MessageBox.Show(this, QueueBulkActions.RemoveConfirmationMessage(ids.Count, singleName),
                    "Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            // Remove every selected item (skipping any still processing), then refresh from the database.
            List<string> skipped = QueueBulkActions.RemoveMany(_redactionQueueRepository, ids, DeleteRedactionHistory);
            LoadRedactionQueue();

            if (skipped.Count > 0)
            {
                string names = string.Join(Environment.NewLine, skipped.Select(n => "• " + n));
                MessageBox.Show(
                    $"{skipped.Count} item(s) could not be removed because they are currently being processed:" +
                    Environment.NewLine + Environment.NewLine + names,
                    "Cannot Remove", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
