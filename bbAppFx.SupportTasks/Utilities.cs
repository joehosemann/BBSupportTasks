using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace bbAppFx.SupportTasks
{
    public class Utilities
    {
        public static void createCsvFile(IDataReader reader, StreamWriter writer)
        {
            string Delimiter = "\"";
            string Separator = ",";

            // write header row
            for (int columnCounter = 0; columnCounter < reader.FieldCount; columnCounter++)
            {
                if (columnCounter > 0)
                {
                    writer.Write(Separator);
                }
                writer.Write(Delimiter + reader.GetName(columnCounter) + Delimiter);
            }
            writer.WriteLine(string.Empty);

            // data loop
            while (reader.Read())
            {
                // column loop
                for (int columnCounter = 0; columnCounter < reader.FieldCount; columnCounter++)
                {
                    if (columnCounter > 0)
                    {
                        writer.Write(Separator);
                    }
                    writer.Write(Delimiter + reader.GetValue(columnCounter).ToString().Replace('"', '\'') + Delimiter);
                }   // end of column loop
                writer.WriteLine(string.Empty);
            }   // data loop

            writer.Flush();
        }
        /// <span class="code-SummaryComment"><summary></span>
        /// Executes a shell command synchronously.
        /// <span class="code-SummaryComment"></summary></span>
        /// <span class="code-SummaryComment"><param name="command">string command</param></span>
        /// <span class="code-SummaryComment"><returns>string, as output of the command.</returns></span>
        public void ExecuteCommandSync(object command, string workingDirectory)
        {
            try
            {
                // create the ProcessStartInfo using "cmd" as the program to be run,
                // and "/c " as the parameters.
                // Incidentally, /c tells cmd that we want it to execute the command that follows,
                // and then exit.
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);

                // The following commands are needed to redirect the standard output.
                // This means that it will be redirected to the Process.StandardOutput StreamReader.
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                // Do not create the black window.
                procStartInfo.CreateNoWindow = true;
                procStartInfo.WorkingDirectory = System.IO.Path.GetFullPath(workingDirectory);
                // Now we create a process, assign its ProcessStartInfo and start it
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
            }
            catch (Exception objException)
            {
                // Log the exception
                MessageBox.Show(objException.Message.ToString());
            }
        }

      

        [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

        public static void Move(Process proc)
        {
            const short SWP_NOMOVE = 0X2;
            const short SWP_NOSIZE = 1;
            const short SWP_NOZORDER = 0X4;
            const int SWP_SHOWWINDOW = 0x0040;

            IntPtr handle = proc.MainWindowHandle;
            if (handle != IntPtr.Zero)
            {
                SetWindowPos(handle, 0, 10000, 10000, 0, 0, SWP_NOZORDER | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
        }
     
    }
}
