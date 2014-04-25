using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using bbAppFx.SupportTasks.BuildTasks;
using Blackbaud.AppFx.Platform.BuildTasks;
using Blackbaud.AppFx.Platform.Automation;
using MSBuildUtilities = Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using TestStack.White;
using TestStack.White.UIItems.WindowItems;
using TestStack.White.Factory;
using MSForms = System.Windows.Forms;
using TestStack.White.UIItems.Finders;
using TestStack.White.UIItems;
using TestStack.White.InputDevices;
using TestStack.White.WindowsAPI;
using System.IO;
using System.Threading;
using Microsoft.Web.Administration;
using System.DirectoryServices;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Data.SqlClient;

namespace bbAppFx.SupportTasks
{
    public class PerformanceLogs : MSBuildUtilities.Task
    {
        [Required]
        public string SqlServer { get; set; }
        [Required]
        public string Database { get; set; }
        [Required]
        public string LogDestinationPath { get; set; }
        public bool Capture_SqlSnaps { get; set; }
        public bool Capture_IISLogs { get; set; }
        public bool Capture_EventLogs { get; set; }
        public bool Capture_SOAPLogs { get; set; }
        public string SqlSnap_SPIDs { get; set; }
        public string SqlSnap_CaptureInterval { get; set; }
        public string IIS_SiteName { get; set; }

        private TestStack.White.Application _sqlSnaps;
        private string _completedLogPath;
        private DateTime _beginTimestamp;
        private DateTime _endTimestamp;

        //Application Settings
        private string pathSqlSnaps = System.IO.Path.GetFullPath(@"..\vroot\browser\SQLSnap2006\SQLSnap2006.exe");
        private string pathWebConfig = System.IO.Path.GetFullPath(@"..\vroot\web.config");
        private const string logParserDownloadLink = "http://www.microsoft.com/en-us/download/details.aspx?id=24659";
        //End Application Settings


        public override bool Execute()
        {

            ValidationMethods.ValidateStringParameter("SqlServer", SqlServer);
            ValidationMethods.ValidateStringParameter("Database", Database);
            ValidationMethods.ValidateStringParameter("LogDestinationPath", LogDestinationPath);

            if (!PreCheck())
            {
                MSForms.MessageBox.Show("Log Parser 2.2 required.");
                Process.Start(logParserDownloadLink);
                Utility.MyLogMessage(this, "Log Parser 2.2 required.", MessageImportance.High);
                return true;
            }

            _beginTimestamp = DateTime.Now;

            if (Capture_SqlSnaps)
                InitiateSqlSnap();
            if (Capture_SOAPLogs)
                InitiateSOAPLog();



            MSForms.DialogResult dialogResult = MSForms.MessageBox.Show("Running tests...\r\nPlease press OK to finish collecting data.", "Please press OK to finish collecting data.", MSForms.MessageBoxButtons.OK);
            if (dialogResult == MSForms.DialogResult.OK)
            {
                _endTimestamp = DateTime.Now;
                _completedLogPath = LogDestinationPath + _beginTimestamp.ToString("MMddyyyy-HHmmss") + "-" + _endTimestamp.ToString("MMddyyyy-HHmmss");

                Directory.CreateDirectory(_completedLogPath);

                if (Capture_SqlSnaps)
                    CleanSqlSnap();
                if (Capture_IISLogs)
                    CleanupIISLog();
                if (Capture_SOAPLogs)
                    CleanupSOAPLog();
                if (Capture_EventLogs)
                    CleanupEventLog();
            }

            return true;
        }

        private bool PreCheck()
        {
            if (!File.Exists(Environment.ExpandEnvironmentVariables("%PROGRAMFILES(x86)%\\Log Parser 2.2\\LogParser.exe")))
                return false;

            return true;
        }

        private void InitiateSqlSnap()
        {
            var app = TestStack.White.Application.Launch(pathSqlSnaps);
            var window = app.GetWindows()[0];

            var scFilterDBs = SearchCriteria.ByAutomationId("txt_FilterDB");
            var scFilterSPIDs = SearchCriteria.ByAutomationId("txt_FilterSPID");
            var scSnapInterval = SearchCriteria.ByAutomationId("NumericUpDown1");
            var scSqlServer = SearchCriteria.ByAutomationId("TextBox1");
            var scSnapToggle = SearchCriteria.ByAutomationId("chk_AutoSnap");

            if (!String.IsNullOrWhiteSpace(SqlSnap_CaptureInterval))
            {

                window.Get(scSnapInterval).Focus();
                Keyboard.Instance.HoldKey(KeyboardInput.SpecialKeys.CONTROL);
                Keyboard.Instance.Enter("a");
                Keyboard.Instance.LeaveKey(KeyboardInput.SpecialKeys.CONTROL);
                Keyboard.Instance.Enter(SqlSnap_CaptureInterval);
            }
            if (!String.IsNullOrWhiteSpace(Database))
                window.Get<TextBox>(scFilterDBs).SetValue(Database);
            if (!String.IsNullOrWhiteSpace(SqlSnap_SPIDs))
                window.Get<TextBox>(scFilterSPIDs).SetValue(SqlSnap_SPIDs);


            if (!String.IsNullOrWhiteSpace(SqlServer))
                window.Get<TextBox>(scSqlServer).SetValue(SqlServer);

            window.Get<CheckBox>(scSnapToggle).Select();
            _sqlSnaps = app;
        }

