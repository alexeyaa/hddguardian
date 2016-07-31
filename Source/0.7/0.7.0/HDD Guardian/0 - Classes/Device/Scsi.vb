﻿'HDD Guardian is a GUI for smartcl utility, part of smartmontools
'
'home page is http://hddguardian.codeplex.com/
'
'Copyright (C) 2010-2016 Samuele Parise
'
'This Source Code Form is subject to the terms of the Mozilla Public
'License, v. 2.0. If a copy of the MPL was not distributed with this
'file, You can obtain one at http://mozilla.org/MPL/2.0/.

Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Xml
Imports hdd_guardian

Partial Class Device

#Region "SCSI variables"
    Private scsiIsDisk As Boolean = False
    Private scsiVendor, scsiProduct, scsiRevision, scsiSerialNumber, scsiRotation As String
    Private scsiSize, scsiBlockSize As String
    Private scsiUnitId, scsiProtocol As String
    Private scsiGrownDefects, scsiNonMediumErrors, scsiTestDuration As String
    Private scsiWriteCacheEnabled, scsiReadCacheEnabled As Boolean
    Private scsiWriteCacheAvailable, scsiReadCacheAvailable As Boolean
    Private scsiGltsdEnabled As Boolean = False
    Private scsiHasErrorLog As Boolean = False
    Private scsiHasSelfTestLog As Boolean = False
    Private devIsScsi As Boolean = False
    Private scsiLogRead As ScsiErrorItem
    Private scsiLogWrite As ScsiErrorItem
    Private scsiLogVerify As ScsiErrorItem
    Private scsiSelfTests As New List(Of ScsiSelfTestItem)
    Private scsiWorkTime As String = "N/A"
    Private scsiWorkTimeHuman As String
    Private _scsiDevice As ScsiDevice
#End Region

