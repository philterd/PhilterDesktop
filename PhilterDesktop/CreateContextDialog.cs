namespace PhilterDesktop
{
    public partial class CreateContextDialog : Form
    {
        public string ContextName => txtInput.Text.Trim();

        public CreateContextDialog()
        {
            InitializeComponent();
            ModernTheme.Apply(this);
            ModernTheme.MakePrimary(btnOk);
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }
    }
}
