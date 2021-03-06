﻿'HDD Guardian is a GUI for smartcl utility, part of smartmontools
'
'home pages are http://code.google.com/p/hddguardian/
'               http://hddguardian.codeplex.com/
'
'Copyright (C) 2010-2015 Samuele Parise
'
'This Source Code Form is subject to the terms of the Mozilla Public
'License, v. 2.0. If a copy of the MPL was not distributed with this
'file, You can obtain one at http://mozilla.org/MPL/2.0/.

Imports System.IO

Partial Class Main

    Private Sub LoadIcons()
        On Error Resume Next

        Dim iconsfolder As String = My.Application.Info.DirectoryPath & "\graphics\icons\"
        Dim pichelp As Image = Image.FromFile(iconsfolder & "help.png")

        picAdmin.Image = Image.FromFile(iconsfolder & "exclamation.png")
        picNewVersion.Image = Image.FromFile(iconsfolder & "new.png")

        '**************************************
        '*** overview and other big buttons ***
        '**************************************
        rbtUpdateAll.Icon = My.Resources.view_refresh
        rbtRescanUsb.Icon = My.Resources.rescan_usb
        rbtAddVirtual.Icon = My.Resources.add_virtual
        rbtSaveDebug.Icon = My.Resources.document_save
        rbtClean.Icon = My.Resources.trash
        'help
        picOverviewHelp.Image = pichelp

        '*****************************
        '*** manage ribbon buttons ***
        '*****************************
        'help
        picManageHelp.Image = pichelp
        'main
        robInfo.Icon = Image.FromFile(iconsfolder & "information.png")
        robHealth.Icon = Image.FromFile(iconsfolder & "health.png")
        robAttributes.Icon = Image.FromFile(iconsfolder & "smart-list.png")
        robEventsLog.Icon = Image.FromFile(iconsfolder & "events-list.png")
        robEventsChart.Icon = Image.FromFile(iconsfolder & "chart.png")
        robTemperatures.Icon = Image.FromFile(iconsfolder & "temperature.png")
        'setup
        robSmartSetup.Icon = Image.FromFile(iconsfolder & "smart-setup.png")
        robAam.Icon = Image.FromFile(iconsfolder & "aam.png")
        robApm.Icon = Image.FromFile(iconsfolder & "apm.png")
        robStandby.Icon = Image.FromFile(iconsfolder & "standby.png")
        robReadWrite.Icon = Image.FromFile(iconsfolder & "read-write.png")
        'errors and tests
        robErrorsLog.Icon = Image.FromFile(iconsfolder & "errors-log.png")
        robTestsLog.Icon = Image.FromFile(iconsfolder & "tests-results.png")
        robRunTest.Icon = Image.FromFile(iconsfolder & "tests-run.png")
        'smartctl
        robOutput.Icon = Image.FromFile(iconsfolder & "output.png")
        robAdvanced.Icon = Image.FromFile(iconsfolder & "switch.png")

        '************************
        '*** advanced section ***
        '************************
        Dim advImgList As New ImageList
        With advImgList
            .Images.Add(Image.FromFile(iconsfolder & "format.png"))
            .Images.Add(Image.FromFile(iconsfolder & "debug.png"))
            .Images.Add(Image.FromFile(iconsfolder & "powermode.png"))
        End With
        tabAdvanced.ImageList = advImgList
        tpRemap.ImageIndex = 0
        tpFirmware.ImageIndex = 1
        tpPower.ImageIndex = 2

        '****************************
        '*** events log and chart ***
        '****************************
        'log
        btnPrev.Image = Image.FromFile(iconsfolder & "calendar-previous.png")
        btnNext.Image = Image.FromFile(iconsfolder & "calendar-next.png")
        btnToday.Image = Image.FromFile(iconsfolder & "calendar-today.png")
        btnReload.Image = Image.FromFile(iconsfolder & "update.png")
        btnCopy.Image = Image.FromFile(iconsfolder & "copy.png")
        btnSaveLog.Image = Image.FromFile(iconsfolder & "export.png")
        'chart
        btnReloadDevice.Image = Image.FromFile(iconsfolder & "update.png")

        '**********************
        '*** advanced panel ***
        '**********************
        With imlAttr.Images
            .Clear()
            .Add("never", My.Resources.failed_never)
            .Add("inthepast", My.Resources.failed_inthepast)
            .Add("now", My.Resources.failed_now)
            .Add("warning", My.Resources.failed_warning)
            .Add("unknown", My.Resources.failed_unknown)
        End With

        '***********************
        '*** settings window ***
        '***********************
        With Settings
            .robGeneral.Icon = Image.FromFile(iconsfolder & "settings-appearance.png")
            .robDetection.Icon = Image.FromFile(iconsfolder & "check.png")
            .robTranslation.Icon = My.Resources.worldflag
            .robMailAlerts.Icon = Image.FromFile(iconsfolder & "mail.png")
            .robUpdate.Icon = Image.FromFile(iconsfolder & "update.png")
            .robMonitoring.Icon = Image.FromFile(iconsfolder & "health.png")
            .robWarnings.Icon = Image.FromFile(iconsfolder & "settings-bell.png")
            .robShare.Icon = Image.FromFile(iconsfolder & "settings-share.png")
            .robRating.Icon = Image.FromFile(iconsfolder & "star.png")
            .robTolerance.Icon = Image.FromFile(iconsfolder & "tolerance.png")
            .robControllers.Icon = Image.FromFile(iconsfolder & "controllers.png")
            .robSmartctl.Icon = Image.FromFile(iconsfolder & "smartctl.png")
            'warning
            .picNeedRestart.Image = Image.FromFile(iconsfolder & "exclamation.png")
            .picNeedRestartControllers.Image = Image.FromFile(iconsfolder & "exclamation.png")
            'help icons
            .picHelpSetupGeneral.Image = pichelp
            .picHelpSetupDetection.Image = pichelp
            .picHelpSetupTranslation.Image = pichelp
            .picHelpSetupUpdate.Image = pichelp
            .picHelpSetupMonitoring.Image = pichelp
            .picHelpSetupWarnings.Image = pichelp
            .picHelpSetupShare.Image = pichelp
            .picHelpSetupRating.Image = pichelp
            .picHelpSetupMailAlerts.Image = pichelp
            .picHelpSetupTolerance.Image = pichelp
            .picHelpSetupControllers.Image = pichelp
            .picHelpSetupSmartctl.Image = pichelp
        End With

        '**************************
        '*** system list images ***
        '**************************
        imlDevice.Images.Clear()
        imlDevice.Images.Add("pie", Image.FromFile(iconsfolder & "pie.png"))
        imlDevice.Images.Add("folder", Image.FromFile(iconsfolder & "folder-doc.png"))
        imlDevice.Images.Add("windrive", My.Resources.drive_ok)
        imlDevice.Images.Add("pc", Image.FromFile(iconsfolder & "monitor.png"))

        '**********************
        '*** system buttons ***
        '**********************
        rbtRefresh.Icon = My.Resources.view_refresh
        picWmiDevicesHelp.Image = pichelp

        '********************************
        '*** contacts into about page ***
        '********************************
        With About
            .picGroup.Image = Image.FromFile(iconsfolder & "group.png")
            .picEmail.Image = Image.FromFile(iconsfolder & "mail.png")
            .picPlus.Image = Image.FromFile(iconsfolder & "plus.png")
        End With
    End Sub

End Class