#Region "SCSI data processors"
    Private Sub GetScsiAttributes()
        Dim e As Short = 0

        Try
            Dim fulloutput As String = ""
            For i As Integer = 0 To devOutput.Length - 1
                If Not IsNothing(devOutput(i)) Then
                    fulloutput += devOutput(i)
                End If
            Next

            scsiVendor = GetInfo("Vendor")
            scsiProduct = GetInfo("Product")
            scsiRevision = GetInfo("Revision")

            Dim size As String = GetInfo("User Capacity")
            Dim s() As String = size.Split("bytes")
            scsiSize = ToNumber(s(0))
            If scsiSize = "" Then scsiSize = "N/A"

            scsiBlockSize = GetInfo("Logical block size")
            scsiUnitId = GetInfo("Logical Unit id")
            scsiSerialNumber = GetInfo("Serial number")
            scsiProtocol = GetInfo("Transport protocol")
            devLastCheck = GetInfo("Local Time is")
            scsiRotation = GetInfo("Rotation Rate")

            Dim smart As String = GetInfo("SMART support is")
            devSmartAvailable = Support.Unavailable
            devSmartEnabled = Support.Disabled
            If smart.Contains("Available - device has SMART capability.") Then
                devSmartAvailable = Support.Available
                For i As Short = 0 To devOutput.Length - 2
                    If devOutput(i).Contains("SMART support is") And devOutput(i).Contains("Enabled") Then
                        devSmartEnabled = Support.Enabled
                        Exit For
                    End If
                Next
            End If

            Dim health As String = GetInfo("SMART Health Status")
            If health = "OK" Then
                devHealth = Status.Passed
            Else
                If devSmartAvailable = Support.Unavailable Then
                    devHealth = Status.Unknown
                Else
                    devHealth = Status.Failed
                End If
            End If

            devTemperature = GetInfo("Current Drive Temperature").Replace(" C", "")
            If Not IsNumeric(devTemperature) Then devTemperature = "N/A"

            scsiGrownDefects = GetInfo("Elements in grown defect list")
            scsiNonMediumErrors = GetInfo("Non-medium error count")

            Dim duration As String = GetInfo("Long (extended) Self Test duration")
            Dim d() As String = duration.Split(" ")
            scsiTestDuration = d(0).Trim

            Dim wcache As String = GetInfo("Writeback Cache is")
            scsiWriteCacheEnabled = False
            scsiWriteCacheAvailable = False
            If wcache.Contains("Disabled") Then
                scsiWriteCacheEnabled = False
                scsiWriteCacheAvailable = True
            ElseIf wcache.Contains("Enabled") Then
                scsiWriteCacheEnabled = True
                scsiWriteCacheAvailable = True
            End If

            Dim rcache As String = GetInfo("Read Cache is")
            scsiReadCacheEnabled = False
            scsiReadCacheAvailable = False
            If rcache.Contains("Disabled") Then
                scsiReadCacheEnabled = False
                scsiReadCacheAvailable = True
            ElseIf rcache.Contains("Enabled") Then
                scsiReadCacheEnabled = True
                scsiReadCacheAvailable = True
            End If

            For i As Short = 0 To devOutput.Length - 2
                e = i
                If Not IsNothing(devOutput(i)) Then
                    If devOutput(i).Contains("Autosave enabled (GLTSD bit set).") Then
                        scsiGltsdEnabled = True
                        Exit For
                    End If
                End If
            Next

            For i As Short = 0 To devOutput.Length - 2
                If devOutput(i).Contains("number of hours powered up") Then
                    Dim t() As String = devOutput(i).Split("=")
                    WorkTimeScsi(t(1).Trim)
                    Exit For
                End If
            Next
        Catch ex As Exception
            Dim diagnostic As New System.Diagnostics.StackTrace(ex, True)
            Main.PrintDebug("Encountered an error! " & ex.StackTrace & " at line " & diagnostic.GetFrame(0).GetFileLineNumber)
            Main.PrintDebug("Output content that generate the error is: '" & devOutput(e) & "'")
        End Try

    End Sub

    Private Sub WorkTimeScsi(ByVal wt As String)
        Dim t As Integer
        If wt.Contains(".") Then
            Dim tt() As String = wt.Split(".")
            t = Val(tt(0))
        Else
            t = Val(wt)
        End If

        'calculate years of activity
        Dim y As Integer = t \ (365 * 24)
        'calculate months of activity
        Dim m As Integer = (t - y * 365 * 24) \ (30 * 24)
        'calculate days of activity
        Dim d As Integer = (t - y * 365 * 24 - m * 30 * 24) \ 24
        'calculate hours of activity
        Dim h As Integer = t - y * 365 * 24 - m * 30 * 24 - d * 24

        'now, build time string
        Dim ts As String = ""

        If t = 1 Then
            scsiWorkTime = t & " " & _h
        Else
            scsiWorkTime = t & " " & _hh
        End If

        If y > 0 Then
            If y = 1 Then
                ts += y & " " & _y
            Else
                ts += y & " " & _yy
            End If
        End If

        If m > 0 Then
            If ts.Length > 0 And y > 0 Then ts += ", "
            If m = 1 Then
                ts += m & " " & _m
            Else
                ts += m & " " & _mm
            End If
        End If

        If d > 0 Then
            If ts.Length > 0 And (m > 0 Or y > 0) Then ts += ", "
            If d = 1 Then
                ts += d & " " & _d
            Else
                ts += d & " " & _dd
            End If
        End If

        If h > 0 Then
            If ts.Length > 0 And (m > 0 Or y > 0 Or d > 0) Then ts += ", "
            If h = 1 Then
                ts += h & " " & _h
            Else
                ts += h & " " & _hh
            End If
        End If

        scsiWorkTimeHuman = ts
    End Sub

    Private Sub GetScsiErrorLog()
        Dim e As Short = 0

        Try
            Dim l As Short = 0

            For i As Short = 0 To devOutput.Length - 2
                e = i
                If Not IsNothing(devOutput(i)) Then
                    If devOutput(i).Contains("Error counter log:") Then
                        scsiHasErrorLog = True
                        l = i + 4
                        Exit For
                    End If
                End If
            Next

            If scsiHasErrorLog Then
                scsiLogRead = New ScsiErrorItem(False)
                scsiLogWrite = New ScsiErrorItem(False)
                scsiLogVerify = New ScsiErrorItem(False)
                For el As Short = l To l + 2
                    If devOutput(el).Contains("read") Then
                        Dim r() As String = devOutput(l).Trim.Split(" ", 8, StringSplitOptions.RemoveEmptyEntries)
                        If r.Length > 0 Then
                            scsiLogRead = New ScsiErrorItem(True, r)
                        End If
                    ElseIf devOutput(el).Contains("write") Then
                        Dim w() As String = devOutput(l + 1).Trim.Split(" ", 8, StringSplitOptions.RemoveEmptyEntries)
                        If w.Length > 0 Then
                            scsiLogWrite = New ScsiErrorItem(True, w)
                        End If
                    ElseIf devOutput(el).Contains("verify") Then
                        Dim v() As String = devOutput(l + 2).Trim.Split(" ", 8, StringSplitOptions.RemoveEmptyEntries)
                        If v.Length > 0 Then
                            scsiLogVerify = New ScsiErrorItem(True, v)
                        End If
                    End If
                Next
            End If
        Catch ex As Exception
            Dim diagnostic As New System.Diagnostics.StackTrace(ex, True)
            Main.PrintDebug("Encountered an error! " & ex.StackTrace & " at line " & diagnostic.GetFrame(0).GetFileLineNumber)
            Main.PrintDebug("Output content that generate the error is: '" & devOutput(e) & "'")
        End Try
    End Sub

    Private Sub GetScsiSelfTest()
        Dim e As Short = 0

        devLastTest = New DeviceLastTest(TestStatus.Unknown)

        Try
            Dim l As Short = 0

            For i As Short = 0 To devOutput.Length - 2
                e = i
                If Not IsNothing(devOutput(i)) Then
                    If devOutput(i).Contains("SMART Self-test log") Then
                        scsiHasSelfTestLog = True
                        l = i + 3
                        Exit For
                    End If
                End If
            Next

            scsiSelfTests.Clear()

            For r As Integer = l To devOutput.Length - 1
                Dim row As String = devOutput(r).Replace(vbCr, "").Replace(vbLf, "")
                If Not row.StartsWith("#") Then Exit For
                Dim s() As String = devOutput(r).Split("[")
                Dim st() As String = s(0).Replace("#", "").Trim.Replace("  ", "_") _
                                   .Split("_", 6, StringSplitOptions.RemoveEmptyEntries)
                Dim skascasq() As String = s(1).Replace("[", "").Replace("]", "").Split(" ", 3, StringSplitOptions.RemoveEmptyEntries)

                If st(0).Trim = "1" Then
                    If st(2).Trim.Contains("Completed") Then
                        devLastTest = New DeviceLastTest(TestStatus.Passed)
                    ElseIf st(2).Trim.Contains("Aborted") Then
                        devLastTest = New DeviceLastTest(TestStatus.Aborted)
                    Else
                        devLastTest = New DeviceLastTest(TestStatus.FailureUnknown)
                    End If
                End If

                scsiSelfTests.Add(New ScsiSelfTestItem(devOutput(r)))
            Next
        Catch ex As Exception
            Dim diagnostic As New System.Diagnostics.StackTrace(ex, True)
            Main.PrintDebug("Encountered an error! " & ex.StackTrace & " at line " & diagnostic.GetFrame(0).GetFileLineNumber)
            Main.PrintDebug("Output content that generate the error is: '" & devOutput(e) & "'")
        End Try
    End Sub
