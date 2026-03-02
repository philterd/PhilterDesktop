Public Class Conditional

    Public Property Expression As String
    Public Property ConditionalOperator As String
    Public Property Value As String

    Public Sub New(conditional As String)

        ' TODO: Parse the string into a conditional.

    End Sub

    Public Sub New(expression As String, conditionalOperator As String, value As String)

        Me.Expression = expression
        Me.ConditionalOperator = conditionalOperator
        Me.Value = value

    End Sub

    Public Function GetConditionalString() As String

        If String.IsNullOrEmpty(Expression) Or String.IsNullOrWhiteSpace(Expression) Then
            Return "no conditional"
        Else
            Return Expression & " " & ConditionalOperator & " " & Chr(34) & Value & Chr(34)
        End If

    End Function

    Public Overrides Function ToString() As String

        Return GetConditionalString()

    End Function

End Class
