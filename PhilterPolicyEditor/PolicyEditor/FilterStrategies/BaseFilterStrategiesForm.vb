Imports System.Windows.Forms
Imports Philter.Model
Imports Philter.Model.Policy
Imports Philter.Model.Policy.Filters.Strategies

Public MustInherit Class BaseFilterStrategiesForm(Of T As BaseFilterStrategy)
    Inherits Form

    Public ReadOnly Property Policy As Policy
    Public ReadOnly Property FilterType As String

    Public Function GetFilterStrategies() As List(Of T)

        Dim l As New List(Of T)

        For Each fs As T In FilterStrategiesListBox.Items
            l.Add(fs)
        Next

        Return l

    End Function

    Public Sub New(Policy As Policy, FilterType As String, FilterStrategies As IEnumerable(Of T))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.Policy = Policy
        Me.FilterType = FilterType
        Me.Text = FilterType & " Filter Strategies"

        For Each fs As T In FilterStrategies

            FilterStrategiesListBox.Items.Add(fs)
        Next

    End Sub

    Private Sub CloseButton_Click(sender As Object, e As EventArgs) Handles OkButton.Click

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub RemoveButton_Click(sender As Object, e As EventArgs) Handles RemoveButton.Click

        If FilterStrategiesListBox.SelectedItems.Count > 0 Then
            FilterStrategiesListBox.Items.RemoveAt(FilterStrategiesListBox.SelectedIndex)
        End If

    End Sub

    Private Sub FilterStrategiesListBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles FilterStrategiesListBox.SelectedIndexChanged

        If FilterStrategiesListBox.SelectedItems.Count > 0 Then
            EditButton.Enabled = True
            RemoveButton.Enabled = True
        Else
            EditButton.Enabled = False
            RemoveButton.Enabled = False
        End If

    End Sub

    Private Sub BaseFilterStrategiesForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

End Class
