﻿Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Deployment.Application

Module modUpdateHttp

    Function AutoUpdateHttp(ByVal CurrentVersion As String) As Integer

#Const ForceUpdate = False    ' True = Force an update regardless of server version.

        ' KXG Apr 2015
        ' This is a new Version to replace the FTP version that I have used upto version v2.0.0.5
        ' It has been written because the FTP version required the use of a UserName and a Password to
        ' gain access to the updated files hosted on the FTP server provided by TheQuestor.
        ' As we go-live in a true OpenSource environment I needed to remove this Vulnerability.
        ' The basic principal of operation remains though.

        ' Due to TheQuestor's servers not allowing http access we have changed to x10Hosting.com
        ' for the primary update server and 000webhost.com as the secondary update server. 

        ' Due to issues downloading some file types from almost all the web servers the files names are
        ' appended with teh characters "apm" - after downloading the files are renamed.

        ' Encryption has been removed as this is no longer required.

        ' ---------------------------------------------------------------------------------------------

        'The function will check a website to see if there are any updates for a program.
        'If found it will download a program called UpdaterProgram.exe (used to update the
        'folders on the local machine).
        'Once we have the UpdaterProgram.exe is will create a file on the local machine called 
        'Update.txt, this file contains all the information like version numbers and installation
        'paths that will be required by the UpdaterProgram. Once these are in place the
        'APM_Log_Analiser program will pass control to the UpdaterProgram and then close down.
        'The UpdaterProgram will install the new APM Log Analiser.exe and then start the
        'new version, UpdaterProgram will also pull any new files that are required.

        'The Users CurrentVersion must be passed in the format "v0.0.0.0"
        'The function will return:
        '   False = Failed to execute the code successfully
        '   True = Code executed successfully but program did not require updating.
        '   99 = Code executed successfully and a new update was downloaded as required.

        'The website must contain a minimum of three files:
        '   APM Log File Analiser.exe = The latest program .exe file.
        '   UpdaterProgram.exe = The lastest updater program .exe file.
        '   Versions.txt = Must contain a list of programs and versions available.

        'Versions.txt formatting (create in notepad):
        '   [ProgramName]=[V?.?]=[DownloadLink]
        '   [ProgramName]=[V?.?]=[DownloadLink]
        '   [ProgramName]=[V?.?]=[DownloadLink]
        '   etc.

        'Versions.txt Example:
        'APM Log File Analiser.exe=v1.0=ftp://bionicbone.sshcs.com/APM Log File Analiser.exe
        'Avitar.exe=v1.0=ftp://bionicbone.sshcs.com/UpdateAvitar.exe
        'ThisProgram.exe=v1.0=ftp://bionicbone.sshcs.com/Updates with this name for example.exe

        Try
            Debug.Print("Checking for updates...")

            Debug.Print("ProgramInstallationFolder: " & ProgramInstallationFolder)
            Debug.Print("TempProgramInstallationFolder " & TempProgramInstallationFolder)

            If CurrentVersion = "" Then CurrentVersion = "v0.0.0.1"             'if the function is called incorrectly, force an update!
            AutoUpdateHttp = False                                              'Set as function failed
            Dim ProgramName As String = "APM Log File Analiser.exe"             'This program we want to update.
            Dim UpdaterProgramName As String = "UpdaterProgram.exe"             'This program we will use to update the main program.
            Dim UpdateToLocaleFolder As String = "C:\Temp"
            Dim SiteName1 As String = "http://apmloganalyser.x10host.com/"      'New http address for Update Server 1
            Dim SiteName2 As String = "http://www.apmloganalyser.net63.net/"    'New http address for Update Server 2
            Dim SiteName As String = ""
            Dim SiteUpdatePath As String = "update_v2_1_onwards/"
            Dim SiteVersionsPath As String = "versions/"
            Dim New_VersionFileName As String = "versions.html"                 ' Changed to html from v2.1.0.0
            Dim GetVer As String = ""
            Dim GetUpd As Integer = 0
            Dim WebRequest As Net.HttpWebRequest = Nothing
            Dim WebResponse As Net.HttpWebResponse = Nothing
            Dim stream1 As StreamReader = Nothing
            Dim ReadSource As String = Nothing


            ' If we are developing then Exit the Update checks
#If ForceUpdate = False Then
            If IsDeveloping() = True Then
                Debug.Print("Forcing code to not allow update checks")
                UpdateYesNo = 98 'signal finished OK
                Exit Function
            End If
#Else
            Debug.Print("WARNING: Updates are Being Forced by Testing Code!!!")
