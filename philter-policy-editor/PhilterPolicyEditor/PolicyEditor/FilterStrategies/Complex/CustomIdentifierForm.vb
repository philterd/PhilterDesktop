Imports Philter.Model.Policy.Filters

Public Class CustomIdentifierForm

    Public Property CustomIdentifier As Identifier

    Public Sub New(CustomIdentifier As Identifier)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        Me.CustomIdentifier = CustomIdentifier

        LabelTextBox.Text = CustomIdentifier.Label
        PatternTextBox.Text = CustomIdentifier.Pattern
        CaseSensitiveCheckBox.Checked = CustomIdentifier.CaseSensitive
        EnabledCheckBox.Enabled = CustomIdentifier.Enabled

    End Sub

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.CustomIdentifier = New Identifier

    End Sub

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click

        CustomIdentifier.Label = LabelTextBox.Text
        CustomIdentifier.Pattern = PatternTextBox.Text
        CustomIdentifier.CaseSensitive = CaseSensitiveCheckBox.Checked
        CustomIdentifier.Enabled = EnabledCheckBox.Checked

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub CustomIdentifierForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        LabelTextBox.Text = CustomIdentifier.Label
        PatternTextBox.Text = CustomIdentifier.Pattern
        CaseSensitiveCheckBox.Checked = CustomIdentifier.CaseSensitive
        EnabledCheckBox.Checked = CustomIdentifier.Enabled

    End Sub

End Class
