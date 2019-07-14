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
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "bogus";
            process.StartInfo.Arguments = "also bogus";
            //process.StartInfo.FileName = "pwsh";
            //process.StartInfo.Arguments = "-File ./RunPwsh-Backups.ps1 -WorkingDirectory ~/scripts";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string err = process.StandardError.ReadToEnd();

            log.LogInformation($"stdout: {output}");
            log.LogInformation($"error: {err}");

            process.WaitForExit();

        }
    }
}
