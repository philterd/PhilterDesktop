using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Philter;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    public partial class Form1 : Form
    {
        LiteDbRepository<FilterResult> _repo;

        public Form1()
        {
            InitializeComponent();

            string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // 2. Combine with your specific App Folder
            string folder = Path.Combine(root, "PhilterDesktop");
            string dbPath = Path.Combine(folder, "data.db");

            // 3. The Magic Step: Ensure the directory exists
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            _repo = new LiteDbRepository<FilterResult>(dbPath);

            //    repo.EnsureIndex(x => x.CreatedAt);

            //var record = new FilterResult { FilteredText = result.FilteredText };
            //repo.Insert(record);

            //var all = repo.GetAll(); // Read
            //repo.Update(record);    // Update
            //repo.Delete(record.Id); // Delete

        }

        private void button1_Click(object sender, EventArgs e)
        {

            var policy = new PhileasPolicy
            {
                Name = "my-policy",
                Identifiers = new Identifiers
                {
                    EmailAddress = new EmailAddress()
                }
            };

            var result = new FilterService().Filter(
                policy: policy,
                context: "default",
                piece: 0,
                input: "Contact john.doe@example.com for help."
            );

            System.Diagnostics.Debug.WriteLine(result.FilteredText);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void policiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var f = new PolicyEditorForm();
            f.ShowDialog();
        }
    }

    // Entity — LiteDB maps the Id property automatically
    public class FilterResult
    {
        public ObjectId Id { get; set; } = ObjectId.NewObjectId();
        public string FilteredText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
