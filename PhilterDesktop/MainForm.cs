using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Philter;
using PhilterData;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Text.Json;
using PhileasPolicy = Phileas.Policy.Policy;

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
        private bool _loggingEnabled;

        private const string HelpUrl = "https://philterd.github.io/philterdesktop";
        private static readonly string[] SupportedExtensions = { ".txt", ".docx", ".pdf" };

        private Label _emptyStateLabel = null!;
        private ToolStripStatusLabel _queueSummaryLabel = null!;
        private Image? _documentImage;
        private System.Windows.Forms.Timer _statusAnimTimer = null!;
        private int _statusAnimPhase;

        private const int RowHeight = 32;

        public MainForm()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            InitializeQueueUi();

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 2. Combine with your specific App Folder
            string folder = Path.Combine(root, "PhilterDesktop");
            string dbPath = Path.Combine(folder, "data.db");

            // 3. The Magic Step: Ensure the directory exists
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Create a single shared database instance
            _database = new LiteDatabase(dbPath);

            // Pass the shared database to all repositories
            _policyRepository = new PolicyRepository(_database);
            _contextRepository = new ContextRepository(_database);
            _contextEntryRepository = new ContextEntryRepository(_database);
            _settingsRepository = new SettingsRepository(_database);
            _redactionQueueRepository = new RedactionQueueRepository(_database);

            // Load settings and check if logging is enabled
            var settings = _settingsRepository.GetSettings();
            _loggingEnabled = settings.LoggingEnabled;

            if (_loggingEnabled)
            {
                Logger.LogInfo("PhilterDesktop application started");
            }

            // Insert default policy.
            if (_policyRepository.FindByName("default") == null)
            {
                PolicyEntity policyEntity = new PolicyEntity
                {
                    Name = "default",
                    Json = "{}"

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
            listView1.DrawColumnHeader += ListView1_DrawColumnHeader;
            listView1.DrawItem += ListView1_DrawItem;
            listView1.DrawSubItem += ListView1_DrawSubItem;
            listView1.Resize += (_, _) => { AdjustColumns(); PositionEmptyState(); };

            // --- Drag-and-drop files onto the queue ---
            listView1.AllowDrop = true;
            listView1.DragEnter += QueueList_DragEnter;
            listView1.DragDrop += QueueList_DragDrop;

            // --- Empty state overlay ---
            _emptyStateLabel = new Label
            {
                Text = "No documents queued\r\n\r\nClick \"Redact\" or drag .txt, .docx, or .pdf files here",
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
                ForeColor = ModernTheme.SubtleText
            };
            statusStrip1.Items.Add(_queueSummaryLabel);

            // --- Wire the previously-dead Help buttons ---
            HelpToolStripButton.Click += OpenHelp;
            helpToolStripMenuItem1.Click += OpenHelp;

            // --- Modern monochrome toolbar icons + animated processing badges ---
            ApplyToolbarIcons();

            _statusAnimTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _statusAnimTimer.Tick += StatusAnimTimer_Tick;
        }

        private void ApplyToolbarIcons()
        {
            const int size = 24;
            // Segoe Fluent / MDL2 glyphs. Redact is accent-colored as the primary action.
            toolStripButtonRedactDocuments.Image = ModernTheme.CreateGlyphImage("\uE72E", size, ModernTheme.Accent); // Lock
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
            redactionQueueTimer.Start();
            AdjustColumns();
            LoadRedactionQueue();
        }

        // --- Owner-draw handlers ---------------------------------------------

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

            TextRenderer.DrawText(
                e.Graphics, e.Header?.Text, ModernTheme.UiFont,
                Rectangle.Inflate(e.Bounds, -8, 0), ModernTheme.SubtleText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        // Row background/selection is painted per-cell in DrawSubItem; nothing to do here.
        private void ListView1_DrawItem(object? sender, DrawListViewItemEventArgs e) { }

        private void ListView1_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
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

            int added = 0;
            var skipped = new List<string>();

            foreach (string path in paths)
            {
                if (!File.Exists(path))
                {
                    continue;
                }

                string ext = Path.GetExtension(path);
                if (!SupportedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                {
                    skipped.Add(Path.GetFileName(path));
                    continue;
                }

                _redactionQueueRepository.Insert(new RedactionQueueEntity
                {
                    Name = path,
                    Policy = "default",
                    Context = "default"
                });
                added++;

                if (_loggingEnabled)
                {
                    Logger.LogInfo($"File queued via drag-drop onto main window: {path}");
                }
            }

            if (added > 0)
            {
                LoadRedactionQueue();
            }

            if (skipped.Count > 0)
            {
                MessageBox.Show(
                    $"These files were skipped (supported types: .txt, .docx, .pdf):\n\n{string.Join("\n", skipped)}",
                    "Unsupported File Type",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
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

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening About dialog");
            }

            var aboutForm = new AboutForm();
            aboutForm.ShowDialog();
        }

        private void licenseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening License dialog");
            }

            var licenseForm = new LicenseForm();
            licenseForm.ShowDialog();
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

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Settings dialog");
            }

            var settingsForm = new SettingsForm(_settingsRepository);
            var result = settingsForm.ShowDialog();

            // Reload logging setting in case it changed
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
        }

        private void toolStripButtonRedactDocuments_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled);
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

        private void settingsToolStripButton_Click(object sender, EventArgs e)
        {
            if (_loggingEnabled)
            {
                Logger.LogInfo("Opening Settings dialog");
            }

            var settingsForm = new SettingsForm(_settingsRepository);
            var result = settingsForm.ShowDialog();

            // Reload logging setting in case it changed
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
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            LoadRedactionQueue();
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

            _redactionQueueRepository.DeleteWhere(x => x.Status == "Completed");
            LoadRedactionQueue();
        }

        private void addFilesToRedactToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var redactDocumentsForm = new RedactDocuments(_policyRepository, _contextRepository, _redactionQueueRepository, _loggingEnabled);
            redactDocumentsForm.ShowDialog();
            LoadRedactionQueue();
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

                    try
                    {
                        var policyEntity = _policyRepository.FindByName(entity.Policy);
                        if (policyEntity == null)
                        {
                            UpdateEntityStatus(entity, "Failed");

                            if (_loggingEnabled)
                            {
                                Logger.LogError($"Policy '{entity.Policy}' not found for file: {entity.Name}");
                            }

                            continue;
                        }

                        if (!File.Exists(entity.Name))
                        {
                            UpdateEntityStatus(entity, "Failed");

                            if (_loggingEnabled)
                            {
                                Logger.LogError($"File not found: {entity.Name}");
                            }

                            continue;
                        }

                        var policy = PolicySerializer.DeserializeFromJson(policyEntity.Json);
                        string outputPath = GetOutputPath(entity.Name, settings);
                        string extension = Path.GetExtension(entity.Name).ToLowerInvariant();

                        if (extension == ".docx")
                        {
                            // Word redaction is synchronous; run it off the UI thread.
                            await Task.Run(() => WordDocumentRedactor.Redact(
                                entity.Name,
                                outputPath,
                                text => filterService.Filter(policy, entity.Context, 0, text).FilteredText));
                        }
                        else
                        {
                            string input = await File.ReadAllTextAsync(entity.Name);
                            var result = filterService.Filter(policy, entity.Context, 0, input);
                            await File.WriteAllTextAsync(outputPath, result.FilteredText);
                        }

                        UpdateEntityStatus(entity, "Completed");

                        if (_loggingEnabled)
                        {
                            Logger.LogInfo($"Redaction completed: {entity.Name} -> {outputPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        UpdateEntityStatus(entity, "Failed");

                        if (_loggingEnabled)
                        {
                            Logger.LogError($"Redaction failed for {entity.Name}", ex);
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

        private static string GetOutputPath(string inputPath, SettingsEntity settings)
        {
            string directory = settings.OutputToOriginalLocation
                ? Path.GetDirectoryName(inputPath) ?? string.Empty
                : settings.CustomOutputFolder;

            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(inputPath);
            string extension = Path.GetExtension(inputPath);

            return Path.Combine(directory, $"{fileNameWithoutExt}_redacted{extension}");
        }

        private void LoadRedactionQueue()
        {
            listView1.BeginUpdate();
            listView1.Items.Clear();

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int total = 0;

            foreach (RedactionQueueEntity entity in _redactionQueueRepository.GetAll())
            {
                var item = new ListViewItem(entity.Name);
                item.SubItems.Add(entity.Status);
                item.SubItems.Add(entity.Policy);
                item.SubItems.Add(entity.Context);
                item.Tag = entity.Id;
                item.ImageIndex = 0;
                listView1.Items.Add(item);

                counts[entity.Status] = counts.GetValueOrDefault(entity.Status) + 1;
                total++;
            }

            listView1.EndUpdate();

            UpdateEmptyState(total);
            UpdateQueueSummary(total, counts);
            EnsureStatusAnimation();
        }

        private void UpdateQueueSummary(int total, IReadOnlyDictionary<string, int> counts)
        {
            if (total == 0)
            {
                _queueSummaryLabel.Text = "No documents in queue";
                return;
            }

            var parts = new List<string> { $"{total} file{(total == 1 ? "" : "s")}" };
            foreach (string status in new[] { "Pending", "Processing", "Completed", "Failed" })
            {
                if (counts.TryGetValue(status, out int n) && n > 0)
                {
                    parts.Add($"{n} {status.ToLowerInvariant()}");
                }
            }

            _queueSummaryLabel.Text = string.Join("  ·  ", parts);
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

            string outputPath = GetOutputPath(entity.Name, _settingsRepository.GetSettings());

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
            listView1.Items.Remove(selected);
        }
    }
}
