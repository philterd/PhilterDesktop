Imports System.Windows.Forms

<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ViewJsonForm
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(ViewJsonForm))
        Me.JsonTextBox = New System.Windows.Forms.TextBox()
        Me.ToolStrip1 = New System.Windows.Forms.ToolStrip()
        Me.CopyToClipboardToolStripButton = New System.Windows.Forms.ToolStripButton()
        Me.ToolStrip1.SuspendLayout()
        Me.SuspendLayout()
        '
        'JsonTextBox
        '
        Me.JsonTextBox.Dock = System.Windows.Forms.DockStyle.Fill
        Me.JsonTextBox.Location = New System.Drawing.Point(0, 86)
        Me.JsonTextBox.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.JsonTextBox.Multiline = True
        Me.JsonTextBox.Name = "JsonTextBox"
        Me.JsonTextBox.ReadOnly = True
        Me.JsonTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.JsonTextBox.Size = New System.Drawing.Size(1293, 833)
        Me.JsonTextBox.TabIndex = 2
        '
        'ToolStrip1
        '
        Me.ToolStrip1.ImageScalingSize = New System.Drawing.Size(24, 24)
        Me.ToolStrip1.Items.AddRange(New System.Windows.Forms.ToolStripItem() {Me.CopyToClipboardToolStripButton})
        Me.ToolStrip1.Location = New System.Drawing.Point(0, 0)
        Me.ToolStrip1.Name = "ToolStrip1"
        Me.ToolStrip1.Padding = New System.Windows.Forms.Padding(0, 0, 3, 0)
        Me.ToolStrip1.Size = New System.Drawing.Size(1293, 57)
        Me.ToolStrip1.TabIndex = 3
        Me.ToolStrip1.Text = "ToolStrip1"
        '
        'CopyToClipboardToolStripButton
        '
        Me.CopyToClipboardToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text
        Me.CopyToClipboardToolStripButton.Image = CType(resources.GetObject("CopyToClipboardToolStripButton.Image"), System.Drawing.Image)
        Me.CopyToClipboardToolStripButton.ImageTransparentColor = System.Drawing.Color.Magenta
        Me.CopyToClipboardToolStripButton.Name = "CopyToClipboardToolStripButton"
        Me.CopyToClipboardToolStripButton.Size = New System.Drawing.Size(211, 52)
        Me.CopyToClipboardToolStripButton.Text = "Copy JSON to Clipboard"
        '
        'ViewJsonForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(862, 612)
        Me.Controls.Add(Me.JsonTextBox)
        Me.Controls.Add(Me.ToolStrip1)
        Me.Icon = CType(resources.GetObject("$this.Icon"), System.Drawing.Icon)
        Me.Margin = New System.Windows.Forms.Padding(4, 5, 4, 5)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "ViewJsonForm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Policy Json"
        Me.ToolStrip1.ResumeLayout(False)
        Me.ToolStrip1.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents JsonTextBox As TextBox
    Friend WithEvents ToolStrip1 As ToolStrip
    Friend WithEvents CopyToClipboardToolStripButton As ToolStripButton
End Class