#End If


            ' First we must detect which update server is available
            If CheckAddress(SiteName1 & SiteVersionsPath & New_VersionFileName) = True Then
                SiteName = SiteName1
            End If
            ' Only look at Site2 if Site1 was not available.
            If SiteName = "" Then
                If CheckAddress(SiteName2 & SiteVersionsPath & New_VersionFileName) = True Then
                    SiteName = SiteName2
                End If
            End If
            If SiteName = "" Then
                'No Update Servers Available
                AutoUpdateHttp = True
                MsgBox("No Update Servers Available", vbOKOnly)
                UpdateYesNo = 98 'signal finished OK
                AutoUpdateHttp = False
                Exit Function
            End If


            ' At this point we know everything except the Update Server Version.

            ' #####################################
            ' ### Get the Server Version Number ###
            ' #####################################
            Debug.Print("Update Server: " & SiteName)
            Debug.Print("Update Server Path: " & SiteName & SiteUpdatePath)
            Debug.Print("Program Name: " & ProgramName)
            Debug.Print("Current User Version: " & MyCurrentVersionNumber)
            Debug.Print("Opening http update Connection...")

            ' Create the connection to the update server version file
            WebRequest = Net.WebRequest.Create(SiteName & SiteVersionsPath & New_VersionFileName)
            Debug.Print("Requesting file: " & New_VersionFileName)
            Debug.Print("Waiting for a response...")
            WebResponse = WebRequest.GetResponse
            ' The StreamReader will read the returned file.
            stream1 = New StreamReader(WebResponse.GetResponseStream())
            ReadSource = stream1.ReadToEnd
            Debug.Print(New_VersionFileName & " contents: " & ReadSource)
            Dim Regex As New Regex(ProgramName & "=v(\d+).(\d+).(\d+).(\d+)=(\d+)")  '=(.*?).exe")
            Debug.Print("Using Regex to find Mastches: " & ProgramName)
            Dim matches As MatchCollection = Regex.Matches(ReadSource)
            Debug.Print("Found: " & matches.Count & ", program is known to server!")

            For Each match As Match In matches
                Dim RegSplit() As String = Split(ReadSource.ToString, "=")
                Debug.Print("Getting Current Server Version for Match: " & match.Index + 1 & "...")
#If ForceUpdate = False Then
                ' Verison will be returned with the format v?.?.?.?
                GetVer = RegSplit(1)
                Debug.Print("Current Server Version: " & GetVer)
#Else
                GetVer = "v9.9.9.9"
                Debug.Print("Forced Server Version: " & GetVer)
