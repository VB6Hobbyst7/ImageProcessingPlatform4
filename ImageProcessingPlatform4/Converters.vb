Namespace NSConverters

    Public Class UriToThumbConverter
        Implements IValueConverter

        Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            If value Is Nothing Then
                Return Nothing
            End If
            If New IO.FileInfo(value).Exists = False Then
                Return Nothing
            End If
            Dim bi As BitmapImage = New BitmapImage()
            bi.BeginInit()
            bi.DecodePixelWidth = 96
            bi.DecodePixelHeight = 72
            bi.UriSource = New Uri(value)
            bi.EndInit()
            Return bi
        End Function

        Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class ProcessStateToUriConverter
        Implements IValueConverter

        Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            If value Is Nothing Then
                Return Nothing
            ElseIf value = 0 Then
                Return Nothing
            ElseIf value = 1 Then
                Dim bi As BitmapImage = New BitmapImage()
                bi.BeginInit()
                bi.DecodePixelWidth = 16
                bi.DecodePixelHeight = 16
                bi.UriSource = New Uri("Icons\Check16.png", UriKind.Relative)
                bi.EndInit()
                Return bi
            Else
                Return Nothing
            End If
        End Function

        Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class ModifyStateToUriConverter
        Implements IValueConverter

        Public Function Convert(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            If value Is Nothing Then
                Return Nothing
            ElseIf value = 0 Then
                Return Nothing
            ElseIf value = 1 Then
                Dim bi As BitmapImage = New BitmapImage()
                bi.BeginInit()
                bi.DecodePixelWidth = 16
                bi.DecodePixelHeight = 16
                bi.UriSource = New Uri("Icons\User16.png", UriKind.Relative)
                bi.EndInit()
                Return bi
            Else
                Return Nothing
            End If
        End Function

        Public Function ConvertBack(ByVal value As Object, ByVal targetType As System.Type, ByVal parameter As Object, ByVal culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class VisibleToStr
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            If value Is Nothing Then
                Return Nothing
            ElseIf value = 0 Then
                Return "隐藏"
            Else
                Return "显示"
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class IsWrongToColor
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            If value Is Nothing Then
                Return "Red"
            ElseIf value = 0 Then
                Return "Red"
            ElseIf value = 1 Then
                Return "Gray"
            ElseIf value = 2 Then
                Return "Firebrick"
            Else
                Return "Gray"
            End If
        End Function

        Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class SignFlagToText
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            Select Case CType(value, Integer) \ 30
                Case 1
                    Return "警示"
                Case 2
                    Return "指路"
                Case 3
                    Return "指示"
                Case 4
                    Return "警告"
                Case Else
                    Return "自定义"
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

    Public Class SignFlagToColor
        Implements IValueConverter

        Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
            Select Case CType(value, Integer) \ 30
                Case 1
                    Return "Crimson"
                Case 2
                    Return "LightGreen"
                Case 3
                    Return "SteelBlue"
                Case 4
                    Return "Gold"
                Case Else
                    Return "White"
            End Select
        End Function

        Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function
    End Class

End Namespace
