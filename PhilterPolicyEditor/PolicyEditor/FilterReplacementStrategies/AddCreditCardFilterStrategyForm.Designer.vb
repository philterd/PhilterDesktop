Imports System.Windows.Forms
Imports Phileas.Policy.Filters.Strategies

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class AddCreditCardFilterStrategyForm
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
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.OkButton = New System.Windows.Forms.Button()
        Me.Cancel_Button = New System.Windows.Forms.Button()
        Me.RedactRadioButton = New System.Windows.Forms.RadioButton()
        Me.RedactionFormatTextBox = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.RandomReplacementRadioButton = New System.Windows.Forms.RadioButton()
        Me.ScopeContextCheckBox = New System.Windows.Forms.CheckBox()
        Me.StaticReplaceRadioButton = New System.Windows.Forms.RadioButton()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.StaticReplacementValueTextBox = New System.Windows.Forms.TextBox()
        Me.StrategyOptionsGroupBox = New System.Windows.Forms.GroupBox()
        Me.ConditionalValueTextBox = New System.Windows.Forms.TextBox()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.EnableConditionalCheckBox = New System.Windows.Forms.CheckBox()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.StrategyOptionsGroupBox.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.OkButton, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.Cancel_Button, 1, 0)
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(329, 403)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 29.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(146, 29)
        Me.TableLayoutPanel1.TabIndex = 0
        '
        'OkButton
        '
        Me.OkButton.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.OkButton.Location = New System.Drawing.Point(3, 3)
        Me.OkButton.Name = "OkButton"
        Me.OkButton.Size = New System.Drawing.Size(67, 23)
        Me.OkButton.TabIndex = 0
        Me.OkButton.Text = "OK"
        '
        'Cancel_Button
        '
        Me.Cancel_Button.Anchor = System.Windows.Forms.AnchorStyles.None
        Me.Cancel_Button.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Cancel_Button.Location = New System.Drawing.Point(76, 3)
        Me.Cancel_Button.Name = "Cancel_Button"
        Me.Cancel_Button.Size = New System.Drawing.Size(67, 23)
        Me.Cancel_Button.TabIndex = 1
        Me.Cancel_Button.Text = "Cancel"
        '
        'RedactRadioButton
        '
        Me.RedactRadioButton.AutoSize = True
        Me.RedactRadioButton.Checked = True
        Me.RedactRadioButton.Location = New System.Drawing.Point(20, 30)
        Me.RedactRadioButton.Name = "RedactRadioButton"
        Me.RedactRadioButton.Size = New System.Drawing.Size(204, 17)
        Me.RedactRadioButton.TabIndex = 1
        Me.RedactRadioButton.TabStop = True
        Me.RedactRadioButton.Text = "Redact text identified as a credit card."
        Me.RedactRadioButton.UseVisualStyleBackColor = True
        '
        'RedactionFormatTextBox
        '
        Me.RedactionFormatTextBox.Location = New System.Drawing.Point(142, 53)
        Me.RedactionFormatTextBox.Name = "RedactionFormatTextBox"
        Me.RedactionFormatTextBox.Size = New System.Drawing.Size(293, 20)
        Me.RedactionFormatTextBox.TabIndex = 2
        Me.RedactionFormatTextBox.Text = "{{{REDACTED-%t}}}"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(42, 56)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(94, 13)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Redaction Format:"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(139, 76)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(174, 13)
        Me.Label2.TabIndex = 4
        Me.Label2.Text = "%t will be replaced by the filter type."
        '
        'RandomReplacementRadioButton
        '
        Me.RandomReplacementRadioButton.AutoSize = True
        Me.RandomReplacementRadioButton.Location = New System.Drawing.Point(20, 106)
        Me.RandomReplacementRadioButton.Name = "RandomReplacementRadioButton"
        Me.RandomReplacementRadioButton.Size = New System.Drawing.Size(307, 17)
        Me.RandomReplacementRadioButton.TabIndex = 5
        Me.RandomReplacementRadioButton.Text = "Replace text identified as a credit card with a random value."
        Me.RandomReplacementRadioButton.UseVisualStyleBackColor = True
        '
        'ScopeContextCheckBox
        '
        Me.ScopeContextCheckBox.AutoSize = True
        Me.ScopeContextCheckBox.Location = New System.Drawing.Point(45, 131)
        Me.ScopeContextCheckBox.Name = "ScopeContextCheckBox"
        Me.ScopeContextCheckBox.Size = New System.Drawing.Size(267, 17)
        Me.ScopeContextCheckBox.TabIndex = 6
        Me.ScopeContextCheckBox.Text = "Replace consistently across all document contexts."
        Me.ScopeContextCheckBox.UseVisualStyleBackColor = True
        '
        'StaticReplaceRadioButton
        '
        Me.StaticReplaceRadioButton.AutoSize = True
        Me.StaticReplaceRadioButton.Location = New System.Drawing.Point(20, 167)
        Me.StaticReplaceRadioButton.Name = "StaticReplaceRadioButton"
        Me.StaticReplaceRadioButton.Size = New System.Drawing.Size(297, 17)
        Me.StaticReplaceRadioButton.TabIndex = 7
        Me.StaticReplaceRadioButton.Text = "Replace text identified as a credit card with a static value."
        Me.StaticReplaceRadioButton.UseVisualStyleBackColor = True
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(42, 192)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(118, 13)
        Me.Label3.TabIndex = 9
        Me.Label3.Text = "Static replacement text:"
        '
        'StaticReplacementValueTextBox
        '
        Me.StaticReplacementValueTextBox.Location = New System.Drawing.Point(166, 189)
        Me.StaticReplacementValueTextBox.Name = "StaticReplacementValueTextBox"
        Me.StaticReplacementValueTextBox.Size = New System.Drawing.Size(269, 20)
        Me.StaticReplacementValueTextBox.TabIndex = 8
        '
        'StrategyOptionsGroupBox
        '
        Me.StrategyOptionsGroupBox.Controls.Add(Me.RedactRadioButton)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.Label3)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.RedactionFormatTextBox)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.StaticReplacementValueTextBox)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.Label1)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.StaticReplaceRadioButton)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.Label2)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.ScopeContextCheckBox)
        Me.StrategyOptionsGroupBox.Controls.Add(Me.RandomReplacementRadioButton)
        Me.StrategyOptionsGroupBox.Location = New System.Drawing.Point(12, 12)
        Me.StrategyOptionsGroupBox.Name = "StrategyOptionsGroupBox"
        Me.StrategyOptionsGroupBox.Size = New System.Drawing.Size(459, 233)
        Me.StrategyOptionsGroupBox.TabIndex = 10
        Me.StrategyOptionsGroupBox.TabStop = False
        Me.StrategyOptionsGroupBox.Text = "Filter Strategy"
        '
        'ConditionalValueTextBox
        '
        Me.ConditionalValueTextBox.Enabled = False
        Me.ConditionalValueTextBox.Location = New System.Drawing.Point(20, 94)
        Me.ConditionalValueTextBox.Name = "ConditionalValueTextBox"
        Me.ConditionalValueTextBox.Size = New System.Drawing.Size(407, 20)
        Me.ConditionalValueTextBox.TabIndex = 26
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.EnableConditionalCheckBox)
        Me.GroupBox1.Controls.Add(Me.Label5)
        Me.GroupBox1.Controls.Add(Me.ConditionalValueTextBox)
        Me.GroupBox1.Location = New System.Drawing.Point(12, 251)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(458, 136)
        Me.GroupBox1.TabIndex = 12
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Conditional"
        '
        'EnableConditionalCheckBox
        '
        Me.EnableConditionalCheckBox.AutoSize = True
        Me.EnableConditionalCheckBox.Location = New System.Drawing.Point(19, 70)
        Me.EnableConditionalCheckBox.Name = "EnableConditionalCheckBox"
        Me.EnableConditionalCheckBox.Size = New System.Drawing.Size(169, 17)
        Me.EnableConditionalCheckBox.TabIndex = 13
        Me.EnableConditionalCheckBox.Text = "Only apply filter strategy when:"
        Me.EnableConditionalCheckBox.UseVisualStyleBackColor = True
        '
        'Label5
        '
        Me.Label5.Location = New System.Drawing.Point(17, 26)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(410, 32)
        Me.Label5.TabIndex = 14
        Me.Label5.Text = "The filter strategy defined above will only be applied when the following conditi" &
    "onal is satisfied."
        '
        'AddCreditCardFilterStrategyForm
        '
        Me.AcceptButton = Me.OkButton
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.Cancel_Button
        Me.ClientSize = New System.Drawing.Size(487, 444)
        Me.Controls.Add(Me.GroupBox1)
        Me.Controls.Add(Me.StrategyOptionsGroupBox)
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "AddCreditCardFilterStrategyForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Credit Card Filter Strategy"
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.StrategyOptionsGroupBox.ResumeLayout(False)
        Me.StrategyOptionsGroupBox.PerformLayout()
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents TableLayoutPanel1 As System.Windows.Forms.TableLayoutPanel
    Friend WithEvents OkButton As System.Windows.Forms.Button
    Friend WithEvents Cancel_Button As System.Windows.Forms.Button
    Friend WithEvents RedactRadioButton As RadioButton
    Friend WithEvents RedactionFormatTextBox As TextBox
    Friend WithEvents Label1 As Label
    Friend WithEvents Label2 As Label
    Friend WithEvents RandomReplacementRadioButton As RadioButton
    Friend WithEvents ScopeContextCheckBox As CheckBox
    Friend WithEvents StaticReplaceRadioButton As RadioButton
    Friend WithEvents Label3 As Label
    Friend WithEvents StaticReplacementValueTextBox As TextBox
    Friend WithEvents StrategyOptionsGroupBox As GroupBox
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Label5 As Label
    Friend WithEvents ConditionalValueTextBox As TextBox
    Friend WithEvents EnableConditionalCheckBox As CheckBox
End Class
