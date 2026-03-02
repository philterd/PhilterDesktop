namespace PhilterDesktop
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            toolsToolStripMenuItem = new ToolStripMenuItem();
            policiesToolStripMenuItem = new ToolStripMenuItem();
            redactionContextsToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem = new ToolStripMenuItem();
            helpToolStripMenuItem1 = new ToolStripMenuItem();
            toolStripSeparator1 = new ToolStripSeparator();
            aboutToolStripMenuItem = new ToolStripMenuItem();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            listView1 = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            panel1 = new Panel();
            panel2 = new Panel();
            filesListBox = new ListBox();
            button3 = new Button();
            label3 = new Label();
            button2 = new Button();
            contextsComboBox = new ComboBox();
            label2 = new Label();
            comboBox1 = new ComboBox();
            label1 = new Label();
            openFileDialog1 = new OpenFileDialog();
            statusStrip1 = new StatusStrip();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            panel1.SuspendLayout();
            panel2.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, toolsToolStripMenuItem, helpToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(4, 1, 0, 1);
            menuStrip1.Size = new Size(539, 24);
            menuStrip1.TabIndex = 1;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(37, 22);
            fileToolStripMenuItem.Text = "File";
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(92, 22);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // toolsToolStripMenuItem
            // 
            toolsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { policiesToolStripMenuItem, redactionContextsToolStripMenuItem });
            toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            toolsToolStripMenuItem.Size = new Size(47, 22);
            toolsToolStripMenuItem.Text = "Tools";
            // 
            // policiesToolStripMenuItem
            // 
            policiesToolStripMenuItem.Name = "policiesToolStripMenuItem";
            policiesToolStripMenuItem.Size = new Size(185, 22);
            policiesToolStripMenuItem.Text = "Redaction Policies...";
            policiesToolStripMenuItem.Click += policiesToolStripMenuItem_Click;
            // 
            // redactionContextsToolStripMenuItem
            // 
            redactionContextsToolStripMenuItem.Name = "redactionContextsToolStripMenuItem";
            redactionContextsToolStripMenuItem.Size = new Size(185, 22);
            redactionContextsToolStripMenuItem.Text = "Redaction Contexts...";
            redactionContextsToolStripMenuItem.Click += redactionContextsToolStripMenuItem_Click;
            // 
            // helpToolStripMenuItem
            // 
            helpToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripMenuItem1, toolStripSeparator1, aboutToolStripMenuItem });
            helpToolStripMenuItem.Name = "helpToolStripMenuItem";
            helpToolStripMenuItem.Size = new Size(44, 22);
            helpToolStripMenuItem.Text = "Help";
            // 
            // helpToolStripMenuItem1
            // 
            helpToolStripMenuItem1.Name = "helpToolStripMenuItem1";
            helpToolStripMenuItem1.Size = new Size(116, 22);
            helpToolStripMenuItem1.Text = "Help";
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(113, 6);
            // 
            // aboutToolStripMenuItem
            // 
            aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            aboutToolStripMenuItem.Size = new Size(116, 22);
            aboutToolStripMenuItem.Text = "About...";
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 277);
            tabControl1.Margin = new Padding(2);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(539, 329);
            tabControl1.TabIndex = 2;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(listView1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Margin = new Padding(2);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(2);
            tabPage1.Size = new Size(531, 301);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Redacted Files";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // listView1
            // 
            listView1.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2 });
            listView1.Dock = DockStyle.Fill;
            listView1.Location = new Point(2, 2);
            listView1.Margin = new Padding(2);
            listView1.Name = "listView1";
            listView1.Size = new Size(527, 297);
            listView1.TabIndex = 3;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "File Name";
            columnHeader1.Width = 600;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Status";
            columnHeader2.Width = 180;
            // 
            // panel1
            // 
            panel1.Controls.Add(panel2);
            panel1.Controls.Add(button2);
            panel1.Controls.Add(contextsComboBox);
            panel1.Controls.Add(label2);
            panel1.Controls.Add(comboBox1);
            panel1.Controls.Add(label1);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 24);
            panel1.Margin = new Padding(2);
            panel1.Name = "panel1";
            panel1.Size = new Size(539, 253);
            panel1.TabIndex = 0;
            // 
            // panel2
            // 
            panel2.Controls.Add(filesListBox);
            panel2.Controls.Add(button3);
            panel2.Controls.Add(label3);
            panel2.Location = new Point(20, 22);
            panel2.Margin = new Padding(2);
            panel2.Name = "panel2";
            panel2.Size = new Size(495, 122);
            panel2.TabIndex = 4;
            // 
            // filesListBox
            // 
            filesListBox.FormattingEnabled = true;
            filesListBox.Location = new Point(12, 49);
            filesListBox.Name = "filesListBox";
            filesListBox.Size = new Size(464, 49);
            filesListBox.TabIndex = 2;
            // 
            // button3
            // 
            button3.Location = new Point(12, 15);
            button3.Margin = new Padding(2);
            button3.Name = "button3";
            button3.Size = new Size(160, 23);
            button3.TabIndex = 1;
            button3.Text = "Select files to redact...";
            button3.UseVisualStyleBackColor = true;
            button3.Click += button3_Click;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(176, 18);
            label3.Margin = new Padding(2, 0, 2, 0);
            label3.Name = "label3";
            label3.Size = new Size(146, 15);
            label3.TabIndex = 0;
            label3.Text = "or drop files here to redact";
            // 
            // button2
            // 
            button2.Anchor = AnchorStyles.None;
            button2.Location = new Point(117, 218);
            button2.Margin = new Padding(2);
            button2.Name = "button2";
            button2.Size = new Size(118, 23);
            button2.TabIndex = 3;
            button2.Text = "Start Redaction";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // contextsComboBox
            // 
            contextsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            contextsComboBox.FormattingEnabled = true;
            contextsComboBox.Location = new Point(117, 191);
            contextsComboBox.Margin = new Padding(2);
            contextsComboBox.Name = "contextsComboBox";
            contextsComboBox.Size = new Size(398, 23);
            contextsComboBox.TabIndex = 3;
            contextsComboBox.DropDown += contextsComboBox_DropDown;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(20, 194);
            label2.Margin = new Padding(2, 0, 2, 0);
            label2.Name = "label2";
            label2.Size = new Size(93, 15);
            label2.TabIndex = 2;
            label2.Text = "Use this context:";
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(117, 164);
            comboBox1.Margin = new Padding(2);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(398, 23);
            comboBox1.TabIndex = 1;
            comboBox1.DropDown += comboBox1_DropDown;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(20, 167);
            label1.Margin = new Padding(2, 0, 2, 0);
            label1.Name = "label1";
            label1.Size = new Size(86, 15);
            label1.TabIndex = 0;
            label1.Text = "Use this policy:";
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            // 
            // statusStrip1
            // 
            statusStrip1.Location = new Point(0, 606);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(539, 22);
            statusStrip1.TabIndex = 3;
            statusStrip1.Text = "statusStrip1";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(539, 628);
            Controls.Add(tabControl1);
            Controls.Add(panel1);
            Controls.Add(menuStrip1);
            Controls.Add(statusStrip1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MainMenuStrip = menuStrip1;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Philter Desktop";
            Load += Form1_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem;
        private ToolStripMenuItem helpToolStripMenuItem1;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripMenuItem aboutToolStripMenuItem;
        private ToolStripMenuItem toolsToolStripMenuItem;
        private ToolStripMenuItem policiesToolStripMenuItem;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private Panel panel1;
        private Panel panel2;
        private Button button3;
        private Label label3;
        private Button button2;
        private ComboBox contextsComboBox;
        private Label label2;
        private ComboBox comboBox1;
        private Label label1;
        private ListView listView1;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private OpenFileDialog openFileDialog1;
        private ListBox filesListBox;
        private StatusStrip statusStrip1;
        private ToolStripMenuItem redactionContextsToolStripMenuItem;
    }
}
