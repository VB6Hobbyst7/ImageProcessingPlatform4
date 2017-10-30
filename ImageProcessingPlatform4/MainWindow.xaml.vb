Imports ImageProcessingPlatform4.Application    '引入应用程序的命名空间，需要用到Application.xaml.vb的一些变量
Imports System.Windows                          '引入Windows命名空间
Imports Microsoft.Win32                         '引入WinForm命名空间，打开文件，保存文件对话框需要用到
'Imports System.Windows.Media                    '引入Media命名空间，图像解码和显示时需要用到
Imports System.Threading                        '引入线程命名空间，UI线程和工作线程分开需要用到
Imports System.Data                             '引入系统数据命名空间，ADO.NET需要用到
Imports System.Data.SQLite                      '引入SQLite命名空间，对SQLite数据库的操作需要用到
Imports System.Reflection                       '引入Reflection命名空间，Dll的自动导入需要此命名
Partial Class MainWindow
    Public Shared ds As DataSet                 'ds,ds2,ds3分别对应SQLite数据库中的DirTB，FilesDB，ObjectDB
    Public Shared ds2 As DataSet
    Public Shared ds3 As DataSet
    Public Shared con As SQLiteConnection
    Friend Shared WithEvents OpenFileDialog1 As OpenFileDialog  '声明打开文件对话框，带事件的对象
    Friend Shared WithEvents SaveFileDialog1 As SaveFileDialog  '声明保存文件的对话框，带事件的对象，用于保存csv或xml的对话框
    Public Shared matobj As Object                                     '声明一个任意对象，装载Assembly组件
    Public Shared methods() As MethodInfo                              '方法数组

    Public Sub New()
        Try
            InitializeComponent()                   '初始化组件，系统自动处理
            'Debug.Assert(False)
            Dim win1 As New Window1
            win1.ShowDialog()
            ds = New DataSet                    '实例化ds,ds2,ds3
            ds2 = New DataSet
            ds3 = New DataSet
            '实例化OpenFileDialog1与SaveFiledialog
            OpenFileDialog1 = New OpenFileDialog With {.Title = "请选择Matlab的Script文件", .Multiselect = False, .Filter = "Pictures (*.m) |*.m|All Files (*.*) |*.*"}
            SaveFileDialog1 = New SaveFileDialog
            '连接并打开数据库
            con = New SQLiteConnection("Data Source=.\imdb2.db")
            con.Open()
            '从SQLite数据库查询所需要的部分，并分别装载数据到数据适配器
            Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
            Dim sql2 = "SELECT * FROM DirTB"
            'Dim sql3 = "SELECT * FROM ObjTB WHERE (ParentId = 1)"
            Using da1 = New SQLiteDataAdapter(sql, con), da2 = New SQLiteDataAdapter(sql2, con) ', da3 = New SQLiteDataAdapter(sql3, con)
                da1.Fill(ds, "Fid")
                da2.Fill(ds2, "DirId")
                'da3.Fill(ds3, "TargetId")
            End Using
            '晚绑定声明，分别指定lstbox，folderList，ShapeCanvas的数据上下文
            lstbox.DataContext = ds
            folderList.DataContext = ds2
            'ShapeCanvas.DataContext = ds3
            'Fun1子程序是自动载入plugin文件夹下的Dll的过程
            Fun1()

        Catch ex As Exception
            Throw ex
            MsgBox("初始化异常")
            'Return
        End Try
    End Sub

