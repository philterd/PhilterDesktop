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

using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using LiteDB;
using PhilterData;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests that redirect the app-wide log file; they must not overlap other tests that log.</summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class LogFileCollection
    {
        public const string Name = "Log file";
    }

    /// <summary>
    /// The Settings window's Open Log and Clear Log buttons use the logger's own path, so there's one
    /// definition of where the log lives. The log is redirected to a temporary file here.
    /// </summary>
    [Collection(LogFileCollection.Name)]
    public sealed class SettingsLogButtonsTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CachedPath = typeof(Logger).GetField("_logFilePath", BindingFlags.Static | BindingFlags.NonPublic)!;

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "settings-log-" + Guid.NewGuid().ToString("N"));
        private readonly string _log;
        private readonly object? _originalPath = CachedPath.GetValue(null);
        private readonly List<(string Text, string Caption, MessageBoxButtons Buttons)> _shown = new();

        public SettingsLogButtonsTests()
        {
            Directory.CreateDirectory(_dir);
            _log = Path.Combine(_dir, "application.log");
            CachedPath.SetValue(null, _log);
        }

        public void Dispose()
        {
            CachedPath.SetValue(null, _originalPath);
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        // ---- Where the log lives ----

        [Fact]
        public void LoggerPath_IsUnchanged()
        {
            CachedPath.SetValue(null, null); // resolve afresh, as on startup
            string expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhilterDesktop", "application.log");

            Assert.Equal(expected, Logger.LogFilePath);
        }

        [Fact]
        public void SettingsForm_NoLongerResolvesTheLogPathItself() =>
            Assert.Null(typeof(SettingsForm).GetMethod("GetLogFilePath", Flags));

        [Fact]
        public void LogFileName_IsDefinedInExactlyOnePlace()
        {
            string app = Path.GetDirectoryName(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"))!;
            var hits = Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar)
                            && !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                .SelectMany(f => Regex.Matches(File.ReadAllText(f), "\"application\\.log\"").Select(_ => Path.GetFileName(f)))
                .ToList();

            Assert.Equal("Logger.cs", Assert.Single(hits));
        }

        // ---- Open Log ----

        [Fact]
        public void OpenLog_WithNoLogFile_SaysItDoesNotExistYet() => WithSettings(form =>
        {
            Click(form, "BtnOpenLog_Click");

            var message = Assert.Single(_shown);
            Assert.Equal("Log File Not Found", message.Caption);
            Assert.StartsWith("Log file does not exist yet.", message.Text);
            Assert.False(File.Exists(_log));
        });

        // ---- Clear Log ----

        [Fact]
        public void ClearLog_WithNoLogFile_SaysThereIsNothingToClear() => WithSettings(form =>
        {
            Click(form, "BtnClearLog_Click");

            var message = Assert.Single(_shown);
            Assert.Equal("There is no log file to clear.", message.Text);
            Assert.Equal("Clear Log File", message.Caption);
        });

        [Fact]
        public void ClearLog_Declined_KeepsTheLog() => WithSettings(form =>
        {
            File.WriteAllText(_log, "entry");

            Click(form, "BtnClearLog_Click", answer: DialogResult.No);

            var message = Assert.Single(_shown);
            Assert.Equal(MessageBoxButtons.YesNo, message.Buttons);
            Assert.StartsWith("Permanently delete the application log file?", message.Text);
            Assert.True(File.Exists(_log));
        });

        [Fact]
        public void ClearLog_Confirmed_DeletesTheLoggersFile() => WithSettings(form =>
        {
            File.WriteAllText(_log, "entry");

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.False(File.Exists(_log));
            Assert.Equal(2, _shown.Count);
            Assert.Equal("The application log file has been cleared.", _shown[1].Text);
        });

        [Fact]
        public void ClearLog_AfterLogging_FindsTheFileTheLoggerWrote() => WithSettings(form =>
        {
            Logger.Log("written by the logger");
            Assert.True(File.Exists(_log));

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.False(File.Exists(_log));
        });

        // ---- Helpers ----

        private void Click(SettingsForm form, string handler, DialogResult answer = DialogResult.OK)
        {
            form.ShowLogMessage = (text, caption, buttons, _) =>
            {
                _shown.Add((text, caption, buttons));
                return buttons == MessageBoxButtons.YesNo ? answer : DialogResult.OK;
            };
            typeof(SettingsForm).GetMethod(handler, Flags)!.Invoke(form, new object?[] { null, EventArgs.Empty });
        }

        private void WithSettings(Action<SettingsForm> test)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try
                {
                    using var db = new LiteDatabase(Path.Combine(_dir, "settings.db"));
                    var policies = new PolicyRepository(db);
                    policies.Insert(new PolicyEntity { Name = "default", Json = DefaultPolicy.Json() });
                    var contexts = new ContextRepository(db);
                    contexts.Insert(new ContextEntity { Name = "default" });
                    using var form = new SettingsForm(new SettingsRepository(db), policies, contexts, new WatchedFolderRepository(db));
                    test(form);
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }

        private static string RepoFile(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine([dir.FullName, .. parts]);
                if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "PhilterDesktop.slnx")))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts));
        }
    }
}
