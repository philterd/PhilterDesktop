Public Class NewPolicyForm

    Public Property PolicyName As String

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub OK_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OK_Button.Click

        ' TODO: Make sure the policy name is unique.

    End Sub

    Private Sub Cancel_Button_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Cancel_Button.Click

        Me.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.Close()

    End Sub

    Private Sub NewPolicyForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

End Class