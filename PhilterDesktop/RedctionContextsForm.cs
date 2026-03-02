using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using PhilterData;

namespace PhilterDesktop
{
    public partial class RedctionContextsForm : Form
    {
        private ContextRepository _repo;

        public RedctionContextsForm(ContextRepository repo)
        {
            InitializeComponent();
            _repo = repo;
        }

        private void RedctionContextsForm_Load(object sender, EventArgs e)
        {

        }
    }
}
