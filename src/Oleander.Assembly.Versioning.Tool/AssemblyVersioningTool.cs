using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning.Tool;

internal class AssemblyVersioningTool(ILoggerFactory loggerFactory)
{
    private readonly Versioning _versioning = new(loggerFactory.CreateLogger<Versioning>());
    private readonly ILogger _logger = loggerFactory.CreateLogger<AssemblyVersioningTool>();
    public int UpdateAssemblyVersion(FileInfo targetFileInfo)
    {
        this._logger.LogInformation("UpdateAssemblyVersion({targetFileName})", targetFileInfo.Name);
        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, FileInfo projectFileInfo)
    {
        this._logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectFileName})", targetFileInfo.Name, projectFileInfo.Name);
        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo)
    {
        this._logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName})",
            targetFileInfo.Name, projectDirInfo.Name, projectFileInfo.Name);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo, DirectoryInfo gitRepositoryDirInfo)
    {
        this._logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName}, {gitRepositoryDirName})",
            targetFileInfo.Name, projectDirInfo.Name, projectFileInfo.Name, gitRepositoryDirInfo.Name);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName, gitRepositoryDirInfo.FullName));
    }

    private int LogResult(VersioningResult result)
    {
        var exitCode = result.ExternalProcessResult?.ExitCode ?? 0;
        if (exitCode == 0) exitCode = (int)result.ErrorCode;

        if (exitCode == 0)
        {
            this._logger.LogInformation("CalculatedVersion: {calculatedVersion}", result.CalculatedVersion);
            return exitCode;
        }

        var msBuildLog = result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0 ?
            result.ExternalProcessResult.StandardErrorOutput ?? result.ExternalProcessResult.ExitCode.ToString() : 
            result.ErrorCode.ToString();

        this._logger.LogWarning("ErrorCode: {errorCode}, ExternalProcessResult: {externalProcessResult}", result.ErrorCode, result.ExternalProcessResult);

        MSBuildLogFormatter.CreateMSBuildWarning($"AVT{exitCode}", msBuildLog, "assembly-versioning");
        return exitCode;
    }
}
