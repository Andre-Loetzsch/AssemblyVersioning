using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
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
        private string _tempExceptionLogFile = Path.GetTempFileName();
        public VersioningTask()
        {
            this._taskLogger = new TaskLogger(this);
            this._versioning = new(this._taskLogger);
        }


        public override bool Execute()
        {
            try
            {
                var result = this.InnerExecute();
                if (result == null) return !this.Log.HasLoggedErrors;

                if (Directory.Exists(result.VersioningCacheDir))
                {
                    File.AppendAllLines(Path.Combine(result.VersioningCacheDir, "versioning.log"), this._taskLogger.GetLogs());
                }
                else if (Directory.Exists(result.ProjectDirName))
                {
                    var cacheDir = Path.Combine(result.ProjectDirName, ".versioning", "cache");
                    if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                    this._taskLogger.LogWarning("Versioning cache dir '{cacheDir}' does not exist!", result.VersioningCacheDir);
                    File.AppendAllLines(Path.Combine(cacheDir, "versioning.log"), this._taskLogger.GetLogs());
                }

                return !this.Log.HasLoggedErrors;
            }
            catch (Exception ex)
            {

                var process = Process.GetCurrentProcess();
                var lines = new List<string>
                {
                    $"DateTime: {DateTime.Now:yyyy.MM.dd HH:mm:ss}",
                    $"Process: {process.ProcessName} {process.Id}",
                };

                lines.AddRange(this._taskLogger.GetLogs());
                lines.Add($"Exeption: {ex}");
                lines.Add($" ");

                File.AppendAllLines(this._tempExceptionLogFile, lines);
                throw new ApplicationException($"An '{ex.GetType()}' exception has occurred. Further information can be found in the file '{this._tempExceptionLogFile}'.", ex);
                throw;
            }
        }

        private VersioningResult? InnerExecute()
        {
            VersioningResult result = null;

            if (this.TargetFileName == null)
            {
                this._taskLogger.CreateMSBuildError("1", "Property TargetFileName is null!", "OAVT");
                return result;
            }

            var now = DateTime.Now;
            var process = Process.GetCurrentProcess();
            var assemblyName = this.GetType().Assembly.GetName();

            this._taskLogger.LogInformation("Task started at {time}: Version={version}, process name={processName}, process id={processId}.",
                DateTime.Now.ToString("HH:mm:ss"), assemblyName.Version, process.ProcessName, process.Id);

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
                this._taskLogger.CreateMSBuildError($"{(int)result.ErrorCode:000}", $"ERROR: {result.ErrorCode}", "OAVT");
                return result;
            }

            if (result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0)
            {
                this._taskLogger.CreateMSBuildError($"{result.ExternalProcessResult.ExitCode:000}", $"ERROR: {result.ExternalProcessResult}", "OAVT");
                return result;
            }

            this._taskLogger.LogInformation("CalculatedVersion: {calculatedVersion}", result.CalculatedVersion);
            this._taskLogger.LogInformation("Task completed at {time} and took {seconds} seconds.", now.ToString("HH:mm:ss"), (DateTime.Now - now).TotalSeconds.ToString("F"));
            return result;
        }
    }
}