#End If
                Debug.Print("Current User Version: " & MyCurrentVersionNumber)
            Next

            'Close current connections in case Http will only allow one connection!
            Debug.Print("Closing current update Http connection...")
            stream1.Close()
            WebResponse.Close()
            WebRequest = Nothing

            ' Versions will have the format v?.?.?.?
            Debug.Print("Checking versions...")
            If GetVer > MyCurrentVersionNumber Then
                Debug.Print("Update is available!")

                ' Update the Updates Form to display the version numbers
                frmUpdate.ActivateUpdateLabels(True, MyCurrentVersionNumber, GetVer)
                UpdateYesNo = 99

                ' At this stage the Updates Form is displaying the current version and the available update version.
                ' The user must select Yes or No at this stage.
                ' Wait for response from the updates form.
                While UpdateYesNo = 99
                    Application.DoEvents()
                End While
                frmUpdate.Close()

                'UpdateYesNo = True when the user selected Yes on the Update Form
                If UpdateYesNo = True Then
                    Debug.Print("User selected ""Yes"" to update...")

                    'Prepare to Get the updated program file via Http.
                    Dim buffer(1023) As Byte        ' Allocate a read buffer of 1kB size
                    Dim output As IO.Stream         ' A file to save response
                    Dim bytesIn As Integer = 0      ' Number of bytes read to buffer
                    Dim totalBytesIn As Integer = 0 ' Total number of bytes received (= filesize)

                    'Prepare to Get the updater program file via Http.
                    bytesIn = 0                     ' Number of bytes read to buffer
                    totalBytesIn = 0                ' Total number of bytes received (= filesize)

                    ' #####################################
                    ' ### Get the Updater Program       ###
                    ' #####################################

                    'Prepare new Http connection to Get the required file.
                    Debug.Print("Preparing a new Http Download connection...")
                    Debug.Print("Requesting file: " & UpdaterProgramName & "...")
                    WebRequest = Net.WebRequest.Create(SiteName & SiteUpdatePath & UpdaterProgramName & "apm")
                    WebRequest.Method = Net.WebRequestMethods.Http.Get

                    Debug.Print("Waiting for a response...")
                    WebResponse = WebRequest.GetResponse
                    Debug.Print("Found Http File.")

                    'Need to check local folder exists, if not create it.
                    Debug.Print("Preparing to copy file to local machine...")
                    Debug.Print("Checking for the local folder exists: " & UpdateToLocaleFolder)
                    If Directory.Exists(UpdateToLocaleFolder) Then
                        Debug.Print("Found: " & UpdateToLocaleFolder & ", no further action required.")
                    Else
                        Debug.Print("Not Found: " & UpdateToLocaleFolder)
                        Debug.Print("Creating: " & UpdateToLocaleFolder)
                        Directory.CreateDirectory(UpdateToLocaleFolder)
                        Debug.Print("Success!")
                    End If


                    'Need to check that the file we are about to create does not already exist.
                    'We know the folder already exists from the step above
                    Debug.Print("Checking previous update does not still exist: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                    If File.Exists(UpdateToLocaleFolder & "\" & UpdaterProgramName & "apm") Then
                        Debug.Print("Found: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                        Debug.Print("Attempting to delete old update file: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                        File.Delete(UpdateToLocaleFolder & "\" & UpdaterProgramName & "apm")
                        Debug.Print("Success!")
                    Else
                        Debug.Print("Not Found: " & UpdateToLocaleFolder & "\" & UpdaterProgramName & ", no further action required.")
                    End If

                    ' Write the content to the output file
                    Debug.Print("Creating Local Update File: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                    output = File.Create(UpdateToLocaleFolder & "\" & UpdaterProgramName & "apm")
                    Debug.Print("Success, at least by name, file is still empty!")

                    Debug.Print("Opening the stream...")
                    Dim stream3 As Stream = WebRequest.GetResponse.GetResponseStream

                    Debug.Print("Writing file contents...")
                    bytesIn = 1 ' Set initial value to 1 to get into loop. We get out of the loop when bytesIn is zero
                    Do Until bytesIn < 1
                        bytesIn = stream3.Read(buffer, 0, 1024) ' Read max 1024 bytes to buffer and get the actual number of bytes received
                        If bytesIn > 0 Then
                            ' Dump the buffer to a file
                            output.Write(buffer, 0, bytesIn)
                            ' Calc total filesize
                            totalBytesIn += bytesIn
                            ' Show user the filesize
                            Application.DoEvents()
                        End If
                    Loop
                    Debug.Print("Total Bytes Downloaded: " & totalBytesIn.ToString)
                    Debug.Print("Success!")
                    ' Close streams
                    Debug.Print("Closing Http download connection...")
                    output.Close()
                    stream3.Close()
                    WebResponse.Close()

                    ' Need to check that the file without the "apm" does not already exist.
                    Debug.Print("Checking previous update does not still exist: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                    If File.Exists(UpdateToLocaleFolder & "\" & UpdaterProgramName) Then
                        Debug.Print("Found: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                        Debug.Print("Attempting to delete old update file: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)
                        File.Delete(UpdateToLocaleFolder & "\" & UpdaterProgramName)
                        Debug.Print("Success!")
                    Else
                        Debug.Print("Not Found: " & UpdateToLocaleFolder & "\" & UpdaterProgramName & ", no further action required.")
                    End If

                    ' Rename the file to loose the "apm" from the end.
                    Rename(UpdateToLocaleFolder & "\" & UpdaterProgramName & "apm", UpdateToLocaleFolder & "\" & UpdaterProgramName)


                    'Call the updater program to do its stuff
                    Debug.Print("Start the Updater Program: " & UpdateToLocaleFolder & "\" & UpdaterProgramName)

                    Dim StartInfo As New ProcessStartInfo
                    StartInfo.FileName = UpdateToLocaleFolder & "\" & UpdaterProgramName
                    StartInfo.Arguments = """" & ProgramName & """ " & MyCurrentVersionNumber & " " & GetVer & " """ & ProgramInstallationFolder & """ " & """" & UpdateToLocaleFolder & """ " & SiteName & SiteUpdatePath
                    Process.Start(StartInfo)

                    Debug.Print("Close the APM Log Anayliser Program so it can update...")
                    Debug.Print(vbNewLine)
                    frmMainForm.Close()

                    WebResponse.Close()
                    WebResponse = Nothing
                    WebRequest = Nothing
                Else
                    Debug.Print("User selected not to update...")
                    UpdateYesNo = 98   ' Update finished
                End If
            Else
                'Close current connections!
                Debug.Print("Program does not require an update.")
                Debug.Print("Closing Http update connection...")
                stream1.Close()
                WebResponse.Close()
                Debug.Print("Success!")
                AutoUpdateHttp = True
            End If
            Debug.Print("The check for updates has completed successfully")
            Debug.Print(vbNewLine)
            UpdateYesNo = 98   ' Update finished
            WebResponse.Close()
            stream1.Close()
            WebRequest = Nothing
            WebResponse = Nothing
            stream1 = Nothing
            ReadSource = Nothing

        Catch
            AutoUpdateHttp = False
            UpdateYesNo = 98   ' Update finished
            Debug.Print("An Error Occured in the Update Function")
            Debug.Print("Exit Function")
            Debug.Print(vbNewLine)
            Exit Function
        End Try

    End Function

End Module