        private void InitiateSOAPLog()
        {
            Utility.MyLogMessage(this, "Initiating requestlogs...", MessageImportance.Normal);
            Utility.MyLogMessage(this, "Backing up web.config...", MessageImportance.Normal);
            File.Copy(pathWebConfig, pathWebConfig + _beginTimestamp.ToString("MMddyyyy-HHmmss") + ".bak");

            var webconfig = File.ReadAllText(pathWebConfig);
            if (Regex.Match(webconfig, "<add\\s+key=\"logrequests\"\\s+value=\"((.+)|)\"\\s+/>").Success)
            {
                if (Regex.Match(webconfig, "((<!--\\s+)|(<!--))<add\\s+key=\"logrequests\"\\s+value=\"((.+)|)\"\\s+/>((\\s+-->)|(-->))").Success)
                {
                    Utility.MyLogMessage(this, "ATTENTION! logrequests section was commented, uncommenting. Do not comment back, instead remove * from value property.", MessageImportance.High);
                    File.WriteAllText(pathWebConfig, Regex.Replace(webconfig, "<!--\\s+<add\\s+key=\"logrequests\"\\s+value=\".+\"\\s+/>\\s+-->", "<add key=\"logrequests\" value=\"*\" />"));
                }
                else
                {
                    var appcmd = string.Format("appcmd.exe set config \"{0}\" -section:appSettings /[key='logrequests'].value:\"*\"", IIS_SiteName);
                    new Utilities().ExecuteCommandSync(string.Format("appcmd.exe set config \"{0}\" -section:appSettings /[key='logrequests'].value:\"*\"", IIS_SiteName), Environment.ExpandEnvironmentVariables("%systemroot%\\system32\\inetsrv\\"));
                }
            }
            else
            {
                Utility.MyLogMessage(this, "ATTENTION!!! logrequests entry not found in bbAppFx web.config", MessageImportance.Normal);
                Capture_SOAPLogs = false;
            }
        }

        private void CleanSqlSnap()
        {
            Directory.CreateDirectory(_completedLogPath + @"\SqlSnaps");
            var win = _sqlSnaps.GetWindows()[0];
            win.Get<Button>(SearchCriteria.ByText("Save"))
                .Click();

            win.Get<TextBox>(SearchCriteria.ByAutomationId("1001").AndByClassName("Edit"))
                .SetValue(_completedLogPath + @"\SqlSnaps\SQLSnaps.xml");

            win.Get<Button>(SearchCriteria.ByClassName("Button").AndByText("Save"))
                .Click();

            win.Close();
        }

