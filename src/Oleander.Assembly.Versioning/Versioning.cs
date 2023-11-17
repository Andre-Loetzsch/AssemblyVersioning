using System.Diagnostics.CodeAnalysis;
using Oleander.Assembly.Comparator;
using Oleander.Assembly.Versioning.ExternalProcesses;
using SysAssembly = System.Reflection.Assembly;

namespace Oleander.Assembly.Versioning;

public class Versioning
{
    private const string versionInfoFileName = ".versionInfo";
    private string _targetFileName = string.Empty;
    private string _projectDirName = string.Empty;
    private string _projectFileName = string.Empty;
    private string _gitRepositoryDirName = string.Empty;

    private readonly string _currentDirectory = Directory.GetCurrentDirectory();

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
        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitGetHash().Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        hash = result.StandardOutput.Trim();

        return true;
    }

    protected virtual bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        gitChanges = null;

        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitDiffNameOnly(gitHash).Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput))
        {
            gitChanges = Array.Empty<string>();
            return true;
        }

        gitChanges = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return true;
    }

    protected virtual string[] GetGitDiffFiler()
    {
        if (Directory.Exists(this._projectDirName) &&
            File.Exists(Path.Combine(this._projectDirName, ".gitdiff")))
        {
            return File.ReadAllLines(Path.Combine(this._projectDirName, ".gitdiff"));
        }

        return new[] { ".cs", ".xaml" };
    }

    #endregion

    #region internal / private members

    internal static Version CalculateVersion(Version version, bool increaseMajor, bool increaseMinor, bool increaseBuild, bool increaseRevision)
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
        var versionChange = this.TryGetRefAssemblyFileInfo(shortGitHash, out var refAssemblyFileInfo) ?
            new AssemblyComparison(refAssemblyFileInfo, targetAssemblyFileInfo).VersionChange : VersionChange.None;
        
        var projectFileAssemblyVersion = this.GetAssemblyVersionFromProjectFile();
        var lastCalculatedVersion = this.TryGetLastCalculatedVersionVersion(shortGitHash, out var v) ? v : projectFileAssemblyVersion;
        var refAssemblyVersion = this.TryGetRefAssemblyVersion(shortGitHash, out v) ? v : new Version(0, 0, 0, 0);

        if (this.IncreaseBuild(gitChanges) && versionChange < VersionChange.Build) versionChange = VersionChange.Build;
        updateResult.CalculatedVersion = CalculateVersion(refAssemblyVersion, versionChange);

        if (projectFileAssemblyVersion <= lastCalculatedVersion)
        {
            var versionSuffix = updateResult.CalculatedVersion.Major == 0 ? "alpha" :
                updateResult.CalculatedVersion.Minor == 0 ? "beta" : string.Empty;

            UpdateProjectFile(this._projectFileName, updateResult.CalculatedVersion, versionSuffix, longGtHash);
        }

        this.RememberLastCalculatedVersion(shortGitHash, updateResult.CalculatedVersion);
        this.CopyTargetFileToProjectRefFile();

        return updateResult;
    }

    private bool TryGetLastCalculatedVersionVersion(string gitHash, [MaybeNullWhen(false)] out Version lastCalculatedVersion)
    {
        lastCalculatedVersion = null;

        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var lastCalculatedVersionPath = Path.Combine(versioningDir, 
            string.Concat(Path.GetFileName(this._targetFileName), versionInfoFileName));

        if (!File.Exists(lastCalculatedVersionPath)) return false;

        var fileContent = File.ReadAllLines(lastCalculatedVersionPath).FirstOrDefault();

        return fileContent != null && Version.TryParse(fileContent, out lastCalculatedVersion);
    }

    public static Version CalculateVersion(Version version, VersionChange versionChange)
    {
        var versionAsList = new List<int> { version.Major, version.Minor, version.Build, version.Revision };

        switch (versionChange)
        {
            case VersionChange.Major:
                versionAsList[0] = version.Major + 1;
                versionAsList[1] = 0;
                versionAsList[2] = 0;
                versionAsList[3] = 0;
                break;
            case VersionChange.Minor:
                versionAsList[1] = version.Minor + 1;
                versionAsList[2] = 0;
                versionAsList[3] = 0;
                break;
            case VersionChange.Build:
                versionAsList[2] = version.Build + 1;
                versionAsList[3] = 0;
                break;
            case VersionChange.Revision:
                versionAsList[3] = version.Revision + 1;
                break;
            case VersionChange.None:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(versionChange), versionChange, null);
        }

        if (version.Major == 0)         // beta
        {
            versionAsList.Insert(0, 0); // alpha

            if (version.Minor == 0)
            {
                versionAsList.Insert(0, 0);
            }
        }

        return new Version(versionAsList[0], versionAsList[1], versionAsList[2], versionAsList[3]);
    }

    private bool TryGetRefAssemblyVersion(string gitHash, [MaybeNullWhen(false)] out Version version)
    {
        version = null;

        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var refAssemblyPath = Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName)));

        if (!File.Exists(refAssemblyPath)) return false;

        version = SysAssembly.Load(File.ReadAllBytes(refAssemblyPath)).GetName().Version;
        return version != null;
    }

    private  bool TryGetRefAssemblyFileInfo(string gitHash, [MaybeNullWhen(false)]out FileInfo fileInfo)
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

    private void RememberLastCalculatedVersion(string gitHash, Version calculatedVersion)
    {
        var versioningDir = Path.Combine(this._projectDirName, ".versioning", gitHash);
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        File.WriteAllText(Path.Combine(versioningDir, 
            string.Concat(Path.GetFileName(this._targetFileName), versionInfoFileName)), calculatedVersion.ToString());
    }

    private void CopyTargetFileToProjectRefFile()
    {
        if (!File.Exists(this._targetFileName)) return;

        var projectRefAssemblyPath = Path.Combine(this._projectDirName, versionInfoFileName);
        File.Copy(this._targetFileName, projectRefAssemblyPath, true);
    }

    private bool IncreaseBuild(IEnumerable<string> gitChanges)
    {
        var gitDiffFilter = this.GetGitDiffFiler();
        var projectFiles = Directory.GetFiles(this._projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => gitDiffFilter.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x[(this._gitRepositoryDirName.Length + 1)..].Replace('\\', '/'));

        return projectFiles.Any(
            projectFile => gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));

    }

    private Version GetAssemblyVersionFromProjectFile()
    {
        var vsProject = new VSProject(this._projectFileName);
        var projectFileAssemblyVersion = vsProject.AssemblyVersion;

        if (projectFileAssemblyVersion != null &&
            Version.TryParse(projectFileAssemblyVersion, out var version)) return version;

        version = new Version(0, 0, 0, 0);
        vsProject.AssemblyVersion = version.ToString();
        vsProject.SaveChanges();

        return version;
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

    private static void UpdateProjectFile(string projectFileName, Version assemblyVersion, string versionSuffix,  string sourceRevisionId)
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
}