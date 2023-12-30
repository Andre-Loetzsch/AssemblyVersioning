using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Oleander.Assembly.Comparator;
using Oleander.Assembly.Versioning.ExternalProcesses;
using Oleander.Assembly.Versioning.NuGet;

namespace Oleander.Assembly.Versioning;

internal class Versioning(ILogger logger)
{
    private readonly Dictionary<string, AssemblyFrameworkInfo> _assemblyFrameworkInfoCache = new();

    private string _targetFileName = string.Empty;
    private string _projectDirName = string.Empty;
    private string _projectFileName = string.Empty;
    private string _gitRepositoryDirName = string.Empty;

    private MSBuildProject? _msBuildProject;


    #region UpdateAssemblyVersion

    public VersioningResult UpdateAssemblyVersion(string targetFileName)
    {
        var fileSystem = new FileSystem(logger)
        {
            TargetFileName = targetFileName
        };

        var updateResult = this.ValidateFileSystem(fileSystem);
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion(fileSystem);
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectFileName)
    {
        var fileSystem = new FileSystem(logger)
        {
            TargetFileName = targetFileName,
            ProjectFileName = projectFileName
        };

        var updateResult = this.ValidateFileSystem(fileSystem);
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion(fileSystem);
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName)
    {
        var fileSystem = new FileSystem(logger)
        {
            TargetFileName = targetFileName,
            ProjectDirName = projectDirName,
            ProjectFileName = projectFileName
        };

        var updateResult = this.ValidateFileSystem(fileSystem);
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion(fileSystem);
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {

        var fileSystem = new FileSystem(logger)
        {
            TargetFileName = targetFileName,
            ProjectDirName = projectDirName,
            ProjectFileName = projectFileName,
            GitRepositoryDirName = gitRepositoryDirName
        };

        var updateResult = this.ValidateFileSystem(fileSystem);
        return updateResult.ErrorCode != VersioningErrorCodes.Success ?
            updateResult : this.PrivateUpdateAssemblyVersion(fileSystem);
    }

    #endregion

    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string gitHash)
    {
        gitHash = null;
        result = new GitGetHash(this._gitRepositoryDirName).Start();

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        gitHash = result.StandardOutput!.Trim();

        return true;
    }

    protected virtual bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        gitChanges = null;
        result = new GitDiffNameOnly(gitHash, this._gitRepositoryDirName).Start();

        if (result.ExitCode != 0) return false;

        if (string.IsNullOrEmpty(result.StandardOutput))
        {
            gitChanges = Array.Empty<string>();
            return true;
        }

