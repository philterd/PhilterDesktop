Imports System.Windows.Forms
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports Phileas.Policy

Public Class ViewJsonForm

    Public Property Policy As Policy

    Public Sub New(policy As Policy)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        Me.Policy = policy

    End Sub

    Private Sub ViewJson_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Me.Text = Chr(34) & Policy.Name & Chr(34) & " Policy JSON"

        Dim Json As String = PolicySerializer.SerializeToJson(Policy)
        Dim prettyJson As String = JToken.Parse(Json).ToString(Formatting.Indented)
        JsonTextBox.Text = prettyJson

    End Sub

    Private Sub CopyToClipboardToolStripButton_Click(sender As Object, e As EventArgs) Handles CopyToClipboardToolStripButton.Click
        Clipboard.SetText(JsonTextBox.Text)
    End Sub

End Class