#End Region

#Region "SCSI classes"
    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiDevice
        Dim pd As Device
        Dim _scsiInfo As ScsiInfo
        Dim _scsiTest As ScsiTest
        Dim _scsiLog As ScsiErrorLog
        Dim _scsihealth As ScsiHealth
        Dim _scsiSelfTest As ScsiSelfTestLog
        Dim _scsiFeatures As ScsiFeatures

        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
            _scsiInfo = New ScsiInfo(parentDevice)
            _scsiTest = New ScsiTest(parentDevice)
            _scsiLog = New ScsiErrorLog(parentDevice)
            _scsihealth = New ScsiHealth(parentDevice)
            _scsiSelfTest = New ScsiSelfTestLog(parentDevice)
            _scsiFeatures = New ScsiFeatures(parentDevice)
        End Sub

        Public ReadOnly Property Info As ScsiInfo
            Get
                Return _scsiInfo
            End Get
        End Property

        Public ReadOnly Property Test As ScsiTest
            Get
                Return _scsiTest
            End Get
        End Property

        Public ReadOnly Property ErrorLog As ScsiErrorLog
            Get
                Return _scsiLog
            End Get
        End Property

        Public ReadOnly Property Health As ScsiHealth
            Get
                Return _scsihealth
            End Get
        End Property

        Public ReadOnly Property SelfTestLog As ScsiSelfTestLog
            Get
                Return _scsiSelfTest
            End Get
        End Property

        Public ReadOnly Property Features As ScsiFeatures
            Get
                Return _scsiFeatures
            End Get
        End Property

        Public ReadOnly Property IsScsiDisk As Boolean
            Get
                Return pd.scsiIsDisk
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiInfo
        Dim pd As Device
        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
        End Sub

        Public ReadOnly Property Vendor As String
            Get
                Return pd.scsiVendor
            End Get
        End Property

        Public ReadOnly Property Product As String
            Get
                Return pd.scsiProduct
            End Get
        End Property

        Public ReadOnly Property Revision As String
            Get
                Return pd.scsiRevision
            End Get
        End Property

        Public ReadOnly Property LogicalBlockSize As String
            Get
                Return pd.scsiBlockSize
            End Get
        End Property

        Public ReadOnly Property LogicalUnitId As String
            Get
                Return pd.scsiUnitId
            End Get
        End Property

        Public ReadOnly Property SerialNumber As String
            Get
                Return pd.scsiSerialNumber
            End Get
        End Property

        Public ReadOnly Property UserCapacity As String
            Get
                Return pd.scsiSize
            End Get
        End Property

        Public ReadOnly Property RotationRate As String
            Get
                Return pd.scsiRotation
            End Get
        End Property

        Public ReadOnly Property InterfaceProtocol As String
            Get
                Return pd.scsiProtocol
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiTest
        Dim pd As Device
        Dim _percentage As Long
        Dim _endTest As Date
        Dim _startTest As Date
        Dim _isRunning As Boolean = False
        Dim WithEvents _timer As New Timer

        Private Function ToSeconds(ByVal duration As String) As Integer
            If duration.Contains("minutes") Then
                Return Val(duration) * 60
            Else
                Return Val(duration)
            End If
        End Function

        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
        End Sub

        Public Sub Run()
            _percentage = 0
            Dim d As String = "0"
            Dim smartctl As New Console
            smartctl.SendCmd("-t long " & pd.devLocation)
            _isRunning = True
            _startTest = Now
            _endTest = _startTest.AddSeconds(ToSeconds(pd.scsiTestDuration))
            With _timer
                .Interval = 1000 '1 second
                .Start()
            End With
            smartctl = Nothing
        End Sub

        Private Sub _timer_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles _timer.Tick
            If Now <= _endTest Then
                _percentage = (_endTest.Ticks - Now.Ticks) * 100 \ (_endTest.Ticks - _startTest.Ticks)
            Else
                _percentage = 0
                _isRunning = False
                _endTest = ""
                _timer.Stop()
            End If
        End Sub

        Public Sub Abort()
            Dim smartctl As New Console
            smartctl.SendCmd("-X " & pd.devLocation)
            _timer.Stop()
            _percentage = 0
            _isRunning = False
            _endTest = ""
            smartctl = Nothing
        End Sub

        Public ReadOnly Property Percentage As Long
            Get
                Return _percentage
            End Get
        End Property

        Public ReadOnly Property EndTest As Date
            Get
                Return _endTest
            End Get
        End Property

        Public ReadOnly Property IsRunning As Boolean
            Get
                Return _isRunning
            End Get
        End Property

        Public ReadOnly Property Duration As String
            Get
                Return pd.scsiTestDuration
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiErrorItem
        Private _eccFast, _eccDelayed, _eccReReadsReWrites, _errorsCorrected,
            _correctionAlgorithmInvocations, _gbProcessed, _uncorrectedErrors As String
        Private _hasData As Boolean

        Public Sub New(ByVal hasData As Boolean, Optional ByVal item() As String = Nothing)
            _hasData = hasData
            If Not hasData Then Exit Sub
            _eccFast = item(1)
            _eccDelayed = item(2)
            _eccReReadsReWrites = item(3)
            _errorsCorrected = item(4)
            _correctionAlgorithmInvocations = item(5)
            _gbProcessed = item(6)
            _uncorrectedErrors = item(7)
        End Sub

        Public ReadOnly Property HasData As Boolean
            Get
                Return _hasData
            End Get
        End Property

        Public ReadOnly Property EccFast As String
            Get
                If _hasData Then Return _eccFast Else Return ""
            End Get
        End Property

        Public ReadOnly Property EccDelayed As String
            Get
                If _hasData Then Return _eccDelayed Else Return ""
            End Get
        End Property

        Public ReadOnly Property EccReReadReWrites As String
            Get
                If _hasData Then Return _eccReReadsReWrites Else Return ""
            End Get
        End Property

        Public ReadOnly Property ErrorsCorrected As String
            Get
                If _hasData Then Return _errorsCorrected Else Return ""
            End Get
        End Property

        Public ReadOnly Property CorrectionAlgorithmInvocations As String
            Get
                If _hasData Then Return _correctionAlgorithmInvocations Else Return ""
            End Get
        End Property

        Public ReadOnly Property GbProcessed As String
            Get
                If _hasData Then Return _gbProcessed Else Return ""
            End Get
        End Property

        Public ReadOnly Property UncorrectedErrors As String
            Get
                If _hasData Then Return _uncorrectedErrors Else Return ""
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiErrorLog
        Dim pd As Device

        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
        End Sub

        Public ReadOnly Property Read As ScsiErrorItem
            Get
                Return pd.scsiLogRead
            End Get
        End Property

        Public ReadOnly Property Write As ScsiErrorItem
            Get
                Return pd.scsiLogWrite
            End Get
        End Property

        Public ReadOnly Property Verify As ScsiErrorItem
            Get
                Return pd.scsiLogVerify
            End Get
        End Property

        Public ReadOnly Property HasLog As Boolean
            Get
                Return pd.scsiHasErrorLog
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiHealth
        Dim pd As Device

        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
        End Sub

        Public ReadOnly Property GrownDefectsCount As String
            Get
                Return pd.scsiGrownDefects
            End Get
        End Property

        Public ReadOnly Property NonMediumErrors As String
            Get
                Return pd.scsiNonMediumErrors
            End Get
        End Property
    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiSelfTestItem
        Dim _num, _description, _status, _segmentNumber, _lifeTime, _lbaFirstError, _SK, _ASC, _ASQ As String
        Public Sub New(ByVal selfTest As String)
            Dim s() As String = selfTest.Split("[")
            Dim st() As String = s(0).Replace("#", "").Trim.Replace("  ", "_") _
                                   .Split("_", 6, StringSplitOptions.RemoveEmptyEntries)
            Dim skascasq() As String = s(1).Replace("[", "").Replace("]", "").Split(" ", 3, StringSplitOptions.RemoveEmptyEntries)

            _num = st(0).Trim
            _description = st(1).Trim
            _status = st(2).Trim
            _segmentNumber = st(3).Trim
            _lifeTime = st(4).Trim
            _lbaFirstError = st(5).Trim
            _SK = skascasq(0).Trim
            _ASC = skascasq(1).Trim
            _ASQ = skascasq(2).Trim
        End Sub

        Public ReadOnly Property Number As String
            Get
                Return _num
            End Get
        End Property

        Public ReadOnly Property Description As String
            Get
                Return _description
            End Get
        End Property

        Public ReadOnly Property Status As String
            Get
                Return _status
            End Get
        End Property

        Public ReadOnly Property SegmentNumber As String
            Get
                Return _segmentNumber
            End Get
        End Property

        Public ReadOnly Property LifeTime As String
            Get
                Return _lifeTime
            End Get
        End Property

        Public ReadOnly Property LbaOfFirstError As String
            Get
                Return _lbaFirstError
            End Get
        End Property

        Public ReadOnly Property SK As String
            Get
                Return _SK
            End Get
        End Property

        Public ReadOnly Property ASC As String
            Get
                Return _ASC
            End Get
        End Property

        Public ReadOnly Property ASQ As String
            Get
                Return _ASQ
            End Get
        End Property

    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiSelfTestLog
        Dim pd As Device

        Public Sub New(ByRef parentDevice As Device)
            pd = parentDevice
        End Sub

        Public ReadOnly Property HasLog As Boolean
            Get
                Return pd.scsiHasSelfTestLog
            End Get
        End Property

        Public ReadOnly Property Item As List(Of ScsiSelfTestItem)
            Get
                Return pd.scsiSelfTests
            End Get
        End Property
    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiWriteCache
        Dim _pd As Device
        Public Sub New(ByRef parentDevice As Device)
            _pd = parentDevice
        End Sub

        Public ReadOnly Property Available As Boolean
            Get
                Return _pd.scsiWriteCacheAvailable
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return _pd.scsiWriteCacheEnabled
            End Get
        End Property

        Public Sub Disable()
            _pd.smartctl.SendCmd("-s wcache,off " & _pd.devLocation)
            _pd.Update()
        End Sub

        Public Sub Enable()
            _pd.smartctl.SendCmd("-s wcache,on " & _pd.devLocation)
            _pd.Update()
        End Sub
    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiReadCache
        Dim _pd As Device
        Public Sub New(ByRef parentDevice As Device)
            _pd = parentDevice
        End Sub

        Public ReadOnly Property Available As Boolean
            Get
                Return _pd.scsiReadCacheAvailable
            End Get
        End Property

        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return _pd.scsiReadCacheEnabled
            End Get
        End Property

        Public Sub Disable()
            _pd.smartctl.SendCmd("-s rcache,off " & _pd.devLocation)
            _pd.Update()
        End Sub

        Public Sub Enable()
            _pd.smartctl.SendCmd("-s rcache,on " & _pd.devLocation)
            _pd.Update()
        End Sub
    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiGltsd
        Dim _pd As Device
        Public Sub New(ByRef parentDevice As Device)
            _pd = parentDevice
        End Sub

        Public ReadOnly Property IsEnabled As Boolean
            Get
                Return _pd.scsiGltsdEnabled
            End Get
        End Property

        Public Sub Disable()
            _pd.smartctl.SendCmd("-S off " & _pd.devLocation)
            _pd.Update()
        End Sub

        Public Sub Enable()
            _pd.smartctl.SendCmd("-S on " & _pd.devLocation)
            _pd.Update()
        End Sub
    End Class

    <System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)>
    Public Class ScsiFeatures
        Dim _scsiWCache As ScsiWriteCache
        Dim _scsiRCache As ScsiReadCache
        Dim _scsiGltsd As ScsiGltsd

        Public Sub New(ByRef parentDevice As Device)
            _scsiWCache = New ScsiWriteCache(parentDevice)
            _scsiRCache = New ScsiReadCache(parentDevice)
            _scsiGltsd = New ScsiGltsd(parentDevice)
        End Sub

        Public ReadOnly Property WriteCache As ScsiWriteCache
            Get
                Return _scsiWCache
            End Get
        End Property

        Public ReadOnly Property ReadCache As ScsiReadCache
            Get
                Return _scsiRCache
            End Get
        End Property

        Public ReadOnly Property Gltsd As ScsiGltsd
            Get
                Return _scsiGltsd
            End Get
        End Property
    End Class
#End Region

#Region "SCSI properties"
    Public ReadOnly Property Scsi As ScsiDevice
        Get
            Return _scsiDevice
        End Get
    End Property
#End Region
End Class
