using System.Diagnostics.CodeAnalysis;
using Oleander.Assembly.Comparator;
using Oleander.Assembly.Versioning.ExternalProcesses;

namespace Oleander.Assembly.Versioning;

public class Versioning
{
    private const string versionInfoFileName = ".versionInfo";
    private string _targetFileName = string.Empty;
    private string _projectDirName = string.Empty;
    private string _projectFileName = string.Empty;
    private string _gitRepositoryDirName = string.Empty;

    #region UpdateAssemblyVersion

    public VersioningResult UpdateAssemblyVersion(string targetFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = string.Empty;
        this._projectFileName = string.Empty;
        this._gitRepositoryDirName = string.Empty;

        var updateResult = new VersioningResult();

        if (!File.Exists(targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        var targetDir = Path.GetDirectoryName(targetFileName);

        if (targetDir == null || !Directory.Exists(targetDir))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetDirNotExist;
            return updateResult;
        }

        if (!VSProject.TryFindVSProject(targetDir, out var projectDirName, out var projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = string.Empty;

        var updateResult = new VersioningResult();

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = gitRepositoryDirName;

        var updateResult = new VersioningResult();

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    #endregion

    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string hash)
    {
        hash = null;
        result = new GitGetHash(this._gitRepositoryDirName).Start();

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        hash = result.StandardOutput.Trim();

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

        gitChanges = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
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

    #endregion

    #region private members

    private VersioningResult PrivateUpdateAssemblyVersion()
    {
        var updateResult = new VersioningResult();

        if (!this.TryGetGitHash(out var result, out var longGtHash))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitHashFailed;
            return updateResult;
        }

        if (!this.TryGetGitChanges(longGtHash, out result, out var gitChanges))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitDiffNameOnlyFailed;
            return updateResult;
        }

        var shortGitHash = longGtHash[..8];
        var targetAssemblyFileInfo = new FileInfo(this._targetFileName);


        var versionChange = VersionChange.None;

        if (this.TryGetRefAssemblyFileInfo(shortGitHash, out var refAssemblyFileInfo))
        {
            var comparison = new AssemblyComparison(refAssemblyFileInfo, targetAssemblyFileInfo);
            versionChange = comparison.VersionChange;

            this.WriteChangeLog(shortGitHash, versionChange, comparison.ToXml());
        }

        var projectFileVersion = this.TryGetProjectFileAssemblyVersion(out var v) ? v : new Version(0, 0, 1, 0);

        if (!this.TryGetRefAndLastCalculatedVersion(shortGitHash, out var refVersion, out var lastCalculatedVersion))
        {
            refVersion = projectFileVersion;
            lastCalculatedVersion = projectFileVersion;

            this.SaveRefAndLastCalculatedVersion(shortGitHash, refVersion, lastCalculatedVersion);
        }

        if (this.ShouldIncreaseBuildVersion(gitChanges, versionChange)) versionChange = VersionChange.Build;
        if (this.ShouldIncreaseRevisionVersion(gitChanges, versionChange)) versionChange = VersionChange.Revision;

        updateResult.CalculatedVersion = CalculateVersion(refVersion, versionChange);

        if (projectFileVersion <= lastCalculatedVersion && projectFileVersion != updateResult.CalculatedVersion)
        {
            var versionSuffix = updateResult.CalculatedVersion.Major == 0 ? "alpha" :
                updateResult.CalculatedVersion.Minor == 0 ? "beta" : string.Empty;

            UpdateProjectFile(this._projectFileName, updateResult.CalculatedVersion, versionSuffix, longGtHash);

            var gitChangesList = gitChanges.ToList();
            gitChangesList.Add(this._projectFileName);
            gitChanges = gitChangesList.ToArray();
        }

        this.SaveRefAndLastCalculatedVersion(shortGitHash, refVersion, updateResult.CalculatedVersion);
        this.CopyTargetFileToProjectRefFile(gitChanges.Any());

        return updateResult;
    }

    private bool TryGetProjectFileAssemblyVersion([MaybeNullWhen(false)] out Version version)
    {
        version = null;
        var vsProject = new VSProject(this._projectFileName);
        var projectFileAssemblyVersion = vsProject.AssemblyVersion;

        return projectFileAssemblyVersion != null &&
               Version.TryParse(projectFileAssemblyVersion, out version);

    }

    private bool TryGetRefAndLastCalculatedVersion(string gitHash,
        [MaybeNullWhen(false)] out Version refVersion,
        [MaybeNullWhen(false)] out Version lastCalculatedVersion)
    {
        refVersion = null;
        lastCalculatedVersion = null;

        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var lastCalculatedVersionPath = Path.Combine(versioningDir,
            string.Concat(Path.GetFileName(this._targetFileName), versionInfoFileName));

        if (!File.Exists(lastCalculatedVersionPath)) return false;

        var fileContent = File.ReadAllLines(lastCalculatedVersionPath).ToList();

        return fileContent.Count > 1 &&
               Version.TryParse(fileContent[0], out refVersion) &&
               Version.TryParse(fileContent[1], out lastCalculatedVersion);
    }

    private bool TryGetRefAssemblyFileInfo(string gitHash, [MaybeNullWhen(false)] out FileInfo fileInfo)
    {
        fileInfo = null;

        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var refAssemblyPath = Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName)));

