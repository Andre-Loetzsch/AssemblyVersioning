﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Oleander.Assembly.Comparator;
using Oleander.Assembly.Versioning.ExternalProcesses;
using Oleander.Assembly.Versioning.NuGet;

namespace Oleander.Assembly.Versioning;

internal class Versioning(ILogger logger)
{
    private class GitHashCacheItem
    {
        public string? Hash { get; set; }
        public ExternalProcessResult? Result { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    private class GitChangesCacheItem
    {
        public string[]? Changes { get; set; }
        public ExternalProcessResult? Result { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    private static readonly object syncDownloadNugetPackages = new();
    private static readonly Dictionary<string, GitHashCacheItem> gitHashCache = new();
    private static readonly Dictionary<string, GitChangesCacheItem> gitChangesCache = new();
    private readonly Dictionary<string, AssemblyFrameworkInfo> _assemblyFrameworkInfoCache = new();
    private MSBuildProject? _msBuildProject;

    internal FileSystem FileSystem = new(logger);

    #region UpdateAssemblyVersion

    public VersioningResult UpdateAssemblyVersion(string targetFileName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileName = targetFileName
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectFileName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileName = targetFileName,
            ProjectFileName = projectFileName
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileName = targetFileName,
            ProjectDirName = projectDirName,
            ProjectFileName = projectFileName
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileName = targetFileName,
            ProjectDirName = projectDirName,
            ProjectFileName = projectFileName,
            GitRepositoryDirName = gitRepositoryDirName
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    #endregion

    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string gitHash)
    {
        var key = this.FileSystem.GitRepositoryDirName;

        if (!gitHashCache.TryGetValue(key, out var cacheItem))
        {
            cacheItem = new GitHashCacheItem();
            gitHashCache.Add(key, cacheItem);
        }

        if (cacheItem.Hash != null && cacheItem.Result != null && (DateTime.Now - cacheItem.LastUpdated).TotalSeconds < 30)
        {
            result = cacheItem.Result;
            gitHash = cacheItem.Hash;
            logger.LogInformation("Get git-hash from cache: {gitHash}", gitHash);
            return true;
        }

        gitHash = null;
        result = new GitGetHash(this.FileSystem.GitRepositoryDirName).Start();
        logger.LogDebug("GitGetHash result: {CommandLine}, ExitCode={exitCode}, Win32ExitCode={win32ExitCode}", result.CommandLine, result.ExitCode, result.Win32ExitCode);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        gitHash = result.StandardOutput!.Trim();
        cacheItem.LastUpdated = DateTime.Now;
        cacheItem.Result = result;
        cacheItem.Hash = gitHash;

        logger.LogDebug("GitGetHash value: {gitHash}", gitHash);
        return true;
    }

    protected virtual bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        var key = this.FileSystem.GitRepositoryDirName;

        if (!gitChangesCache.TryGetValue(key, out var cacheItem))
        {
            cacheItem = new GitChangesCacheItem();
            gitChangesCache.Add(key, cacheItem);
        }

        if (cacheItem.Changes != null && cacheItem.Result != null && (DateTime.Now - cacheItem.LastUpdated).TotalSeconds < 30)
        {
            result = cacheItem.Result;
            gitChanges = cacheItem.Changes;
            logger.LogInformation("Get git-changes from cache: {gitChanges}", gitChanges.Length);
            return true;
        }

        gitChanges = null;
        result = new GitDiffNameOnly(gitHash, this.FileSystem.GitRepositoryDirName).Start();
        logger.LogDebug("GitDiffNameOnly result: {CommandLine}, ExitCode={exitCode}, Win32ExitCode={win32ExitCode}", result.CommandLine, result.ExitCode, result.Win32ExitCode);

        if (result.ExitCode != 0) return false;

        if (string.IsNullOrEmpty(result.StandardOutput))
        {
            gitChanges = Array.Empty<string>();
            return true;
        }

        gitChanges = result.StandardOutput!.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        cacheItem.LastUpdated = DateTime.Now;
        cacheItem.Result = result;
        cacheItem.Changes = gitChanges;

        logger.LogDebug("GitDiffNameOnly value: {gitChanges}", gitChanges.Length);
        return true;
    }

    protected virtual string[] GetGitDiffFilter()
    {
        var projectDirName = this.FileSystem.ProjectDirName;

        if (Directory.Exists(projectDirName) &&
            File.Exists(Path.Combine(projectDirName, ".gitdiff")))
        {
            return File.ReadAllLines(Path.Combine(projectDirName, ".gitdiff"));
        }

        return new[] { ".cs", ".xaml" };
    }

    protected virtual bool TryDownloadNugetPackage(string outDir)
    {
        if (this._msBuildProject is not { IsPackable: true }) return false;
        var packageId = this._msBuildProject.PackageId;
        if (packageId == null) return false;

        lock (syncDownloadNugetPackages)
        {
            var packageSource = this._msBuildProject.PackageSource;
            using var nuGetDownLoader = new NuGetDownLoader(new NuGetLogger(logger), Path.GetFileName(this.FileSystem.TargetFileName));
            var sources = packageSource == null ? nuGetDownLoader.GetNuGetConfigSources() : new[] { Repository.Factory.GetCoreV3(packageSource) };

            var versions = nuGetDownLoader.GetAllVersionsAsync(sources, packageId, CancellationToken.None).GetAwaiter().GetResult();

            if (!versions.Any()) return false;

            var (source, version) = versions.First(x => x.Item2.Version == versions.Max(x1 => x1.Item2.Version));

            return nuGetDownLoader.DownloadPackageAsync(source, packageId, version, outDir, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    #endregion

    #region private members

    private VersioningResult PrivateUpdateAssemblyVersion()
    {
        #region initialisations

        var updateResult = new VersioningResult();
        this._msBuildProject = new MSBuildProject(this.FileSystem.ProjectFileName);
        this._assemblyFrameworkInfoCache.Clear();

        #endregion

        #region TryGetGitChanges

        if (!this.TryGetGitChanges(this.FileSystem.GitHash, out var result, out var gitChanges))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitDiffNameOnlyFailed;

            logger.LogWarning("TryGetGitChanges failed! {externalProcessResult}", result);
            return updateResult;
        }

        gitChanges = this.FilterProjectFilesFromGitChanges(gitChanges).ToArray();

        #endregion

        #region AssemblyComparison

        this.ResolveRefTargetFile();

        var targetFileInfo = new FileInfo(this.FileSystem.TargetFileName);
        var refTargetFileInfo = new FileInfo(this.FileSystem.RefTargetFileName);

        logger.LogInformation("Assembly comparison target:    {targetFileInfo}", targetFileInfo);
        logger.LogInformation("Assembly Comparison reference: {refTargetFileInfo}", refTargetFileInfo);

        var comparison = new AssemblyComparison(refTargetFileInfo, targetFileInfo, true);
        var versionChange = comparison.VersionChange;

        logger.LogInformation("Assembly comparison result is: {versionChange}", versionChange);

        var xml = comparison.ToXml() ?? "Xml is (null)";
        logger.LogInformation("{xml}", xml);
        this.WriteChangeLog(versionChange, gitChanges, xml);

        #endregion

        #region TryGetProjectFileAssemblyVersion

        if (!this.TryGetProjectFileAssemblyVersion(out var projectFileVersion))
        {
            projectFileVersion = new Version(0, 0, 1, 0);
            logger.LogInformation("Project file does not contain assembly version information. Use start version '{projectFileVersion}'.", projectFileVersion);
        }
        else
        {
            logger.LogInformation("Use project file assembly version '{projectFileVersion}'.", projectFileVersion);
        }

        #endregion

        #region TryGetRefAndLastCalculatedVersion

        if (!this.TryGetRefAndLastCalculatedVersion(out var refVersion, out var lastCalculatedVersion))
        {
            refVersion = projectFileVersion;
            lastCalculatedVersion = projectFileVersion;

            this.SaveRefAndLastCalculatedVersion(refVersion, lastCalculatedVersion);
            logger.LogInformation("Reference version was not found. Use version from project file.");
        }

        #endregion

        #region Increase Build- Revision- Version

        if (this.ShouldIncreaseBuildVersion(gitChanges, versionChange)) versionChange = VersionChange.Build;
        if (this.ShouldIncreaseRevisionVersion(gitChanges, versionChange)) versionChange = VersionChange.Revision;

        #endregion

        #region CalculateVersion

        updateResult.CalculatedVersion = CalculateVersion(refVersion, versionChange);
        logger.LogInformation("Version '{calculatedVersion}' was calculated.", updateResult.CalculatedVersion);

        #endregion

        #region Update new version

        if (projectFileVersion <= lastCalculatedVersion && projectFileVersion != updateResult.CalculatedVersion)
        {
            var versionSuffix = this._msBuildProject.VersionSuffix ?? string.Empty;

            if (string.IsNullOrEmpty(versionSuffix) || versionSuffix == "alpha" || versionSuffix == "beta")
            {
                if (updateResult.CalculatedVersion.Major == 0)
                {
                    versionSuffix = updateResult.CalculatedVersion.Minor == 0 ? "alpha" : "beta";
                }
            }

            this.UpdateProjectFile(updateResult.CalculatedVersion, versionSuffix, this.FileSystem.GitHash);

            var gitChangesList = gitChanges.ToList();
            gitChangesList.Add(this.FileSystem.ProjectFileName);
            gitChanges = gitChangesList.ToArray();
        }

        this.SaveRefAndLastCalculatedVersion(refVersion, updateResult.CalculatedVersion);
        this.CopyTargetFileToProjectRefDir(gitChanges.Any());

        #endregion
        
        return updateResult;
    }



    private bool TryGetProjectFileAssemblyVersion([MaybeNullWhen(false)] out Version version)
    {
        version = null;
        var projectFileAssemblyVersion = this._msBuildProject?.AssemblyVersion;

        return projectFileAssemblyVersion != null &&
               Version.TryParse(projectFileAssemblyVersion, out version!);

    }

    private bool TryGetRefAndLastCalculatedVersion([MaybeNullWhen(false)] out Version refVersion, [MaybeNullWhen(false)] out Version lastCalculatedVersion)
    {
        refVersion = null;
        lastCalculatedVersion = null;

        var lastCalculatedVersionPath = this.FileSystem.VersionInfoFileName;

        if (!File.Exists(lastCalculatedVersionPath))
        {
            if (!this.TryGetProjectFileAssemblyVersion(out var projectFileVersion))
            {
                projectFileVersion = new Version(0, 0, 0, 0);
            }

            if (!this.FileSystem.ExistProjectRefDir)
            {
                if (!this.TryGetAssemblyFrameworkInfo(this.FileSystem.RefTargetFileName, out var assemblyFrameworkInfo)) return false;
                refVersion = assemblyFrameworkInfo.Version;
            }
            else
            {
                refVersion = projectFileVersion;
            }

            this.SaveRefAndLastCalculatedVersion(refVersion, projectFileVersion);
        }

        var fileContent = File.ReadAllLines(lastCalculatedVersionPath).ToList();

        return fileContent.Count > 1 &&
               Version.TryParse(fileContent[0], out refVersion!) &&
               Version.TryParse(fileContent[1], out lastCalculatedVersion!);
    }

    private bool TryGetAssemblyFrameworkInfo(string assemblyLocation, [MaybeNullWhen(false)] out AssemblyFrameworkInfo assemblyFrameworkInfo)
    {
        if (this._assemblyFrameworkInfoCache.TryGetValue(assemblyLocation, out assemblyFrameworkInfo)) return true;
        if (!File.Exists(assemblyLocation)) return false;

        assemblyFrameworkInfo = new AssemblyFrameworkInfo(assemblyLocation);
        this._assemblyFrameworkInfoCache.Add(assemblyLocation, assemblyFrameworkInfo);

        return true;
    }

    private void ResolveRefTargetFile()
    {
        var refTargetFileName = this.FileSystem.RefTargetFileName;
        var cacheBaseDir = this.FileSystem.CacheBaseDir;

        if (File.Exists(refTargetFileName)) return;

        if (this.TryDownloadNugetPackage(cacheBaseDir) && File.Exists(refTargetFileName))
        {
            this.FileSystem.DeleteProjectRefDirIfExist();
            logger.LogInformation("File '{refAssemblyPath}' was downloaded from NuGet.", refTargetFileName);
            return;
        }

        logger.LogInformation("The file '{refAssemblyPath}' could not be downloaded from NuGet.", refTargetFileName);

        var projectRefFile = this.FileSystem.ProjectRefFile;

        if (File.Exists(projectRefFile))
        {
            File.Copy(projectRefFile, refTargetFileName);
            return;
        }

        var targetFileName = this.FileSystem.TargetFileName;

        if (!File.Exists(targetFileName)) return;
        File.Copy(targetFileName, refTargetFileName, true);
    }

    private void SaveRefAndLastCalculatedVersion(Version refVersion, Version calculatedVersion)
    {
        File.WriteAllLines(this.FileSystem.VersionInfoFileName, new[] { refVersion.ToString(), calculatedVersion.ToString() });
    }

    private void WriteChangeLog(VersionChange versionChange, IEnumerable<string> gitChanges, string xmlDiff)
    {
        var log = new List<string> { $"[{DateTime.Now:yyyy:MM:dd HH:mm:ss}] - {versionChange}" };

        log.AddRange(xmlDiff.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        log.Add("");
        log.AddRange(gitChanges.Select(x => $" -> {x}"));

        var max = log.Select(x => x.Length).Max();
        log.Insert(0, "".PadLeft(max, '='));

        File.AppendAllLines(this.FileSystem.ChangelogFileName, log);
    }

    private void CopyTargetFileToProjectRefDir(bool hasGitChanges)
    {
        if (!this.FileSystem.ExistProjectRefDir) return;

        var targetFileName = this.FileSystem.TargetFileName;

        if (!File.Exists(targetFileName)) return;

        var projectRefFile = this.FileSystem.ProjectRefFile;

        if (File.Exists(projectRefFile))
        {
            if (!hasGitChanges) return;

            var projectRefFileInfo = new FileInfo(projectRefFile);
            var targetFileInfo = new FileInfo(targetFileName);

            if (targetFileInfo.Length == projectRefFileInfo.Length)
            {
                using var fs1 = projectRefFileInfo.OpenRead();
                using var fs2 = targetFileInfo.OpenRead();
                var contentIsDifferent = false;

                for (var i = 0; i < targetFileInfo.Length; i++)
                {
                    contentIsDifferent = fs1.ReadByte() != fs2.ReadByte();
                    if (contentIsDifferent) break;
                }

                if (!contentIsDifferent) return;
            }
        }

        File.Copy(targetFileName, projectRefFile, true);
        logger.LogInformation("File was copied from '{targetFileName}' to '{versionBinPath}'.", targetFileName, projectRefFile);
    }

    private IEnumerable<string> FilterProjectFilesFromGitChanges(IEnumerable<string> gitChanges)
    {
        var gitRepositoryDirName = this.FileSystem.GitRepositoryDirName.Trim();
        var gitRepositoryDirNameLength = gitRepositoryDirName.EndsWith(Path.DirectorySeparatorChar.ToString()) ?
            gitRepositoryDirName.Length : gitRepositoryDirName.Length + 1;

        var projectFiles = Directory.GetFiles(this.FileSystem.ProjectDirName, "*.*", SearchOption.AllDirectories)
          .Select(x => x.Substring(gitRepositoryDirNameLength).Replace('\\', '/')).ToList();

        return projectFiles.Where(projectFile => gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));
    }

    private bool ShouldIncreaseBuildVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        if (versionChange > VersionChange.Revision) return false;

        var gitDiffFilter = this.GetGitDiffFilter();
        return gitChanges.Any(x => gitDiffFilter.Contains(Path.GetExtension(x).ToLower()));
    }

    private bool ShouldIncreaseRevisionVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        var projectRefFileName = Path.GetFileName(this.FileSystem.ProjectRefFileName);
        return versionChange < VersionChange.Revision && gitChanges.Any(x => !x.EndsWith(projectRefFileName));
    }

    private void UpdateProjectFile(Version assemblyVersion, string versionSuffix, string sourceRevisionId)
    {
        if (this._msBuildProject == null) return;
        this._msBuildProject.AssemblyVersion = assemblyVersion.ToString();
        this._msBuildProject.SourceRevisionId = sourceRevisionId;
        this._msBuildProject.VersionSuffix = versionSuffix;
        this._msBuildProject.SaveChanges();

        logger.LogInformation("Project file was updated.");
    }

    private VersioningResult ValidateFileSystem()
    {
        var updateResult = new VersioningResult();

        if (!File.Exists(this.FileSystem.TargetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this.FileSystem.ProjectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this.FileSystem.ProjectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this.FileSystem.GitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        if (!this.TryGetGitHash(out var result, out var gitHash))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitHashFailed;

            logger.LogWarning("TryGetGitHash failed! {externalProcessResult}", result);
            return updateResult;
        }

        this.FileSystem.GitHash = gitHash;

        if (this.TryGetAssemblyFrameworkInfo(this.FileSystem.TargetFileName, out var assemblyFrameworkInfo))
        {
            this.FileSystem.TargetFramework = assemblyFrameworkInfo.FrameworkShortFolderName ?? string.Empty;
            this.FileSystem.TargetPlatform = assemblyFrameworkInfo.TargetPlatform ?? string.Empty;
        }

        updateResult.ErrorCode = VersioningErrorCodes.Success;
        return updateResult;
    }

    #endregion

    #region public static

    public static Version CalculateVersion(Version version, VersionChange versionChange)
    {
        return CalculateVersion(version,
            versionChange == VersionChange.Major,
            versionChange == VersionChange.Minor,
            versionChange == VersionChange.Build,
            versionChange == VersionChange.Revision);
    }

    public static Version CalculateVersion(Version version, bool increaseMajor, bool increaseMinor, bool increaseBuild, bool increaseRevision)
    {
        var major = version.Major;
        var minor = version.Minor;
        var build = version.Build;
        var revision = version.Revision;


        // beta version
        if (increaseMajor && major == 0)
        {
            increaseMajor = false;
            increaseMinor = true;
        }

        // alpha version
        if (increaseMinor && major == 0 && minor == 0)
        {
            increaseMinor = false;
            increaseBuild = true;
        }

        if (increaseMajor)
        {
            major++;
            minor = 0;
            build = 0;
            revision = 0;
            increaseMinor = false;
            increaseBuild = false;
            increaseRevision = false;
        }

        if (increaseMinor)
        {
            minor++;
            build = 0;
            revision = 0;
            increaseBuild = false;
            increaseRevision = false;
        }

        if (increaseBuild)
        {
            build++;
            revision = 0;
            increaseRevision = false;
        }

        if (increaseRevision)
        {
            revision++;
        }

        return new Version(major, minor, build, revision);
    }

    #endregion
}