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
    /// Tests the extracted per-item queue logic (missing policy / missing file / success), redacting
    /// a real temp file through a real policy and FilterService.
    /// </summary>
    public sealed class QueueProcessorTests : IDisposable
    {
        private readonly string _dbPath;
        private readonly LiteDatabase _db;
        private readonly PolicyRepository _policies;
        private readonly string _tempDir;
        private readonly FilterService _filterService = new();

        public QueueProcessorTests()
        {
            _dbPath = Path.Combine(Path.GetTempPath(), "philter-qp-" + Guid.NewGuid().ToString("N") + ".db");
            _db = new LiteDatabase(_dbPath);
            _policies = new PolicyRepository(_db);
            _tempDir = Path.Combine(Path.GetTempPath(), "philter-qp-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { File.Delete(_dbPath); } catch { /* best effort */ }
            try { Directory.Delete(_tempDir, recursive: true); } catch { /* best effort */ }
        }

        [Fact]
        public async Task ProcessAsync_MissingPolicy_FailsWithoutOutput()
        {
            var entity = new RedactionQueueEntity { Name = Path.Combine(_tempDir, "x.txt"), Policy = "nope", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, new SettingsEntity(), _filterService);

            Assert.False(result.Success);
            Assert.Contains("nope", result.ErrorMessage);
            Assert.Null(result.OutputPath);
        }

        [Fact]
        public async Task ProcessAsync_MissingFile_Fails()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });
            var entity = new RedactionQueueEntity { Name = Path.Combine(_tempDir, "gone.txt"), Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, new SettingsEntity(), _filterService);

            Assert.False(result.Success);
            Assert.Equal("File not found", result.ErrorMessage);
        }

        [Fact]
        public async Task ProcessAsync_ValidTextFile_SucceedsAndWritesRedactedOutput()
        {
            // Policy enabling SSN/email/phone (the JSON the editor would produce).
            _policies.Insert(new PolicyEntity
            {
                Name = "p",
                Json = "{\"identifiers\":{\"ssn\":{},\"emailAddress\":{},\"phoneNumber\":{}}}"
            });

            string input = Path.Combine(_tempDir, "in.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com phone 555-123-4567 ssn 123-45-6789.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.NotNull(result.OutputPath);
            Assert.True(File.Exists(result.OutputPath!));

            string redacted = await File.ReadAllTextAsync(result.OutputPath!);
            Assert.DoesNotContain("john@example.com", redacted);
            Assert.DoesNotContain("123-45-6789", redacted);
        }

        [Fact]
        public async Task ProcessAsync_AppliesGlobalAlwaysRedact_EvenWhenPolicyRedactsNothing()
        {
            // An empty policy redacts nothing on its own — proving the global list is what removed it.
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            string input = Path.Combine(_tempDir, "global-redact.txt");
            await File.WriteAllTextAsync(input, "Codename Bluebird is confidential.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true, GlobalAlwaysRedact = "Bluebird" };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            string redacted = await File.ReadAllTextAsync(result.OutputPath!);
            Assert.DoesNotContain("Bluebird", redacted);
        }

        [Fact]
        public async Task ProcessAsync_AppliesGlobalAlwaysIgnore_OverThePolicy()
        {
            // Policy redacts email; the global ignore list must keep one specific address.
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string input = Path.Combine(_tempDir, "global-ignore.txt");
            await File.WriteAllTextAsync(input, "Contact a@b.com or keep@example.com today.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true, GlobalAlwaysIgnore = "keep@example.com" };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            string redacted = await File.ReadAllTextAsync(result.OutputPath!);
            Assert.DoesNotContain("a@b.com", redacted);          // normal email redacted
            Assert.Contains("keep@example.com", redacted);        // global-ignore address preserved
        }

        [Fact]
        public async Task ProcessAsync_Csv_AppliesFullyRedactedColumns_KeepsHeader()
        {
            // Empty policy: nothing is detected, so only the selected full column should be cleared,
            // proving the queued column selection travels through to the redactor.
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            string input = Path.Combine(_tempDir, "people.csv");
            await File.WriteAllTextAsync(input, "Name,Email\r\nAlice,a@example.com\r\nBob,b@example.com\r\n");

            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            var entity = new RedactionQueueEntity
            {
                Name = input,
                Policy = "p",
                Context = "ctx",
                FullyRedactedColumns = new List<int> { 1 } // column A (Name)
            };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            string redacted = await File.ReadAllTextAsync(result.OutputPath!);
            Assert.DoesNotContain("Alice", redacted);
            Assert.DoesNotContain("Bob", redacted);
            Assert.Contains("Name", redacted);                 // header preserved
            Assert.Contains("a@example.com", redacted);        // column B untouched (empty policy)
        }

        [Fact]
        public async Task ProcessAsync_Success_RecordsRedactionDuration()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });
            string input = Path.Combine(_tempDir, "timed.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com please.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.True(result.DurationMs >= 0); // measured end-to-end; populated on the result
        }

        [Fact]
        public async Task ProcessAsync_WithVerificationEnabled_AttachesCleanResult()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string input = Path.Combine(_tempDir, "verify.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com please.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true, VerifyAfterRedaction = true };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.NotNull(result.Verification);
            // The redactor removed the email it detected, so re-scanning the output is clean.
            Assert.Equal(VerificationStatus.Clean, result.Verification!.Status);
        }

        [Fact]
        public async Task ProcessAsync_WithVerificationDisabled_DoesNotVerify()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            string input = Path.Combine(_tempDir, "noverify.txt");
            await File.WriteAllTextAsync(input, "Plain content.");

            var settings = new SettingsEntity { OutputToOriginalLocation = true, VerifyAfterRedaction = false };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.Null(result.Verification);
        }

        [Fact]
        public void DescribeFailure_NonExceptionResult_UsesItsMessage()
        {
            var result = QueueRedactionResult.Failed("Policy 'nope' was not found");
            Assert.Equal("Policy 'nope' was not found", QueueProcessor.DescribeFailure(result, @"C:\docs\x.txt"));
        }

        [Fact]
        public void DescribeFailure_EmptyMessage_FallsBackToGenericText()
        {
            var result = QueueRedactionResult.Failed("");
            Assert.Equal("The redaction could not be completed.", QueueProcessor.DescribeFailure(result, @"C:\docs\x.txt"));
        }

        [Fact]
        public void DescribeFailure_ExceptionResult_IsTranslatedToFriendlyText()
        {
            var result = QueueRedactionResult.Failed("raw", new UnauthorizedAccessException("denied"));
            string text = QueueProcessor.DescribeFailure(result, @"C:\docs\report.docx");

            // UserError turns this into plain-language guidance mentioning the file name.
            Assert.Contains("report.docx", text);
            Assert.Contains("permission", text);
            Assert.DoesNotContain("raw", text);
        }
    }
}
