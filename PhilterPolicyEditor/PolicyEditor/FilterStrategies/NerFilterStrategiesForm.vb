Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters.Strategies

Public Class NerFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of NerFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of NerFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddNerFilterStrategyForm(New NerFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

        a.Dispose()

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As NerFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, NerFilterStrategy)

        Dim a As New AddNerFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
