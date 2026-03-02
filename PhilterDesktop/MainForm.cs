using LiteDB;
using Phileas.Policy;
using Phileas.Policy.Filters;
using Phileas.Services;
using Philter;
using PhilterData;
using PhileasPolicy = Phileas.Policy.Policy;

namespace PhilterDesktop
{
    public partial class MainForm : Form
    {
        private readonly LiteDatabase _database;
        private readonly PolicyRepository _policyRepository;
        private readonly ContextRepository _contextRepository;
        private readonly ContextEntryRepository _contextEntryRepository;
        public MainForm()
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

            // Create a single shared database instance
            _database = new LiteDatabase(dbPath);

            // Pass the shared database to all repositories
            _policyRepository = new PolicyRepository(_database);
            _contextRepository = new ContextRepository(_database);
            _contextEntryRepository = new ContextEntryRepository(_database);

            // Insert default policy.
            // TODO

            // Insert default context.
            if (_contextRepository.FindByName("default") == null)
            {
                ContextEntity contextEntity = new ContextEntity
                {
                    Name = "default"
                };
                _contextRepository.Insert(contextEntity);
            }

        }

        private void button1_Click(object sender, EventArgs e)
        {


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
            var f = new PolicyEditorForm(_policyRepository);
            f.ShowDialog();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.CheckFileExists = true;
            openFileDialog1.CheckPathExists = true;
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "Text Files (*.txt)|*.txt|PDF Documents (*.pdf)|*.pdf|Microsoft Word Documents (*.docx)|*.docx";
            openFileDialog1.FilterIndex = 0;
            openFileDialog1.FileName = "";
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (string s in openFileDialog1.FileNames)
                {
                    filesListBox.Items.Add(s);
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
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

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void comboBox1_DropDown(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (PolicyEntity p in _policyRepository.GetAll())
            {
                comboBox1.Items.Add(p.Name);
            }
        }

        private void contextsComboBox_DropDown(object sender, EventArgs e)
        {
            contextsComboBox.Items.Clear();
            foreach (ContextEntity p in _contextRepository.GetAll())
            {
                contextsComboBox.Items.Add(p.Name);
            }
        }

        private void redactionContextsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var redactionContextsForm = new RedctionContextsForm(_contextRepository, _contextEntryRepository);
            redactionContextsForm.ShowDialog();
        }
    }

}
