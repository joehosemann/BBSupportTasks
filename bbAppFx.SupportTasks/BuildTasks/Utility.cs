using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Blackbaud.AppFx.Platform.Automation;
using Blackbaud.AppFx.Platform.Automation.PathHelpers;
using Blackbaud.AppFx.Platform.BuildTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualBasic.Devices;
using Microsoft.VisualBasic.FileIO;

namespace bbAppFx.SupportTasks.BuildTasks
{
    class Utility
    {
        // Events
        public static event EventHandler<AutomationLogEventArgs> AutomationLogEvent;

        // Methods
        public static void CheckFileArgument(string argumentValue, string argumentName)
        {
            if (Strings.Len(argumentValue) == 0)
            {
                throw new ArgumentException("The " + argumentName + " cannot be null or an emptry string.");
            }
            if (!File.Exists(argumentValue))
            {
                throw new FileNotFoundException(argumentValue + " does not exist");
            }
        }

        public static MemoryStream ConvertXMLStringToMemoryStream(string XMLString)
        {
            MemoryStream stream2 = new MemoryStream(XMLString.Length - 1);
            foreach (char ch in XMLString.ToCharArray())
            {
                stream2.WriteByte(Convert.ToByte(ch));
            }
            stream2.Position = 0L;
            return stream2;
        }

        public static string ExtractPatchNumberFromBuildNumber(string buildNumberWithPatch)
        {
            string[] array = Strings.Split(buildNumberWithPatch, ".", -1, CompareMethod.Binary);
            return array[Information.UBound(array, 1)];
        }

        public static bool FileShouldBeCopied(string sourceFile, string destinationFile)
        {
            return FileShouldBeCopied(sourceFile, destinationFile, true);
        }

        public static bool FileShouldBeCopied(string sourceFile, string destinationFile, bool checkFileVersion)
        {
            if (File.Exists(destinationFile))
            {
                bool flag = true;
                if (checkFileVersion)
                {
                    Version safeVersion = GetSafeVersion(FileVersionInfo.GetVersionInfo(sourceFile).FileVersion);
                    if (safeVersion != null)
                    {
                        Version version2 = GetSafeVersion(FileVersionInfo.GetVersionInfo(destinationFile).FileVersion);
                        if (version2 != null)
                        {
                            flag = safeVersion > version2;
                            if (flag)
                            {
                                Trace.WriteLine(string.Format("{0} will be copied because its file version of '{1}' is higher than the target's file version of '{2}'.", sourceFile, safeVersion.ToString(), version2.ToString()));
                            }
                        }
                    }
                }
                if (!flag)
                {
                    FileInfo info = new FileInfo(sourceFile);
                    FileInfo info2 = new FileInfo(destinationFile);
                    flag = DateTime.Compare(info.LastWriteTime, info2.LastWriteTime) > 0;
                    if (flag)
                    {
                        Trace.WriteLine(string.Format("{0} will be copied because its modified date of '{1}' is more recent than the modified date of the target file '{2}'.", sourceFile, info.LastWriteTime.ToString(), info2.LastWriteTime.ToString()));
                    }
                }
                return flag;
            }
            return true;
        }

        public static int GetEnumeratedValueFromName(Type enumeratedType, string memberName)
        {
            IEnumerator enumerator = null;
            try
            {
                enumerator = Enum.GetValues(enumeratedType).GetEnumerator();
                while (enumerator.MoveNext())
                {
                    object objectValue = RuntimeHelpers.GetObjectValue(enumerator.Current);
                    if (Strings.StrComp(objectValue.ToString(), memberName, CompareMethod.Text) == 0)
                    {
                        return Conversions.ToInteger(objectValue);
                    }
                }
            }
            finally
            {
                if (enumerator is IDisposable)
                {
                    (enumerator as IDisposable).Dispose();
                }
            }
            return -1;
        }

        public static string GetExceptionData(Exception ex)
        {
            StringBuilder builder = new StringBuilder();
            try
            {
                if ((ex.Data != null) && (ex.Data.Count > 0))
                {
                    IEnumerator enumerator = null;
                    try
                    {
                        enumerator = ex.Data.Values.GetEnumerator();
                        while (enumerator.MoveNext())
                        {
                            string str2 = Conversions.ToString(enumerator.Current);
                            if (builder.Length > 0)
                            {
                                builder.Append("\r\n");
                            }
                            else
                            {
                                builder.Append(ex.Message);
                                builder.Append("\r\n");
                            }
                            builder.Append(str2);
                        }
                    }
                    finally
                    {
                        if (enumerator is IDisposable)
                        {
                            (enumerator as IDisposable).Dispose();
                        }
                    }
                }
            }
            catch (Exception exception1)
            {
                ProjectData.SetProjectError(exception1);
                ProjectData.ClearProjectError();
            }
            if (builder.Length > 0)
            {
                return builder.ToString();
            }
            return ex.Message;
        }

