Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters.Strategies

Public Class StateAbbreviationFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of StateAbbreviationFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of StateAbbreviationFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddStateAbbreviationFilterStrategyForm(New StateAbbreviationFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

        a.Dispose()

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As StateAbbreviationFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, StateAbbreviationFilterStrategy)

        Dim a As New AddStateAbbreviationFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
