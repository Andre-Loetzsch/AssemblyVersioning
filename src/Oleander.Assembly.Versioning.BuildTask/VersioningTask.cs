using System.Diagnostics;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Extensionss;
using MSBuildTask = Microsoft.Build.Utilities.Task;

namespace Oleander.Assembly.Versioning.BuildTask
{
    public class VersioningTask : MSBuildTask
    {
        [Required]
        public string? TargetFileName { get; set; }
        public string? ProjectDirName { get; set; }
        public string? ProjectFileName { get; set; }
        public string? GitRepositoryDirName { get; set; }

        public bool DisableTask { get; set; }
        public string? LogLevel { get; set; }

        private readonly Versioning _versioning;
        private readonly TaskLogger _taskLogger;
        private readonly string _tempExceptionLogFile = Path.Combine(Path.GetTempPath(), "_versioning.tmp");
        
        public VersioningTask()
        {
            this._taskLogger = new TaskLogger(this);
            this._versioning = new(this._taskLogger);
        }

        public override bool Execute()
        {
            if (!string.IsNullOrEmpty(this.LogLevel))
            {
                if (Enum.TryParse<LogLevel>(this.LogLevel, true, out var logLevel))
                {
                    this._taskLogger.LogFilter = logLevel;
                }
                else
                {
                    this._taskLogger.LogWarning(EventIds.InvalidLogLevel, "--property:versioningTask-logLevel={logLevel} is not a valid LogLevel value!", this.LogLevel);
                }
            }

            if (this.DisableTask)
            {
                this._taskLogger.LogWarning(EventIds.TaskDisabled, "Oleander.Assembly.Versioning.BuildTask.targets is disabled!  --property:versioningTask-disabled=true");
                return true;
            }

            this._taskLogger.LogDebug(EventIds.LogLevel, "LogLevel is '{logLevel}'", this.LogLevel);

            try
            {

                if (!this.ValidateProperties()) return false;

                this.InnerExecute();
                var cacheDirInfo = this._versioning.FileSystem.CacheDirInfo;
                var projectDirInfo = this._versioning.FileSystem.ProjectDirInfo;

                if (cacheDirInfo.Exists)
                {
                    File.AppendAllLines(Path.Combine(cacheDirInfo.FullName, "versioning.log"), this._taskLogger.GetLogs());
                }
                else if (projectDirInfo.Exists)
                {
                    this._taskLogger.LogWarning(EventIds.VersioningCacheDirNotExist, "Versioning cache dir '{cacheDir}' does not exist!", projectDirInfo.FullName);

                    var cacheDir = Path.Combine(projectDirInfo.FullName, ".versioning", "cache");
                    if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);

                    File.AppendAllLines(Path.Combine(cacheDir, "versioning.log"), this._taskLogger.GetLogs());
                }
                else
                {
                    this._taskLogger.LogWarning(EventIds.ProjectDirNotExist, "Project dir '{projectDir}' does not exist!", projectDirInfo.FullName);
                }

            }
            catch (Exception ex)
            {
                var process = Process.GetCurrentProcess();
                var lines = new List<string>
                {
                    $"DateTime: {DateTime.Now:yyyy.MM.dd HH:mm:ss}",
                    $"Process: {process.ProcessName} {process.Id}",
                };

                lines.AddRange(this._taskLogger.GetStackTrace());
                lines.Add($"Exception: {ex}");
                lines.Add($" ");

                File.AppendAllLines(this._tempExceptionLogFile, lines);

                this._taskLogger.LogError(EventIds.AnExceptionHasOccurred,
                    "An exception has occurred: {ex} A diagnostic log has been written to the following location: '{tempExceptionLogFile}'.", 
                    ex.GetAllMessages(), this._tempExceptionLogFile);
            }

            return !this.Log.HasLoggedErrors;

        }

        private void InnerExecute()
        {
            VersioningResult result;

            var now = DateTime.Now;
            var process = Process.GetCurrentProcess();
            var assemblyName = this.GetType().Assembly.GetName();

            this._taskLogger.LogInformation(EventIds.TaskStarted,
                "Task started at {time}: Version={version}, process name={processName}, process id={processId}.",
                DateTime.Now.ToString("HH:mm:ss"), assemblyName.Version, process.ProcessName, process.Id);

            this._taskLogger.LogDebug(EventIds.DebugTargetFileName,       "TargetFileName:       {targetFileName}", this.TargetFileName);
            this._taskLogger.LogDebug(EventIds.DebugProjectDirName,       "ProjectDirName:       {projectDirName}", this.ProjectDirName);
            this._taskLogger.LogDebug(EventIds.DebugProjectFileName,      "ProjectFileName:      {projectFileName}", this.ProjectFileName);
            this._taskLogger.LogDebug(EventIds.DebugGitRepositoryDirName, "GitRepositoryDirName: {gitRepositoryDirName}", this.GitRepositoryDirName);

            this.TargetFileName ??= string.Empty;

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
                this._taskLogger.LogError(EventIds.VersioningFailed, "Versioning failed with error code {errorCode}!", $"{(int)result.ErrorCode}-{result.ErrorCode}");
                return;
            }

            if (result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0)
            {
                this._taskLogger.LogError(EventIds.ExternalProcessFailed, "External process failed with exit code {exitCode}! {externalResult}", 
                    result.ExternalProcessResult.ExitCode, result.ExternalProcessResult);

                return;
            }

            this._taskLogger.LogInformation(EventIds.CalculatedVersion, "CalculatedVersion: {calculatedVersion}", result.CalculatedVersion);
            this._taskLogger.LogInformation(EventIds.TaskCompleted, "Task completed at {time} and took {seconds} seconds.", 
                now.ToString("HH:mm:ss"), (DateTime.Now - now).TotalSeconds.ToString("F"));
        }

        private bool ValidateProperties()
        {
            if (this.TargetFileName == null)
            {
                this._taskLogger.LogError(EventIds.PropertyTargetFileNameIsNull, "Property TargetFileName is null!");
            }

            if (this.ProjectDirName == null)
            {
                this._taskLogger.LogWarning(EventIds.PropertyProjectDirNameIsNull, "Property ProjectDirName is null!");
            }

            if (this.ProjectFileName == null)
            {
                this._taskLogger.LogWarning(EventIds.PropertyProjectFileNameIsNull, "Property ProjectFileName is null!");
            }

            if (this.GitRepositoryDirName == null)
            {
                this._taskLogger.LogWarning(EventIds.PropertyGitRepositoryDirNameIsNull, "Property GitRepositoryDirName is null!");
            }

            return !this.Log.HasLoggedErrors;
        }

    }
}