#Region "列表控制"
    '该事件响应lstbox的选择变动过程
    Private Sub lstbox_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles lstbox.SelectionChanged
        If lstbox.SelectedItem Is Nothing Then
            Return
        End If
        '如果选择变动，先禁用向前，向后，重设结果按钮，以免出现混乱
        btn_Prior.IsEnabled = False
        btn_Next.IsEnabled = False
        btn_Resetresult.IsEnabled = False
        '创建一个执行MATLAB DLL的线程
        Dim tdExe As Thread = New Thread(AddressOf ExecuteMScript)
        '线程开始，并将lstbox的SelectedItem作为参数传递到该线程
        tdExe.Start(lstbox.SelectedItem)

    End Sub

    Sub Fun1()      '自动载入Dll的过程
        Dim sr As IO.StreamReader = New IO.StreamReader("plugin\plugin.txt", System.Text.Encoding.UTF8)     '读取文件流，文本类型为Unicode-UTF8
        Dim ass As Assembly                     '.NET Assembly Dll
        Dim mytypes As Type() = Nothing         '类型数组
        methods = New MethodInfo(255) {}        '新建方法，255个为上限
        Dim i As Integer = 0
        While Not sr.EndOfStream                '如果文件内容未结束，在循环中一行一行的独有
            Dim strline = sr.ReadLine           '读取一行并移到下一行以备下次读取

            If strline.StartsWith("[") And strline.EndsWith("]") Then                           '如果某行被 [ ] 包含，则
                ass = Assembly.LoadFrom("plugin\" & strline.Substring(1, strline.Length - 2))   '载入除了 [ ] 符号的其他字符
                mytypes = ass.GetTypes                                                          '获取ass的所有类，规定用MATLAB生成一个类即可
                matobj = Activator.CreateInstance(mytypes(0))                                   '实例化这个类到matobj
            Else                                                                                '否则
                Dim funstrline() As String = strline.Split(",")                                 '将行用逗号分隔
                methods(i) = mytypes(0).GetMethod(funstrline(0), New Type() {GetType(Integer), GetType(Object)})        '获取类型中的方法
                i += 1
                cbQua.Items.Add(New Label With {.Content = funstrline(1)})                      '在UI上将这些方法显示在Combox中
            End If
        End While
        'Dim firstline As String = sr.ReadLine
        'Dim ass As Assembly = Assembly.LoadFrom("plugin\" & firstline)
        'Dim mytypes As Type() = ass.GetTypes
        'matobj = Activator.CreateInstance(mytypes(0))

        'Dim input As String = sr.ReadToEnd
        'Dim funstrline() As String = input.Replace(vbCrLf, ";").Split(";")
        'sr.Close()
        'methods = New MethodInfo(funstrline.Length - 1) {}

        'For i = 0 To funstrline.Length - 1
        '    Dim sb() As String = funstrline(i).Split(",")
        '    methods(i) = mytypes(0).GetMethod(sb(0), New Type() {GetType(Integer), GetType(Object)})
        '    cbQua.Items.Add(New Label With {.Content = sb(1)})
        'Next i
    End Sub

    '响应向前按钮点击的过程
    Private Sub btn_Prior_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_Prior.Click
        lstbox.Items.MoveCurrentToPrevious()                '移动lstbox当前选项到前一个
        lstbox.ScrollIntoView(lstbox.Items.CurrentItem)     '将视野滚到所选定的选项
        If lstbox.Items.IsCurrentBeforeFirst Then           '若当前项超出了第一个，则移到第一个
            lstbox.Items.MoveCurrentToFirst()
        End If
    End Sub

    '响应向后按钮点击的过程
    Private Sub btn_Next_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_Next.Click
        lstbox.Items.MoveCurrentToNext()                    '与以上向前类似
        lstbox.ScrollIntoView(lstbox.Items.CurrentItem)
        If lstbox.Items.IsCurrentAfterLast Then
            lstbox.Items.MoveCurrentToLast()
        End If
    End Sub

    '响应清除所有结果按钮点击的过程
    Private Sub btn_Resetresult_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_Resetresult.Click
        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con
        '数据库事务处理
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        For Each li As DataRowView In folderList.Items
            Dim aa As Integer = li("DirId")
            If li("Visible") = 1 Then
                cmd.CommandText = String.Format("UPDATE FilesDB SET CamaraPosition=NULL,IsProcessed=0,IsModified=0,Total = NULL,Red=NULL,Green=NULL,Blue=NULL,Yellow=NULL WHERE (DirectoryIndex = {0});DELETE FROM ObjTB WHERE (DirID={0});UPDATE DirTB SET ProcessedCount = 0 WHERE (DirId = {0});", aa)
                cmd.ExecuteNonQuery()
            End If
        Next li
        '执行事务
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

#End Region

#Region "播放方法"
    Public Shared tdstop As Integer = 0     '一个用于控制线程停止的标志
    Private td As Thread

    '响应播放按钮点击的过程
    Private Sub btn_Play_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_Play.Checked
        '为避免逻辑混乱，播放后关闭并隐藏某些按钮
        lstbox.IsEnabled = False
        btn_OKonce.Visibility = System.Windows.Visibility.Collapsed
        btn_OKonce.IsEnabled = False
        btn_Prior.IsEnabled = False
        btn_Prior.Visibility = System.Windows.Visibility.Collapsed
        btn_Next.IsEnabled = False
        btn_Next.Visibility = System.Windows.Visibility.Collapsed
        btn_Resetresult.IsEnabled = False
        btn_Resetresult.Visibility = System.Windows.Visibility.Collapsed
        tiSetting.IsEnabled = False
        tiSetting.Visibility = System.Windows.Visibility.Collapsed
        Edittoolbar.IsEnabled = False
        Edittoolbar.Visibility = System.Windows.Visibility.Collapsed
        btn_BackGround.Visibility = System.Windows.Visibility.Visible
        tdstop = 0                          '线程标志置0
        RemoveHandler lstbox.SelectionChanged, AddressOf lstbox_SelectionChanged        '移除lstbox的选择变动响应，即选择后不响应
        If lstbox.Items.CurrentPosition = lstbox.Items.Count - 1 Then                   '如果播放到最后，则停止
            btn_Play.IsChecked = False
            Return
        End If
        Stopwatch.StartNew()                                                            '秒表计时
        If Stopwatch.GetTimestamp > 700 Then                                            '若超过700毫秒，则开始BackImgProc图像载入线程，从当前选项开始
            td = New Thread(AddressOf BackImgProc)
            td.Start(lstbox.Items.CurrentPosition)
        End If

    End Sub

    '播放反选后的过程
    Private Sub btn_Play_Unchecked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_Play.Unchecked
        tdstop = 1
        lstbox.IsEnabled = True
        btn_OKonce.Visibility = System.Windows.Visibility.Visible
        btn_OKonce.IsEnabled = True
        btn_Prior.IsEnabled = True
        btn_Prior.Visibility = System.Windows.Visibility.Visible
        btn_Next.IsEnabled = True
        btn_Next.Visibility = System.Windows.Visibility.Visible
        btn_Resetresult.IsEnabled = True
        btn_Resetresult.Visibility = System.Windows.Visibility.Visible
        tiSetting.IsEnabled = True
        tiSetting.Visibility = System.Windows.Visibility.Visible
        Edittoolbar.IsEnabled = True
        Edittoolbar.Visibility = System.Windows.Visibility.Visible
        btn_BackGround.Visibility = System.Windows.Visibility.Collapsed
        If btn_BackGround.IsChecked = True Then
            btn_BackGround.IsChecked = False
        End If
        AddHandler lstbox.SelectionChanged, AddressOf lstbox_SelectionChanged           '恢复lstbox的选择改变句柄
    End Sub

    '一键OK按钮点击，与播放按钮的响应大同小异
    Private Sub btn_OKonce_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_OKonce.Checked
        lstbox.IsEnabled = False
        btn_Prior.IsEnabled = False
        btn_Prior.Visibility = System.Windows.Visibility.Collapsed
        btn_Next.IsEnabled = False
        btn_Next.Visibility = System.Windows.Visibility.Collapsed
        btn_Resetresult.IsEnabled = False
        btn_Resetresult.Visibility = System.Windows.Visibility.Collapsed
        btn_Play.Visibility = System.Windows.Visibility.Collapsed
        btn_Play.IsEnabled = False
        tiSetting.IsEnabled = False
        tiSetting.Visibility = System.Windows.Visibility.Collapsed
        Edittoolbar.IsEnabled = False
        Edittoolbar.Visibility = System.Windows.Visibility.Collapsed
        btn_BackGround.Visibility = System.Windows.Visibility.Visible
        btn_BackGround.IsChecked = True
        tdstop = 0
        Stopwatch.StartNew()
        If Stopwatch.GetTimestamp > 700 Then
            td = New Thread(AddressOf BackImgProc)
            td.Start(0)
        End If
    End Sub

    '一键OK反选
    Private Sub btn_OKonce_Unchecked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_OKonce.Unchecked
        tdstop = 1
        lstbox.IsEnabled = True
        btn_Prior.IsEnabled = True
        btn_Prior.Visibility = System.Windows.Visibility.Visible
        btn_Next.IsEnabled = True
        btn_Next.Visibility = System.Windows.Visibility.Visible
        btn_Resetresult.IsEnabled = True
        btn_Resetresult.Visibility = System.Windows.Visibility.Visible
        btn_Play.Visibility = System.Windows.Visibility.Visible
        btn_Play.IsEnabled = True
        tiSetting.IsEnabled = True
        tiSetting.Visibility = System.Windows.Visibility.Visible
        Edittoolbar.IsEnabled = True
        Edittoolbar.Visibility = System.Windows.Visibility.Visible
        btn_BackGround.Visibility = System.Windows.Visibility.Collapsed
        If btn_BackGround.IsChecked = True Then
            btn_BackGround.IsChecked = False
        End If
    End Sub

    '代理声明
    Private Delegate Sub MyStatus(ByVal ind As Integer)
    '非UI线程图像载入过程
    Private Sub BackImgProc(ByVal eps As Integer)
        Dim EndStatusDelegate As New MyStatus(AddressOf SetEndStatus)
        Dim ActSetListScroll As New MyStatus(AddressOf SetListScroll)
        Dim i = eps '+ 1
        If i = -1 Then
            i = 0
        End If
        Do Until i = ds.Tables(0).Rows.Count
            Thread.Sleep(150)       '线程等待
            Dispatcher.Invoke(Threading.DispatcherPriority.Normal, ActSetListScroll, i)     '更新UI，通过呼叫SetListScroll
            ExecuteMScript(lstbox.Items(i))                                                 '执行MATLAB函数
            If tdstop = 1 Or i = ds.Tables(0).Rows.Count - 1 Then
                tdstop = 0
                Dispatcher.Invoke(Threading.DispatcherPriority.Normal, EndStatusDelegate, i)    '更新UI，通过呼叫SetEndStatus
                Return
            End If
            i += 1
        Loop
    End Sub

    'lstbox卷动的UI过程
    Private Sub SetListScroll(ByVal ind As Integer)
        If bgstate = 0 Then
            lstbox.ScrollIntoView(lstbox.Items.Item(ind))
        ElseIf bgstate = 1 Then
            txtState.Text = ind.ToString & "/" & (lstbox.Items.Count - 1).ToString
        End If
    End Sub

    '图片播放结束后更新UI过程
    Private Sub SetEndStatus(ByVal EndEps As Integer)

        lstbox.Items.MoveCurrentTo(lstbox.Items.Item(EndEps))
        lstbox.ScrollIntoView(lstbox.Items.Item(EndEps))

        If btn_Play.IsChecked = True Then
            btn_Play.IsChecked = False
        End If
        If btn_OKonce.IsChecked = True Then
            btn_OKonce.IsChecked = False
        End If
    End Sub
#End Region

#Region "函数执行过程，工作线程和UI线程"
    Public Delegate Sub UIDelegate(ByRef bmpi As BitmapImage, ByRef fid As Int64)

    Private Sub ExecuteMScript(ByVal obj As DataRowView)
        SyncLock lstbox     '互斥锁
            Dispatcher.Invoke(Threading.DispatcherPriority.Normal, Sub()
                                                                       Me.Cursor = Cursors.Wait
                                                                   End Sub)
            Dim UIDelegate1 As New UIDelegate(AddressOf UpdateViewRegion)
            Dim str As String = obj("FullName")
            If New IO.FileInfo(str).Exists = False Then
                Dispatcher.Invoke(Threading.DispatcherPriority.Normal, Sub()
                                                                           If btn_Play.IsChecked = True Then
                                                                               btn_Play.IsChecked = False
                                                                           End If
                                                                           If btn_OKonce.IsChecked = True Then
                                                                               btn_OKonce.IsChecked = False
                                                                           End If
                                                                           MessageBox.Show("文件夹或文件不存在！请配置数据库或检查文件是否存在。" & vbNewLine & "如果你的电脑是第一次使用本软件，请删除原先所有的数据。")
                                                                       End Sub)
                Return
            End If
            If obj("IsProcessed") = 0 Then

                Dim bi As New BitmapImage

                bi.BeginInit()
                bi.UriSource = New Uri(str)
                bi.EndInit()

                bi.Freeze()
                obj("Width") = bi.PixelWidth
                obj("Height") = bi.PixelHeight

                Dim OutVar As Object = methods(cbQuaState).Invoke(matobj, New Object() {1, str})
                Dim A As Double(,) = OutVar(0)
                'Update dataset and source
                obj.BeginEdit()
                obj("IsProcessed") = 1
                obj.EndEdit()
                Dim cmd = con.CreateCommand()
                cmd.Connection = con
                cmd.CommandText = String.Format("UPDATE FilesDB SET IsProcessed=@v1,Total=@v2,Red=@v3,Green=@v4,Blue=@v5,Yellow=@v6,Width=@v7,Height=@v8 WHERE (Fid={0})", obj("Fid"))
                cmd.Parameters.AddWithValue("@v1", 1)
                cmd.Parameters.AddWithValue("@v2", A.Length / 12)
                Dim cr = 0, cg = 0, cb = 0, cy = 0
                If Not A Is Nothing Then
                    For i As Integer = 0 To A.Length / 12 - 1
                        Select Case CType(A(i, 10), Integer) \ 30
                            Case 1
                                cr += 1
                            Case 2
                                cg += 1
                            Case 3
                                cb += 1
                            Case 4
                                cy += 1
                        End Select
                    Next i
                End If
                obj("Red") = cr
                obj("Green") = cg
                obj("Blue") = cb
                obj("Yellow") = cy
                cmd.Parameters.AddWithValue("@v3", cr)
                cmd.Parameters.AddWithValue("@v4", cg)
                cmd.Parameters.AddWithValue("@v5", cb)
                cmd.Parameters.AddWithValue("@v6", cy)
                cmd.Parameters.AddWithValue("@v7", bi.PixelWidth)
                cmd.Parameters.AddWithValue("@v8", bi.PixelHeight)
                cmd.ExecuteNonQuery()

                Dim D As Decimal
                If Not A Is Nothing Then
                    For i As Integer = 0 To A.Length / 12 - 1
                        cmd.CommandText = "REPLACE INTO ObjTB VALUES (@TargetId,@DirId,@ParentId,@ParentName,@Left,@Right,@Top,@Bottom,@Width,@Height,@Flag,@IsWrong)"
                        D = obj("DirectoryIndex") * 10 ^ 10 + obj("Fid") * 10 ^ 5 + i
                        cmd.Parameters.AddWithValue("@TargetId", D)
                        cmd.Parameters.AddWithValue("@DirId", obj("DirectoryIndex"))
                        cmd.Parameters.AddWithValue("@ParentId", obj("Fid"))
                        cmd.Parameters.AddWithValue("@ParentName", obj("FullName"))
                        cmd.Parameters.AddWithValue("@Left", A(i, 1))
                        cmd.Parameters.AddWithValue("@Right", A(i, 2))
                        cmd.Parameters.AddWithValue("@Top", A(i, 3))
                        cmd.Parameters.AddWithValue("@Bottom", A(i, 4))
                        cmd.Parameters.AddWithValue("@Width", A(i, 2) - A(i, 1) + 1)
                        cmd.Parameters.AddWithValue("@Height", A(i, 4) - A(i, 3) + 1)
                        cmd.Parameters.AddWithValue("@Flag", A(i, 10))
                        cmd.Parameters.AddWithValue("@IsWrong", 0)
                        cmd.ExecuteNonQuery()
                    Next i
                End If
                Dispatcher.Invoke(Threading.DispatcherPriority.Normal, UIDelegate1, bi, obj("Fid"))

            Else
                If New IO.FileInfo(str).Exists = False Then
                    Dispatcher.Invoke(Threading.DispatcherPriority.Normal, Sub()
                                                                               If btn_Play.IsChecked = True Then
                                                                                   btn_Play.IsChecked = False
                                                                               End If
                                                                               If btn_OKonce.IsChecked = True Then
                                                                                   btn_OKonce.IsChecked = False
                                                                               End If
                                                                               MessageBox.Show("文件夹或文件不存在！请配置数据库或检查文件是否存在。" & vbNewLine & "如果你的电脑是第一次使用本软件，请删除原先所有的数据。")
                                                                           End Sub)
                    Return
                End If
                Dim bi As New BitmapImage
                bi.BeginInit()
                bi.UriSource = New Uri(str)
                bi.EndInit()
                bi.Freeze()
                obj("Width") = bi.PixelWidth
                obj("Height") = bi.PixelHeight
                Dispatcher.Invoke(Threading.DispatcherPriority.Normal, UIDelegate1, bi, obj("Fid"))
            End If
            Dispatcher.Invoke(Threading.DispatcherPriority.Normal, Sub()
                                                                       Me.Cursor = Cursors.Arrow
                                                                   End Sub)
        End SyncLock
    End Sub

    Private Sub UpdateViewRegion(ByRef bmpi As BitmapImage, ByRef fid As Int64)
        If bgstate = 0 Then
            ShapeCanvas.Width = bmpi.PixelWidth
            ShapeCanvas.Height = bmpi.PixelHeight
            OriImg.Source = bmpi

            ds3.Clear()
            Dim sql = String.Format("SELECT * FROM ObjTB WHERE (ParentId={0})", fid)
            Using daa1 = New SQLiteDataAdapter(sql, con)
                daa1.Fill(ds3, "TargetId")
            End Using
        End If
        'Cursor = Cursors.Arrow
        btn_Prior.IsEnabled = True
        btn_Next.IsEnabled = True
        btn_Resetresult.IsEnabled = True
        'tiSetting.IsEnabled = True
    End Sub
#End Region

#Region "窗体控制"
    '标题栏双击切换最大最小窗口状态
    Private Sub Title_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        If Me.WindowState = System.Windows.WindowState.Normal Then
            Me.WindowState = System.Windows.WindowState.Maximized
        Else
            Me.WindowState = System.Windows.WindowState.Normal
        End If
    End Sub

    '标题栏拖动则窗口随着拖动
    Private Sub Title_MouseDown(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        Me.DragMove()
    End Sub

    '最小化窗口
    Private Sub MinimumWin(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Me.WindowState = System.Windows.WindowState.Minimized
    End Sub

    '最大化窗口
    Private Sub MaximumWin(sender As System.Object, e As System.Windows.RoutedEventArgs)
        If Me.WindowState = System.Windows.WindowState.Normal Then
            Me.WindowState = System.Windows.WindowState.Maximized
        Else
            Me.WindowState = System.Windows.WindowState.Normal
        End If
    End Sub

    '关闭窗口
    Private Sub CloseWin(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Application.Current.Shutdown()
    End Sub
#End Region

#Region "删除选定方法"

    '删除选定目标按钮点击后的响应过程
    Private Sub btn_TargetDelete_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_TargetDelete.Click
        For Each shp As DataRowView In ShapeCanvas.SelectedItems
            shp.BeginEdit()
            shp("IsWrong") = 1
            shp.EndEdit()

            Dim cmd = con.CreateCommand()
            cmd.Connection = con
            cmd.CommandText = String.Format("UPDATE ObjTB SET IsWrong=@v1 WHERE (TargetId={0})", shp("TargetId"))
            cmd.Parameters.AddWithValue("@v1", 1)
            cmd.ExecuteNonQuery()
        Next
        ShapeCanvas.SelectedIndex = -1

        lstbox.SelectedItem("IsModified") = 1
        Dim sql2 = String.Format("UPDATE FilesDB SET IsModified=1 WHERE (Fid={0})", lstbox.SelectedItem("Fid"))
        Dim cmd3 As New SQLiteCommand(sql2, con)
        cmd3.ExecuteNonQuery()

    End Sub

#End Region

#Region "画框方法"
    Dim tp As New Point(0, 0)
    Dim r As Rectangle
    Dim editsignlist As List(Of SignTarget)
    Dim editVB As New Viewbox
    Dim editCV As New Canvas With {.Background = New SolidColorBrush(Color.FromArgb(120, 0, 0, 0)), .HorizontalAlignment = System.Windows.HorizontalAlignment.Center, .VerticalAlignment = System.Windows.VerticalAlignment.Center}

    '添加物体按钮点击后的响应
    Private Sub btn_TargetAdd_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_TargetAdd.Checked
        If editCV.Children.Count > 0 Then
            editCV.Children.Clear()
        End If

        editCV.Width = OriImg.ActualWidth
        editCV.Height = OriImg.ActualHeight

        Grid.SetColumn(editVB, 0)
        Grid.SetRow(editVB, 1)
        Grid.SetRowSpan(editVB, 2)
        Panel.SetZIndex(editVB, 1)
        editCV.Cursor = Cursors.Cross

        LayoutRoot.Children.Add(editVB)
        editVB.Child = editCV

        editsignlist = New List(Of SignTarget)

        AddHandler editCV.MouseDown, AddressOf editCV_MouseDown
        AddHandler editCV.MouseUp, AddressOf editCV_MouseUp
        btn_TargetAddOK.Visibility = System.Windows.Visibility.Visible
        btn_TargetDelete.Visibility = System.Windows.Visibility.Collapsed
    End Sub

    '添加物体按钮取消点击后的响应
    Private Sub btn_TargetAdd_Unchecked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_TargetAdd.Unchecked
        editCV.Cursor = Cursors.Arrow
        LayoutRoot.Children.Remove(editVB)
        btn_TargetAddOK.Visibility = System.Windows.Visibility.Collapsed
        btn_TargetDelete.Visibility = System.Windows.Visibility.Visible
        RemoveHandler editCV.MouseDown, AddressOf editCV_MouseDown
        RemoveHandler editCV.MouseUp, AddressOf editCV_MouseUp
    End Sub

    '鼠标编辑画布区域的事件-鼠标按下事件
    Private Sub editCV_MouseDown(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        r = New Rectangle()
        With r
            .MinWidth = 16
            .MinHeight = 12
            .Fill = New SolidColorBrush(Color.FromArgb(120, 255, 255, 255))
            .Stroke = Brushes.Azure
        End With

        editCV.Children.Add(r)
        tp = e.GetPosition(editCV)
        Canvas.SetLeft(r, tp.X)
        Canvas.SetTop(r, tp.Y)
        AddHandler editCV.MouseMove, AddressOf editCV_MouseMove
    End Sub

    '鼠标编辑画布区域的事件-鼠标移动事件
    Private Sub editCV_MouseMove(sender As Object, e As System.Windows.Input.MouseEventArgs)
        If e.LeftButton = MouseButtonState.Pressed Then
            If e.GetPosition(editCV).X < tp.X Or e.GetPosition(editCV).Y < tp.Y Then
                Return
            End If
            r.Width = e.GetPosition(editCV).X - tp.X
            r.Height = e.GetPosition(editCV).Y - tp.Y
        End If
    End Sub

    '鼠标编辑画布区域的事件-鼠标弹起事件
    Private Sub editCV_MouseUp(sender As Object, e As System.Windows.Input.MouseButtonEventArgs)
        Dim pr As Double = lstbox.SelectedItem("Width") / OriImg.ActualWidth 'OriImg.Width / OriImg.ActualWidth
        Dim editsign As New SignTarget With {.left = pr * tp.X, .right = pr * (tp.X + r.Width - 1), .top = pr * tp.Y, .bottom = pr * (tp.Y + r.Height - 1), .width = pr * r.Width, .height = pr * r.Height}
        editsignlist.Add(editsign)

        RemoveHandler editCV.MouseMove, AddressOf editCV_MouseMove

    End Sub

    '确认添加目标的按钮按下后的响应
    Private Sub btn_TargetAddOK_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_TargetAddOK.Click
        If editCV.Children.Count = 0 Then
            Return
        End If

        Dim sql1 = String.Format("SELECT * FROM ObjTB WHERE (ParentId={0})", lstbox.SelectedItem("Fid"))
        Dim cmd2 As New SQLiteCommand(sql1, con)
        Dim i As Integer = 0
        Using reader1 = cmd2.ExecuteReader()
            Dim k As Integer = 0
            While reader1.Read = True
                k += 1
            End While
            i = k + 1
        End Using

        For Each rct As SignTarget In editsignlist
            Dim vDirid As Integer = lstbox.SelectedItem("DirectoryIndex")
            Dim vParentid As Integer = lstbox.SelectedItem("Fid")
            Dim vParentname As String = lstbox.SelectedItem("FullName")
            Dim D As Decimal = vDirid * 10 ^ 10 + vParentid * 10 ^ 5 + i
            i += 1
            Dim cmd = con.CreateCommand()
            cmd.Connection = con
            cmd.CommandText = "INSERT INTO ObjTB VALUES(@TargetId,@DirId,@ParentId,@ParentName,@Left,@Right,@Top,@Bottom,@Width,@Height,@Flag,@IsWrong)"
            cmd.Parameters.AddWithValue("@TargetId", D)
            cmd.Parameters.AddWithValue("@DirId", vDirid)
            cmd.Parameters.AddWithValue("@ParentId", vParentid)
            cmd.Parameters.AddWithValue("@ParentName", vParentname)
            cmd.Parameters.AddWithValue("@Left", Fix(rct.left))
            cmd.Parameters.AddWithValue("@Right", Fix(rct.right))
            cmd.Parameters.AddWithValue("@Top", Fix(rct.top))
            cmd.Parameters.AddWithValue("@Bottom", Fix(rct.bottom))
            cmd.Parameters.AddWithValue("@Width", Fix(rct.width))
            cmd.Parameters.AddWithValue("@Height", Fix(rct.height))
            cmd.Parameters.AddWithValue("@Flag", 160 + i)
            cmd.Parameters.AddWithValue("@IsWrong", 2)
            cmd.ExecuteNonQuery()
        Next rct
        ds3.Clear()
        Dim sql = String.Format("SELECT * FROM ObjTB WHERE (ParentId={0})", lstbox.SelectedItem("Fid"))
        Using daa1 = New SQLiteDataAdapter(sql, con)
            daa1.Fill(ds3, "TargetId")
        End Using

        lstbox.SelectedItem("IsModified") = 1
        Dim sql2 = String.Format("UPDATE FilesDB SET IsModified=1 WHERE (Fid={0})", lstbox.SelectedItem("Fid"))
        Dim cmd3 As New SQLiteCommand(sql2, con)
        cmd3.ExecuteNonQuery()


        editCV.Children.Clear()
        editsignlist.Clear()
    End Sub
#End Region

#Region "前台，后台控制"
    Dim bgstate = 0
    '后台按钮开启后的过程
    Private Sub btn_BackGround_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_BackGround.Checked
        bgstate = 1
        Infotoolbar.Visibility = System.Windows.Visibility.Visible
    End Sub

    '后台按钮关闭后的过程
    Private Sub btn_BackGround_Unchecked(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_BackGround.Unchecked
        bgstate = 0
        Infotoolbar.Visibility = System.Windows.Visibility.Collapsed
    End Sub
#End Region

#Region "物体选择方法"

    'Private Sub ShapeCanvas_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles ShapeCanvas.SelectionChanged

    'End Sub

#End Region

End Class

Partial Class MainWindow
    Structure SignTarget
        'Dim id As Decimal
        Dim left As Double
        Dim right As Double
        Dim top As Double
        Dim bottom As Double
        Dim width As Double
        Dim height As Double
        'Dim flag As Double
    End Structure

    Private Function GetAllImageFiles(ByVal dirinfo As IO.DirectoryInfo, ByVal searchPattern As String) As IO.FileInfo()
        Dim searchPatterns() As String = searchPattern.Split("|")
        Dim files As New List(Of IO.FileInfo)
        For Each sp As String In searchPatterns
            files.AddRange(dirinfo.GetFiles(sp))
        Next sp
        'files.Sort()
        Return files.ToArray
    End Function
End Class

Partial Class MainWindow
    Dim tmpList As New List(Of DataRowView)

    '导入按钮按下后的响应
    Private Sub btn_import_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_import.Click
        Dim aaa() As String = TextBox1.Text.Replace(vbCrLf, ";").Split(";")

        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con

        Dim trans As SQLiteTransaction = con.BeginTransaction()
        For i As Integer = 0 To aaa.Length - 1
            If Not aaa(i) = "" And IO.Directory.Exists(aaa(i)) Then

                Dim didir As New IO.DirectoryInfo(aaa(i))
                Dim imgfiles = GetAllImageFiles(didir, "*.jpg|*.bmp|*.png")
                cmd.CommandText = "INSERT INTO DirTB VALUES(@DirId,@DirName,@ImgCount,@ProcessedCount,@Visible)"
                cmd.Parameters.AddWithValue("@DirId", Nothing)
                cmd.Parameters.AddWithValue("@DirName", aaa(i))
                cmd.Parameters.AddWithValue("@ImgCount", imgfiles.Length)
                cmd.Parameters.AddWithValue("@ProcessedCount", 0)
                cmd.Parameters.AddWithValue("Visible", 1)
                cmd.ExecuteNonQuery()

                cmd.CommandText = String.Format("SELECT DirId FROM DirTB WHERE (DirName='{0}')", aaa(i))
                Dim myid As Integer = 0
                Using reader As SQLiteDataReader = cmd.ExecuteReader()
                    While reader.Read = True
                        myid = Convert.ToInt32(reader(0))
                    End While
                End Using

                For Each fi As IO.FileInfo In imgfiles
                    cmd.CommandText = "INSERT INTO FilesDB VALUES(@Fid,@DirectoryIndex,@DirectoryName,@Name,@FullName,@FileLength,@Width,@Height,@CompressionRatio,@CamaraPosition,@IsProcessed,@IsModified,@Visible,@Total,@Red,@Green,@Blue,@Yellow)"
                    cmd.Parameters.AddWithValue("@Fid", Nothing)
                    cmd.Parameters.AddWithValue("@DirectoryIndex", myid)
                    cmd.Parameters.AddWithValue("@DirectoryName", fi.DirectoryName)
                    cmd.Parameters.AddWithValue("@Name", fi.Name)
                    cmd.Parameters.AddWithValue("@FullName", fi.FullName)
                    cmd.Parameters.AddWithValue("@FileLength", fi.Length)
                    cmd.Parameters.AddWithValue("@Width", Nothing)
                    cmd.Parameters.AddWithValue("@Height", Nothing)
                    cmd.Parameters.AddWithValue("@CompressionRatio", Nothing)
                    cmd.Parameters.AddWithValue("@CamaraPosition", Nothing)
                    cmd.Parameters.AddWithValue("@IsProcessed", 0)
                    cmd.Parameters.AddWithValue("@IsModified", 0)
                    cmd.Parameters.AddWithValue("@Visible", 1)
                    cmd.Parameters.AddWithValue("@Total", Nothing)
                    cmd.Parameters.AddWithValue("@Red", Nothing)
                    cmd.Parameters.AddWithValue("@Green", Nothing)
                    cmd.Parameters.AddWithValue("@Blue", Nothing)
                    cmd.Parameters.AddWithValue("@Yellow", Nothing)
                    cmd.ExecuteNonQuery()
                Next fi
            End If
        Next i

        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
        TextBox1.Clear()
    End Sub

    '文件夹统计按钮按下后的响应
    Private Sub btn_dirsta_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_dirsta.Click
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        For Each li As DataRowView In folderList.Items
            Dim aa As Long = li("DirId")

            Dim sql = String.Format("SELECT SUM(IsProcessed) AS Expr1 FROM FilesDB WHERE (IsProcessed = 1) AND (DirectoryIndex = {0})", aa)
            Dim sql3 As String = Nothing
            Dim cmd As New SQLiteCommand(sql, con)
            Dim reader = cmd.ExecuteReader()
            reader.Read()
            If Not IsDBNull(reader(0)) Then
                sql3 = String.Format("UPDATE DirTB SET ProcessedCount = {0} WHERE (DirId = {1})", reader(0), aa)
                Dim cmd2 = New SQLiteCommand(sql3, con)
                cmd2.ExecuteNonQuery()
            End If
        Next li
        '执行事务()
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        ds2.Clear()
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp2 As New SQLiteDataAdapter(sql2, con)
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

    '文件夹显示按钮按下后的响应
    Private Sub btn_dirshow_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_dirshow.Click
        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con
        '事务处理
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        '对选择项的数据进行修改
        For Each li As DataRowView In folderList.SelectedItems
            Dim aa As Integer = li("DirId")
            '同时删除两表中某文件夹Id的项
            cmd.CommandText = String.Format("UPDATE DirTB SET Visible = 1 WHERE (DirId = {0});UPDATE FilesDB SET Visible = 1 WHERE (DirectoryIndex = {0});", aa)
            cmd.ExecuteNonQuery()
        Next li

        '执行事务
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

    '文件夹隐藏按钮按下后的响应
    Private Sub btn_dirhide_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_dirhide.Click
        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con
        '事务处理
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        '对选择项的数据进行修改
        For Each li As DataRowView In folderList.SelectedItems
            Dim aa As Integer = li("DirId")
            '同时删除两表中某文件夹Id的项
            cmd.CommandText = String.Format("UPDATE DirTB SET Visible = 0 WHERE (DirId = {0});UPDATE FilesDB SET Visible = 0 WHERE (DirectoryIndex = {0});", aa)
            cmd.ExecuteNonQuery()
        Next li

        '执行事务
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

    '文件夹重置按钮按下后的响应
    Private Sub btn_dirreset_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_dirreset.Click
        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con
        '事务处理
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        '对选择项的数据进行修改
        For Each li As DataRowView In folderList.SelectedItems
            Dim aa As Integer = li("DirId")
            '同时删除两表中某文件夹Id的项
            cmd.CommandText = String.Format("UPDATE FilesDB SET CamaraPosition=NULL,IsProcessed=0,IsModified=0,Total = NULL,Red=NULL,Green=NULL,Blue=NULL,Yellow=NULL WHERE (DirectoryIndex = {0});DELETE FROM ObjTB WHERE (DirID={0});UPDATE DirTB SET ProcessedCount = 0 WHERE (DirId = {0});", aa)
            cmd.ExecuteNonQuery()
        Next li

        '执行事务
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

    '文件夹删除按钮按下后的响应
    Private Sub btn_dirdel_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_dirdel.Click
        '清楚临时列表
        tmpList.Clear()
        Dim cmd As SQLiteCommand = con.CreateCommand()
        cmd.Connection = con
        '事务处理
        Dim trans As SQLiteTransaction = con.BeginTransaction()
        '对选择项的数据进行修改
        For Each li As DataRowView In folderList.SelectedItems
            tmpList.Add(li)

            Dim aa As Integer = li("DirId")
            '同时删除两表中某文件夹Id的项
            cmd.CommandText = String.Format("DELETE FROM DirTB WHERE (DirId={0});DELETE FROM FilesDB WHERE (DirectoryIndex = {0});DELETE FROM ObjTB WHERE (DirId = {0});", aa)
            cmd.ExecuteNonQuery()
        Next li

        '执行事务
        Try
            trans.Commit()
        Catch ex As Exception
            trans.Rollback()
            Throw
        End Try

        '删除行表显示
        For i As Integer = 0 To tmpList.Count - 1
            tmpList(i).Delete()
        Next i
        lstbox.SelectedIndex = -1
        ds.Clear()
        ds2.Clear()
        Dim sql = "SELECT * FROM FilesDB WHERE (Visible = 1)"
        Dim sql2 = "SELECT * FROM DirTB"
        Using adp As New SQLiteDataAdapter(sql, con), adp2 As New SQLiteDataAdapter(sql2, con)
            adp.Fill(ds, "Fid")
            adp2.Fill(ds2, "DirId")
        End Using
    End Sub

    '文件夹导入按钮按下后的相应
    Private Sub btn_direxport_csv_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_direxport_csv.Click
        Dim fs As IO.FileStream = Nothing
        Dim sw As IO.StreamWriter = Nothing
        SaveFileDialog1.Filter = "逗号分隔文本文件 (*.csv) |*.csv|All Files (*.*) |*.*"
        SaveFileDialog1.FileName = Nothing

        If SaveFileDialog1.ShowDialog = True Then
            Dim tmpds As New DataSet
            Dim sql = "SELECT * FROM ObjTB"
            Using daa1 As New SQLiteDataAdapter(sql, con)
                daa1.Fill(tmpds, "TargetId")
            End Using
            fs = IO.File.Create(SaveFileDialog1.FileName)
            sw = New IO.StreamWriter(fs, Text.Encoding.UTF8)

            sw.WriteLine("TargetId,DirId,ParentId,ParentName,Left,Right,Top,Bottom,Width,Height,Flag,IsWrong")
            For Each drv As DataRow In tmpds.Tables("TargetId").Rows
                sw.WriteLine(drv(0).ToString & "," & drv(1).ToString & "," & drv(2).ToString & "," _
                             & drv(3).ToString & "," & drv(4).ToString & "," & drv(5).ToString & "," _
                             & drv(6).ToString & "," & drv(7).ToString & "," & drv(8).ToString & "," _
                             & drv(9).ToString & "," & drv(10).ToString & "," & drv(11).ToString
                             )
            Next
            sw.Close()
            fs.Close()
        End If
    End Sub

    '数据导出为xml按钮按下后的响应
    Private Sub btn_direxport_xml_Click(sender As Object, e As System.Windows.RoutedEventArgs) Handles btn_direxport_xml.Click
        Dim fs As IO.FileStream = Nothing
        SaveFileDialog1.Filter = "xml数据文件 (*.xml) |*.xml|All Files (*.*) |*.*"
        SaveFileDialog1.FileName = Nothing
        If SaveFileDialog1.ShowDialog = True Then
            Dim tmpds As New DataSet
            Dim sql = "SELECT * FROM ObjTB"
            Using daa1 As New SQLiteDataAdapter(sql, con)
                daa1.Fill(tmpds, "TargetId")
            End Using
            tmpds.WriteXml(SaveFileDialog1.FileName, XmlWriteMode.WriteSchema)
            'tmpds.WriteXmlSchema(SaveFileDialog1.FileName)
        End If

    End Sub

    '全选勾勾选取后的响应
    Private Sub cb_CheckAllDir_Checked(sender As Object, e As System.Windows.RoutedEventArgs) Handles cb_CheckAllDir.Checked
        folderList.SelectAll()
    End Sub

    '全选勾勾反选后的响应
    Private Sub cb_CheckAllDir_Unchecked(sender As Object, e As System.Windows.RoutedEventArgs) Handles cb_CheckAllDir.Unchecked
        folderList.UnselectAll()
    End Sub

    '文本框初始化
    Private Sub TextBox1_Initialized(sender As Object, e As System.EventArgs) Handles TextBox1.Initialized
        TextBox1.Text = "在此处键入包含图像文件的文件夹名，如下所示" & vbNewLine & "C:\文件夹1" & vbNewLine & "C:\Folder2" & vbNewLine & "D:\Folder3" & vbNewLine & "..."
    End Sub

    Dim editfirsttime = 0           '用来标记是否是第一次编辑
    '文本框获取焦点后的过程
    Private Sub TextBox1_GotFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles TextBox1.GotFocus
        If editfirsttime = 0 Then   '若是第一次编辑
            TextBox1.Text = vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf
        End If
        editfirsttime += 1          '改变标记，表明第一次编辑已过

    End Sub

    Dim cbQuaState As Integer = 0   '一个用于标记采用何种处理方法的标记
    '不同函数方法选取后的过程
    Private Sub cbQua_SelectionChanged(sender As Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles cbQua.SelectionChanged
        cbQuaState = cbQua.SelectedIndex
    End Sub

End Class