Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters.Strategies

Public Class VinFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of VinFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of VinFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddVinFilterStrategyForm(New VinFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

        a.Dispose()

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As VinFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, VinFilterStrategy)

        Dim a As New AddVinFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
