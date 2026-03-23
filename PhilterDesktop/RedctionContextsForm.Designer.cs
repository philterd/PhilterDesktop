namespace PhilterDesktop
{
    partial class RedctionContextsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            listViewContexts = new ListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderEntries = new ColumnHeader();
            btnCreate = new Button();
            btnDelete = new Button();
            btnEmpty = new Button();
            btnClose = new Button();
            lblContexts = new Label();
            SuspendLayout();
            // 
            // listViewContexts
            // 
            listViewContexts.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderEntries });
            listViewContexts.FullRowSelect = true;
            listViewContexts.GridLines = true;
            listViewContexts.Location = new Point(17, 53);
            listViewContexts.Margin = new Padding(4, 5, 4, 5);
            listViewContexts.MultiSelect = false;
            listViewContexts.Name = "listViewContexts";
            listViewContexts.Size = new Size(748, 339);
            listViewContexts.TabIndex = 0;
            listViewContexts.UseCompatibleStateImageBehavior = false;
            listViewContexts.View = View.Details;
            listViewContexts.SelectedIndexChanged += ListViewContexts_SelectedIndexChanged;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "Context Name";
            columnHeaderName.Width = 350;
            // 
            // columnHeaderEntries
            // 
            columnHeaderEntries.Text = "Entries";
            columnHeaderEntries.Width = 200;
            // 
            // btnCreate
            // 
            btnCreate.Location = new Point(797, 53);
            btnCreate.Margin = new Padding(4, 5, 4, 5);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new Size(172, 47);
            btnCreate.TabIndex = 1;
            btnCreate.Text = "New Context...";
            btnCreate.UseVisualStyleBackColor = true;
            btnCreate.Click += BtnCreate_Click;
            // 
            // btnDelete
            // 
            btnDelete.Enabled = false;
            btnDelete.Location = new Point(797, 113);
            btnDelete.Margin = new Padding(4, 5, 4, 5);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(172, 47);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
            // 
            // btnEmpty
            // 
            btnEmpty.Enabled = false;
            btnEmpty.Location = new Point(797, 173);
            btnEmpty.Margin = new Padding(4, 5, 4, 5);
            btnEmpty.Name = "btnEmpty";
            btnEmpty.Size = new Size(172, 47);
            btnEmpty.TabIndex = 3;
            btnEmpty.Text = "Empty";
            btnEmpty.UseVisualStyleBackColor = true;
            btnEmpty.Click += BtnEmpty_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(797, 345);
            btnClose.Margin = new Padding(4, 5, 4, 5);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(172, 47);
            btnClose.TabIndex = 4;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            // 
            // lblContexts
            // 
            lblContexts.AutoSize = true;
            lblContexts.Location = new Point(17, 15);
            lblContexts.Margin = new Padding(4, 0, 4, 0);
            lblContexts.Name = "lblContexts";
            lblContexts.Size = new Size(168, 25);
            lblContexts.TabIndex = 5;
            lblContexts.Text = "Redaction Contexts:";
            // 
            // RedctionContextsForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(994, 420);
            Controls.Add(lblContexts);
            Controls.Add(btnClose);
            Controls.Add(btnEmpty);
            Controls.Add(btnDelete);
            Controls.Add(btnCreate);
            Controls.Add(listViewContexts);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "RedctionContextsForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Redaction Contexts";
            Load += RedctionContextsForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListView listViewContexts;
        private ColumnHeader columnHeaderName;
        private ColumnHeader columnHeaderEntries;
        private Button btnCreate;
        private Button btnDelete;
        private Button btnEmpty;
        private Button btnClose;
        private Label lblContexts;
    }
}