        if (File.Exists(refAssemblyPath))
        {
            fileInfo = new FileInfo(refAssemblyPath);
            return true;
        }

        var projectRefAssemblyPath = Path.Combine(this._projectDirName, versionInfoFileName);

        if (File.Exists(projectRefAssemblyPath))
        {
            File.Copy(projectRefAssemblyPath, refAssemblyPath);
            fileInfo = new FileInfo(refAssemblyPath);
            return true;
        }

        if (!File.Exists(this._targetFileName)) return false;
        File.Copy(this._targetFileName, refAssemblyPath);
        fileInfo = new FileInfo(this._targetFileName);
        return true;
    }

    private void SaveRefAndLastCalculatedVersion(string gitHash, Version refVersion, Version calculatedVersion)
    {
        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        File.WriteAllLines(Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName), versionInfoFileName)),
            new[] { refVersion.ToString(), calculatedVersion.ToString() });
    }


    private void WriteChangeLog(string gitHash, VersionChange versionChange, string? xmlDiff)
    {
        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var log = new List<string>
        {
            $"[{DateTime.Now:yyyy:MM:dd HH:mm:ss}] {versionChange}"
        };

        if (xmlDiff != null) log.Add(xmlDiff);

        File.WriteAllLines(Path.Combine(versioningDir, "changelog.txt"), log);
    }


    private void CopyTargetFileToProjectRefFile(bool hasGitChanges)
    {
        if (!File.Exists(this._targetFileName)) return;

        var projectRefAssemblyPath = Path.Combine(this._projectDirName, versionInfoFileName);

        if (File.Exists(projectRefAssemblyPath))
        {
            if (!hasGitChanges) return;

            var projectRefFileInfo = new FileInfo(projectRefAssemblyPath);
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

        File.Copy(this._targetFileName, projectRefAssemblyPath, true);
    }

    private bool ShouldIncreaseBuildVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        if (versionChange > VersionChange.Revision) return false;

        var gitDiffFilter = this.GetGitDiffFilter();
        var projectFiles = Directory.GetFiles(this._projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => gitDiffFilter.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x[(this._gitRepositoryDirName.Length + 1)..].Replace('\\', '/'));

        return projectFiles.Any(projectFile => 
            gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));

    }

    private bool ShouldIncreaseRevisionVersion(IEnumerable<string> gitChanges, VersionChange versionChange)
    {
        return versionChange < VersionChange.Revision && gitChanges.Any(x => x != ".versionInfo");
    }

    private static bool TryFindGitRepositoryDirName(string? startDirectory, out string gitRepositoryDirName)
    {
        gitRepositoryDirName = string.Empty;
        if (string.IsNullOrEmpty(startDirectory)) return false;
        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            if (parentDir.GetDirectories(".git").Any())
            {
                gitRepositoryDirName = parentDir.FullName;
                return true;
            }

            parentDir = parentDir.Parent;
        }

        return false;
    }

    private static void UpdateProjectFile(string projectFileName, Version assemblyVersion, string versionSuffix, string sourceRevisionId)
    {
        var vsProject = new VSProject(projectFileName)
        {
            AssemblyVersion = assemblyVersion.ToString(),
            SourceRevisionId = sourceRevisionId,
            VersionSuffix = versionSuffix
        };

        vsProject.SaveChanges();
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