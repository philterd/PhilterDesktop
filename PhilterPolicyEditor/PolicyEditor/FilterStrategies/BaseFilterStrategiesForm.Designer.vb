Imports System.Windows.Forms
Imports Phileas.Policy.Filters.Strategies

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class BaseFilterStrategiesForm(Of T As AbstractFilterStrategy)
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.FilterStrategiesListBox = New System.Windows.Forms.ListBox()
        Me.OkButton = New System.Windows.Forms.Button()
        Me.NewButton = New System.Windows.Forms.Button()
        Me.EditButton = New System.Windows.Forms.Button()
        Me.RemoveButton = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'FilterStrategiesListBox
        '
        Me.FilterStrategiesListBox.FormattingEnabled = True
        Me.FilterStrategiesListBox.Location = New System.Drawing.Point(12, 12)
        Me.FilterStrategiesListBox.Name = "FilterStrategiesListBox"
        Me.FilterStrategiesListBox.Size = New System.Drawing.Size(411, 186)
        Me.FilterStrategiesListBox.TabIndex = 1
        '
        'OkButton
        '
        Me.OkButton.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.OkButton.Location = New System.Drawing.Point(348, 213)
        Me.OkButton.Name = "OkButton"
        Me.OkButton.Size = New System.Drawing.Size(75, 23)
        Me.OkButton.TabIndex = 2
        Me.OkButton.Text = "OK"
        Me.OkButton.UseVisualStyleBackColor = True
        '
        'NewButton
        '
        Me.NewButton.Location = New System.Drawing.Point(12, 213)
        Me.NewButton.Name = "NewButton"
        Me.NewButton.Size = New System.Drawing.Size(75, 23)
        Me.NewButton.TabIndex = 3
        Me.NewButton.Text = "New..."
        Me.NewButton.UseVisualStyleBackColor = True
        '
        'EditButton
        '
        Me.EditButton.Enabled = False
        Me.EditButton.Location = New System.Drawing.Point(93, 213)
        Me.EditButton.Name = "EditButton"
        Me.EditButton.Size = New System.Drawing.Size(75, 23)
        Me.EditButton.TabIndex = 4
        Me.EditButton.Text = "Edit..."
        Me.EditButton.UseVisualStyleBackColor = True
        '
        'RemoveButton
        '
        Me.RemoveButton.Enabled = False
        Me.RemoveButton.Location = New System.Drawing.Point(174, 213)
        Me.RemoveButton.Name = "RemoveButton"
        Me.RemoveButton.Size = New System.Drawing.Size(75, 23)
        Me.RemoveButton.TabIndex = 5
        Me.RemoveButton.Text = "Remove..."
        Me.RemoveButton.UseVisualStyleBackColor = True
        '
        'BaseFilterStrategiesForm
        '
        Me.AcceptButton = Me.OkButton
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.OkButton
        Me.ClientSize = New System.Drawing.Size(435, 251)
        Me.Controls.Add(Me.RemoveButton)
        Me.Controls.Add(Me.EditButton)
        Me.Controls.Add(Me.NewButton)
        Me.Controls.Add(Me.OkButton)
        Me.Controls.Add(Me.FilterStrategiesListBox)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "BaseFilterStrategiesForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Filter Strategies"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents FilterStrategiesListBox As ListBox
    Friend WithEvents OkButton As Button
    Friend WithEvents NewButton As Button
    Friend WithEvents EditButton As Button
    Friend WithEvents RemoveButton As Button
End Class
