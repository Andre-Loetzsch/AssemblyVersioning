using Microsoft.Extensions.Logging;
using Oleander.Assembly.Comparator;

namespace Oleander.Assembly.Versioning.Tool;

internal class AssemblyVersioningTool(ILogger<AssemblyVersioningTool> logger)
{
    private readonly Versioning _versioning = new();

    public int UpdateAssemblyVersion(FileInfo targetFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName})", targetFileInfo.Name);
        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, FileInfo projectFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectFileName})", targetFileInfo.Name, projectFileInfo.Name);
        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName})",
            targetFileInfo.Name, projectDirInfo.Name, projectFileInfo.Name);

        return this.LogResult(this._versioning.UpdateAssemblyVersion(targetFileInfo.FullName, projectDirInfo.FullName, projectFileInfo.FullName));
    }

    public int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo projectDirInfo, FileInfo projectFileInfo, DirectoryInfo gitRepositoryDirInfo)
    {
        logger.LogInformation("UpdateAssemblyVersion({targetFileName}, {projectDirName}, {projectFileName}, {gitRepositoryDirName})",
            targetFileInfo.Name, projectDirInfo.Name, projectFileInfo.Name, gitRepositoryDirInfo.Name);

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


    public int CompareAssemblies(FileInfo target1, FileInfo target2)
    {
        if (!target1.Exists) return -1;
        if (!target2.Exists) return -2;

        var assemblyComparison = new AssemblyComparison(target1, target2);

        logger.LogInformation("recommended version change: {versionChange}", assemblyComparison.VersionChange);

        var xml = assemblyComparison.ToXml();

        if (xml != null) logger.LogInformation("{xml}", xml);

        return 0;
    }
}