        private void CleanupIISLog()
        {
            var relativePath = System.IO.Path.GetFullPath(@".\Tasks\SupportTasks");
            Utility.MyLogMessage(this, "Collecting IIS logs...", MessageImportance.Normal);
            try
            {
                var iisLogDirectory = string.Empty;
                List<string> iisLogFiles = new List<string>();
                var logParserPath = "\"" + Environment.ExpandEnvironmentVariables("%PROGRAMFILES(x86)%\\Log Parser 2.2\\LogParser.exe") + "\"";

                // IIS log directory
                using (ServerManager serverManager = new ServerManager())
                {
                    var site = serverManager.Sites.Where(s => s.Name == IIS_SiteName).Single();
                    iisLogDirectory = Environment.ExpandEnvironmentVariables(site.LogFile.Directory);
                    if (Directory.Exists(iisLogDirectory + @"\W3SVC" + site.Id.ToString()))
                        iisLogDirectory = iisLogDirectory + @"\W3SVC" + site.Id.ToString();
                    else
                    {
                        Utility.MyLogMessage(this, "IIS Logs seem to be turned off for this site.", MessageImportance.High);
                        return;
                    }
                }

                // Gets the IIS logs that were modified/written during the capture period.
                var files = new DirectoryInfo(iisLogDirectory).GetFiles().Where(log => log.LastWriteTime.Date >= _beginTimestamp.Date && log.LastWriteTime.Date <= _endTimestamp.Date);
                foreach (FileInfo file in files)
                {

                    Utility.MyLogMessage(this, "Generating IIS log from date: " + file.CreationTime.Date.ToString(), MessageImportance.Normal);
                    iisLogFiles.Add(file.FullName);
                }

                if (files.Count() == 0)
                    Utility.MyLogMessage(this, "Found no updated IIS logs.", MessageImportance.Normal);


                // Builds the Log Parser command.
                var i = 0;
                foreach (var logFilePath in iisLogFiles)
                {
                    // Build Log Parser CMD          
                    // The conditions in the query are odd because I was having problems with greater-than and less-than working properly.
                    var logParserCmd = String.Format("{0} -o:TPL -i:W3C -tpl:\"{7}\" \"select * into '{1}' from '{2}' where (date = '{3}' OR date = '{4}' OR (date > '{3}' AND date < '{4}')) AND time > '{5}' AND time < '{6}'\"",
                        "logparser",
                        _completedLogPath + String.Format(@"\IISLogs\IISLogs{0}.html", i),
                        logFilePath,
                        _beginTimestamp.ToUniversalTime().ToString("yyyy-MM-dd"),
                        _endTimestamp.ToUniversalTime().ToString("yyyy-MM-dd"),
                        _beginTimestamp.ToUniversalTime().ToString("HH:mm:ss"),
                        _endTimestamp.ToUniversalTime().ToString("HH:mm:ss"),
                        relativePath+"\\iistemplate.tpl"
                        );

                    Utility.MyLogMessage(this, logParserCmd, MessageImportance.Normal);
                    new Utilities().ExecuteCommandSync(@"netsh http flush logbuffer & " + logParserCmd, Environment.ExpandEnvironmentVariables("%PROGRAMFILES(x86)%\\Log Parser 2.2\\"));

                    i++;
                }
            }
            catch (Exception ex)
            {
                Utility.MyLogException(this, ex, true);
            }
        }

        private void CleanupSOAPLog()
        {
            Utility.MyLogMessage(this, "Restoring logrequests to blank value in bbAppFx web.config", MessageImportance.Normal);
            new Utilities().ExecuteCommandSync(string.Format("appcmd.exe set config \"{0}\" -section:appSettings /[key='logrequests'].value:\"\"", IIS_SiteName), Environment.ExpandEnvironmentVariables("%systemroot%\\system32\\inetsrv\\"));
            var cs = Regex.Match(File.ReadAllText(pathWebConfig), "connectionString=\"(.+" + Database + ".+)\"", RegexOptions.IgnoreCase).ToString().Replace("connectionString=\"", "").Replace("\"", "");

            var sqlConnection = new SqlConnection(cs);
            try
            {
                sqlConnection.Open();
                var cmd = new SqlCommand(String.Format("select * from dbo.wsrequestlog where dateadded > '{0}' AND dateadded < '{1}' ",
                    _beginTimestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                    _endTimestamp.ToString("yyyy-MM-dd HH:mm:ss")),
                    sqlConnection);

                //Utility.MyLogMessage(this, cmd.CommandText, MessageImportance.Normal);

                var reader = cmd.ExecuteReader();
                Directory.CreateDirectory(_completedLogPath + @"\SOAPLog");
                var writer = new StreamWriter(_completedLogPath + @"\SOAPLog\SOAPLog.csv");
                Utilities.createCsvFile(reader, writer);

            }
            catch (Exception ex)
            {
                Utility.MyLogError(this, "SQL Connection Error: " + ex.Message.ToString());
                return;
            }



        }

        private void CleanupEventLog()
        {
            var relativePath = System.IO.Path.GetFullPath(@".\Tasks\SupportTasks");
            Utility.MyLogMessage(this, "Collecting Event Viewer logs...", MessageImportance.Normal);

            var iisLogDirectory = string.Empty;
            List<string> iisLogFiles = new List<string>();
            var logParserPath = "\"" + Environment.ExpandEnvironmentVariables("%PROGRAMFILES(x86)%\\Log Parser 2.2\\LogParser.exe") + "\"";

            var logParserCmd = String.Format("\"{0} \"select * into '{1}' from {2} where TimeGenerated >= '{3} {4}' AND TimeGenerated <= '{5} {6}'\" -tpl:{7}\"",
                logParserPath,
                _completedLogPath + @"\EventViewerLogs\EventLog.html",
                "Application",
                _beginTimestamp.ToString("yyyy-MM-dd"),
                _beginTimestamp.ToString("HH:mm:ss"),
                _endTimestamp.ToString("yyyy-MM-dd"),
                _endTimestamp.ToString("HH:mm:ss"),
                relativePath+"\\eventtemplate.tpl"
                );

            new Utilities().ExecuteCommandSync(logParserCmd, "/");
        }
    }
}