        public static string GetItemMetaData(ITaskItem item, string metaDataName, string defaultValue = "")
        {
            string expression = defaultValue;
            try
            {
                expression = item.GetMetadata(metaDataName);
                if (Strings.Len(expression) == 0)
                {
                    expression = defaultValue;
                }
            }
            catch (Exception exception1)
            {
                ProjectData.SetProjectError(exception1);
                ProjectData.ClearProjectError();
            }
            return expression;
        }

        public static Version GetSafeVersion(string fileVersionString)
        {
            Version version2 = null;
            if (Strings.Len(fileVersionString) > 0)
            {
                try
                {
                    version2 = new Version(fileVersionString);
                }
                catch (Exception exception1)
                {
                    ProjectData.SetProjectError(exception1);
                    Exception exception = exception1;
                    ProjectData.ClearProjectError();
                }
            }
            return version2;
        }

        public static bool IsRunFromMSBuild(Task myTask)
        {
            return ((myTask != null) && (myTask.BuildEngine != null));
        }

        public static string MergePatchNumberIntoBuildNumber(string buildNumber, string patchNumber)
        {
            if (Strings.Len(buildNumber) <= 0)
            {
                throw new ArgumentException("Unable to merge patch number into build number. One of these values can be an empty string.");
            }
            if (Strings.Len(patchNumber) > 0)
            {
                string[] array = Strings.Split(buildNumber, ".", -1, CompareMethod.Binary);
                array[Information.UBound(array, 1)] = patchNumber;
                return Strings.Join(array, ".");
            }
            return buildNumber;
        }

        public static void MyCopyAllFilesInFolder(Task buildTask, string sourceFolder, string targetFolder, bool overWrite, bool onlyReplaceWithNewerFile, bool continueOnError)
        {
            MyCopyAllFilesInFolder(buildTask, sourceFolder, targetFolder, "*.*", overWrite, onlyReplaceWithNewerFile, continueOnError);
        }

        public static void MyCopyAllFilesInFolder(Task buildTask, string sourceFolder, string targetFolder, string wildCards, bool overWrite, bool onlyReplaceWithNewerFile, bool continueOnError)
        {
            MyCopyAllFilesInFolder(buildTask, sourceFolder, targetFolder, wildCards, overWrite, onlyReplaceWithNewerFile, continueOnError, Microsoft.VisualBasic.FileIO.SearchOption.SearchAllSubDirectories);
        }

        public static void MyCopyAllFilesInFolder(Task buildTask, string sourceFolder, string targetFolder, string wildCards, bool overWrite, bool onlyReplaceWithNewerFile, bool continueOnError, Microsoft.VisualBasic.FileIO.SearchOption searchOption)
        {
            MyCopyAllFilesInFolder(buildTask, sourceFolder, targetFolder, wildCards, overWrite, onlyReplaceWithNewerFile, continueOnError, searchOption, false, string.Empty, string.Empty);
        }

