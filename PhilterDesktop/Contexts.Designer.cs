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

namespace PhilterDesktop
{
    partial class Contexts
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
            helpButton = new Button();
            SuspendLayout();
            // 
            // listViewContexts
            // 
            listViewContexts.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderEntries });
            listViewContexts.FullRowSelect = true;
            listViewContexts.GridLines = true;
            listViewContexts.Location = new Point(12, 32);
            listViewContexts.MultiSelect = false;
            listViewContexts.Name = "listViewContexts";
            listViewContexts.Size = new Size(525, 205);
            listViewContexts.TabIndex = 0;
            listViewContexts.UseCompatibleStateImageBehavior = false;
            listViewContexts.View = View.Details;
            listViewContexts.SelectedIndexChanged += ListViewContexts_SelectedIndexChanged;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "Context Name";
            columnHeaderName.Width = 250;
            // 
            // columnHeaderEntries
            // 
            columnHeaderEntries.Text = "Entries";
            columnHeaderEntries.Width = 200;
            // 
            // btnCreate
            // 
            btnCreate.Location = new Point(558, 32);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new Size(110, 34);
            btnCreate.TabIndex = 1;
            btnCreate.Text = "New Context";
            btnCreate.UseVisualStyleBackColor = true;
            btnCreate.Click += BtnCreate_Click;
            // 
            // btnDelete
            // 
            btnDelete.Enabled = false;
            btnDelete.Location = new Point(558, 74);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(110, 34);
            btnDelete.TabIndex = 2;
            btnDelete.Text = "Delete";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += BtnDelete_Click;
            // 
            // btnEmpty
            // 
            btnEmpty.Enabled = false;
            btnEmpty.Location = new Point(558, 116);
            btnEmpty.Name = "btnEmpty";
            btnEmpty.Size = new Size(110, 34);
            btnEmpty.TabIndex = 3;
            btnEmpty.Text = "Empty";
            btnEmpty.UseVisualStyleBackColor = true;
            btnEmpty.Click += BtnEmpty_Click;
            // 
            // btnClose
            // 
            btnClose.Location = new Point(558, 209);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(110, 34);
            btnClose.TabIndex = 4;
            btnClose.Text = "Close";
            btnClose.UseVisualStyleBackColor = true;
            btnClose.Click += BtnClose_Click;
            // 
            // lblContexts
            // 
            lblContexts.AutoSize = true;
            lblContexts.Location = new Point(12, 9);
            lblContexts.Name = "lblContexts";
            lblContexts.Size = new Size(112, 15);
            lblContexts.TabIndex = 5;
            lblContexts.Text = "Redaction Contexts:";
            // 
            // helpButton
            // 
            helpButton.Location = new Point(558, 158);
            helpButton.Name = "helpButton";
            helpButton.Size = new Size(110, 34);
            helpButton.TabIndex = 6;
            helpButton.Text = "Help";
            helpButton.UseVisualStyleBackColor = true;
            // 
            // Contexts
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(696, 252);
            Controls.Add(helpButton);
            Controls.Add(lblContexts);
            Controls.Add(btnClose);
            Controls.Add(btnEmpty);
            Controls.Add(btnDelete);
            Controls.Add(btnCreate);
            Controls.Add(listViewContexts);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Contexts";
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
        private Button helpButton;
    }
}