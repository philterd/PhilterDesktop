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

                // If all retries failed, try fallback location
                TryLogToFallback(logEntry);
            }
        }

        /// <summary>
        /// Attempts to write to a fallback log location if the primary log fails.
        /// </summary>
        /// <param name="logEntry">The log entry to write.</param>
        private static void TryLogToFallback(string logEntry)
        {
            try
            {
                // Try writing to temp directory as fallback
                string tempLogPath = Path.Combine(Path.GetTempPath(), "PhilterDesktop_fallback.log");
                File.AppendAllText(tempLogPath, logEntry + Environment.NewLine);
                
                // Also add a note that this was written to fallback
                File.AppendAllText(tempLogPath, 
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [WARNING] Previous entry was written to fallback log due to primary log access failure{Environment.NewLine}");
            }
            catch
            {
                // Last resort: write to Windows Event Log
                TryLogToEventLog(logEntry);
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