        public static void MyCopyAllFilesInFolder(Task buildTask, string sourceFolder, string targetFolder, string wildCards, bool overWrite, bool onlyReplaceWithNewerFile, bool continueOnError, Microsoft.VisualBasic.FileIO.SearchOption searchOption, bool useRoboCopy, string versionToCopy, string logText)
        {
            var myComputer = new Computer();
            RobocopyWrapper myWrapper = null;
            if (useRoboCopy)
            {
                myWrapper = new RobocopyWrapper();
                useRoboCopy = myWrapper.IsInstalled;
                if (useRoboCopy)
                {
                    if (myWrapper.SupportsMultiThreading)
                    {
                        myWrapper.NumberOfThreads = 8;
                    }
                    else
                    {
                        myWrapper.NumberOfThreads = 1;
                    }
                    switch (searchOption)
                    {
                        case Microsoft.VisualBasic.FileIO.SearchOption.SearchTopLevelOnly:
                            myWrapper.RecursiveMode = RoboCopyRecursiveCopy.CopyTopLevelOnly;
                            break;

                        case Microsoft.VisualBasic.FileIO.SearchOption.SearchAllSubDirectories:
                            myWrapper.RecursiveMode = RoboCopyRecursiveCopy.CopyAllSubdirectories;
                            break;
                    }
                }
            }
            if (Strings.Len(wildCards) == 0)
            {
                wildCards = "*.*";
            }
            if (Strings.Len(logText) == 0)
            {
                logText = "Copying folder from '{0}' to '{1}'...";
            }
            if (Directory.Exists(sourceFolder))
            {
                MyLogMessage(buildTask, string.Format(logText, sourceFolder, targetFolder), MessageImportance.Normal);
                if (useRoboCopy)
                {
                    MyCopyAllFilesWithRoboCopy(buildTask, myWrapper, sourceFolder, targetFolder, wildCards, onlyReplaceWithNewerFile, continueOnError, versionToCopy);
                }
                else
                {
                    //foreach (string str2 in _MyComputer.FileSystem.GetFiles()
                    foreach (string str2 in myComputer.FileSystem.GetFiles(sourceFolder, searchOption, new string[] { wildCards }))
                    {
                        string targetFile = "";
                        try
                        {
                            string fullPath = Path.GetFullPath(sourceFolder);
                            targetFile = PathHelper.Combine(targetFolder, Strings.Right(str2, (str2.Length - fullPath.Length) - 1));
                            MyCopyFile(buildTask, str2, targetFile, overWrite, onlyReplaceWithNewerFile, "Copying file from '{0}' to '{1}'...", false);
                        }
                        catch (Exception exception1)
                        {
                            ProjectData.SetProjectError(exception1);
                            Exception ex = exception1;
                            if (!continueOnError)
                            {
                                throw;
                            }
                            string exceptionData = GetExceptionData(ex);
                            MyLogWarning(buildTask, string.Format("Error copying file '{0}' to '{1}'. The following error was raised: '{2}'.", str2, targetFile, exceptionData));
                            ProjectData.ClearProjectError();
                        }
                    }
                }
            }
            else
            {
                MyLogMessage(buildTask, string.Format("Source folder '{0}' does not exist and cannot be copied to '{1}'.", sourceFolder, targetFolder), MessageImportance.Normal);
            }
        }

        private static void MyCopyAllFilesWithRoboCopy(Task buildTask, RobocopyWrapper myWrapper, string sourceFolder, string targetFolder, string wildCards, bool onlyReplaceWithNewerFile, bool continueOnError, string versionToCopy)
        {
            myWrapper.SourceFolder = sourceFolder;
            myWrapper.TargetFolder = targetFolder;
            myWrapper.FilesToInclude.Add(wildCards);
            myWrapper.UseRestartMode = false;
            myWrapper.CopyNewerFilesOnly = onlyReplaceWithNewerFile;
            if (Strings.Len(versionToCopy) > 0)
            {
                myWrapper.LogFileName = string.Format("FileDeploymentRobocopyLog_{0}.txt", versionToCopy);
            }
            else
            {
                myWrapper.LogFileName = "FileDeploymentRobocopyLog.txt";
            }
            myWrapper.AppendToExistingLogFile = true;
            try
            {
                RoboCopyExitCodes codes = myWrapper.Execute();
                switch (codes)
                {
                    case RoboCopyExitCodes.SeveralFilesFailedtoCopy:
                    case RoboCopyExitCodes.SevereErrorNoFilesCopied:
                        if (!continueOnError)
                        {
                            throw new AutomationException(string.Format("RoboCopy returned an error code which indicates that some or all files may not have copied successfully: {0}", codes.ToString()));
                        }
                        MyLogWarning(buildTask, string.Format("RoboCopy returned an error code which indicates that some or all files may not have copied successfully: {0}", codes.ToString()));
                        break;
                }
            }
            catch (Exception exception1)
            {
                ProjectData.SetProjectError(exception1);
                Exception exception = exception1;
                if (continueOnError)
                {
                    MyLogWarning(buildTask, string.Format("Unexpected exception occurred while using RoboCopy. The continueOnError flag as set to true and the exception will be logged as a warning: {0}", exception.Message));
                }
                ProjectData.ClearProjectError();
            }
        }