        gitChanges = result.StandardOutput!.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        return true;
    }

    protected virtual string[] GetGitDiffFilter()
    {
        if (Directory.Exists(this._projectDirName) &&
            File.Exists(Path.Combine(this._projectDirName, ".gitdiff")))
        {
            return File.ReadAllLines(Path.Combine(this._projectDirName, ".gitdiff"));
        }

        return new[] { ".cs", ".xaml" };
    }

    protected virtual bool TryDownloadNugetPackage(string outDir)
    {
        if (this._msBuildProject is not { IsPackable: true }) return false;
        var packageId = this._msBuildProject.PackageId;
        if (packageId == null) return false;

        var packageSource = this._msBuildProject.PackageSource;
        using var nuGetDownLoader = new NuGetDownLoader(new NuGetLogger(logger), Path.GetFileName(this._targetFileName));
        var sources = packageSource == null ? nuGetDownLoader.GetNuGetConfigSources() :
            new[] { Repository.Factory.GetCoreV3(packageSource) };

        var versions = nuGetDownLoader.GetAllVersionsAsync(sources, packageId, CancellationToken.None).GetAwaiter().GetResult();

        if (!versions.Any()) return false;

        var (source, version) = versions.First(x => x.Item2.Version == versions.Max(x1 => x1.Item2.Version));

        return nuGetDownLoader.DownloadPackageAsync(source, packageId, version, outDir, CancellationToken.None).GetAwaiter().GetResult();
    }

    #endregion

    #region private members

    private bool VersioningNuGetFileExist
    {
        get => File.Exists(Path.Combine(this.CreateVersioningDirIfNotExists(), ".nuget"));
        set
        {
            var nuGetFilePath = Path.Combine(this.CreateVersioningDirIfNotExists(), ".nuget");

            switch (value)
            {
                case true when !File.Exists(nuGetFilePath):
                    File.WriteAllLines(nuGetFilePath, new[]
                    {
                        $"[{DateTime.Now:yyyy.MM.dd HH:mm:ss}]",
                        "This file is used to signal that the version reference assembly has been downloaded via Nuget."
                    });
                    break;
                case false when File.Exists(nuGetFilePath):
                    File.Delete(nuGetFilePath);
                    break;
            }
        }
    }

    private VersioningResult PrivateUpdateAssemblyVersion(FileSystem fileSystem)
    {
        #region initialisation s

        var updateResult = new VersioningResult
        {
            GitRepositoryDirName = this._gitRepositoryDirName,
            ProjectDirName = this._projectDirName,
            ProjectFileName = this._projectFileName,
            TargetFileName = this._targetFileName
        };

        this._msBuildProject = new MSBuildProject(fileSystem.ProjectFileName);
        this._assemblyFrameworkInfoCache.Clear();

        #endregion

        updateResult.VersioningCacheDir = fileSystem.CacheDir;

        #region TryGetGitChanges

        if (!this.TryGetGitChanges(fileSystem.GitHash, out var result, out var gitChanges))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitDiffNameOnlyFailed;

            logger.LogWarning("TryGetGitChanges failed! {externalProcessResult}", result);
            return updateResult;
        }

        #endregion

        #region AssemblyComparison

        this.ResolveRefTargetFile(fileSystem);

        var targetFileInfo = new FileInfo(fileSystem.TargetFileName);
        var refTargetFileInfo = new FileInfo(fileSystem.RefTargetFileName);

        logger.LogInformation("Assembly comparison target:    {targetFileInfo}", targetFileInfo);
        logger.LogInformation("Assembly Comparison reference: {refTargetFileInfo}", refTargetFileInfo);

        var comparison = new AssemblyComparison(refTargetFileInfo, targetFileInfo, true);
        var versionChange = comparison.VersionChange;

        logger.LogInformation("Assembly comparison result is: {versionChange}", versionChange);

        var xml = comparison.ToXml() ?? "Xml is (null)";
        logger.LogInformation("{xml}", xml);
        this.WriteChangeLog(fileSystem.GitHash, versionChange, xml);


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

        if (!this.TryGetRefAndLastCalculatedVersion(fileSystem.GitHash, out var refVersion, out var lastCalculatedVersion))
        {
            refVersion = projectFileVersion;
            lastCalculatedVersion = projectFileVersion;

            this.SaveRefAndLastCalculatedVersion(fileSystem.GitHash, refVersion, lastCalculatedVersion);
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
            var versionSuffix = string.Empty;

            if (updateResult.CalculatedVersion.Major == 0)
            {
                versionSuffix = updateResult.CalculatedVersion.Minor == 0 ? "alpha" : "beta";
            }

            this.UpdateProjectFile(updateResult.CalculatedVersion, versionSuffix, fileSystem.GitHash);

            var gitChangesList = gitChanges.ToList();
            gitChangesList.Add(this._projectFileName);
            gitChanges = gitChangesList.ToArray();
        }

        this.SaveRefAndLastCalculatedVersion(fileSystem.GitHash, refVersion, updateResult.CalculatedVersion);
        this.CopyTargetFileToRefVersionBin(gitChanges.Any());

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

    private bool TryGetRefAndLastCalculatedVersion(string gitHash,
        [MaybeNullWhen(false)] out Version refVersion, [MaybeNullWhen(false)] out Version lastCalculatedVersion)
    {
        refVersion = null;
        lastCalculatedVersion = null;

        var versioningDir = this.CreateVersioningCacheTargetDirIfNotExists(gitHash);
        var lastCalculatedVersionPath = Path.Combine(versioningDir, "versionInfo.txt");

        if (!File.Exists(lastCalculatedVersionPath))
        {
            if (!this.TryGetRefAssemblyFileInfo(gitHash, out var fileInfo)) return false;

            if (!this.TryGetProjectFileAssemblyVersion(out var projectFileVersion))
            {
                projectFileVersion = new Version(0, 0, 0, 0);
            }

            if (this.VersioningNuGetFileExist)
            {
                if (!this.TryGetAssemblyFrameworkInfo(fileInfo.FullName, out var assemblyFrameworkInfo)) return false;
                refVersion = assemblyFrameworkInfo.Version;
            }
            else
            {
                refVersion = projectFileVersion;
            }

            this.SaveRefAndLastCalculatedVersion(gitHash, refVersion, projectFileVersion);
        }

        var fileContent = File.ReadAllLines(lastCalculatedVersionPath).ToList();

        return fileContent.Count > 1 &&
               Version.TryParse(fileContent[0], out refVersion!) &&
               Version.TryParse(fileContent[1], out lastCalculatedVersion!);
    }

    private void ResolveRefTargetFile(FileSystem fileSystem)
    {
        var refTargetFileName = fileSystem.RefTargetFileName;
        var cacheBaseDir = fileSystem.CacheBaseDir;
        var projectRefDir = fileSystem.ProjectRefDir;

        if (File.Exists(refTargetFileName)) return;

        if (this.TryDownloadNugetPackage(cacheBaseDir) && File.Exists(refTargetFileName))
        {
            if (Directory.Exists(projectRefDir)) Directory.Delete(projectRefDir, true);

            this.VersioningNuGetFileExist = true;
            logger.LogInformation("File '{refAssemblyPath}' was downloaded from NuGet.", refTargetFileName);
            return;
        }

        logger.LogInformation("The file '{refAssemblyPath}' could not be downloaded from NuGet.", refTargetFileName);
        this.VersioningNuGetFileExist = false;

        var projectRefFileName = fileSystem.ProjectRefFileName;

        if (File.Exists(projectRefFileName))
        {
            File.Copy(projectRefFileName, refTargetFileName);
            return;
        }

        var targetFileName = fileSystem.TargetFileName;

        if (!File.Exists(targetFileName)) return;
        File.Copy(targetFileName, refTargetFileName, true);
    }

    private bool TryGetRefAssemblyFileInfo(string gitHash, out FileInfo fileInfo)
    {
        var versioningDir = this.CreateVersioningCacheTargetDirIfNotExists(gitHash);
        var refAssemblyPath = Path.Combine(versioningDir, Path.GetFileName(this._targetFileName));

        if (File.Exists(refAssemblyPath))
        {
            fileInfo = new FileInfo(refAssemblyPath);
            return true;
        }

        var versionBinPath = this.GetVersioningRefBinPath();

        if (this.TryDownloadNugetPackage(this.CreateVersioningCacheDirIfNotExists(gitHash)) && File.Exists(refAssemblyPath))
        {
            if (File.Exists(versionBinPath)) File.Delete(versionBinPath);
            fileInfo = new FileInfo(refAssemblyPath);

            this.VersioningNuGetFileExist = true;
            logger.LogInformation("File '{refAssemblyPath}' was downloaded from NuGet.", refAssemblyPath);
            return true;
        }

        logger.LogInformation("The file '{refAssemblyPath}' could not be downloaded from NuGet.", refAssemblyPath);
        this.VersioningNuGetFileExist = false;

        if (File.Exists(versionBinPath))
        {
            File.Copy(versionBinPath, refAssemblyPath);
            fileInfo = new FileInfo(refAssemblyPath);
            return true;
        }

        fileInfo = new FileInfo(this._targetFileName);
        if (!fileInfo.Exists) return false;

        File.Copy(this._targetFileName, refAssemblyPath, true);
        fileInfo = new FileInfo(refAssemblyPath);
        return true;
    }



    private void SaveRefAndLastCalculatedVersion(string gitHash, Version refVersion, Version calculatedVersion)
    {
        var versioningDir = this.CreateVersioningCacheTargetDirIfNotExists(gitHash);
        File.WriteAllLines(Path.Combine(versioningDir, "versionInfo.txt"), new[] { refVersion.ToString(), calculatedVersion.ToString() });
    }

    private void WriteChangeLog(string gitHash, VersionChange versionChange, string xmlDiff)
    {
        var versioningDir = this.CreateVersioningCacheTargetDirIfNotExists(gitHash);
        var log = new List<string>
        {
            $"[{DateTime.Now:yyyy:MM:dd HH:mm:ss}] {versionChange}",
            xmlDiff,
            " "
        };

        File.AppendAllLines(Path.Combine(versioningDir, "changelog.log"), log);
    }

    private void CopyTargetFileToRefVersionBin(bool hasGitChanges)
    {
        if (this.VersioningNuGetFileExist) return;
        if (!File.Exists(this._targetFileName)) return;

        var versionBinPath = this.GetVersioningRefBinPath();

        if (File.Exists(versionBinPath))
        {
            if (!hasGitChanges) return;

            var projectRefFileInfo = new FileInfo(versionBinPath);
            var targetFileInfo = new FileInfo(this._targetFileName);

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

        File.Copy(this._targetFileName, versionBinPath, true);
        logger.LogInformation("File was copied from '{targetFileName}' to '{versionBinPath}'.", this._targetFileName, versionBinPath);
    }

    private bool ShouldIncreaseBuildVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        if (versionChange > VersionChange.Revision) return false;

        var gitDiffFilter = this.GetGitDiffFilter();
        var projectFiles = Directory.GetFiles(this._projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => gitDiffFilter.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x.Substring(this._gitRepositoryDirName.Length + 1).Replace('\\', '/'));

        return projectFiles.Any(projectFile =>
            gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));

    }

    private bool ShouldIncreaseRevisionVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        return versionChange < VersionChange.Revision && gitChanges.Any(x => !x.EndsWith("version.bin"));
    }

    private string GetVersioningRefBinPath()
    {
        var pathItems = new List<string> { this._projectDirName, ".versioning", "ref" };

        pathItems.AddRange(this.GetTargetFrameworkPlatformName());

        var path = Path.Combine(pathItems.ToArray());

        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        return Path.Combine(path, "version.bin");
    }

    private string CreateVersioningDirIfNotExists()
    {
        var versioningCacheDir = Path.Combine(this._projectDirName, ".versioning", "cache");
        if (Directory.Exists(versioningCacheDir)) return versioningCacheDir;
        Directory.CreateDirectory(versioningCacheDir);
        return versioningCacheDir;
    }

    private string CreateVersioningCacheDirIfNotExists(string gitHash)
    {
        var versioningCacheDir = Path.Combine(this._projectDirName, ".versioning", "cache", gitHash);
        if (Directory.Exists(versioningCacheDir)) return versioningCacheDir;
        Directory.CreateDirectory(versioningCacheDir);
        return versioningCacheDir;
    }

    private string CreateVersioningCacheTargetDirIfNotExists(string gitHash)
    {
        var pathItems = new List<string> { this.CreateVersioningCacheDirIfNotExists(gitHash) };

        pathItems.AddRange(this.GetTargetFrameworkPlatformName());
        var versioningDir = Path.Combine(pathItems.ToArray());

        if (Directory.Exists(versioningDir)) return versioningDir;

        this.AddToGitIgnore("Versioning cache", "**/.[Vv]ersioning/[Cc]ache/");

        Directory.CreateDirectory(versioningDir);

        return versioningDir;
    }

    private void AddToGitIgnore(string description, string ignorePattern)
    {
        var gitIgnorePath = Path.Combine(this._gitRepositoryDirName, ".gitignore");

        if (!File.Exists(gitIgnorePath)) return;

        var allLines = File.ReadAllLines(gitIgnorePath).ToList();

        if (allLines.Any(x => x == ignorePattern)) return;

        if (!description.StartsWith("#")) description = string.Concat("# ", description);
        File.AppendAllLines(gitIgnorePath, new[] { description, ignorePattern });
        logger.LogInformation("Add '{ignorePattern} to '{gitIgnorePath}'.", ignorePattern, gitIgnorePath);
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

    private IEnumerable<string> GetTargetFrameworkPlatformName()
    {
        if (!this.TryGetAssemblyFrameworkInfo(this._targetFileName, out var assemblyFrameworkInfo)) return Enumerable.Empty<string>();

        var result = new List<string>();

        if (!string.IsNullOrEmpty(assemblyFrameworkInfo.FrameworkShortFolderName))
        {
            result.Add(assemblyFrameworkInfo.FrameworkShortFolderName);
        }

        if (!string.IsNullOrEmpty(assemblyFrameworkInfo.TargetPlatform))
        {
            result.Add(assemblyFrameworkInfo.TargetPlatform);
        }

        return result.ToArray();
    }

    private bool TryGetAssemblyFrameworkInfo(string assemblyLocation, [MaybeNullWhen(false)] out AssemblyFrameworkInfo assemblyFrameworkInfo)
    {
        if (this._assemblyFrameworkInfoCache.TryGetValue(assemblyLocation, out assemblyFrameworkInfo)) return true;
        if (!File.Exists(assemblyLocation)) return false;

        assemblyFrameworkInfo = new AssemblyFrameworkInfo(assemblyLocation);
        this._assemblyFrameworkInfoCache.Add(assemblyLocation, assemblyFrameworkInfo);

        return true;
    }

    public VersioningResult ValidateFileSystem(FileSystem fileSystem)
    {
        var updateResult = new VersioningResult();

        if (!File.Exists(fileSystem.TargetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(fileSystem.ProjectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(fileSystem.ProjectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(fileSystem.GitRepositoryDirName))
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

        fileSystem.GitHash = gitHash;

        if (this.TryGetAssemblyFrameworkInfo(fileSystem.TargetFileName, out var assemblyFrameworkInfo))
        {
            fileSystem.TargetFramework = assemblyFrameworkInfo.FrameworkShortFolderName ?? string.Empty;
            fileSystem.TargetPlatform = assemblyFrameworkInfo.TargetPlatform ?? string.Empty;
        }

        this._targetFileName = fileSystem.TargetFileName;
        this._projectDirName = fileSystem.ProjectDirName;
        this._projectFileName = fileSystem.ProjectFileName;
        this._gitRepositoryDirName = fileSystem.GitRepositoryDirName;

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