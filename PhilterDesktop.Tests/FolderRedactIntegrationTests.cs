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
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// End-to-end test of the "Redact Folder" flow without the UI: enumerate a folder, enqueue the
    /// selected files into the real queue repository, then drain the queue through the same
    /// <see cref="QueueProcessor"/> the main window uses. Proves the acceptance criteria — all supported
    /// files are redacted, the supported-extension list is honored, and per-file failures land in the
    /// queue (status Failed with a reason) rather than being dropped silently.
    /// </summary>
    public sealed class FolderRedactIntegrationTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly PolicyRepository _policies;
        private readonly RedactionQueueRepository _queue;
        private readonly string _root;
        private readonly FilterService _filterService = new();

        public FolderRedactIntegrationTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-folder-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _policies = new PolicyRepository(_db);
            _queue = new RedactionQueueRepository(_db);
            _root = Path.Combine(Path.GetTempPath(), "philter-folder-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
            try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        }

        private string Write(string relativePath, string content)
        {
            string full = Path.Combine(_root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, content);
            return full;
        }

        // Mirrors the enqueue half of FolderRedactForm.OnRedact and the drain half of MainForm's
        // queue timer, so the test exercises the real selection + processing logic.
        private async Task<List<RedactionQueueEntity>> RedactFolderAsync(string policy, bool recursive)
        {
            var settings = new SettingsEntity { OutputToOriginalLocation = true };

            foreach (string file in FolderEnumerator.EnumerateRedactable(_root, recursive, settings.RedactedSuffix))
            {
                _queue.Insert(new RedactionQueueEntity { Name = file, Policy = policy, Context = "ctx" });
            }

            foreach (RedactionQueueEntity entity in _queue.Find(x => x.Status == "Pending").ToList())
            {
                QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);
                if (result.Success)
                {
                    entity.Status = "Completed";
                    entity.ErrorMessage = string.Empty;
                }
                else
                {
                    entity.Status = "Failed";
                    entity.ErrorMessage = QueueProcessor.DescribeFailure(result, entity.Name);
                }
                _queue.Update(entity);
            }

            return _queue.GetAll().ToList();
        }

        [Fact]
        public async Task RedactFolder_RedactsAllSupportedFiles_SkipsUnsupportedAndPriorOutputs()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            Write("alice.txt", "Reach Alice at alice@example.com.");
            Write("sub/bob.txt", "Reach Bob at bob@example.com.");
            Write("photo.png", "not redactable");                  // unsupported extension
            Write("old_redacted-draft.txt", "leftover@example.com"); // looks like a prior output

            List<RedactionQueueEntity> queue = await RedactFolderAsync("p", recursive: true);

            // Only the two real .txt files were enqueued (png + prior output skipped by the enumerator).
            Assert.Equal(2, queue.Count);
            Assert.All(queue, q => Assert.Equal("Completed", q.Status));

            // Both outputs exist on disk and the email is gone.
            foreach (string name in new[] { "alice", "bob" })
            {
                string output = Path.Combine(Path.GetDirectoryName(queue.Single(q => Path.GetFileNameWithoutExtension(q.Name) == name).Name)!,
                    name + "_redacted-draft.txt");
                Assert.True(File.Exists(output), $"expected redacted output for {name}");
                Assert.DoesNotContain("@example.com", await File.ReadAllTextAsync(output));
            }
        }

        [Fact]
        public async Task RedactFolder_NonRecursive_LeavesSubfolderFilesAlone()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            Write("top.txt", "hello");
            Write("sub/nested.txt", "hello");

            List<RedactionQueueEntity> queue = await RedactFolderAsync("p", recursive: false);

            Assert.Single(queue);
            Assert.EndsWith("top.txt", queue[0].Name);
        }

        [Fact]
        public async Task RedactFolder_SurfacesPerFileFailures_WithoutStoppingTheBatch()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            Write("good.txt", "Email good@example.com.");
            Write("broken.docx", "this is not a real docx, it has no zip header"); // fails OfficeDocument.Inspect

            List<RedactionQueueEntity> queue = await RedactFolderAsync("p", recursive: false);

            Assert.Equal(2, queue.Count);

            RedactionQueueEntity good = queue.Single(q => q.Name.EndsWith("good.txt", StringComparison.OrdinalIgnoreCase));
            RedactionQueueEntity broken = queue.Single(q => q.Name.EndsWith("broken.docx", StringComparison.OrdinalIgnoreCase));

            Assert.Equal("Completed", good.Status);          // the good file still succeeded
            Assert.Equal("Failed", broken.Status);           // the bad one failed
            Assert.False(string.IsNullOrWhiteSpace(broken.ErrorMessage)); // and surfaced a reason
        }
    }
}
