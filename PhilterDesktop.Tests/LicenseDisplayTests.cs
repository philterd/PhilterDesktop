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

using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Xunit;

namespace PhilterDesktop.Tests
{
    /// <summary>
    /// How licenses reach the user: the acceptance screen shows only the EULA (the one agreement accepted)
    /// with a link to the Apache License; About's View licenses shows both texts; LICENSE and NOTICE ship next to
    /// the exe, and the About box links the notices.
    /// </summary>
    public sealed class LicenseDisplayTests
    {
        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        // --- Acceptance screen -----------------------------------------------------

        [Fact]
        public void Gate_ShowsTheEulaButNotTheApacheText() => Sta(() =>
        {
            using var f = new LicenseForm();
            _ = f.Handle;
            Assert.False(f.ShowsApacheText);
            Assert.Equal(string.Empty, Field<TextBox>(f, "_licenseBody").Text);
            Assert.False(string.IsNullOrWhiteSpace(Field<TextBox>(f, "_eulaBody").Text));
        });

        // The hidden Apache row must not keep its half of the height; the EULA pane takes it.
        [Fact]
        public void Gate_GivesTheApacheRowsHeightToTheEula() => Sta(() =>
        {
            using var f = new LicenseForm();
            var body = Field<TableLayoutPanel>(f, "_body");
            Assert.Equal(SizeType.Absolute, body.RowStyles[1].SizeType);
            Assert.Equal(0F, body.RowStyles[1].Height);
            Assert.Equal(SizeType.Percent, body.RowStyles[3].SizeType); // the EULA row still stretches
        });

        // The header names the agreement; there's no separate heading, and no "updated" notice.
        [Fact]
        public void Gate_HeaderNamesTheAgreement_WithoutAnUpdatedNotice() => Sta(() =>
        {
            using var f = new LicenseForm();
            _ = f.Handle;
            Assert.Equal(LicenseForm.GateTitle, Field<Label>(f, "_title").Text);
            Assert.Equal(LicenseForm.GateSubtitle, Field<Label>(f, "_subtitle").Text);
            Assert.False(Field<Label>(f, "_eulaHeading").Visible);
            Assert.False(Field<Label>(f, "_licenseHeading").Visible);
            foreach (Control c in All(f))
            {
                Assert.DoesNotContain("updated", c is TextBox ? string.Empty : c.Text, StringComparison.OrdinalIgnoreCase);
            }
        });

        // The header band, hairlines and borderless reading pane survive the theme (which repaints panels).
        [Fact]
        public void Chrome_IsStyledAfterTheTheme() => Sta(() =>
        {
            using var f = new LicenseForm();
            Assert.Equal(ModernTheme.Surface, Field<TableLayoutPanel>(f, "_header").BackColor);
            Assert.Equal(ModernTheme.Border, Field<Panel>(f, "_headerDivider").BackColor);
            Assert.Equal(ModernTheme.Border, Field<Panel>(f, "_footerDivider").BackColor);
            Assert.Equal(ModernTheme.Border, Field<Panel>(f, "_eulaFrame").BackColor);
            TextBox eula = Field<TextBox>(f, "_eulaBody");
            Assert.Equal(BorderStyle.None, eula.BorderStyle);
            Assert.Equal(ModernTheme.Surface, eula.BackColor);
            Assert.NotNull(Field<PictureBox>(f, "_headerIcon").Image);
        });

        [Fact]
        public void Gate_HasAgreeAndDisagree_WithAgreeAsDefault() => Sta(() =>
        {
            using var f = new LicenseForm();
            Button agree = Field<Button>(f, "_agree");
            Button disagree = Field<Button>(f, "_disagree");
            Assert.Same(agree, f.AcceptButton);
            Assert.Same(disagree, f.CancelButton);
            Assert.Equal(DialogResult.OK, agree.DialogResult);
            Assert.Equal(DialogResult.Cancel, disagree.DialogResult);
            Assert.Equal("I &Agree", agree.Text);
            Assert.Equal("I &Disagree", disagree.Text);
        });

