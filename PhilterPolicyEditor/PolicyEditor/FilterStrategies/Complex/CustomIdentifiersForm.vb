Imports System.Windows.Forms
Imports Philter.Model.Policy.Filters

Public Class CustomIdentifiersForm

    Public Property CustomIdentifiers As List(Of Identifier)

    Public Sub New(CustomIdentifiers As List(Of Identifier))

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

        For Each i As Identifier In CustomIdentifiers

            Dim lvi As New ListViewItem(i.Label)
            lvi.SubItems.Add(i.Pattern)
            lvi.Tag = i

            CustomIdentifiersListView.Items.Add(lvi)

        Next

    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub OkButton_Click(sender As Object, e As EventArgs) Handles OkButton.Click

        CustomIdentifiers = New List(Of Identifier)

        For Each lvi As ListViewItem In CustomIdentifiersListView.Items
            CustomIdentifiers.Add(DirectCast(lvi.Tag, Identifier))
        Next

        Me.DialogResult = DialogResult.OK
        Me.Close()

    End Sub

    Private Sub AddButton_Click(sender As Object, e As EventArgs) Handles AddButton.Click

        Dim ci As New CustomIdentifierForm
        If ci.ShowDialog = DialogResult.OK Then

            Dim i As Identifier = ci.CustomIdentifier

            Dim lvi As New ListViewItem(i.Label)
            lvi.SubItems.Add(i.Pattern)
            lvi.Tag = i

            CustomIdentifiersListView.Items.Add(lvi)

        End If

    End Sub

    Private Sub EditButton_Click(sender As Object, e As EventArgs) Handles EditButton.Click

        Dim i As Identifier = DirectCast(CustomIdentifiersListView.SelectedItems(0).Tag, Identifier)

        Dim ci As New CustomIdentifierForm(i)
        If ci.ShowDialog = DialogResult.OK Then

            CustomIdentifiersListView.SelectedItems(0).Tag = ci.CustomIdentifier

        End If

    End Sub

    Private Sub CustomIdentifiersListView_SelectedIndexChanged(sender As Object, e As EventArgs) Handles CustomIdentifiersListView.SelectedIndexChanged

        If CustomIdentifiersListView.SelectedItems.Count > 0 Then
            EditButton.Enabled = True
            RemoveButton.Enabled = True
        Else
            EditButton.Enabled = False
            RemoveButton.Enabled = False
        End If

    End Sub

    Private Sub RemoveButton_Click(sender As Object, e As EventArgs) Handles RemoveButton.Click

        If MessageBox.Show("Are you sure you want to remove the selected custom identifier?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then

            CustomIdentifiersListView.SelectedItems(0).Remove()

        End If

    End Sub

    Private Sub CustomIdentifiersForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

End Class
