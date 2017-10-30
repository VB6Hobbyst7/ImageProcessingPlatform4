Imports System.Threading

Class Application

    Private Sub Application_Exit(sender As Object, e As System.Windows.ExitEventArgs) Handles Me.Exit
        ImageProcessingPlatform4.MainWindow.tdstop = 1
        Thread.Sleep(3000)
        ImageProcessingPlatform4.MainWindow.con.Close()
    End Sub

End Class