        public static bool MyCopyFile(Task buildTask, string sourceFile, string targetFile, bool overWrite = true, bool onlyReplaceWithNewerFile = false, string logText = "Copying file from '{0}' to '{1}'...", bool continueOnError = false)
        {
            var myComputer = new Computer();
            bool flag = true;
            if (overWrite && onlyReplaceWithNewerFile)
            {
                flag = FileShouldBeCopied(sourceFile, targetFile);
                if (!flag)
                {
                    MyLogMessage(buildTask, string.Format("Skipping file copy operation from '{0}' to '{1}' because the source file was the same version or older than the target file.", sourceFile, targetFile), MessageImportance.Normal);
                }
            }
            if (flag && !overWrite)
            {
                flag = !myComputer.FileSystem.FileExists(targetFile);
                if (!flag)
                {
                    MyLogMessage(buildTask, string.Format("Skipping file copy operation from '{0}' to '{1}' because the target file already exists and the overwrite flag is set to false.", sourceFile, targetFile), MessageImportance.Normal);
                }
            }
            if (flag)
            {
                MyLogMessage(buildTask, string.Format(logText, sourceFile, targetFile), MessageImportance.Normal);
                try
                {
                    myComputer.FileSystem.CopyFile(sourceFile, targetFile, true);
                }
                catch (UnauthorizedAccessException exception1)
                {
                    ProjectData.SetProjectError(exception1);
                    UnauthorizedAccessException exception = exception1;
                    if (File.Exists(targetFile))
                    {
                        MyLogWarning(buildTask, string.Format("File '{0}' is read-only. Its attributes will be changed to normal.", targetFile));
                        try
                        {
                            File.SetAttributes(targetFile, FileAttributes.Normal);
                        }
                        catch (Exception exception4)
                        {
                            ProjectData.SetProjectError(exception4);
                            ProjectData.ClearProjectError();
                        }
                        try
                        {
                            myComputer.FileSystem.CopyFile(sourceFile, targetFile, true);
                            goto Label_00FF;
                        }
                        catch (Exception exception5)
                        {
                            ProjectData.SetProjectError(exception5);
                            Exception exception2 = exception5;
                            if (!continueOnError)
                            {
                                throw;
                            }
                            MyLogWarning(buildTask, string.Format("Copying '{0}' to '{1}' failed: {2}.", sourceFile, targetFile, exception.Message));
                            ProjectData.ClearProjectError();
                            goto Label_00FF;
                        }
                    }
                    throw;
                Label_00FF:
                    ProjectData.ClearProjectError();
                }
                catch (Exception exception6)
                {
                    ProjectData.SetProjectError(exception6);
                    Exception exception3 = exception6;
                    if (!continueOnError)
                    {
                        throw;
                    }
                    MyLogWarning(buildTask, string.Format("Copying '{0}' to '{1}' failed: {2}.", sourceFile, targetFile, exception3.Message));
                    ProjectData.ClearProjectError();
                }
            }
            return flag;
        }

        public static void MyCreateFolder(Task buildTask, string folderName, string logText = "About to create folder '{0}'")
        {
            var myComputer = new Computer();
            if (!Directory.Exists(folderName))
            {
                MyLogMessage(buildTask, string.Format(logText, folderName), MessageImportance.Normal);
                myComputer.FileSystem.CreateDirectory(folderName);
            }
        }

        public static void MyDeleteFile(string fileName)
        {
            MyDeleteFile(fileName, false);
        }

        [MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining), DebuggerStepThrough]
        public static void MyDeleteFile(string fileName, bool force)
        {
            try
            {
                bool flag = true;
                if (force)
                {
                    if (File.Exists(fileName))
                    {
                        Microsoft.VisualBasic.FileSystem.SetAttr(fileName, FileAttribute.Normal);
                    }
                    else
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    Microsoft.VisualBasic.FileSystem.Kill(fileName);
                    Trace.WriteLine(string.Format("Deleted file '{0}'", fileName));
                }
            }
            catch (Exception exception1)
            {
                ProjectData.SetProjectError(exception1);
                ProjectData.ClearProjectError();
            }
        }

        public static void MyDeleteFolder(Task buildTask, string folderName)
        {
            MyDeleteFolder(buildTask, folderName, DeleteDirectoryOption.DeleteAllContents, false, "About to delete folder '{0}'");
        }

        public static void MyDeleteFolder(Task buildTask, string folderName, bool failOnError)
        {
            MyDeleteFolder(buildTask, folderName, DeleteDirectoryOption.DeleteAllContents, failOnError, "About to delete folder '{0}'");
        }

        public static void MyDeleteFolder(Task buildTask, string folderName, DeleteDirectoryOption OnDirectoryNotEmpty, bool failOnError)
        {
            MyDeleteFolder(buildTask, folderName, OnDirectoryNotEmpty, failOnError, "About to delete folder '{0}'");
        }

