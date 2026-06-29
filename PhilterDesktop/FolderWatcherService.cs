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

using System.Collections.Concurrent;
using Phileas.Policy;
using Phileas.Services;
using PhilterData;

namespace PhilterDesktop
{
    /// <summary>
    /// Continuously monitors a set of watched folders for new .txt/.docx/.pdf/.rtf/.xlsx/.csv/.eml/.msg
    /// files and redacts each one (with its folder's policy/context) to the output directory. Uses a
    /// <see cref="FileSystemWatcher"/> per folder plus an initial scan so files dropped while the
    /// app was closed are still picked up. Redactions are serialized to avoid overloading the
    /// machine, and redacted output (files ending with the configured suffix) is ignored to prevent loops.
    /// </summary>
    internal sealed class FolderWatcherService : IDisposable
    {
        /// <summary>
        /// Raised after a watched file is processed, but only for folders with
        /// <see cref="WatchedFolderEntity.Notify"/> enabled. May fire on a background thread.
        /// </summary>
        public event EventHandler<WatchedFileProcessedEventArgs>? FileProcessed;

        private readonly PolicyRepository _policyRepository;
        private readonly WatchedFolderLogRepository? _logRepository;
        private readonly SettingsRepository? _settingsRepository;
        private readonly FilterService _filterService = new();
        private readonly List<FileSystemWatcher> _watchers = new();
        private readonly object _gate = new();
        // Limits how many files redact at once (default 1). Re-created from settings on Restart; large
        // files always run solo. The shared FilterService is stateless, so concurrent use is safe.
        private RedactionConcurrencyGate _redactionGate = new(1);
        private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.OrdinalIgnoreCase);

        private bool _loggingEnabled;
        private bool _disposed;

        public FolderWatcherService(
            PolicyRepository policyRepository,
            bool loggingEnabled,
            WatchedFolderLogRepository? logRepository = null,
            SettingsRepository? settingsRepository = null)
        {
            _policyRepository = policyRepository ?? throw new ArgumentNullException(nameof(policyRepository));
            _loggingEnabled = loggingEnabled;
            _logRepository = logRepository;
            _settingsRepository = settingsRepository;
        }

        /// <summary>The current redacted-output suffix from settings (or the default).</summary>
        private string CurrentSuffix() =>
            RedactionService.NormalizeSuffix(_settingsRepository?.GetSettings().RedactedSuffix);

        public bool LoggingEnabled
        {
            get => _loggingEnabled;
            set => _loggingEnabled = value;
        }

        /// <summary>
        /// Stops any existing watchers and starts watching the given folders. Safe to call
        /// repeatedly (e.g., after the user edits the watched-folder list in Settings).
        /// </summary>
        public void Restart(IEnumerable<WatchedFolderEntity> folders)
        {
            ArgumentNullException.ThrowIfNull(folders);

            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }

                StopWatchersNoLock();

                // Size the concurrency gate from settings (large files still run solo). In-flight
                // redactions keep using the gate they captured, so swapping the reference is safe.
                int maxConcurrency = _settingsRepository?.GetSettings().WatchedFolderMaxConcurrency ?? 1;
                _redactionGate = new RedactionConcurrencyGate(maxConcurrency);

