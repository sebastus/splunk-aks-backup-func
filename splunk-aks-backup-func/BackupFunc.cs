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
            var scriptFileName = getEnvironmentVariable("SCRIPT_TO_RUN");
            if (scriptFileName.Length == 0)
            {
                scriptFileName = "Exit-Powershell.ps1";
            }

            var scriptFilePath = getFilename($"scripts/{scriptFileName}");

            log.LogInformation($"C# Timer trigger function executing script {scriptFileName} at: {DateTime.Now}");

            ProcessStartInfo startInfo = new ProcessStartInfo(getPowershell());
            startInfo.Arguments = $" -ExecutionPolicy Unrestricted -File {scriptFilePath}";
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;

            try
            {
                startInfo.EnvironmentVariables.Add("AZURE_TENANT_ID", getEnvironmentVariable("AZURE_TENANT_ID"));
                startInfo.EnvironmentVariables.Add("AZURE_APP_ID", getEnvironmentVariable("AZURE_APP_ID"));
                startInfo.EnvironmentVariables.Add("AZURE_APP_KEY", getEnvironmentVariable("AZURE_APP_KEY"));
                startInfo.EnvironmentVariables.Add("AZURE_SUBSCRIPTION_ID", getEnvironmentVariable("AZURE_SUBSCRIPTION_ID"));
                startInfo.EnvironmentVariables.Add("AKS_RG", getEnvironmentVariable("AKS_RG"));
                startInfo.EnvironmentVariables.Add("AKS_ASSET_RG", getEnvironmentVariable("AKS_ASSET_RG"));
                startInfo.EnvironmentVariables.Add("AKS_NAME", getEnvironmentVariable("AKS_NAME"));
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
                throw new ArgumentNullException($"Key {name} does not exist in environment variables");

            return result;
        }
    }
}
