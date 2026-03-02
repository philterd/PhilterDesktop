Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters.Strategies

Public Class HospitalAbbreviationFilterStrategiesForm
    Inherits BaseFilterStrategiesForm(Of HospitalAbbreviationFilterStrategy)

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of HospitalAbbreviationFilterStrategy))
        MyBase.New(Policy, FilterType, FilterStrategies)
    End Sub

    Private Sub NewButton_Click(sender As Object, e As EventArgs) Handles NewButton.Click

        Dim a As New AddHospitalAbbreviationFilterStrategyForm(New HospitalAbbreviationFilterStrategy)

        If a.ShowDialog() = DialogResult.OK Then

            FilterStrategiesListBox.Items.Add(a.FilterStrategy)

        End If

        a.Dispose()

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim strategy As HospitalAbbreviationFilterStrategy = DirectCast(FilterStrategiesListBox.SelectedItem, HospitalAbbreviationFilterStrategy)

        Dim a As New AddHospitalAbbreviationFilterStrategyForm(strategy)

        If a.ShowDialog() = DialogResult.OK Then

            Dim index As Integer = FilterStrategiesListBox.SelectedIndex

            FilterStrategiesListBox.Items.RemoveAt(index)
            FilterStrategiesListBox.Items.Insert(index, a.FilterStrategy)

        End If

    End Sub

End Class
