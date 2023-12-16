using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using TargetTask = Microsoft.Build.Utilities.Task;

namespace Oleander.Assembly.Versioning.BuildTask
{
    public class VersioningTask : TargetTask
    {
        [Required]
        public string? TargetFileName { get; set; }
        public string? ProjectDirName { get; set; }
        public string? ProjectFileName { get; set; }
        public string? GitRepositoryDirName { get; set; }

        private readonly Versioning _versioning;
        private readonly TaskLogger _taskLogger;
        public VersioningTask()
        {
            this._taskLogger = new TaskLogger(this);
            this._versioning = new(this._taskLogger);
        }

        public override bool Execute()
        {
            VersioningResult result;

            if (this.TargetFileName == null)
            {
                this._taskLogger.LogError("Property TargetFileName is null!");

                this.Log.LogError(subcategory: "OAVT",
                    errorCode: $"OAVT:{-1}",
                    helpKeyword: null,
                    file: string.Empty,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: "Property TargetFileName is null!");
                return false;
            }

            var now = DateTime.Now;
            var process = Process.GetCurrentProcess();
            this._taskLogger.LogInformation("Execute task {processName} {processId} at at {time}", process.ProcessName, process.Id, DateTime.Now.ToString("HH:mm:ss"));
            this._taskLogger.LogDebug("TargetFileName:       {targetFileName}", this.TargetFileName);
            this._taskLogger.LogDebug("ProjectDirName:       {ProjectDirName}", this.ProjectDirName);
            this._taskLogger.LogDebug("ProjectFileName:      {ProjectFileName}", this.ProjectFileName);
            this._taskLogger.LogDebug("GitRepositoryDirName: {GitRepositoryDirName}", this.GitRepositoryDirName);

            if (this.ProjectDirName != null && this.ProjectFileName != null && this.GitRepositoryDirName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName, this.GitRepositoryDirName);
            }
            else if (this.ProjectDirName != null && this.ProjectFileName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectDirName, this.ProjectFileName);
            }
            else if (this.ProjectFileName != null)
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName, this.ProjectFileName);
            }
            else 
            {
                result = this._versioning.UpdateAssemblyVersion(this.TargetFileName);
            }

            if (result.ErrorCode != VersioningErrorCodes.Success)
            {
                this.Log.LogError(subcategory: "OAVT",
                    errorCode: $"OAVT:{(int)result.ErrorCode:000}",
                    helpKeyword: null,
                    file: string.Empty,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: $"Error: {result.ErrorCode}");

                return false;
            }

            if (result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0)
            {
                this.Log.LogError(subcategory: "OAVT",
                    errorCode: $"OAVT:{result.ExternalProcessResult.ExitCode:000}",
                    helpKeyword: null,
                    file: string.Empty,
                    lineNumber: 0,
                    columnNumber: 0,
                    endLineNumber: 0,
                    endColumnNumber: 0,
                    message: result.ExternalProcessResult.ToString());

                return false;
            }

            this.Log.LogMessage(MessageImportance.Normal, $"OAVT: Version: {result.CalculatedVersion}");
            
            this._taskLogger.LogInformation("========== VersioningTask CalculatedVersion: {calculatedVersion} ==========", result.CalculatedVersion);
            this._taskLogger.LogInformation("========== VersioningTask completed at {time} and took {seconds} seconds ==========", now.ToString("HH:mm"), (DateTime.Now - now).TotalSeconds.ToString("F"));
            return true;
        }
    }
}