        public static void MyDeleteFolder(Task buildTask, string folderName, DeleteDirectoryOption OnDirectoryNotEmpty, bool failOnError, string logText)
        {
            var myComputer = new Computer();
            if (Directory.Exists(folderName))
            {
                if (Strings.Len(logText) > 0)
                {
                    MyLogMessage(buildTask, string.Format(logText, folderName), MessageImportance.Normal);
                }
                try
                {
                    myComputer.FileSystem.DeleteDirectory(folderName, OnDirectoryNotEmpty);
                }
                catch (UnauthorizedAccessException exception1)
                {
                    ProjectData.SetProjectError(exception1);
                    UnauthorizedAccessException exception = exception1;
                    if (OnDirectoryNotEmpty == DeleteDirectoryOption.DeleteAllContents)
                    {
                        foreach (string str in Directory.GetFiles(folderName, "*.*", System.IO.SearchOption.AllDirectories))
                        {
                            MyDeleteFile(str, true);
                        }
                        MyDeleteFolder(buildTask, folderName, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty, failOnError, logText);
                    }
                    ProjectData.ClearProjectError();
                }
                catch (IOException exception4)
                {
                    ProjectData.SetProjectError(exception4);
                    IOException exception2 = exception4;
                    if ((Strings.InStr(exception2.Message, "is not empty", CompareMethod.Binary) > 0) && (OnDirectoryNotEmpty == DeleteDirectoryOption.DeleteAllContents))
                    {
                        foreach (string str2 in Directory.GetDirectories(folderName, "*", System.IO.SearchOption.TopDirectoryOnly))
                        {
                            MyDeleteFolder(buildTask, str2, DeleteDirectoryOption.ThrowIfDirectoryNonEmpty, failOnError, logText);
                        }
                    }
                    else
                    {
                        if (failOnError)
                        {
                            throw;
                        }
                        MyLogWarning(buildTask, string.Format("Deleting folder '{0}' failed with exception '{1}'. The exception type was '{2}'. This may result in a later build error.", folderName, exception2.Message, exception2.GetType().Name));
                    }
                    ProjectData.ClearProjectError();
                }
                catch (Exception exception5)
                {
                    ProjectData.SetProjectError(exception5);
                    Exception exception3 = exception5;
                    if (failOnError)
                    {
                        throw;
                    }
                    MyLogWarning(buildTask, string.Format("Deleting folder '{0}' failed with exception '{1}'. The exception type was '{2}'. This may result in a later build error.", folderName, exception3.Message, exception3.GetType().Name));
                    ProjectData.ClearProjectError();
                }
            }
        }

        public static void MyLogError(Task myTask, string message)
        {
            if (Strings.Len(message) > 0)
            {
                if (!IsRunFromMSBuild(myTask))
                {
                    throw new AutomationException(message);
                }
                myTask.Log.LogError(message, new object[0]);
            }
        }

        public static void MyLogException(Task myTask, Exception exception, bool showStackTrace = true)
        {
            if (IsRunFromMSBuild(myTask))
            {
                myTask.Log.LogErrorFromException(exception, showStackTrace);
            }
            else
            {
                Trace.WriteLine(exception.Message, "Error");
                throw exception;
            }
        }

        public static void MyLogMessage(Task myTask, string message, MessageImportance Importance = MessageImportance.Normal)
        {
            if (Strings.Len(message) > 0)
            {
                if (IsRunFromMSBuild(myTask))
                {
                    myTask.Log.LogMessage(Importance, message, null);
                }
                else
                {
                    Trace.WriteLine(message, "Information");
                    EventHandler<AutomationLogEventArgs> automationLogEventEvent = AutomationLogEvent;
                    if (automationLogEventEvent != null)
                    {
                        automationLogEventEvent(myTask, new AutomationLogEventArgs(message, MessageType.Information));
                    }
                }
            }
        }

        public static void MyLogWarning(Task myTask, string message)
        {
            if (IsRunFromMSBuild(myTask))
            {
                myTask.Log.LogWarning(message, null);
            }
            else
            {
                Trace.WriteLine(message, "Warning");
                EventHandler<AutomationLogEventArgs> automationLogEventEvent = AutomationLogEvent;
                if (automationLogEventEvent != null)
                {
                    automationLogEventEvent(myTask, new AutomationLogEventArgs(message, MessageType.Warning));
                }
            }
        }

