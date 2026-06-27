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
using Phileas.Policy.Filters;
using PhilterData;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    public sealed class WatchedFolderTests : IDisposable
    {
        private readonly string _root;
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly PolicyRepository _policyRepo;
        private readonly WatchedFolderLogRepository _logRepo;

        public WatchedFolderTests()
        {
            _root = Path.Combine(Path.GetTempPath(), "philter-watch-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _dbPath = Path.Combine(_root, "data.db");
            _db = new LiteDatabase(_dbPath);
            _policyRepo = new PolicyRepository(_db);
            _logRepo = new WatchedFolderLogRepository(_db);

            var policy = new PhileasPolicy { Name = "p", Identifiers = new Identifiers { EmailAddress = new EmailAddress() } };
            _policyRepo.Insert(new PolicyEntity { Name = "p", Json = PolicySerializer.SerializeToJson(policy) });
        }

        public void Dispose()
        {
            _db.Dispose();
            try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public void Entity_HasGeneratedId_AndDefaults()
        {
            var entity = new WatchedFolderEntity();
            Assert.NotNull(entity.Id);
            Assert.Equal(string.Empty, entity.FolderPath);
            Assert.Equal(string.Empty, entity.OutputFolder);
            Assert.False(entity.Highlight);
            Assert.False(entity.IncludeSubfolders);
            Assert.False(entity.Notify);
            Assert.Empty(entity.FileTypes); // empty = all supported types
        }

        [Fact]
        public void Repository_InsertGetDelete_RoundTrips()
        {
            using var repo = new WatchedFolderRepository(_db);
            var entity = new WatchedFolderEntity { FolderPath = @"C:\in", OutputFolder = @"C:\out", Policy = "p", Context = "ctx", Highlight = true };

            repo.Insert(entity);
            WatchedFolderEntity stored = Assert.Single(repo.GetAll());
            Assert.Equal(@"C:\in", stored.FolderPath);
            Assert.True(stored.Highlight);

            Assert.True(repo.Delete(stored.Id));
            Assert.Empty(repo.GetAll());
        }

        [Fact]
        public async Task Watcher_RedactsPreexistingFile_OnStart()
        {
            string watched = Path.Combine(_root, "watched");
            string output = Path.Combine(_root, "output");
            Directory.CreateDirectory(watched);

            // File present before the watcher starts -> picked up by the initial scan.
            string input = Path.Combine(watched, "doc.txt");
            await File.WriteAllTextAsync(input, "Contact me at jane@example.com please.");

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { Folder(watched, output) });

            string expected = Path.Combine(output, "doc_redacted-draft.txt");
            string redacted = await WaitForFileAsync(expected);

            Assert.DoesNotContain("jane@example.com", redacted);
            Assert.Contains("REDACTED", redacted);
        }

        [Fact]
        public async Task Watcher_RedactsNewFile_AfterStart()
        {
            string watched = Path.Combine(_root, "watched2");
            string output = Path.Combine(_root, "output2");
            Directory.CreateDirectory(watched);

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { Folder(watched, output) });

            // Drop a file after the watcher is running.
            string input = Path.Combine(watched, "new.txt");
            await File.WriteAllTextAsync(input, "Email bob@example.com now.");

            string expected = Path.Combine(output, "new_redacted-draft.txt");
            string redacted = await WaitForFileAsync(expected);

            Assert.DoesNotContain("bob@example.com", redacted);
            Assert.Contains("REDACTED", redacted);
        }

        [Fact]
        public async Task Watcher_IgnoresRedactedOutput_NoLoop()
        {
            string watched = Path.Combine(_root, "watched3");
            Directory.CreateDirectory(watched);

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { Folder(watched, watched) }); // output == watched on purpose

            string input = Path.Combine(watched, "memo.txt");
            await File.WriteAllTextAsync(input, "Email amy@example.com today.");

            string expected = Path.Combine(watched, "memo_redacted-draft.txt");
            await WaitForFileAsync(expected);

            // Give any (incorrect) cascade a chance to occur, then assert it didn't.
            await Task.Delay(800);
            Assert.False(File.Exists(Path.Combine(watched, "memo_redacted-draft_redacted-draft.txt")),
                "redacted output must not be re-redacted");
        }

        [Fact]
        public async Task Watcher_Recursive_RedactsSubfolderFile_MirrorsStructure()
        {
            string watched = Path.Combine(_root, "watched-rec");
            string output = Path.Combine(_root, "output-rec");
            string sub = Path.Combine(watched, "2025", "march");
            Directory.CreateDirectory(sub);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                IncludeSubfolders = true
            };

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { folder });

            string input = Path.Combine(sub, "deep.txt");
            await File.WriteAllTextAsync(input, "Email deep@example.com here.");

            // Output mirrors the relative subfolder path under the output folder.
            string expected = Path.Combine(output, "2025", "march", "deep_redacted-draft.txt");
            string redacted = await WaitForFileAsync(expected);

            Assert.DoesNotContain("deep@example.com", redacted);
            Assert.Contains("REDACTED", redacted);
        }

        [Fact]
        public async Task Watcher_NonRecursive_IgnoresSubfolderFiles()
        {
            string watched = Path.Combine(_root, "watched-flat");
            string output = Path.Combine(_root, "output-flat");
            string sub = Path.Combine(watched, "nested");
            Directory.CreateDirectory(sub);

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { Folder(watched, output) }); // IncludeSubfolders = false

            await File.WriteAllTextAsync(Path.Combine(sub, "deep.txt"), "Email skip@example.com here.");
            // A top-level file should still be processed, proving the watcher is alive.
            await File.WriteAllTextAsync(Path.Combine(watched, "top.txt"), "Email top@example.com here.");

            await WaitForFileAsync(Path.Combine(output, "top_redacted-draft.txt"));

            Assert.False(File.Exists(Path.Combine(output, "deep_redacted-draft.txt")),
                "subfolder files must be ignored when IncludeSubfolders is false");
        }

        [Fact]
        public async Task Watcher_FileTypeFilter_OnlyRedactsSelectedTypes()
        {
            string watched = Path.Combine(_root, "watched-types");
            string output = Path.Combine(_root, "output-types");
            Directory.CreateDirectory(watched);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                FileTypes = new List<string> { ".txt" } // only text files
            };

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { folder });

            // The .docx is filtered out by extension before it is ever opened, so its contents
            // are irrelevant — a dummy file avoids depending on the Xceed (Word) library here.
            await File.WriteAllTextAsync(Path.Combine(watched, "doc.docx"), "ignored by file-type filter");
            await File.WriteAllTextAsync(Path.Combine(watched, "note.txt"), "Email text@example.com here.");

            // The .txt is redacted; the .docx is ignored because it isn't a selected type.
            await WaitForFileAsync(Path.Combine(output, "note_redacted-draft.txt"));
            await Task.Delay(400);
            Assert.False(File.Exists(Path.Combine(output, "doc_redacted-draft.docx")),
                "a .docx must be ignored when only .txt is selected");
        }

        [Fact]
        public async Task Watcher_RaisesFileProcessed_OnSuccess_WhenNotifyEnabled()
        {
            string watched = Path.Combine(_root, "watched-notify");
            string output = Path.Combine(_root, "output-notify");
            Directory.CreateDirectory(watched);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                Notify = true
            };

            var events = new System.Collections.Concurrent.ConcurrentQueue<WatchedFileProcessedEventArgs>();
            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.FileProcessed += (_, e) => events.Enqueue(e);
            watcher.Restart(new[] { folder });

            await File.WriteAllTextAsync(Path.Combine(watched, "n.txt"), "Email n@example.com here.");

            WatchedFileProcessedEventArgs evt = await WaitForEventAsync(events, e => e.Success);
            Assert.True(evt.Success);
            Assert.Contains("n.txt", evt.InputPath);
            Assert.Equal(Path.Combine(output, "n_redacted-draft.txt"), evt.OutputPath);
        }

        [Fact]
        public async Task Watcher_DoesNotRaiseFileProcessed_WhenNotifyDisabled()
        {
            string watched = Path.Combine(_root, "watched-nonotify");
            string output = Path.Combine(_root, "output-nonotify");
            Directory.CreateDirectory(watched);

            var raised = false;
            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.FileProcessed += (_, _) => raised = true;
            watcher.Restart(new[] { Folder(watched, output) }); // Notify defaults to false

            await File.WriteAllTextAsync(Path.Combine(watched, "q.txt"), "Email q@example.com here.");

            // Wait until it's actually redacted, then confirm no event fired.
            await WaitForFileAsync(Path.Combine(output, "q_redacted-draft.txt"));
            await Task.Delay(400);
            Assert.False(raised);
        }

        [Fact]
        public async Task Watcher_RedactsEmail_MsgInput_ProducesRedactedEml()
        {
            string watched = Path.Combine(_root, "watched-email");
            string output = Path.Combine(_root, "output-email");
            Directory.CreateDirectory(watched);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                FileTypes = new List<string> { ".eml", ".msg" }
            };

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { folder });

            // Drop a real Outlook .msg; the watcher should redact it and write a .eml.
            WriteMsg(Path.Combine(watched, "memo.msg"));

            // .msg input maps to .eml output.
            string expected = Path.Combine(output, "memo_redacted-draft.eml");
            string redacted = await WaitForFileAsync(expected);

            Assert.DoesNotContain("secret@example.com", redacted);
            Assert.False(File.Exists(Path.Combine(output, "memo_redacted-draft.msg")),
                "a redacted .msg must be written as .eml, not .msg");
        }

        [Fact]
        public async Task Watcher_RedactsCsv_KeepsCsvExtension()
        {
            string watched = Path.Combine(_root, "watched-csv");
            string output = Path.Combine(_root, "output-csv");
            Directory.CreateDirectory(watched);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                FileTypes = new List<string> { ".xlsx", ".csv" }
            };

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { folder });

            await File.WriteAllTextAsync(Path.Combine(watched, "list.csv"),
                "Name,Email\r\nAlice,watch@example.com\r\n");

            string redacted = await WaitForFileAsync(Path.Combine(output, "list_redacted-draft.csv"));

            Assert.DoesNotContain("watch@example.com", redacted);
            Assert.Contains("Name,Email", redacted);
        }

        [Fact]
        public async Task Watcher_RedactsRtf_KeepsRtfExtension()
        {
            string watched = Path.Combine(_root, "watched-rtf");
            string output = Path.Combine(_root, "output-rtf");
            Directory.CreateDirectory(watched);

            var folder = new WatchedFolderEntity
            {
                FolderPath = watched,
                OutputFolder = output,
                Policy = "p",
                Context = "ctx",
                FileTypes = new List<string> { ".rtf" }
            };

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false);
            watcher.Restart(new[] { folder });

            await File.WriteAllTextAsync(Path.Combine(watched, "note.rtf"),
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs24 Email watch@example.com here.\par}");

            string redacted = await WaitForFileAsync(Path.Combine(output, "note_redacted-draft.rtf"));

            Assert.DoesNotContain("watch@example.com", redacted);
            Assert.StartsWith(@"{\rtf", redacted);
        }

        // Builds a minimal real Outlook .msg fixture with MsgKit (test-only dependency).
        private static void WriteMsg(string path)
        {
            using var email = new MsgKit.Email(
                new MsgKit.Sender("george@fake.com", "George Banks"),
                "Watched email")
            {
                BodyText = "Please email secret@example.com about the matter."
            };
            email.Recipients.AddTo("mary@example.org", "Mary Johnson");
            email.Save(path);
        }

        private static async Task<WatchedFileProcessedEventArgs> WaitForEventAsync(
            System.Collections.Concurrent.ConcurrentQueue<WatchedFileProcessedEventArgs> queue,
            Func<WatchedFileProcessedEventArgs, bool> predicate, int timeoutMs = 15000)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                foreach (WatchedFileProcessedEventArgs e in queue)
                {
                    if (predicate(e))
                    {
                        return e;
                    }
                }
                await Task.Delay(150);
                waited += 150;
            }
            throw new Xunit.Sdk.XunitException($"Expected FileProcessed event did not fire within {timeoutMs} ms.");
        }

        [Fact]
        public async Task Watcher_WritesActivityLog_FoundAndRedacted()
        {
            string watched = Path.Combine(_root, "watched-log");
            string output = Path.Combine(_root, "output-log");
            Directory.CreateDirectory(watched);

            WatchedFolderEntity folder = Folder(watched, output);

            using var watcher = new FolderWatcherService(_policyRepo, loggingEnabled: false, _logRepo);
            watcher.Restart(new[] { folder });

            string input = Path.Combine(watched, "doc.txt");
            await File.WriteAllTextAsync(input, "Email kate@example.com please.");

            // Wait until both the "Found" and "Redacted" entries are present.
            var entries = await WaitForLogAsync(folder,
                log => log.Any(e => e.Message.Contains("Found")) &&
                       log.Any(e => e.Message.Contains("Redacted")));

            Assert.Contains(entries, e => e.Level == "Info" && e.Message.Contains("Found") && e.Message.Contains("doc.txt"));
            Assert.Contains(entries, e => e.Level == "Info" && e.Message.Contains("Redacted") && e.Message.Contains("doc_redacted-draft.txt"));
        }

        private async Task<IReadOnlyList<WatchedFolderLogEntity>> WaitForLogAsync(
            WatchedFolderEntity folder, Func<IReadOnlyList<WatchedFolderLogEntity>, bool> predicate, int timeoutMs = 15000)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                IReadOnlyList<WatchedFolderLogEntity> entries = _logRepo.GetForFolder(folder.Id);
                if (predicate(entries))
                {
                    return entries;
                }
                await Task.Delay(150);
                waited += 150;
            }
            throw new Xunit.Sdk.XunitException($"Expected log entries did not appear within {timeoutMs} ms.");
        }

        private WatchedFolderEntity Folder(string watched, string output) => new()
        {
            FolderPath = watched,
            OutputFolder = output,
            Policy = "p",
            Context = "ctx"
        };

        private static async Task<string> WaitForFileAsync(string path, int timeoutMs = 15000)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                if (File.Exists(path))
                {
                    try { return await File.ReadAllTextAsync(path); }
                    catch (IOException) { /* still being written */ }
                }
                await Task.Delay(150);
                waited += 150;
            }
            throw new Xunit.Sdk.XunitException($"Expected redacted file did not appear within {timeoutMs} ms: {path}");
        }
    }
}
