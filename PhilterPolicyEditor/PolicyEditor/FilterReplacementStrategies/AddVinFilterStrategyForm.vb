Imports Philter.Model.Policy.Filters.Strategies

Public Class AddVinFilterStrategyForm

    Public Property FilterStrategy As VinFilterStrategy

    Public Sub New(FilterStrategy As VinFilterStrategy)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.FilterStrategy = FilterStrategy

    End Sub

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OkButton.Click

        If RedactRadioButton.Checked = True Then
            FilterStrategy.Strategy = BaseFilterStrategy.REDACT
            FilterStrategy.RedactionFormat = RedactionFormatTextBox.Text
        End If

        If StaticReplaceRadioButton.Checked = True Then
            FilterStrategy.Strategy = BaseFilterStrategy.STATIC_REPLACEMENT
            FilterStrategy.StaticReplacement = StaticReplacementValueTextBox.Text
        End If

        If RandomReplacementRadioButton.Checked = True Then
            FilterStrategy.Strategy = BaseFilterStrategy.RANDOM_REPLACEMENT
        End If

        If EnableConditionalCheckBox.Checked = True Then
            FilterStrategy.Condition = New Conditional(ConditionalValueTextBox.Text).ToString
        Else
            FilterStrategy.Condition = String.Empty
        End If

        If RandomReplacementRadioButton.Checked = True Then
            FilterStrategy.ReplacementScope = BaseFilterStrategy.SCOPE_CONTEXT
        Else
            FilterStrategy.ReplacementScope = BaseFilterStrategy.SCOPE_DOCUMENT
        End If

        Me.FilterStrategy = FilterStrategy

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click
        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub RedactRadioButton_CheckedChanged(sender As Object, e As EventArgs) Handles RedactRadioButton.CheckedChanged

        RedactionFormatTextBox.Enabled = RedactRadioButton.Checked
        ScopeContextCheckBox.Enabled = RandomReplacementRadioButton.Checked
        StaticReplacementValueTextBox.Enabled = StaticReplaceRadioButton.Checked

    End Sub

    Private Sub RandomReplacementRadioButton_CheckedChanged(sender As Object, e As EventArgs) Handles RandomReplacementRadioButton.CheckedChanged

        RedactionFormatTextBox.Enabled = RedactRadioButton.Checked
        ScopeContextCheckBox.Enabled = RandomReplacementRadioButton.Checked
        StaticReplacementValueTextBox.Enabled = StaticReplaceRadioButton.Checked

    End Sub

    Private Sub StaticReplaceRadioButton_CheckedChanged(sender As Object, e As EventArgs) Handles StaticReplaceRadioButton.CheckedChanged

        RedactionFormatTextBox.Enabled = RedactRadioButton.Checked
        ScopeContextCheckBox.Enabled = RandomReplacementRadioButton.Checked
        StaticReplacementValueTextBox.Enabled = StaticReplaceRadioButton.Checked

    End Sub

    Private Sub FilterStrategy_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' Set values.

        If FilterStrategy.Strategy = BaseFilterStrategy.REDACT Then
            RedactRadioButton.Checked = True
            RedactionFormatTextBox.Text = FilterStrategy.RedactionFormat
        End If

        If FilterStrategy.Strategy = BaseFilterStrategy.STATIC_REPLACEMENT Then
            StaticReplaceRadioButton.Checked = True
            StaticReplacementValueTextBox.Text = FilterStrategy.StaticReplacement
        End If

        If FilterStrategy.Strategy = BaseFilterStrategy.RANDOM_REPLACEMENT Then
            RandomReplacementRadioButton.Checked = True
        End If

        If FilterStrategy.Condition.Length > 0 Then
            EnableConditionalCheckBox.Checked = True
            ConditionalValueTextBox.Text = FilterStrategy.Condition
        Else
            EnableConditionalCheckBox.Enabled = False
            FilterStrategy.Condition = String.Empty
        End If

        If FilterStrategy.ReplacementScope = BaseFilterStrategy.SCOPE_CONTEXT Then
            RandomReplacementRadioButton.Checked = True
        Else
            RandomReplacementRadioButton.Checked = False
        End If

    End Sub

    Private Sub EnableConditionalCheckBox_CheckedChanged(sender As Object, e As EventArgs) Handles EnableConditionalCheckBox.CheckedChanged

        ConditionalValueTextBox.Enabled = EnableConditionalCheckBox.Checked

    End Sub

End Class