        public static void OverRideExceptionMessage(Exception ex, string additionalContextMessage)
        {
            ex.GetType().GetField("_message", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(ex, string.Format("{0} {1}", ex.Message, additionalContextMessage));
        }

        public static string Unquote(string value)
        {
            if ((!string.IsNullOrEmpty(value) && (value[0] == '"')) && (value[value.Length - 1] == '"'))
            {
                return value.Substring(1, value.Length - 2);
            }
            return value;
        }

        public static bool ValidateCRNumber(string crNumber, ref string reasonForFailure)
        {
            int[] numArray = new int[3];
            crNumber = Strings.UCase(Strings.Trim(crNumber));
            reasonForFailure = "A valid Change Request must start with the letters CR.";
            bool flag = Strings.Left(crNumber, 2) == "CR";
            if (flag)
            {
                string[] array = Strings.Split(crNumber, "-", -1, CompareMethod.Binary);
                flag = Information.UBound(array, 1) == 1;
                reasonForFailure = "A valid CR number contains a CR number as well as a date, separated by a hyphen (CR[0..9]-MMDDYY)";
                if (flag)
                {
                    flag = Strings.Len(array[1]) >= 6;
                    if (flag)
                    {
                        reasonForFailure = "The date portion of the CR number is invalid.";
                        numArray[0] = Conversions.ToInteger(Strings.Left(array[1], 2));
                        numArray[1] = Conversions.ToInteger(Strings.Mid(array[1], 3, 2));
                        string expression = Strings.Right(array[1], Strings.Len(array[1]) - 4);
                        if (Strings.Len(expression) == 2)
                        {
                            numArray[2] = 0x7d0 + Conversions.ToInteger(expression);
                        }
                        else if (Strings.Len(expression) == 4)
                        {
                            numArray[2] = Conversions.ToInteger(expression);
                        }
                        else
                        {
                            flag = false;
                        }
                    }
                    if (flag)
                    {
                        reasonForFailure = "The date portion is in 'MMDDYY' format'. You entered an invalid month.";
                        flag = (numArray[0] >= 1) & (numArray[0] <= 12);
                        if (flag)
                        {
                            reasonForFailure = "The date portion is in 'MMDDYY' format'. You entered an invalid day.";
                            flag = (numArray[1] >= 1) & (numArray[1] <= DateTime.DaysInMonth(numArray[2], numArray[0]));
                        }
                        if (flag)
                        {
                            reasonForFailure = "The date portion is in 'MMDDYY' format'. You entered an invalid year. The year cannot be later than the current year and cannot be before 1990. If you think this is a valid year, make sure you did not reset your system date to an earlier year.";
                            flag = (numArray[2] >= 0x7c6) & (numArray[2] <= DateAndTime.Year(DateAndTime.Now));
                        }
                        if (flag)
                        {
                            flag = Information.IsDate(DateAndTime.DateSerial(numArray[2], numArray[0], numArray[1]));
                        }
                        if (flag)
                        {
                            reasonForFailure = "The date portion of the CR number should be before today's date. If you believe this to be a valid CR number, check the system date on your machine and make sure it is set to the current date.";
                            flag = DateTime.Compare(DateAndTime.DateSerial(numArray[2], numArray[0], numArray[1]), DateTime.Now) <= 0;
                        }
                        if (flag)
                        {
                            reasonForFailure = "The numeric portion of the CR number contains invalid characters";
                            array[0] = Strings.Replace(array[0], "CR", "", 1, -1, CompareMethod.Binary);
                            flag = Versioned.IsNumeric(array[0]);
                        }
                    }
                }
            }
            bool flag2 = flag;
            if (flag)
            {
                reasonForFailure = "";
            }
            return flag2;
        }

        public static void ValidateDirectory(string fileName)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName)))
            {
                File.Create(fileName);
            }
        }

        public static bool ValidateVersion(string version)
        {
            return Regex.IsMatch(version, @"\d{1,2}\.\d{1,2}\.\d{1,4}\.\d{1,4}$");
        }

        public static int VersionCompare(string version1, string Version2)
        {
            if ((Strings.Len(version1) == 0) || (Strings.Len(Version2) == 0))
            {
                return -1;
            }
            Version sourceVersion = new Version(version1);
            Version targetVersion = new Version(Version2);
            return VersionCompare(sourceVersion, targetVersion);
        }

        public static int VersionCompare(Version sourceVersion, Version targetVersion)
        {
            if (sourceVersion != targetVersion)
            {
                if (sourceVersion > targetVersion)
                {
                    return 1;
                }
                if (targetVersion > sourceVersion)
                {
                    return 2;
                }
            }
            return 0;
        }
    }
}
