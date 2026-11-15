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
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using PhilterDesktop;
using Xunit;
using CaseResult = PhilterDesktop.SelfTest.CaseResult;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop.Tests
{
    /// <summary>Tests that replace the process-wide console output; they must not overlap each other.</summary>
    [CollectionDefinition(Name, DisableParallelization = true)]
    public sealed class ConsoleOutputCollection
    {
        public const string Name = "Console output";
    }

    /// <summary>
    /// <c>--selftest</c> redacts a built-in corpus in every format and checks it (exit 0 = nothing failed).
    /// Where the on-device name model is bundled it also exercises name detection through the ONNX runtime.
    /// A redacted PDF has no text to re-scan, so PDF is reported as not checked rather than passed.
    /// </summary>
    [Collection(ConsoleOutputCollection.Name)]
    public sealed class SelfTestTests : IDisposable
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(), "selftest-" + Guid.NewGuid().ToString("N"));

        public SelfTestTests() => Directory.CreateDirectory(_dir);

        public void Dispose()
        {
            try { Directory.Delete(_dir, recursive: true); } catch { }
        }

        [Fact]
        public void Run_RedactsBuiltInCorpus_ReturnsZero()
        {
            // End to end: email/ssn always; +ONNX name detection wherever the model is bundled.
            (int code, string output) = CaptureRun();

            Assert.Equal(0, code);
            foreach (string format in new[] { "txt", "csv", "rtf", "eml", "docx", "xlsx" })
            {
                Assert.Contains($"  PASS  {format}" + Environment.NewLine, output);
            }
        }

        [Fact]
        public void Run_ReportsPdfAsNotChecked_NotPassed()
        {
            (_, string output) = CaptureRun();

            Assert.Contains("  NOT CHECKED  pdf (", output);
            Assert.DoesNotContain("PASS  pdf", output);
            Assert.Contains("Result: PASS (6/6, pdf not checked)", output);
        }

        // ---- The PDF case ----

        [Fact]
        public void PdfCase_RedactsAndIsNotChecked() =>
            Assert.Equal(CaseResult.NotChecked, RunCase("pdf", Generator("WritePdf")));

        [Fact]
        public void PdfCase_FailsWhenThePlantedPiiIsNotInTheInput()
        {
            // Still a real check: an input with nothing detectable fails rather than reporting not checked.
            Assert.Equal(CaseResult.Failed, RunCase("pdf", path =>
            {
                File.WriteAllBytes(path, MinimalPdf.PlainText("Nothing sensitive here."));
                return path;
            }));
        }

        [Fact]
        public void PdfCase_FailsWhenTheInputIsntAReadablePdf() =>
            Assert.Equal(CaseResult.Failed, RunCase("pdf", path =>
            {
                File.WriteAllText(path, "not a pdf, but george@fake.com 123-45-6789");
                return path;
            }));

        [Fact]
        public void TextCase_StillPasses() =>
            Assert.Equal(CaseResult.Passed, RunCase("txt", Generator("WriteTxt")));

        // ---- How a re-scan counts ----

        [Theory]
        [InlineData("Clean", true, "Passed")]
        [InlineData("Clean", false, "NotChecked")]       // a clean PDF proves nothing
        [InlineData("ResidualsFound", true, "Failed")]
        [InlineData("ResidualsFound", false, "Failed")]  // PII in a PDF: a leaked text layer
        [InlineData("Error", true, "Failed")]
        [InlineData("Error", false, "Failed")]
        public void Classify_OnlyAFindingOrErrorFails_AndOnlyACheckableCleanPasses(string status, bool canCheck, string expected) =>
            Assert.Equal(Enum.Parse<CaseResult>(expected),
                SelfTest.Classify(new VerificationOutcome { Status = Enum.Parse<VerificationStatus>(status) }, canCheck));

        [Fact]
        public void ARedactedPdf_IsStillReScanned_SoALeakWouldShow()
        {
            // The PDF case must run the re-scan, not skip it: Classify's leak rule only helps if it's reached.
            string source = File.ReadAllText(RepoFile("PhilterDesktop", "SelfTest.cs"));
            int skip = source.IndexOf("if (!canCheck && !IsPdf(output))", StringComparison.Ordinal);
            int scan = source.IndexOf("RedactionVerifier.Verify(output", StringComparison.Ordinal);

            Assert.True(skip > 0 && scan > skip);
            Assert.DoesNotContain("return CaseResult.NotChecked", source);
        }

        [Theory]
        [InlineData("%PDF-1.7\n...", true)]
        [InlineData("%PDF", false)]
        [InlineData("plain text", false)]
        [InlineData(null, false)] // no file
        public void IsPdf_ChecksTheHeader(string? content, bool expected)
        {
            string path = Path.Combine(_dir, Guid.NewGuid().ToString("N") + ".pdf");
            if (content is not null)
            {
                File.WriteAllText(path, content);
            }

            bool actual = (bool)typeof(SelfTest).GetMethod("IsPdf", BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, new object[] { path })!;
            Assert.Equal(expected, actual);
        }

        // ---- The summary line ----

        [Fact]
        public void ResultLine_NotCheckedFormats_DontCountOrFail() =>
            Assert.Equal("Result: PASS (6/6, pdf not checked) incl. name detection",
                SelfTest.ResultLine(Results(6, failed: 0, notChecked: true), nameModel: true));

        [Fact]
        public void ResultLine_AFailureFails() =>
            Assert.Equal("Result: FAIL (5/6, pdf not checked) incl. name detection",
                SelfTest.ResultLine(Results(5, failed: 1, notChecked: true), nameModel: true));

        [Fact]
        public void ResultLine_WithoutNotCheckedFormats_IsAsBefore() =>
            Assert.Equal("Result: PASS (7/7), name detection NOT tested",
                SelfTest.ResultLine(Results(7, failed: 0, notChecked: false), nameModel: false));

        // ---- Helpers ----

        private static List<(string, CaseResult)> Results(int passed, int failed, bool notChecked)
        {
            var results = Enumerable.Range(0, passed).Select(i => ("f" + i, CaseResult.Passed))
                .Concat(Enumerable.Range(0, failed).Select(i => ("x" + i, CaseResult.Failed)))
                .ToList();
            if (notChecked)
            {
                results.Add(("pdf", CaseResult.NotChecked));
            }
            return results;
        }

        private static (int Code, string Output) CaptureRun()
        {
            TextWriter original = Console.Out;
            var captured = new StringWriter();
            Console.SetOut(captured);
            try
            {
                return (SelfTest.Run(), captured.ToString());
            }
            finally
            {
                Console.SetOut(original);
            }
        }

        private CaseResult RunCase(string label, Func<string, string> generate)
        {
            // The self-test's own policy, minus name detection so these don't need the model.
            var policy = new PhileasPolicy { Name = "selftest", Identifiers = new Identifiers { EmailAddress = new EmailAddress(), Ssn = new Ssn() } };
            TextWriter original = Console.Out;
            Console.SetOut(TextWriter.Null);
            try
            {
                return SelfTest.RunCase(label, generate, _dir, policy, new FilterService(), requireInputName: null);
            }
            finally
            {
                Console.SetOut(original);
            }
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

        private static Func<string, string> Generator(string name)
        {
            MethodInfo method = typeof(SelfTest).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
            return path => (string)method.Invoke(null, new object[] { path })!;
        }
    }
}
