using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning.Tool;

internal class AssemblyVersioningTool(ILogger<AssemblyVersioningTool> logger)
{
    private readonly Versioning _versioning = new();

    public int UpdateAssemblyVersion(FileInfo targetFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName})", targetFileInfo.FullName);
        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, FileInfo projectFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectFileName})",
            targetFileInfo.FullName, projectFileInfo.FullName);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName})",
            targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo, DirectoryInfo gitRepositoryDirInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName}, {gitRepositoryDirName})",
            targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName, gitRepositoryDirInfo.FullName);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName, gitRepositoryDirInfo.FullName));
    }


    private int LogResult(VersioningResult result)
    {
        var exitCode = result.ExternalProcessResult?.ExitCode ?? 0;
        if (exitCode == 0) exitCode = (int)result.ErrorCode;

        if (exitCode == 0)
        {
            logger.LogInformation("CalculatedVersion: {calculatedVersion}", result.CalculatedVersion);
            return exitCode;
        }

        var msBuildLog = result.ExternalProcessResult != null && result.ExternalProcessResult.ExitCode != 0 ?
            result.ExternalProcessResult.StandardErrorOutput ?? result.ExternalProcessResult.ExitCode.ToString() : 
            result.ErrorCode.ToString();

        logger.LogWarning("ErrorCode: {errorCode}, ExternalProcessResult: {externalProcessResult}", result.ErrorCode, result.ExternalProcessResult);

        MSBuildLogFormatter.CreateMSBuildWarning($"AVT{exitCode}", msBuildLog, "assembly-versioning");
        return exitCode;
    }
}