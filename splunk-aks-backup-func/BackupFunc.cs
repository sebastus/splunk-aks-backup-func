using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Diagnostics;

namespace splunk_aks_backup_func
{
    public static class BackupFunc
    {
        [FunctionName("Function1")]
        public static void Run([TimerTrigger("0 * * * * *")]TimerInfo myTimer, ILogger log)
        {
            var previousExecutionStillRunning = getPreviousExecutions();

            if (previousExecutionStillRunning)
            {
                log.LogInformation($"A previous instance of the backup process is still running.");
                return;
            }

            string scriptFileName = "";
            try
            {
                scriptFileName = getEnvironmentVariable("SCRIPT_TO_RUN");
            } catch (ArgumentNullException argumentNull)
            {
                scriptFileName = "Exit-Powershell.ps1";
            }

            var scriptFilePath = getFilename($"scripts/{scriptFileName}");

            log.LogInformation($"C# Timer trigger function executing script {scriptFileName} at: {DateTime.Now}");

            ProcessStartInfo startInfo = new ProcessStartInfo(getPowershell());
            startInfo.Arguments = $" -ExecutionPolicy Unrestricted -File {scriptFilePath}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = false;
            startInfo.RedirectStandardError = false;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;

            try
            {
                startInfo.Environment.Add("AZURE_TENANT_ID", getEnvironmentVariable("AZURE_TENANT_ID"));
                startInfo.Environment.Add("AZURE_APP_ID", getEnvironmentVariable("AZURE_APP_ID"));
                startInfo.Environment.Add("AZURE_APP_KEY", getEnvironmentVariable("AZURE_APP_KEY"));
                startInfo.Environment.Add("AZURE_SUBSCRIPTION_ID", getEnvironmentVariable("AZURE_SUBSCRIPTION_ID"));
                startInfo.Environment.Add("AKS_RG", getEnvironmentVariable("AKS_RG"));
                startInfo.Environment.Add("AKS_ASSET_RG", getEnvironmentVariable("AKS_ASSET_RG"));
                startInfo.Environment.Add("AKS_NAME", getEnvironmentVariable("AKS_NAME"));
            }
            catch (ArgumentNullException argumentNull)
            {
                log.LogError($"Environment is not set. Required values are: AZURE_TENANT_ID, AZURE_APP_IDAZURE_APP_KEY, AZURE_SUBSCRIPTION_ID, AKS_RG, AKS_ASSET_RG, AKS_NAME");
                throw argumentNull;
            }
            catch (Exception ex)
            {
                log.LogError($"Error occurred while getting environment: {ex.Message}");
                throw ex;
            }

            Process process = new Process();
            try
            {
                process.StartInfo = startInfo;
                process.Start();
            } catch (Exception ex)
            {
                log.LogError($"Problem after external process started: {ex.Message}");
                throw ex;
            }

        }

        private static bool getPreviousExecutions()
        {
            var processes = Process.GetProcessesByName("powershell");

            if (processes.Length > 0)
            {
                return true;
            }
            return false;
        }

        public static string getFilename(string basename)
        {
            var filename = "";
            var home = "";
            try
            {
                home = getEnvironmentVariable("HOME");
            }
            catch (ArgumentNullException argumentNull)
            {
                // no "HOME" == dev environment
                filename = "../../../../" + basename;
            }
            filename = home + "/" + basename;
            return filename;
        }

        public static string getPowershell()
        {
            try
            {
                var home = getEnvironmentVariable("HOME");
            }
            catch (ArgumentNullException argumentNull)
            {
                // no "HOME" = windows dev environment
                return "powershell.exe";
            }

            // assumption = runtime environment = Linux
            return "pwsh";
        }

        public static string getEnvironmentVariable(string name)
        {
            var result = System.Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
            if (result == null || result.Length == 0)
                throw new ArgumentNullException($"Key {name} does not exist in environment variables or has no value.");

            return result;
        }
    }
}