        // Shrunk to its minimum size, neither mode overlaps or clips (the panes give up height first).
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void AtMinimumSize_LaysOutCleanly(bool viewOnly) => Sta(() =>
        {
            using var f = new LicenseForm(viewOnly) { StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
            f.Show();
            f.Size = f.MinimumSize;
            Application.DoEvents();
            Assert.Equal(f.MinimumSize, f.Size);
            LayoutAudit.AssertClean(LayoutAudit.Audit(f));
            Assert.True(Field<Panel>(f, "_eulaFrame").Height > 0, "the agreement pane collapsed");
            f.Close();
        });

        // Compact: wider than tall. (Sizes are design units that scale with the theme font and DPI.)
        [Fact]
        public void DefaultSize_IsCompact() => Sta(() =>
        {
            using var f = new LicenseForm();
            Assert.True(f.ClientSize.Height < f.ClientSize.Width, $"client size {f.ClientSize}");
            Assert.True(f.MinimumSize.Height < f.Height, "the minimum must leave room to shrink");
        });

        [Fact]
        public void HeaderText_HasNoEmDashes()
        {
            foreach (string s in new[] { LicenseForm.GateTitle, LicenseForm.GateSubtitle, LicenseForm.ViewTitle, LicenseForm.ViewSubtitle })
            {
                Assert.DoesNotContain(EmDash, s);
            }
        }

        [Fact]
        public void Gate_LinksTheApacheLicense() => Sta(() =>
        {
            using var f = new LicenseForm();
            LinkLabel link = Field<LinkLabel>(f, "_apacheLink");
            Assert.Equal(LicenseForm.ApacheLinkText, link.Text);
            // Only the license name is the clickable part.
            string linked = link.Text.Substring(link.LinkArea.Start, link.LinkArea.Length);
            Assert.Equal("Apache License 2.0", linked);
        });

        [Fact]
        public void ApacheLicenseUrl_IsTheRepositoryLicense()
        {
            Assert.Equal("https://github.com/philterd/PhilterDesktop/blob/main/LICENSE", LicenseForm.ApacheLicenseUrl);
            Assert.StartsWith("https://", LicenseForm.ApacheLicenseUrl);
        }

        [Fact]
        public void ApacheLinkText_HasNoEmDashes() =>
            Assert.DoesNotContain(EmDash, LicenseForm.ApacheLinkText);

        private const char EmDash = '—';

        // --- View licenses (from About) ---------------------------------------------

        [Fact]
        public void ViewOnly_ShowsBothTexts_AndKeepsTheirRows() => Sta(() =>
        {
            using var f = new LicenseForm(viewOnly: true);
            _ = f.Handle;
            Assert.True(f.ShowsApacheText);
            Assert.Contains("Apache License", Field<TextBox>(f, "_licenseBody").Text);
            Assert.Equal(SizeType.Percent, Field<TableLayoutPanel>(f, "_body").RowStyles[1].SizeType);
            Assert.Equal(LicenseForm.ViewTitle, Field<Label>(f, "_title").Text);
            Assert.Equal("&Close", Field<Button>(f, "_agree").Text);
        });

        // --- Files shipped next to the exe ---------------------------------------------

        [Theory]
        [InlineData("LICENSE", "LICENSE.txt")]
        [InlineData("NOTICE", "NOTICE.txt")]
        [InlineData("THIRD-PARTY-LICENSES.txt", "THIRD-PARTY-LICENSES.txt")]
        public void Project_CopiesLicenseFilesToTheOutput(string source, string link)
        {
            string csproj = File.ReadAllText(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"));
            Match item = Regex.Match(csproj,
                $@"<None Include=""\.\.\\{source}"" Link=""{Regex.Escape(link)}"">\s*<CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>");
            Assert.True(item.Success, $"{source} must be copied to the output as {link}");
        }

        // The copies next to the binaries match the repo's files (the build copies, never edits, them).
        [SkippableTheory]
        [InlineData("LICENSE", "LICENSE.txt")]
        [InlineData("NOTICE", "NOTICE.txt")]
        [InlineData("THIRD-PARTY-LICENSES.txt", "THIRD-PARTY-LICENSES.txt")]
        public void OutputCopies_MatchTheRepoFiles(string source, string copy)
        {
            string output = Path.Combine(AppContext.BaseDirectory, copy);
            Skip.IfNot(File.Exists(output), $"{copy} not copied next to the test binaries");
            Assert.Equal(File.ReadAllText(RepoFile(source)), File.ReadAllText(output));
        }

        [Fact]
        public void NoticeFile_ListsThirdPartyComponents()
        {
            string notice = File.ReadAllText(RepoFile("NOTICE"));
            Assert.Contains("Third-party", notice, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("License:", notice);
        }

        // Packages used only at build time; nothing from them ships, so they need no notice.
        private static readonly string[] BuildOnlyPackages =
        {
            "Microsoft.Windows.SDK.BuildTools", "Microsoft.Windows.SDK.BuildTools.MSIX"
        };

        // NOTICE must name every package the app restores, at the restored version, so an upgrade or a new
        // dependency can't ship without its attribution.
        [SkippableFact]
        public void Notice_CoversEveryRestoredPackage()
        {
            string assets = Path.Combine(Path.GetDirectoryName(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"))!, "obj", "project.assets.json");
            Skip.IfNot(File.Exists(assets), "project.assets.json not found (restore the app project first)");
            string notice = File.ReadAllText(RepoFile("NOTICE"));

            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(assets));
            var missing = new List<string>();
            foreach (var lib in doc.RootElement.GetProperty("libraries").EnumerateObject())
            {
                if (lib.Value.GetProperty("type").GetString() != "package")
                {
                    continue;
                }
                string[] parts = lib.Name.Split('/');
                string name = parts[0], version = parts[1];
                if (BuildOnlyPackages.Contains(name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }
                // Grouped entries list sub-packages as ".Suffix version" under their family.
                string suffix = name.Contains('.') ? name[name.LastIndexOf('.')..] : name;
                bool listed = Regex.IsMatch(notice, $@"(?m)^{Regex.Escape(name)} {Regex.Escape(version)}\b")
                    || Regex.IsMatch(notice, $@"{Regex.Escape(suffix)} {Regex.Escape(version)}\b");
                if (!listed)
                {
                    missing.Add(lib.Name);
                }
            }
            Assert.True(missing.Count == 0, "NOTICE is missing (or has a stale version for): " + string.Join(", ", missing));
        }

        [Fact]
        public void Notice_AttributesTheBundledModel()
        {
            string notice = File.ReadAllText(RepoFile("NOTICE"));
            Assert.Contains("ph-eye-pii-en-xsmall", notice);
            Assert.Contains("CC-BY-4.0", notice);
            Assert.Contains("Nemotron-PII", notice);
        }

        // The installer build refuses to package without them.
        [Fact]
        public void InstallerBuild_RequiresLicenseAndNotice()
        {
            string script = File.ReadAllText(RepoFile("Installer", "build-setup.ps1"));
            Assert.Contains("foreach ($required in 'LICENSE.txt', 'NOTICE.txt', 'THIRD-PARTY-LICENSES.txt')", script);
            Assert.Contains(@"scripts\generate-third-party-licenses.ps1') -Check", script);
        }

        // The installer copies the whole publish folder, so the files ride along without their own entries.
        [Fact]
        public void Installer_CopiesThePublishFolder()
        {
            string iss = File.ReadAllText(RepoFile("Installer", "PhilterDesktop.iss"));
            Assert.Contains(@"Source: ""{#PublishDirX64}\*""; DestDir: ""{app}""", iss);
            Assert.DoesNotMatch(@"Excludes: ""[^""]*(LICENSE|NOTICE)", iss);
        }

        // --- THIRD-PARTY-LICENSES.txt --------------------------------------------------------

        // Every restored package has a section with its license, so a new or upgraded dependency can't
        // ship without its license text.
        [SkippableFact]
        public void Licenses_CoverEveryRestoredPackage()
        {
            var lines = new HashSet<string>(File.ReadAllLines(RepoFile("THIRD-PARTY-LICENSES.txt")));
            var missing = RestoredPackages()
                .Where(p => !lines.Contains($"{p.Name} {p.Version}"))
                .Select(p => $"{p.Name}/{p.Version}")
                .ToList();
            Assert.True(missing.Count == 0, "THIRD-PARTY-LICENSES.txt has no section for: " + string.Join(", ", missing));
        }

        // The MIT/BSD templates must have had their copyright line filled in.
        [Fact]
        public void Licenses_HaveNoUnfilledTemplates()
        {
            string licenses = File.ReadAllText(RepoFile("THIRD-PARTY-LICENSES.txt"));
            Assert.DoesNotMatch(@"(?m)^Copyright \(c\) <year> <(copyright holders|owner)>", licenses);
        }

        // Every license a section points to is printed in the shared texts, and the model's license is there.
        [Fact]
        public void Licenses_IncludeEverySharedTextTheyReference()
        {
            string licenses = File.ReadAllText(RepoFile("THIRD-PARTY-LICENSES.txt"));
            int shared = licenses.IndexOf("SHARED LICENSE TEXTS", StringComparison.Ordinal);
            Assert.True(shared > 0);
            foreach (Match m in Regex.Matches(licenses, @"Licensed under the (\S+) license; its full text is in the shared"))
            {
                Assert.Contains($"--- {m.Groups[1].Value} ---", licenses[shared..]);
            }
            Assert.Contains("--- Apache-2.0 ---", licenses[shared..]);
            Assert.Contains("PhEye person-name detection model (ph-eye-pii-en-xsmall)", licenses);
            Assert.Contains("Creative Commons Attribution 4.0 International", licenses);
        }

        // The committed file is exactly what the generator produces from the current dependencies.
        [SkippableFact]
        public void Licenses_MatchTheGenerator()
        {
            string assets = AssetsFile();
            Skip.IfNot(File.Exists(assets), "project.assets.json not found (restore the app project first)");
            string script = RepoFile("scripts", "generate-third-party-licenses.ps1");
            var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Check")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            string output = proc.StandardOutput.ReadToEnd() + proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            Assert.True(proc.ExitCode == 0, "Regenerate with scripts\\generate-third-party-licenses.ps1. " + output);
        }

        [Fact]
        public void LicenseTexts_AreVendoredWithTheirSource()
        {
            foreach (string id in new[] { "MIT", "BSD-3-Clause", "MS-PL", "MPL-2.0", "MPL-1.1", "CC-BY-4.0" })
            {
                Assert.True(new FileInfo(RepoFile("scripts", "license-texts", id + ".txt")).Length > 500, id);
            }
            Assert.Contains("v3.26.0", File.ReadAllText(RepoFile("scripts", "license-texts", "README.md")));
        }

        // Links use philterd.ai directly: www only redirects there, costing a hop.
        [Fact]
        public void AppLinks_UseTheBareDomain()
        {
            string app = Path.GetDirectoryName(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"))!;
            var offenders = Directory.EnumerateFiles(app, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar))
                .Where(f => File.ReadAllText(f).Contains("www.philterd.ai", StringComparison.OrdinalIgnoreCase))
                .Select(Path.GetFileName)
                .ToList();
            Assert.True(offenders.Count == 0, "www.philterd.ai in: " + string.Join(", ", offenders));
            Assert.DoesNotContain("www.philterd.ai", File.ReadAllText(RepoFile("Installer", "PhilterDesktop.iss")));
        }

        // --- About box -------------------------------------------------------------------

        [Fact]
        public void Notices_OpensTheInstalledFile_WhenPresent()
        {
            string dir = Directory.CreateTempSubdirectory("notices-").FullName;
            try
            {
                string file = Path.Combine(dir, AboutForm.NoticesFileName);
                File.WriteAllText(file, "notices");
                Assert.Equal(file, AboutForm.NoticesTarget(dir));
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void Notices_FallsBackToGitHub_WhenTheFileIsMissing()
        {
            string dir = Directory.CreateTempSubdirectory("notices-").FullName;
            try
            {
                Assert.Equal(AboutForm.NoticesUrl, AboutForm.NoticesTarget(dir));
                Assert.Equal("https://github.com/philterd/PhilterDesktop/blob/main/NOTICE", AboutForm.NoticesUrl);
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void Licenses_OpensTheInstalledFile_OrGitHub()
        {
            string dir = Directory.CreateTempSubdirectory("licenses-").FullName;
            try
            {
                Assert.Equal(AboutForm.LicensesUrl, AboutForm.LicensesTarget(dir));
                string file = Path.Combine(dir, AboutForm.LicensesFileName);
                File.WriteAllText(file, "licenses");
                Assert.Equal(file, AboutForm.LicensesTarget(dir));
            }
            finally { Directory.Delete(dir, recursive: true); }
        }

        [Fact]
        public void About_HasTheLicensesLink() => Sta(() =>
        {
            using var f = new AboutForm();
            _ = f.Handle;
            LinkLabel link = Field<LinkLabel>(f, "linkLicenses");
            Assert.Equal("Third-party licenses", link.Text);
            Assert.NotNull(link.Parent); // placed in the layout
        });

        [Fact]
        public void About_HasTheNoticesLink() => Sta(() =>
        {
            using var f = new AboutForm();
            _ = f.Handle;
            LinkLabel link = Field<LinkLabel>(f, "linkNotices");
            Assert.Equal("Third-party notices", link.Text);
            Assert.True(link.TabStop);
        });

        // No em-dashes in UI text, and no "&" that a label would swallow as a mnemonic marker.
        [Fact]
        public void About_TextHasNoEmDashesOrSwallowedAmpersands() => Sta(() =>
        {
            using var f = new AboutForm();
            foreach (Control c in All(f).Where(c => c is Label or ButtonBase))
            {
                Assert.DoesNotContain(EmDash, c.Text);
                bool mnemonics = c is Label l ? l.UseMnemonic : ((ButtonBase)c).UseMnemonic;
                if (mnemonics)
                {
                    Assert.DoesNotMatch(@"&(?!&)\s", c.Text); // "& " hides the ampersand
                }
            }
        });

        private static IEnumerable<Control> All(Control root) =>
            root.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(All(c)));

        // --- Helpers -----------------------------------------------------------------------

        private static string AssetsFile() =>
            Path.Combine(Path.GetDirectoryName(RepoFile("PhilterDesktop", "PhilterDesktop.csproj"))!, "obj", "project.assets.json");

        private static List<(string Name, string Version)> RestoredPackages()
        {
            string assets = AssetsFile();
            Skip.IfNot(File.Exists(assets), "project.assets.json not found (restore the app project first)");
            using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(assets));
            return doc.RootElement.GetProperty("libraries").EnumerateObject()
                .Where(l => l.Value.GetProperty("type").GetString() == "package")
                .Select(l => (Name: l.Name.Split('/')[0], Version: l.Name.Split('/')[1]))
                .Where(p => !BuildOnlyPackages.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }

        private static T Field<T>(object o, string name) => (T)o.GetType().GetField(name, Flags)!.GetValue(o)!;

        private static string RepoFile(params string[] parts)
        {
            for (DirectoryInfo? dir = new(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            {
                string candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
                // The .slnx marks the repo root; the .sln is gitignored, so CI checkouts don't have it.
                if (File.Exists(candidate) && File.Exists(Path.Combine(dir.FullName, "PhilterDesktop.slnx")))
                {
                    return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate " + Path.Combine(parts));
        }

        private static void Sta(Action action)
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();
        }
    }
}
