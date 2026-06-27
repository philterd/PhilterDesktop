using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.ExceptionServices;
using LiteDB;
using PhilterData;
using PhilterDesktop.PolicyEditing;
using Xunit;
using Xunit.Abstractions;

namespace PhilterDesktop.Tests
{
    // TEMPORARY utility: renders the app's forms to PNG (off-screen, via DrawToBitmap on an STA thread)
    // into the repo's screenshots\ folder. Excludes MainForm, which opens the real user database.
    public sealed class _Screenshots
    {
        private const string OutDir = @"C:\work\philterd\code\PhilterDesktop\screenshots";
        private readonly ITestOutputHelper _out;
        public _Screenshots(ITestOutputHelper o) => _out = o;

        // Skipped by default: it writes PNGs to a hardcoded repo path and opens forms, so it must not
        // run in CI or the normal suite. Remove the Skip (or run with this filter) to regenerate the
        // screenshots\ folder after UI changes.
        [Fact(Skip = "Manual utility — regenerates screenshots\\*.png; un-skip to run")]
        public void Capture()
        {
            ExceptionDispatchInfo? captured = null;
            var thread = new Thread(() =>
            {
                string dbPath = Path.Combine(Path.GetTempPath(), "shots-" + Guid.NewGuid().ToString("N") + ".db");
                try
                {
                    Directory.CreateDirectory(OutDir);
                    using var db = new LiteDatabase(dbPath);
                    Seed(db, out var policies, out var contexts, out var queue, out var settingsRepo, out var watched, out var contextEntries);

                    // The set of forms to capture. This is intentionally limited to the most important
                    // forms; add/remove entries here to change what gets captured.

                    // Main window — the most important shot. Rendered against the seeded database (never
                    // the real user one) with a few queue rows so the list isn't empty.
                    queue.Insert(new RedactionQueueEntity { Name = @"C:\cases\intake-form.pdf", Status = "Completed", Policy = "default", Context = "default" });
                    queue.Insert(new RedactionQueueEntity { Name = @"C:\cases\medical-record.docx", Status = "Completed", Policy = "HIPAA Safe Harbor", Context = "Case-2024-001" });
                    queue.Insert(new RedactionQueueEntity { Name = @"C:\cases\account-ledger.xlsx", Status = "Completed", Policy = "Legal court filing", Context = "default" });
                    queue.Insert(new RedactionQueueEntity { Name = @"C:\cases\client-email.eml", Status = "Failed", Policy = "default", Context = "default", ErrorMessage = "File not found" });
                    Shot("main-form", new MainForm(db, startMinimized: false));

                    Shot("settings", new SettingsForm(settingsRepo, policies, contexts, watched));
                    Shot("settings-watched-folders", new SettingsForm(settingsRepo, policies, contexts, watched), "Watched Folders");
                    Shot("redact-documents", new RedactDocuments(policies, contexts, queue, loggingEnabled: false, settingsRepo));
                    Shot("contexts", new Contexts(contexts, contextEntries));
                    Shot("policy-wizard", new PolicyWizardForm(_ => false));
                    Shot("watched-folder", new WatchedFolderForm(policies, contexts));
                    Shot("policy-editor", new PolicyEditorForm(policies));
                }
                catch (Exception ex) { captured = ExceptionDispatchInfo.Capture(ex); }
                finally { try { File.Delete(dbPath); } catch { } }
            })
            { IsBackground = true };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            captured?.Throw();

            foreach (string f in Directory.GetFiles(OutDir, "*.png").OrderBy(f => f))
            {
                _out.WriteLine($"{Path.GetFileName(f)}: {new FileInfo(f).Length} bytes");
            }
        }

        private static void Seed(LiteDatabase db, out PolicyRepository policies, out ContextRepository contexts,
            out RedactionQueueRepository queue, out SettingsRepository settingsRepo, out WatchedFolderRepository watched,
            out ContextEntryRepository contextEntries)
        {
            policies = new PolicyRepository(db);
            contexts = new ContextRepository(db);
            queue = new RedactionQueueRepository(db);
            settingsRepo = new SettingsRepository(db);
            watched = new WatchedFolderRepository(db);
            contextEntries = new ContextEntryRepository(db);

            policies.Insert(new PolicyEntity { Name = "default", Json = DefaultPolicy.Json() });
            policies.Insert(new PolicyEntity { Name = "HIPAA Safe Harbor", Json = "{}" });
            policies.Insert(new PolicyEntity { Name = "Legal court filing", Json = "{}" });
            contexts.Insert(new ContextEntity { Name = "default" });
            contexts.Insert(new ContextEntity { Name = "Case-2024-001" });
            watched.Insert(new WatchedFolderEntity
            {
                FolderPath = @"C:\Intake\incoming",
                OutputFolder = @"C:\Intake\redacted",
                Policy = "default",
                Context = "default"
            });
        }

        private void Shot(string name, Form form, string? selectTabText = null)
        {
            using (form)
            {
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-32000, -32000);
                form.Show();
                Application.DoEvents();

                if (selectTabText is not null && FindTabControl(form) is { } tabs)
                {
                    foreach (TabPage page in tabs.TabPages)
                    {
                        if (page.Text.Contains(selectTabText, StringComparison.OrdinalIgnoreCase))
                        {
                            tabs.SelectedTab = page;
                            Application.DoEvents();
                            break;
                        }
                    }
                }

                int w = Math.Max(form.Width, 1);
                int h = Math.Max(form.Height, 1);
                using var bmp = new Bitmap(w, h);
                form.DrawToBitmap(bmp, new Rectangle(0, 0, w, h));
                bmp.Save(Path.Combine(OutDir, name + ".png"), ImageFormat.Png);
                form.Close();
            }
        }

        private static TabControl? FindTabControl(Control parent)
        {
            foreach (Control child in parent.Controls)
            {
                if (child is TabControl tc)
                {
                    return tc;
                }
                if (FindTabControl(child) is { } nested)
                {
                    return nested;
                }
            }
            return null;
        }
    }
}
