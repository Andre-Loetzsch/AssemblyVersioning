using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Frameworks;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Oleander.Assembly.Comparers;
using Oleander.Assembly.Versioning.Caching;
using Oleander.Assembly.Versioning.ExternalProcesses;
using Oleander.Assembly.Versioning.FileSystems;
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
            TargetFileInfo = new(targetFileName)
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success || !this.TrySetTargetFrameworkAndPlatform() ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectFileName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileInfo = new(targetFileName),
            ProjectFileInfo = new(projectFileName)
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success || !this.TrySetTargetFrameworkAndPlatform() ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileInfo = new(targetFileName),
            ProjectDirInfo = new(projectDirName),
            ProjectFileInfo = new(projectFileName)
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success || !this.TrySetTargetFrameworkAndPlatform() ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {
        this.FileSystem = new(logger)
        {
            TargetFileInfo = new(targetFileName),
            ProjectDirInfo = new(projectDirName),
            ProjectFileInfo = new(projectFileName),
            GitRepositoryDirInfo = new(gitRepositoryDirName)
        };

        var updateResult = this.ValidateFileSystem();
        return updateResult.ErrorCode != VersioningErrorCodes.Success || !this.TrySetTargetFrameworkAndPlatform() ?
            updateResult : this.PrivateUpdateAssemblyVersion();
    }

    #endregion

    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string gitHash)
    {
        var key = this.FileSystem.GitRepositoryDirInfo.FullName;

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
        result = new GitGetHash(this.FileSystem.GitRepositoryDirInfo.FullName).Start();
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
        var key = this.FileSystem.GitRepositoryDirInfo.FullName;

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
        result = new GitDiffNameOnly(gitHash, this.FileSystem.GitRepositoryDirInfo.FullName).Start();
        logger.LogDebug("GitDiffNameOnly result: {CommandLine}, ExitCode={exitCode}, Win32ExitCode={win32ExitCode}", result.CommandLine, result.ExitCode, result.Win32ExitCode);

        if (result.ExitCode != 0) return false;

        if (string.IsNullOrEmpty(result.StandardOutput))
        {
            gitChanges = Array.Empty<string>();
            return true;
        }

        // ReSharper disable once UseCollectionExpression
        gitChanges = result.StandardOutput!.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        cacheItem.LastUpdated = DateTime.Now;
        cacheItem.Result = result;
        cacheItem.Changes = gitChanges;

        logger.LogDebug("GitDiffNameOnly value: {gitChanges}", gitChanges.Length);
        return true;
    }

    protected virtual string[] GetGitDiffFilter()
    {
        var gitDiffFileInfo = this.FileSystem.GitDiffFileInfo;

        return gitDiffFileInfo.Exists ?
            File.ReadAllLines(gitDiffFileInfo.FullName) :
            new[] { ".cs", ".xaml" };
    }

    protected virtual bool TryDownloadNugetPackage(string outDir)
    {
        if (this._msBuildProject is not { IsPackable: true }) return false;
        var packageId = this._msBuildProject.PackageId;
        if (packageId == null) return false;

        var packageSource = this._msBuildProject.PackageSource;
        using var nuGetDownLoader = new NuGetDownLoader(new NuGetLogger(logger), this.FileSystem.TargetFileInfo.Name);
        var sources = packageSource == null ? NuGetDownLoader.GetNuGetConfigSources() : new[] { Repository.Factory.GetCoreV3(packageSource) };

        var versions = nuGetDownLoader.GetAllVersionsAsync(sources, packageId, CancellationToken.None).GetAwaiter().GetResult();

        if (!versions.Any()) return false;

        var (source, version) = versions.First(x => x.Item2.Version == versions.Max(x1 => x1.Item2.Version));

        return nuGetDownLoader.DownloadPackageAsync(source, packageId, version, outDir, CancellationToken.None).GetAwaiter().GetResult();

    }

    #endregion

    #region private members

    private VersioningResult PrivateUpdateAssemblyVersion()
    {
        #region initialisations

        this._msBuildProject = new MSBuildProject(this.FileSystem.ProjectFileInfo.FullName);
        this._assemblyFrameworkInfoCache.Clear();

        var updateResult = new VersioningResult();


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

        var targetFileInfo = this.FileSystem.TargetFileInfo;
        var refTargetFileInfo = this.FileSystem.RefTargetFileInfo;

        logger.LogInformation("Assembly comparison target:    {targetFileInfo}", targetFileInfo);
        logger.LogInformation("Assembly Comparison reference: {refTargetFileInfo}", refTargetFileInfo);

        var apiIgnoreList = this.GetApiIgnoreList();

        bool APIIgnore(string name)
        {
            var ignore = apiIgnoreList.Contains(name);
            logger.LogInformation("Ignore: {ignore} -> '{name}'", ignore, name);
            return ignore;
        }


        var comparison = new AssemblyComparison(refTargetFileInfo, targetFileInfo, true, APIIgnore);
        var versionChange = comparison.VersionChange;

        logger.LogInformation("Assembly comparison result is: {versionChange}", versionChange);

        var xml = comparison.ToXml() ?? "Xml is (null)";
        logger.LogInformation("{xml}", xml);
        this.WriteChangeLog(versionChange, gitChanges, xml);

        #endregion

        #region Increase Build- Revision- Version

        if (this.ShouldIncreaseBuildVersion(gitChanges, versionChange)) versionChange = VersionChange.Build;
        if (this.ShouldIncreaseRevisionVersion(gitChanges, versionChange)) versionChange = VersionChange.Revision;

        #endregion

        #region create version cache

        var versionCache = this.CreateVersionFileCache();

        #endregion

        #region update DesiredVersion

        if (this.TryGetProjectFileAssemblyVersion(out var projectFileVersion))
        {
            if (versionCache.CurrentVersion != projectFileVersion)
            {
                versionCache.ManuallySetProjectVersion = projectFileVersion;
            }
        }

        #endregion

        #region CalculateVersion

        updateResult.CalculatedVersion = CalculateVersion(versionCache.RefVersion, versionChange);
        logger.LogInformation("Version '{calculatedVersion}' was calculated.", updateResult.CalculatedVersion);

        if (versionCache.ManuallySetProjectVersion > updateResult.CalculatedVersion)
        {
            logger.LogInformation("Use the project file version '{manuallySetProjectVersion}' because it is higher than the calculated version '{calculatedVersion}'.",
                versionCache.ManuallySetProjectVersion, updateResult.CalculatedVersion);

            updateResult.CalculatedVersion = versionCache.ManuallySetProjectVersion;
        }

        #endregion

        #region Update new version

        versionCache.CurrentVersion = updateResult.CalculatedVersion;

        if (projectFileVersion != updateResult.CalculatedVersion)
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
            gitChangesList.Add(this.FileSystem.ProjectFileInfo.FullName);
            gitChanges = gitChangesList.ToArray();
        }

        versionCache.Write();

        this.CopyTargetFileToProjectRefDir(gitChanges.Length > 0);

        #endregion

        return updateResult;
    }

    private bool UseNuGetAsReference => !this.FileSystem.ProjectRefDirInfo.Exists;

    private bool TryGetProjectFileAssemblyVersion([MaybeNullWhen(false)] out Version version)
    {
        version = null;
        var projectFileAssemblyVersion = this._msBuildProject?.AssemblyVersion;

        return projectFileAssemblyVersion != null &&
               Version.TryParse(projectFileAssemblyVersion, out version!);

    }

    private VersionFileCache CreateVersionFileCache()
    {
        var cache = new VersionFileCache(this.FileSystem.VersionFileCacheFileInfo);

        if (cache.CacheFileInfo.Exists)
        {
            cache.Read();
            return cache;
        }

        if (!this.TryGetProjectFileAssemblyVersion(out var projectFileVersion))
        {
            projectFileVersion = new(0, 0, 0, 0);
        }

        cache.ManuallySetProjectVersion = projectFileVersion;
        cache.CurrentVersion = projectFileVersion;

        cache.RefVersion = this.UseNuGetAsReference &&
                           this.TryGetAssemblyFrameworkInfo(this.FileSystem.RefTargetFileInfo, out var assemblyFrameworkInfo) ?
                                assemblyFrameworkInfo.Version ?? projectFileVersion :
                                projectFileVersion;

        if (cache.CacheFileInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{cacheDir}' created.", cache.CacheFileInfo.DirectoryName);
        }

        cache.Write();

        logger.LogInformation("Create version cache: File name='{cacheFile}' RefVersion={refVersion}, CurrentVersion={currentVersion}",
            cache.CacheFileInfo.FullName, cache.RefVersion, cache.CurrentVersion);

        return cache;
    }

    private bool TryGetAssemblyFrameworkInfo(FileInfo assemblyLocationFileInfo, [MaybeNullWhen(false)] out AssemblyFrameworkInfo assemblyFrameworkInfo)
    {
        if (this._assemblyFrameworkInfoCache.TryGetValue(assemblyLocationFileInfo.FullName, out assemblyFrameworkInfo)) return true;
        if (!assemblyLocationFileInfo.Exists) return false;

        assemblyFrameworkInfo = new AssemblyFrameworkInfo(assemblyLocationFileInfo.FullName);

        if (!assemblyFrameworkInfo.CouldResolved) return false;
        this._assemblyFrameworkInfoCache.Add(assemblyLocationFileInfo.FullName, assemblyFrameworkInfo);

        return true;
    }

    private void ResolveRefTargetFile()
    {
        var refTargetFileInfo = this.FileSystem.RefTargetFileInfo;
        var cacheBaseDirInfo = this.FileSystem.CacheBaseDirInfo;
        var projectRefDirInfo = this.FileSystem.ProjectRefDirInfo;

        if (refTargetFileInfo.Exists) return;

        if (cacheBaseDirInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{cacheBaseDir}' created.", cacheBaseDirInfo.FullName);
        }

        #region use nuget as reference

        if (this.TryDownloadNugetPackage(cacheBaseDirInfo.FullName))
        {
            refTargetFileInfo = this.FileSystem.RefTargetFileInfo;

            if (refTargetFileInfo.Exists)
            {
                // “Delete ProjectRef directory” is the indicator that we are using nuget as reference
                if (projectRefDirInfo.DeleteDirectoryIfExist())
                {
                    logger.LogInformation("Directory '{projectRefDir}' was deleted.", projectRefDirInfo.FullName);
                }

                logger.LogInformation("File '{refAssemblyPath}' was downloaded from NuGet.", refTargetFileInfo.FullName);
                return;
            }

            logger.LogWarning("The NuGet package was downloaded, but the expected file “{refTargetFile}” could not be found!", refTargetFileInfo.FullName);
        }

        #endregion

        #region use 'version.bin' as project reference

        logger.LogInformation("The file '{refTargetFile}' could not be downloaded from NuGet.", refTargetFileInfo.FullName);

        // “Create ProjectRef directory” is the indicator that we are using version.bin as a project reference
        if (projectRefDirInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{projectRefDir}' was created.", projectRefDirInfo.FullName);
        }

        if (refTargetFileInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{refTargetDir}' created.", refTargetFileInfo.DirectoryName);
        }

        var projectRefFileInfo = this.FileSystem.ProjectRefFileInfo;

        if (projectRefFileInfo.Exists)
        {
            projectRefFileInfo.CopyTo(refTargetFileInfo.FullName, true);
            return;
        }

        var targetFileInfo = this.FileSystem.TargetFileInfo;

        if (!targetFileInfo.Exists) return;

        targetFileInfo.CopyTo(refTargetFileInfo.FullName, true);

        #endregion
    }

    private void WriteChangeLog(VersionChange versionChange, IEnumerable<string> gitChanges, string xmlDiff)
    {
        var log = new List<string> { $"[{DateTime.Now:yyyy:MM:dd HH:mm:ss}] - {versionChange}" };

        log.AddRange(xmlDiff.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
        log.Add("");
        log.AddRange(gitChanges.Select(x => $" -> {x}"));

        var max = log.Select(x => x.Length).Max();
        log.Insert(0, "".PadLeft(max, '='));

        var changelogFileInfo = this.FileSystem.ChangelogFileInfo;

        if (changelogFileInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{changelogDir}' created.", changelogFileInfo.DirectoryName);
        }

        File.AppendAllLines(changelogFileInfo.FullName, log);
    }

    private void CopyTargetFileToProjectRefDir(bool hasGitChanges)
    {
        if (this.UseNuGetAsReference) return;

        var targetFileInfo = this.FileSystem.TargetFileInfo;
        var projectRefFileInfo = this.FileSystem.ProjectRefFileInfo;

        if (!targetFileInfo.Exists) return;

        if (projectRefFileInfo.Exists)
        {
            if (!hasGitChanges) return;

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

        targetFileInfo.CopyTo(projectRefFileInfo.FullName, true);
        logger.LogInformation("File was copied from '{targetFileName}' to '{versionBinPath}'.", targetFileInfo.FullName, projectRefFileInfo.FullName);
    }

    private IEnumerable<string> FilterProjectFilesFromGitChanges(IEnumerable<string> gitChanges)
    {
        var gitRepositoryDirFullName = this.FileSystem.GitRepositoryDirInfo.FullName.Trim();
        var gitRepositoryDirNameLength = gitRepositoryDirFullName.EndsWith(Path.DirectorySeparatorChar.ToString()) ?
            gitRepositoryDirFullName.Length : gitRepositoryDirFullName.Length + 1;

        var projectFiles = this.FileSystem.ProjectDirInfo.GetFiles("*.*", SearchOption.AllDirectories)
          .Select(x => x.FullName.Substring(gitRepositoryDirNameLength).Replace('\\', '/')).ToList();

        return projectFiles.Where(projectFile => gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));
    }

    private bool ShouldIncreaseBuildVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        if (versionChange > VersionChange.Revision) return false;

        var gitDiffFilter = this.GetGitDiffFilter();

        return gitChanges.Any(x => !string.Equals(Path.GetFileName(x), "AssemblyInfo.cs", StringComparison.OrdinalIgnoreCase) &&
                                   gitDiffFilter.Contains(Path.GetExtension(x).ToLower()));
    }

    private bool ShouldIncreaseRevisionVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        return versionChange < VersionChange.Revision && gitChanges.Any(x => !x.EndsWith(this.FileSystem.ProjectRefFileInfo.Name));
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

        if (!this.FileSystem.TargetFileInfo.Exists)
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!this.FileSystem.ProjectDirInfo.Exists)
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!this.FileSystem.ProjectFileInfo.Exists)
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!this.FileSystem.GitRepositoryDirInfo.Exists)
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
        updateResult.ErrorCode = VersioningErrorCodes.Success;
        return updateResult;
    }

    private bool TrySetTargetFrameworkAndPlatform()
    {
        if (this.TryGetAssemblyFrameworkInfo(this.FileSystem.TargetFileInfo, out var assemblyFrameworkInfo))
        {
            this.FileSystem.TargetFramework = assemblyFrameworkInfo.FrameworkShortFolderName ?? string.Empty;
            this.FileSystem.TargetPlatform = assemblyFrameworkInfo.TargetPlatform ?? string.Empty;

            return true;
        }

        this.FileSystem.TargetFramework = FindTargetFrameworkFromPath(this.FileSystem.TargetFileInfo.FullName);
        this.FileSystem.TargetPlatform = FindTargetPlatformFromPath(this.FileSystem.TargetFileInfo.FullName);

        var cacheDirInfo = this.FileSystem.CacheDirInfo;
        if (cacheDirInfo.CreateDirectoryIfNotExist())
        {
            logger.LogInformation("Directory '{cacheDir}' created.", cacheDirInfo.FullName);
        }

        logger.LogWarning("The assembly '{targetFile}' cannot be loaded. It may not be a CLR assembly. The file is skipped.", this.FileSystem.TargetFileInfo);
        return false;
    }

    private static string FindTargetFrameworkFromPath(string path)
    {
        var result = string.Empty;
        foreach (var folderName in path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {
            var nuGetFramework = NuGetFramework.ParseFolder(folderName);
            if (!nuGetFramework.IsUnsupported) result = nuGetFramework.GetShortFolderName();
        }

        return result;
    }

    private static string FindTargetPlatformFromPath(string path)
    {
        var result = string.Empty;
        foreach (var folderName in path.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
        {

            if (folderName.StartsWith("android", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("android", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("win-", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("linux-", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("ios", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("mac", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("osx", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("tvos", StringComparison.OrdinalIgnoreCase)) result = folderName;
            if (folderName.StartsWith("windows", StringComparison.OrdinalIgnoreCase)) result = folderName;
        }

        return result;
    }

    private List<string> GetApiIgnoreList()
    {
        var result = new List<string>();

        if (this.FileSystem.GitRepositoryApiIgnoreFileInfo.Exists)
        {
            result.AddRange(File.ReadAllLines(this.FileSystem.GitRepositoryApiIgnoreFileInfo.FullName));
            logger.LogInformation("Found git-repository api-ignore file: '{fileName}'", this.FileSystem.GitRepositoryApiIgnoreFileInfo.FullName);
        }

        if (this.FileSystem.ProjectApiIgnoreFileInfo.Exists)
        {
            result.AddRange(File.ReadAllLines(this.FileSystem.ProjectApiIgnoreFileInfo.FullName).Where(x => !result.Contains(x)));
            logger.LogInformation("Found project api-ignore file: '{fileName}'", this.FileSystem.ProjectApiIgnoreFileInfo.FullName);
        }

        return result;
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