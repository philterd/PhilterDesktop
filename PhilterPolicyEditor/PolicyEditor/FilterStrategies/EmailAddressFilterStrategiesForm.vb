Imports Phileas.Policy
Imports Phileas.Policy.Filters.Strategies

Public Class EmailAddressFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of EmailAddressFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of EmailAddressFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddEmailAddressFilterStrategyForm(New EmailAddressFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

        a.Dispose()

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As EmailAddressFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, EmailAddressFilterStrategy)

        Dim a As New AddEmailAddressFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
