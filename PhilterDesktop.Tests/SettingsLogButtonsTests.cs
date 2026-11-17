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
    /// definition of where the log lives. The log is redirected to a temporary file for each test's own work,
    /// so other code still running in the test process can't write into it.
    /// </summary>
    [Collection(LogFileCollection.Name)]
    public sealed class SettingsLogButtonsTests : IDisposable
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly string _dir = Path.Combine(Path.GetTempPath(), "settings-log-" + Guid.NewGuid().ToString("N"));
        private readonly string _log;
        private readonly List<(string Text, string Caption, MessageBoxButtons Buttons)> _shown = new();

        public SettingsLogButtonsTests()
        {
            Directory.CreateDirectory(_dir);
            _log = Path.Combine(_dir, "application.log");
            Logger.TestLogFilePath = _log; // this test's work only; nothing else in the process logs here
        }

        public void Dispose()
        {
            Logger.TestLogFilePath = null;
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        // ---- Where the log lives ----

        [Fact]
        public void LoggerPath_IsUnchanged()
        {
            Logger.TestLogFilePath = null; // the app's own path
            string expected = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhilterDesktop", "application.log");

            Assert.Equal(expected, Logger.LogFilePath);
        }

        [Fact]
        public void OtherWorkInTheProcess_DoesNotLogIntoTheTestsFile()
        {
            // The race that made Clear Log tests flaky: work started outside this test (e.g. left over from an
            // earlier one) logged into the redirected file. It must see the normal path instead.
            AssertLogIsTheTests();
            string? elsewhere = null;
            Task task;
            using (ExecutionContext.SuppressFlow())
            {
                task = Task.Run(() => elsewhere = Logger.LogFilePath);
            }
            task.Wait();

            Assert.NotEqual(_log, elsewhere);
        }

        [Fact]
        public void ThreadsTheTestStarts_DoLogIntoItsFile()
        {
            AssertLogIsTheTests();
            string? seen = null;
            var thread = new Thread(() => seen = Logger.LogFilePath);
            thread.Start();
            thread.Join();

            Assert.Equal(_log, seen);
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

        // ---- Older log files ----

        [Fact]
        public void ClearLog_DeletesTheOlderLogFilesToo() => WithSettings(form =>
        {
            WriteLogs("", ".1", ".2", ".3");

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.Empty(Logger.ExistingLogFiles());
            Assert.StartsWith("Permanently delete all 4 application log files, including older ones?", _shown[0].Text);
            Assert.Equal("The application log files have been cleared.", _shown[1].Text);
        });

        [Fact]
        public void ClearLog_WithOnlyOlderFiles_StillClearsThem() => WithSettings(form =>
        {
            WriteLogs(".1", ".2");

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.DoesNotContain(_shown, m => m.Text == "There is no log file to clear.");
            Assert.StartsWith("Permanently delete all 2 application log files", _shown[0].Text);
            Assert.Empty(Logger.ExistingLogFiles());
        });

        [Fact]
        public void ClearLog_AfterARealRollover_DeletesEveryFile() => WithSettings(form =>
        {
            File.WriteAllText(_log, new string('x', 100));
            Logger.RotateLogIfNeeded(_log, maxBytes: 10, maxBackups: 3);
            File.WriteAllText(_log, "new");
            Assert.Equal(2, Logger.ExistingLogFiles().Count);

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.Empty(Logger.ExistingLogFiles());
        });

        [Fact]
        public void ExistingLogFiles_FindsEveryNumberedFile_AndNothingElse()
        {
            WriteLogs("", ".1", ".3", ".12");
            File.WriteAllText(_log + ".bak", "");
            File.WriteAllText(_log + ".1.tmp", "");
            File.WriteAllText(Path.Combine(_dir, "other.log"), "");
            File.WriteAllText(Path.Combine(_dir, "application.logs"), "");

            Assert.Equal(new[] { "application.log", "application.log.1", "application.log.3", "application.log.12" },
                Logger.ExistingLogFiles().Select(Path.GetFileName));
        }

        [Fact]
        public void ExistingLogFiles_IsEmptyWhenTheFolderIsMissing() =>
            Assert.Empty(Logger.ExistingLogFiles(Path.Combine(_dir, "missing", "application.log")));

        [Fact]
        public void ClearLog_LeavesOtherFilesAlone()
        {
            WriteLogs("", ".1");
            string other = Path.Combine(_dir, "other.log");
            File.WriteAllText(other, "");

            Logger.ClearLog();

            Assert.True(File.Exists(other));
        }

        [Fact]
        public void ClearLog_DeletesAReadOnlyLogFile()
        {
            WriteLogs("", ".1");
            File.SetAttributes(_log + ".1", FileAttributes.ReadOnly);

            Logger.ClearLog();

            Assert.Empty(Logger.ExistingLogFiles());
        }

        // ---- Failures ----

        [Fact]
        public void ClearLog_ALockedFile_FailsAndNamesIt_AfterDeletingTheRest()
        {
            WriteLogs("", ".1", ".2");
            using (new FileStream(_log + ".1", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                IOException error = Assert.Throws<IOException>(() => Logger.ClearLog());

                Assert.StartsWith("1 log file couldn't be deleted (application.log.1).", error.Message);
                Assert.NotNull(error.InnerException);
            }

            Assert.Equal(new[] { "application.log.1" }, Logger.ExistingLogFiles().Select(Path.GetFileName));
        }

        [Fact]
        public void ClearLog_SeveralLockedFiles_NamesThemAll()
        {
            WriteLogs("", ".1");
            using var a = new FileStream(_log, FileMode.Open, FileAccess.Read, FileShare.None);
            using var b = new FileStream(_log + ".1", FileMode.Open, FileAccess.Read, FileShare.None);

            IOException error = Assert.Throws<IOException>(() => Logger.ClearLog());

            Assert.StartsWith("2 log files couldn't be deleted (application.log, application.log.1).", error.Message);
        }

        [Fact]
        public void ClearLogButton_ALockedFile_ShowsTheFailure_NotSuccess() => WithSettings(form =>
        {
            WriteLogs("", ".1");
            using var locked = new FileStream(_log, FileMode.Open, FileAccess.Read, FileShare.None);

            Click(form, "BtnClearLog_Click", answer: DialogResult.Yes);

            Assert.Equal(2, _shown.Count);
            Assert.StartsWith("The log wasn't fully cleared. 1 log file couldn't be deleted (application.log).", _shown[1].Text);
            Assert.DoesNotContain(_shown, m => m.Text.Contains("has been cleared") || m.Text.Contains("have been cleared"));
        });

        [Theory]
        [InlineData(1, "Permanently delete the application log file?")]
        [InlineData(2, "Permanently delete all 2 application log files, including older ones?")]
        [InlineData(4, "Permanently delete all 4 application log files, including older ones?")]
        public void ClearLogQuestion_SaysHowManyFiles(int count, string question) =>
            Assert.Equal(question, SettingsForm.ClearLogQuestion(count));

        // ---- Helpers ----

        // Stops a test before it touches a log that isn't its own (the real one would be deleted by Clear Log).
        private void AssertLogIsTheTests() =>
            Assert.True(Logger.LogFilePath == _log, "The log isn't redirected to this test's file: " + Logger.LogFilePath);

        private void WriteLogs(params string[] suffixes)
        {
            AssertLogIsTheTests();
            foreach (string suffix in suffixes)
            {
                File.WriteAllText(_log + suffix, "entry");
            }
        }

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
            AssertLogIsTheTests();
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
