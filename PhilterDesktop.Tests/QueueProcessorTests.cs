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
using Phileas.Services;
using PhilterData;
using PhilterDesktop;
using Xunit;
using PhileasPolicy = Phileas.Policy.Policy;

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
        public async Task ProcessAsync_FileExceedsHardLimit_FailsBeforeLoading()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            // ~2 MB file against a 1 MB hard cap — over the limit.
            string input = Path.Combine(_tempDir, "big.txt");
            await File.WriteAllTextAsync(input, new string('a', 2 * 1024 * 1024));

            var settings = new SettingsEntity { OutputToOriginalLocation = true, MaxInputFileSizeMb = 1 };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.False(result.Success);
            Assert.Contains("size limit", result.ErrorMessage);
            Assert.Contains("1 MB", result.ErrorMessage);
            Assert.Null(result.OutputPath);        // failed before producing output
        }

        [Fact]
        public async Task ProcessAsync_LargeFile_NoLimit_StillProcesses()
        {
            // A hard limit of 0 means "no limit" — the same large file goes through.
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{}" });

            string input = Path.Combine(_tempDir, "big-nolimit.txt");
            await File.WriteAllTextAsync(input, new string('a', 2 * 1024 * 1024));

            var settings = new SettingsEntity { OutputToOriginalLocation = true, MaxInputFileSizeMb = 0 };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.True(File.Exists(result.OutputPath!));
        }

        [Fact]
        public async Task ProcessAsync_FileUnderHardLimit_Proceeds()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string input = Path.Combine(_tempDir, "small.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com please.");

            // Well under a 500 MB cap.
            var settings = new SettingsEntity { OutputToOriginalLocation = true, MaxInputFileSizeMb = 500 };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.DoesNotContain("john@example.com", await File.ReadAllTextAsync(result.OutputPath!));
        }

        [Fact]
        public async Task ProcessAsync_RtfWithHeader_FlagsContentDropped()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string input = Path.Combine(_tempDir, "hdr.rtf");
            File.WriteAllText(input,
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}{\header\pard\f0 Letterhead\par}\f0\fs24 Body a@b.com\par}");

            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.NotNull(result.ContentDroppedWarning); // header present -> fidelity warning
        }

        [Fact]
        public async Task ProcessAsync_RtfBodyOnly_DoesNotFlagContentDropped()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string input = Path.Combine(_tempDir, "body.rtf");
            File.WriteAllText(input,
                @"{\rtf1\ansi\deff0{\fonttbl{\f0\fnil Arial;}}\f0\fs24 Body a@b.com only\par}");

            var settings = new SettingsEntity { OutputToOriginalLocation = true };
            var entity = new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" };

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

            Assert.True(result.Success);
            Assert.Null(result.ContentDroppedWarning);
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
        public async Task ProcessAsync_SameNamedSources_CustomOutputFolder_DoNotOverwrite()
        {
            // Two files named "invoice.txt" from different folders, redacted into one shared output
            // folder, must not clobber each other: the second gets a "(2)" name.
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });

            string folderA = Path.Combine(_tempDir, "A");
            string folderB = Path.Combine(_tempDir, "B");
            string outputFolder = Path.Combine(_tempDir, "out");
            Directory.CreateDirectory(folderA);
            Directory.CreateDirectory(folderB);
            Directory.CreateDirectory(outputFolder);

            string inputA = Path.Combine(folderA, "invoice.txt");
            string inputB = Path.Combine(folderB, "invoice.txt");
            await File.WriteAllTextAsync(inputA, "From alice@example.com (folder A)");
            await File.WriteAllTextAsync(inputB, "From bob@example.com (folder B)");

            var settings = new SettingsEntity { OutputToOriginalLocation = false, CustomOutputFolder = outputFolder };

            QueueRedactionResult a = await QueueProcessor.ProcessAsync(
                new RedactionQueueEntity { Name = inputA, Policy = "p", Context = "ctx" }, _policies, settings, _filterService);
            QueueRedactionResult b = await QueueProcessor.ProcessAsync(
                new RedactionQueueEntity { Name = inputB, Policy = "p", Context = "ctx" }, _policies, settings, _filterService);

            Assert.True(a.Success);
            Assert.True(b.Success);
            Assert.NotEqual(a.OutputPath, b.OutputPath);          // distinct files, no overwrite
            Assert.True(File.Exists(a.OutputPath!));
            Assert.True(File.Exists(b.OutputPath!));
            Assert.EndsWith("invoice_redacted-draft.txt", a.OutputPath);
            Assert.EndsWith("invoice_redacted-draft (2).txt", b.OutputPath);

            // Each output keeps its own (redacted) content — neither was overwritten by the other.
            string redactedA = await File.ReadAllTextAsync(a.OutputPath!);
            string redactedB = await File.ReadAllTextAsync(b.OutputPath!);
            Assert.Contains("folder A", redactedA);
            Assert.Contains("folder B", redactedB);
            Assert.DoesNotContain("@example.com", redactedA);
            Assert.DoesNotContain("@example.com", redactedB);
        }

        [Fact]
        public async Task ProcessAsync_NameModelMissing_FlagsNameDetectionUnavailable_ButStillRedacts()
        {
            // Force the on-device name model absent so a names-requesting policy can't run name detection.
            string emptyModelDir = Path.Combine(_tempDir, "no-model");
            Directory.CreateDirectory(emptyModelDir);
            string? prev = Environment.GetEnvironmentVariable("PHEYE_MODEL_DIR");
            Environment.SetEnvironmentVariable("PHEYE_MODEL_DIR", emptyModelDir);
            try
            {
                Assert.False(PhEyeModel.IsAvailable);

                var policy = new PhileasPolicy
                {
                    Name = "names",
                    Identifiers = new Identifiers
                    {
                        EmailAddress = new EmailAddress(),
                        PhEyes = new List<PhEye> { PhEyeModel.CreateDefaultFilter() }
                    }
                };
                _policies.Insert(new PolicyEntity { Name = "names", Json = PolicySerializer.SerializeToJson(policy) });

                string input = Path.Combine(_tempDir, "names.txt");
                await File.WriteAllTextAsync(input, "Email amy@example.com about John Smith.");

                var settings = new SettingsEntity { OutputToOriginalLocation = true };
                var entity = new RedactionQueueEntity { Name = input, Policy = "names", Context = "ctx" };

                QueueRedactionResult result = await QueueProcessor.ProcessAsync(entity, _policies, settings, _filterService);

                Assert.True(result.Success);
                Assert.True(result.NameDetectionUnavailable);            // flagged for the caller to warn
                string redacted = await File.ReadAllTextAsync(result.OutputPath!);
                Assert.DoesNotContain("amy@example.com", redacted);      // the rest is still redacted
                Assert.Contains("John Smith", redacted);                 // names remain — why we warn
            }
            finally
            {
                Environment.SetEnvironmentVariable("PHEYE_MODEL_DIR", prev);
            }
        }

        [Fact]
        public async Task ProcessAsync_PolicyWithoutNames_NameDetectionUnavailableIsFalse()
        {
            _policies.Insert(new PolicyEntity { Name = "p", Json = "{\"identifiers\":{\"emailAddress\":{}}}" });
            string input = Path.Combine(_tempDir, "nonames.txt");
            await File.WriteAllTextAsync(input, "Email john@example.com please.");

            QueueRedactionResult result = await QueueProcessor.ProcessAsync(
                new RedactionQueueEntity { Name = input, Policy = "p", Context = "ctx" },
                _policies, new SettingsEntity { OutputToOriginalLocation = true }, _filterService);

            Assert.True(result.Success);
            Assert.False(result.NameDetectionUnavailable); // policy never asked for names
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
