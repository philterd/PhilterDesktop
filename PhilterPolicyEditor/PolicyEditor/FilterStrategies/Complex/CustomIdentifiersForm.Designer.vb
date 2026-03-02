Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CustomIdentifiersForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.CustomIdentifiersListView = New System.Windows.Forms.ListView()
        Me.ColumnHeader1 = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColumnHeader2 = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.AddButton = New System.Windows.Forms.Button()
        Me.EditButton = New System.Windows.Forms.Button()
        Me.RemoveButton = New System.Windows.Forms.Button()
        Me.OkButton = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(12, 20)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(339, 13)
        Me.Label1.TabIndex = 1
        Me.Label1.Text = "A custom identifer is defined by its label and regular expression pattern."
        '
        'CustomIdentifiersListView
        '
        Me.CustomIdentifiersListView.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColumnHeader1, Me.ColumnHeader2})
        Me.CustomIdentifiersListView.FullRowSelect = True
        Me.CustomIdentifiersListView.GridLines = True
        Me.CustomIdentifiersListView.HideSelection = False
        Me.CustomIdentifiersListView.Location = New System.Drawing.Point(15, 45)
        Me.CustomIdentifiersListView.Name = "CustomIdentifiersListView"
        Me.CustomIdentifiersListView.Size = New System.Drawing.Size(408, 223)
        Me.CustomIdentifiersListView.TabIndex = 2
        Me.CustomIdentifiersListView.UseCompatibleStateImageBehavior = False
        Me.CustomIdentifiersListView.View = System.Windows.Forms.View.Details
        '
        'ColumnHeader1
        '
        Me.ColumnHeader1.Text = "Label"
        Me.ColumnHeader1.Width = 123
        '
        'ColumnHeader2
        '
        Me.ColumnHeader2.Text = "Regular Expression Pattern"
        Me.ColumnHeader2.Width = 263
        '
        'AddButton
        '
        Me.AddButton.Location = New System.Drawing.Point(15, 277)
        Me.AddButton.Name = "AddButton"
        Me.AddButton.Size = New System.Drawing.Size(75, 23)
        Me.AddButton.TabIndex = 3
        Me.AddButton.Text = "Add..."
        Me.AddButton.UseVisualStyleBackColor = True
        '
        'EditButton
        '
        Me.EditButton.Enabled = False
        Me.EditButton.Location = New System.Drawing.Point(96, 277)
        Me.EditButton.Name = "EditButton"
        Me.EditButton.Size = New System.Drawing.Size(75, 23)
        Me.EditButton.TabIndex = 4
        Me.EditButton.Text = "Edit..."
        Me.EditButton.UseVisualStyleBackColor = True
        '
        'RemoveButton
        '
        Me.RemoveButton.Enabled = False
        Me.RemoveButton.Location = New System.Drawing.Point(177, 277)
        Me.RemoveButton.Name = "RemoveButton"
        Me.RemoveButton.Size = New System.Drawing.Size(75, 23)
        Me.RemoveButton.TabIndex = 5
        Me.RemoveButton.Text = "Remove..."
        Me.RemoveButton.UseVisualStyleBackColor = True
        '
        'OkButton
        '
        Me.OkButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.OkButton.Location = New System.Drawing.Point(348, 277)
        Me.OkButton.Name = "OkButton"
        Me.OkButton.Size = New System.Drawing.Size(75, 23)
        Me.OkButton.TabIndex = 6
        Me.OkButton.Text = "OK"
        Me.OkButton.UseVisualStyleBackColor = True
        '
        'CustomIdentifiersForm
        '
        Me.AcceptButton = Me.OkButton
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.OkButton
        Me.ClientSize = New System.Drawing.Size(435, 315)
        Me.Controls.Add(Me.OkButton)
        Me.Controls.Add(Me.RemoveButton)
        Me.Controls.Add(Me.EditButton)
        Me.Controls.Add(Me.AddButton)
        Me.Controls.Add(Me.CustomIdentifiersListView)
        Me.Controls.Add(Me.Label1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "CustomIdentifiersForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Custom Identifiers"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents Label1 As Label
    Friend WithEvents CustomIdentifiersListView As ListView
    Friend WithEvents ColumnHeader1 As ColumnHeader
    Friend WithEvents ColumnHeader2 As ColumnHeader
    Friend WithEvents AddButton As Button
    Friend WithEvents EditButton As Button
    Friend WithEvents RemoveButton As Button
    Friend WithEvents OkButton As Button
End Class
