using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.IO;

namespace splunk_aks_backup_func
{
    public static class BackupFunc
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            var scriptFileName = getEnvironmentVariable("SCRIPT_TO_RUN");
            if (scriptFileName.Length == 0)
            {
                scriptFileName = "Exit-Powershell.ps1";
            }

            var scriptFilePath = getFilename($"scripts/{scriptFileName}");

            log.LogInformation($"C# Timer trigger function executing script {scriptFileName} at: {DateTime.Now}");

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            try
            {
                process.StartInfo.FileName = getPowershell();
                process.StartInfo.Arguments = $" -ExecutionPolicy Unrestricted -File {scriptFilePath}";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
            } catch (Exception ex)
            {
                log.LogError($"Problem after external process started: {ex.Message}");
                throw ex;
            }

            string output = process.StandardOutput.ReadToEnd();
            if (output.Length > 0)
            {
                log.LogInformation($"stdout from powershell script: {output}");
            }

            string err = process.StandardError.ReadToEnd();
            if (err.Length > 0)
            {
                log.LogInformation($"error output from powershell: {err}");
            }

            // process.WaitForExit();

        }

        public static string getFilename(string basename)
        {

            var filename = "";
            var home = getEnvironmentVariable("HOME");
            if (home.Length == 0)
            {
                filename = "../../../../" + basename;
            }
            else
            {
                filename = home + "/" + basename;
            }
            return filename;
        }

        public static string getPowershell()
        {
            var home = getEnvironmentVariable("HOME");
            if (home.Length == 0)
            {
                return "powershell.exe";
            } else
            {
                return "pwsh";
            }
        }

        public static string getEnvironmentVariable(string name)
        {
            var result = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (result == null)
                return "";

            return result;
        }
    }
}