                foreach (WatchedFolderEntity folder in folders)
                {
                    if (string.IsNullOrWhiteSpace(folder.FolderPath))
                    {
                        continue;
                    }
                    if (!Directory.Exists(folder.FolderPath))
                    {
                        Log($"Watched folder does not exist, skipping: {folder.FolderPath}", warning: true);
                        continue;
                    }

                    WatchedFolderEntity captured = folder;
                    try
                    {
                        var watcher = new FileSystemWatcher(folder.FolderPath)
                        {
                            IncludeSubdirectories = folder.IncludeSubfolders,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                            EnableRaisingEvents = true
                        };
                        if (folder.IncludeSubfolders)
                        {
                            // Recursive watching produces more events; a larger buffer reduces
                            // overflow (and the rescans that follow) on busy trees. 64 KB is the max.
                            watcher.InternalBufferSize = 64 * 1024;
                        }
                        watcher.Created += (_, e) => OnFileEvent(e.FullPath, captured);
                        watcher.Renamed += (_, e) => OnFileEvent(e.FullPath, captured);
                        watcher.Error += (_, _) => OnWatcherError(captured);
                        _watchers.Add(watcher);

                        Log($"Watching folder: {folder.FolderPath}");
                    }
                    catch (Exception ex)
                    {
                        Log($"Failed to watch folder '{folder.FolderPath}': {ex.Message}", warning: true);
                        continue;
                    }

                    // Pick up files that already exist (e.g., dropped while the app was closed).
                    ScanExisting(captured);
                }
            }
        }

        /// <summary>Stops all watchers without disposing the service (it can be restarted).</summary>
        public void Stop()
        {
            lock (_gate)
            {
                StopWatchersNoLock();
            }
        }

        private void StopWatchersNoLock()
        {
            foreach (FileSystemWatcher watcher in _watchers)
            {
                try
                {
                    watcher.EnableRaisingEvents = false;
                    watcher.Dispose();
                }
                catch
                {
                    // best effort
                }
            }
            _watchers.Clear();
        }

        private void ScanExisting(WatchedFolderEntity folder)
        {
            _ = Task.Run(() =>
            {
                try
                {
                    foreach (string file in EnumerateCandidateFiles(folder))
                    {
                        OnFileEvent(file, folder);
                    }
                }
                catch (Exception ex)
                {
                    Log($"Failed to scan watched folder '{folder.FolderPath}': {ex.Message}", warning: true);
                }
            });
        }

        /// <summary>
        /// Enumerates existing files to (re)scan. For a recursive folder this walks subdirectories,
        /// skipping hidden/system directories, and tolerates per-directory access errors.
        /// </summary>
        private static IEnumerable<string> EnumerateCandidateFiles(WatchedFolderEntity folder)
        {
            if (!folder.IncludeSubfolders)
            {
                return Directory.EnumerateFiles(folder.FolderPath);
            }
            return EnumerateRecursive(folder.FolderPath);
        }

        private static IEnumerable<string> EnumerateRecursive(string root)
        {
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();

                string[] files;
                try { files = Directory.GetFiles(dir); }
                catch { continue; } // access denied / removed mid-walk
                foreach (string file in files)
                {
                    yield return file;
                }

                string[] subDirs;
                try { subDirs = Directory.GetDirectories(dir); }
                catch { continue; }
                foreach (string sub in subDirs)
                {
                    if (IsHiddenOrSystem(sub))
                    {
                        continue;
                    }
                    stack.Push(sub);
                }
            }
        }

        private static bool IsHiddenOrSystem(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                return attributes.HasFlag(FileAttributes.Hidden) || attributes.HasFlag(FileAttributes.System);
            }
            catch
            {
                return false;
            }
        }

        private void OnWatcherError(WatchedFolderEntity folder)
        {
            // The watcher's internal buffer overflowed and events were lost; re-scan to recover.
            Log($"File watcher error for '{folder.FolderPath}'; rescanning.", warning: true);
            ScanExisting(folder);
        }

        private void OnFileEvent(string fullPath, WatchedFolderEntity folder)
        {
            if (!ShouldProcess(fullPath, folder))
            {
                return;
            }
            // Dedup concurrent/duplicate events for the same path.
            if (!_inFlight.TryAdd(fullPath, 0))
            {
                return;
            }
            _ = HandleFileAsync(fullPath, folder);
        }

        private bool ShouldProcess(string fullPath, WatchedFolderEntity folder)
        {
            string fileName = Path.GetFileName(fullPath);
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("~$", StringComparison.Ordinal))
            {
                return false; // Office lock/temp file
            }
            if (!RedactionService.IsSupported(fullPath) || !IsAcceptedType(fullPath, folder))
            {
                return false;
            }
            if (IsHiddenOrSystem(fullPath))
            {
                return false;
            }
            // Never redact our own output (prevents redacting an already-redacted file in a loop).
            return !Path.GetFileNameWithoutExtension(fileName).EndsWith(CurrentSuffix(), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True if the file's extension is among the folder's selected types (empty = all).</summary>
        private static bool IsAcceptedType(string fullPath, WatchedFolderEntity folder)
        {
            IEnumerable<string> accepted = folder.FileTypes is { Count: > 0 }
                ? folder.FileTypes
                : RedactionService.SupportedExtensions;
            return accepted.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase);
        }

        private async Task HandleFileAsync(string fullPath, WatchedFolderEntity folder)
        {
            try
            {
                if (!await WaitForFileReadyAsync(fullPath).ConfigureAwait(false))
                {
                    Log($"Watched file never became readable, skipping: {fullPath}", warning: true);
                    Activity(folder, "Error", $"File never became readable, skipped: {fullPath}");
                    RaiseProcessed(folder, fullPath, null, success: false, error: "File never became readable.");
                    return;
                }
                await RedactWatchedFileAsync(fullPath, folder).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log($"Failed to redact watched file '{fullPath}': {ex.Message}", warning: true);
                Activity(folder, "Error", $"Failed to redact {fullPath}: {ex.Message}");
                RaiseProcessed(folder, fullPath, null, success: false, error: ex.Message);
            }
            finally
            {
                _inFlight.TryRemove(fullPath, out _);
            }
        }

        private async Task RedactWatchedFileAsync(string fullPath, WatchedFolderEntity folder)
        {
            // Capture the current gate (Restart may swap it) and run; large files redact solo so a
            // big, memory-heavy file never runs alongside others.
            RedactionConcurrencyGate gate = _redactionGate;
            bool solo = LargeFileWarning.IsLarge(fullPath);
            using IDisposable slot = await gate.EnterAsync(solo).ConfigureAwait(false);
            {
                if (!File.Exists(fullPath))
                {
                    return;
                }

                Activity(folder, "Info", $"Found: {fullPath}");

                PolicyEntity? policyEntity = _policyRepository.FindByName(folder.Policy);
                if (policyEntity is null)
                {
                    Log($"Watched folder '{folder.FolderPath}' references unknown policy '{folder.Policy}', skipping {fullPath}.", warning: true);
                    Activity(folder, "Error", $"Unknown policy '{folder.Policy}', skipped: {fullPath}");
                    RaiseProcessed(folder, fullPath, null, success: false, error: $"Unknown policy '{folder.Policy}'.");
                    return;
                }

                var policy = PolicySerializer.DeserializeFromJson(
                    string.IsNullOrWhiteSpace(policyEntity.Json) ? "{}" : policyEntity.Json);
                GlobalLists.Apply(policy, _settingsRepository?.GetSettings()); // global lists on top of every policy

                string outputDir = ResolveOutputDirectory(fullPath, folder);
                Directory.CreateDirectory(outputDir);

                string outputPath = Path.Combine(outputDir, RedactionService.ApplySuffix(fullPath, CurrentSuffix()));

                // Skip if we already produced an up-to-date redaction (e.g., on restart re-scan).
                if (File.Exists(outputPath) &&
                    File.GetLastWriteTimeUtc(outputPath) >= File.GetLastWriteTimeUtc(fullPath))
                {
                    Activity(folder, "Info", $"Skipped (already redacted): {fullPath}");
                    return;
                }

                SettingsEntity watcherSettings = _settingsRepository?.GetSettings() ?? new SettingsEntity();

                // Skip files too large to load safely into memory (no partial output, so no false sense
                // of redaction). Surfaced to the activity log so the user knows the file was left alone.
                if (LargeFileWarning.ExceedsHardLimit(fullPath, watcherSettings.MaxInputFileSizeMb))
                {
                    Log($"Watched file exceeds the {watcherSettings.MaxInputFileSizeMb} MB size limit, skipping: {fullPath}", warning: true);
                    Activity(folder, "Error", $"Skipped (exceeds {watcherSettings.MaxInputFileSizeMb} MB size limit): {fullPath}");
                    RaiseProcessed(folder, fullPath, null, success: false, error: $"Exceeds the {watcherSettings.MaxInputFileSizeMb} MB size limit.");
                    return;
                }

                await RedactionService.RedactFileAsync(
                    fullPath, outputPath, policy, folder.Context, _filterService, folder.Highlight,
                    wordScrub: DocumentMetadata.OptionsFor(watcherSettings),
                    ocrScannedPdfs: watcherSettings.OcrScannedPdfs,
                    ocrTextCoverage: watcherSettings.OcrTextCoverageThreshold,
                    ocrImageCoverage: watcherSettings.OcrImageCoverageThreshold,
                    ocrMaxPages: watcherSettings.OcrMaxPages,
                    scrubEmailHeaders: watcherSettings.ScrubEmailHeaders,
                    removeCommonEmailHeaders: watcherSettings.RemoveCommonEmailHeaders).ConfigureAwait(false);

                Log($"Redacted watched file '{fullPath}' -> '{outputPath}'.");
                Activity(folder, "Info", $"Redacted: {fullPath} → {outputPath}");
                RaiseProcessed(folder, fullPath, outputPath, success: true, error: null);
            }
        }

        /// <summary>
        /// Resolves the output directory for a watched file. When subfolders are watched, the
        /// source's relative subfolder path is mirrored under the output folder so files with the
        /// same name in different subfolders don't collide.
        /// </summary>
        private static string ResolveOutputDirectory(string fullPath, WatchedFolderEntity folder)
        {
            string baseDir = string.IsNullOrWhiteSpace(folder.OutputFolder)
                ? Path.GetDirectoryName(fullPath) ?? folder.FolderPath
                : folder.OutputFolder;

            if (!folder.IncludeSubfolders || string.IsNullOrWhiteSpace(folder.OutputFolder))
            {
                return baseDir;
            }

            string sourceDir = Path.GetDirectoryName(fullPath) ?? folder.FolderPath;
            string relative = Path.GetRelativePath(folder.FolderPath, sourceDir);
            if (relative == "." || relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                return baseDir; // file at the watched root, or outside it
            }
            return Path.Combine(baseDir, relative);
        }

        private static async Task<bool> WaitForFileReadyAsync(string fullPath)
        {
            // A newly created file may still be open by the writer; wait until we can open it
            // exclusively (up to ~15s), which signals the copy/write has finished.
            for (int attempt = 0; attempt < 60; attempt++)
            {
                if (!File.Exists(fullPath))
                {
                    return false;
                }
                try
                {
                    using FileStream stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.None);
                    return true;
                }
                catch (IOException)
                {
                    await Task.Delay(250).ConfigureAwait(false);
                }
                catch (UnauthorizedAccessException)
                {
                    await Task.Delay(250).ConfigureAwait(false);
                }
            }
            return false;
        }

        private void RaiseProcessed(WatchedFolderEntity folder, string inputPath, string? outputPath, bool success, string? error)
        {
            if (!folder.Notify)
            {
                return;
            }
            EventHandler<WatchedFileProcessedEventArgs>? handler = FileProcessed;
            if (handler is null)
            {
                return;
            }
            try
            {
                handler(this, new WatchedFileProcessedEventArgs
                {
                    Folder = folder,
                    InputPath = inputPath,
                    OutputPath = outputPath,
                    Success = success,
                    ErrorMessage = error
                });
            }
            catch
            {
                // notifications must never break watching
            }
        }

        private void Activity(WatchedFolderEntity folder, string level, string message)
        {
            if (_logRepository is null)
            {
                return;
            }
            try
            {
                _logRepository.Append(folder.Id, level, message);
            }
            catch
            {
                // per-folder logging must never break watching
            }
        }

        private void Log(string message, bool warning = false)
        {
            if (!_loggingEnabled)
            {
                return;
            }
            try
            {
                if (warning)
                {
                    Logger.LogWarning(message);
                }
                else
                {
                    Logger.LogInfo(message);
                }
            }
            catch
            {
                // logging must never break watching
            }
        }

        public void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                {
                    return;
                }
                _disposed = true;
                StopWatchersNoLock();
            }
            _redactionGate.Dispose();
        }
    }

    /// <summary>Describes a processed file (for notifications). Also reused for main-queue
    /// completions, where <see cref="Folder"/> is null (the file didn't come from a watched folder).</summary>
    internal sealed class WatchedFileProcessedEventArgs : EventArgs
    {
        public WatchedFolderEntity? Folder { get; init; }
        public required string InputPath { get; init; }
        public string? OutputPath { get; init; }
        public bool Success { get; init; }
        public string? ErrorMessage { get; init; }

        /// <summary>Set when post-redaction verification found residual PII (e.g. "2 items may remain").</summary>
        public string? VerificationWarning { get; init; }
    }
}
