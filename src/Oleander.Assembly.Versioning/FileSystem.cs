using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning;

internal class FileSystem(ILogger logger)
{
    #region GiTHash

    public string GitHash { get; set; } = string.Empty;

    #endregion

    #region TargetFileName

    public string TargetFileName { get; set; } = string.Empty;

    #endregion

    #region RefTargetFileName

    public string RefTargetFileName => Path.Combine(this.CacheDir, Path.GetFileName(this.TargetFileName));

    #endregion

    #region GitRepositoryDirName

    private string _gitRepositoryDirName = string.Empty;
    public string GitRepositoryDirName
    {
        get
        {
            if (string.IsNullOrEmpty(this._gitRepositoryDirName) &&
                TryFindGitRepositoryDirName(this.ProjectDirName, out var gitRepositoryDirName))
            {
                this._gitRepositoryDirName = gitRepositoryDirName;
            }

            return this._gitRepositoryDirName;

        }
        set => this._gitRepositoryDirName = value;
    }

    #endregion

    #region ProjectDirName

    private string _projectDirName = string.Empty;
    public string ProjectDirName
    {
        get
        {
            if (!string.IsNullOrEmpty(this._projectDirName)) return this._projectDirName;

            var targetDirName = Path.GetDirectoryName(this.TargetFileName);

            if (targetDirName == null || !MSBuildProject.TryFindVSProject(targetDirName, out var projectDirName, out var projectFileName))
                return this._projectDirName;

            this._projectDirName = projectDirName;
            if (string.IsNullOrEmpty(this._projectFileName)) this._projectFileName = projectFileName;
            return this._projectDirName;

        }
        set => this._projectDirName = value;
    }

    #endregion

    #region ProjectFileName

    private string _projectFileName = string.Empty;
    public string ProjectFileName
    {
        get
        {
            if (!string.IsNullOrEmpty(this._projectFileName)) return this._projectFileName;

            var targetDirName = Path.GetDirectoryName(this.TargetFileName);

            if (targetDirName == null || !MSBuildProject.TryFindVSProject(targetDirName, out var projectDirName, out var projectFileName))
                return this._projectFileName;

            this._projectFileName = projectFileName;


            if (string.IsNullOrEmpty(this._projectDirName)) this._projectDirName = projectDirName;

            return this._projectFileName;

        }
        set => this._projectFileName = value;
    }

    #endregion

    #region VersioningDir

    public string VersioningDir => this.CreateDirectoryInfo(true, this.ProjectDirName, ".versioning").FullName;

    #endregion

    #region TargetFramework

    public string TargetFramework { get; set; } = string.Empty;

    #endregion

    #region TargetPlatform

    private string _targetPlatform = "Any";
    public string TargetPlatform
    {
        get => this._targetPlatform;
        set
        {
            if (string.IsNullOrEmpty(value)) value = "Any";
            this._targetPlatform = value;
        }
    }

    #endregion

    #region CacheBaseDir

    public string CacheBaseDir => this.CreateDirectoryInfo(true, this.VersioningDir, "cache", this.GitHash).FullName;

    #endregion

    #region CacheDir

    public string CacheDir
    {
        get
        {
            var dirInfo = this.CreateDirectoryInfo(false, this.CacheBaseDir, this.TargetFramework, this.TargetPlatform);
            if (dirInfo.Exists) return dirInfo.FullName;
            dirInfo.Create();

            this.AddToGitIgnore("Versioning cache", "**/.[Vv]ersioning/[Cc]ache/");

            return dirInfo.FullName;
        }
    }

    #endregion

    #region ProjectRefBaseDir

    public string ProjectRefBaseDir => this.CreateDirectoryInfo(true, this.VersioningDir, "ref").FullName;


    #endregion

    #region ProjectRefDir

    private string PrivateProjectRefDirName => this.CreateDirectoryInfo(false, this.VersioningDir, "ref", this.TargetFramework, this.TargetPlatform).FullName;

    public bool ExistProjectRefDir => Directory.Exists(this.PrivateProjectRefDirName);

    public void DeleteProjectRefDirIfExist()
    {
        if (Directory.Exists(this.PrivateProjectRefDirName)) Directory.Delete(this.PrivateProjectRefDirName, true);
    }

    public string ProjectRefDir => this.CreateDirectoryInfo(true, this.PrivateProjectRefDirName).FullName;

    #endregion

    #region ProjectRefFileName

    public string ProjectRefFileName => Path.Combine(this.ProjectRefDir, "version.bin");

    #endregion

    #region private members

    private void AddToGitIgnore(string description, string ignorePattern)
    {
        var gitIgnorePath = Path.Combine(this.GitRepositoryDirName, ".gitignore");

        if (!File.Exists(gitIgnorePath)) return;

        var allLines = File.ReadAllLines(gitIgnorePath).ToList();

        if (allLines.Any(x => x == ignorePattern)) return;

        if (!description.StartsWith("#")) description = string.Concat("# ", description);
        File.AppendAllLines(gitIgnorePath, new[] { description, ignorePattern });
        logger.LogInformation("Add '{ignorePattern} to '{gitIgnorePath}'.", ignorePattern, gitIgnorePath);
    }

    private static bool TryFindGitRepositoryDirName(string? startDirectory, [MaybeNullWhen(false)] out string gitRepositoryDirName)
    {
        gitRepositoryDirName = null;
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

    private DirectoryInfo CreateDirectoryInfo(bool createDirectoryIfNotExist, params string[] paths)
    {
        var dir = Path.Combine(paths.Where(x => !string.IsNullOrEmpty(x)).ToArray());
        var dirInfo = new DirectoryInfo(dir);

        if (!createDirectoryIfNotExist || dirInfo.Exists) return dirInfo;
        logger.LogInformation("Create directory '{dir}'.", dir);
        dirInfo.Create();

        return dirInfo;
    }

    #endregion
}