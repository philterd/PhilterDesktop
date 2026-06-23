Imports Phileas.Policy
Imports Phileas.Policy.Filters.Strategies

Public Class IdentifierFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of IdentifierFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of IdentifierFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddIdentifierFilterStrategyForm(New IdentifierFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As IdentifierFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, IdentifierFilterStrategy)

        Dim a As New AddIdentifierFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
