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
    /// <summary>
    /// Provides logging functionality for the application.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lockObject = new();
        private static string? _logFilePath;
        private const int MaxRetries = 3;
        private const int RetryDelayMilliseconds = 100;

        // Cap the log so it can't grow without bound: when application.log reaches this size it's rolled
        // over to application.log.1 (older backups shift up), keeping at most MaxBackups of them. Total
        // on-disk footprint is bounded at roughly (MaxBackups + 1) * MaxLogBytes.
        private const long MaxLogBytes = 5L * 1024 * 1024; // 5 MB per file
        private const int MaxBackups = 3;

        /// <summary>
        /// Gets the path to the log file.
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                if (_logFilePath == null)
                {
                    string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                    string folder = Path.Combine(root, "PhilterDesktop");
                    
                    // Ensure the directory exists
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    _logFilePath = Path.Combine(folder, "application.log");
                }

                return _logFilePath;
            }
        }

        /// <summary>
        /// Writes a log entry with the specified message and log level.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">The log level (e.g., INFO, WARNING, ERROR).</param>
        public static void Log(string message, string level = "INFO")
        {
            ArgumentNullException.ThrowIfNull(message);

            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            string logEntry = $"[{timestamp}] [{level}] {message}";

            // Use lock to ensure thread-safe file access
            lock (_lockObject)
            {
                // Roll the log over before appending if it has reached the size cap.
                RotateLogIfNeeded(LogFilePath, MaxLogBytes, MaxBackups);

                // Try multiple times with exponential backoff
                for (int attempt = 0; attempt < MaxRetries; attempt++)
                {
                    try
                    {
                        File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                        return; // Success - exit method
                    }
                    catch (IOException) when (attempt < MaxRetries - 1)
                    {
                        // File might be locked by another process, wait and retry
                        Thread.Sleep(RetryDelayMilliseconds * (attempt + 1));
                    }
                    catch (UnauthorizedAccessException) when (attempt < MaxRetries - 1)
                    {
                        // Permission issue, wait and retry
                        Thread.Sleep(RetryDelayMilliseconds * (attempt + 1));
                    }
                    catch
                    {
                        // For other exceptions, break and try fallback
                        break;
                    }
                }

                // If all retries failed, fall back to the Windows Event Log. We deliberately do NOT
                // fall back to a temp file: log lines can describe document activity, and %TEMP% is a
                // less-controlled location, so the Event Log is the safer last resort.
                TryLogToEventLog(logEntry);
            }
        }

        /// <summary>
        /// Rolls <paramref name="logPath"/> over when it reaches <paramref name="maxBytes"/>: the oldest
        /// backup is dropped, each <c>.N</c> backup shifts up to <c>.N+1</c>, and the current log becomes
        /// <c>.1</c>, leaving a fresh (empty) log to write to. Keeps at most <paramref name="maxBackups"/>
        /// backups. Best-effort: any failure leaves the current log in place so logging can continue.
        /// </summary>
        internal static void RotateLogIfNeeded(string logPath, long maxBytes, int maxBackups)
        {
            try
            {
                var info = new FileInfo(logPath);
                if (!info.Exists || info.Length < maxBytes)
                {
                    return;
                }

                // Drop the oldest backup, then shift the rest up (.2 -> .3, .1 -> .2, …).
                string oldest = $"{logPath}.{maxBackups}";
                if (File.Exists(oldest))
                {
                    File.Delete(oldest);
                }
                for (int i = maxBackups - 1; i >= 1; i--)
                {
                    string src = $"{logPath}.{i}";
                    if (File.Exists(src))
                    {
                        File.Move(src, $"{logPath}.{i + 1}");
                    }
                }

                File.Move(logPath, $"{logPath}.1");
            }
            catch
            {
                // If rotation fails (e.g. a backup is locked), keep appending to the current log.
            }
        }

        /// <summary>
        /// Attempts to write to Windows Event Log as last resort.
        /// </summary>
        /// <param name="logEntry">The log entry to write.</param>
        private static void TryLogToEventLog(string logEntry)
        {
            try
            {
                const string sourceName = "PhilterDesktop";
                
                // Check if event source exists, create if not
                if (!System.Diagnostics.EventLog.SourceExists(sourceName))
                {
                    System.Diagnostics.EventLog.CreateEventSource(sourceName, "Application");
                }

                System.Diagnostics.EventLog.WriteEntry(sourceName, logEntry, System.Diagnostics.EventLogEntryType.Information);
            }
            catch
            {
                // Truly nothing we can do at this point - swallow the exception
                // to prevent logging failures from crashing the application
            }
        }

        /// <summary>
        /// Writes an informational log entry.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogInfo(string message)
        {
            Log(message, "INFO");
        }

        /// <summary>
        /// Writes a warning log entry.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogWarning(string message)
        {
            Log(message, "WARNING");
        }

        /// <summary>
        /// Writes an error log entry.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogError(string message)
        {
            Log(message, "ERROR");
        }

        /// <summary>
        /// Writes an error log entry with exception details.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="exception">The exception to log.</param>
        public static void LogError(string message, Exception exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            string fullMessage = $"{message} - Exception: {exception.GetType().Name}, Message: {exception.Message}, StackTrace: {exception.StackTrace}";
            Log(fullMessage, "ERROR");
        }

        /// <summary>
        /// Writes a debug log entry.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public static void LogDebug(string message)
        {
            Log(message, "DEBUG");
        }

        /// <summary>
        /// Clears the log file.
        /// </summary>
        public static void ClearLog()
        {
            lock (_lockObject)
            {
                try
                {
                    if (File.Exists(LogFilePath))
                    {
                        File.Delete(LogFilePath);
                    }
                }
                catch
                {
                    // Silently fail
                }
            }
        }
    }
}