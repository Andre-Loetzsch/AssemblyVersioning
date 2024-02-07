using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Oleander.Assembly.Versioning.FileSystems;

internal class FileSystem(ILogger logger)
{
    #region string properties

    #region GiTHash

    public string GitHash { get; set; } = string.Empty;

    #endregion

    #region TargetFramework

    public string TargetFramework { get; set; } = string.Empty;

    #endregion

    #region TargetPlatform

    public string TargetPlatform { get; set; } = string.Empty;

    #endregion

    #endregion

    #region FileInfos

    #region TargetFileInfo

    private FileInfo? _targetFileInfo;

    public FileInfo TargetFileInfo
    {
        get
        {
            if (this._targetFileInfo == null)
                throw new NullReferenceException("TargetFileInfo is null!");

            return this._targetFileInfo;
        }
        set => this._targetFileInfo = value;
    }

    #endregion

    #region RefTargetFileInfo

    public FileInfo RefTargetFileInfo => new(Path.Combine(this.CacheDirInfo.FullName, this.TargetFileInfo.Name));

    #endregion

    #region VersionFileCacheFileInfo

    public FileInfo VersionFileCacheFileInfo => new(Path.Combine(this.CacheDirInfo.FullName, "versionInfo.txt"));

    #endregion

    #region ChangelogFileInfo

    public FileInfo ChangelogFileInfo => new(Path.Combine(this.CacheDirInfo.FullName, "changelog.log"));

    #endregion

    #region ProjectFileInfo

    private FileInfo? _projectFileInfo;
    public FileInfo ProjectFileInfo
    {
        get
        {
            if (this._projectFileInfo != null) return this._projectFileInfo;

            var targetDirName = this.TargetFileInfo.DirectoryName;

            if (targetDirName == null || !MSBuildProject.TryFindVSProject(targetDirName, out var projectDirName, out var projectFileName))
            {
                throw new FileNotFoundException("Project file not found!");
            }

            this._projectFileInfo = new(projectFileName);
            this._projectDirInfo ??= new(projectDirName);

            return this._projectFileInfo;

        }
        set => this._projectFileInfo = value;
    }

    #endregion

    #region ProjectRefFileName

    public FileInfo ProjectRefFileInfo => new(Path.Combine(this.ProjectRefDirInfo.FullName, "version.bin"));

    #endregion

    #region GitDiffFileInfo

    public FileInfo GitDiffFileInfo => new(Path.Combine(this.ProjectDirInfo.FullName, ".gitdiff"));

    #endregion

    #region ProjectApiIgnoreFileInfo

    public FileInfo ProjectApiIgnoreFileInfo => new(Path.Combine(this.ProjectDirInfo.FullName, ".versioningIgnore"));

    #endregion

    #region GitRepositoryApiIgnoreFileInfo

    public FileInfo GitRepositoryApiIgnoreFileInfo => new(Path.Combine(this.GitRepositoryDirInfo.FullName, ".versioningIgnore"));

    #endregion

    #endregion

    #region DirectoryInfos

    #region GitRepositoryDirInfo

    private DirectoryInfo? _gitRepositoryDirInfo;
    public DirectoryInfo GitRepositoryDirInfo
    {
        get
        {
            if (this._gitRepositoryDirInfo != null) return this._gitRepositoryDirInfo;

            if (!TryFindGitRepositoryDirName(this.ProjectDirInfo.FullName, out var gitRepositoryDirName))
            {
                throw new DirectoryNotFoundException("Git repository directory not found!");
            }

            this._gitRepositoryDirInfo = new(gitRepositoryDirName);
            return this._gitRepositoryDirInfo;

        }
        set => this._gitRepositoryDirInfo = value;
    }

    #endregion

    #region ProjectDirInfo

    private DirectoryInfo? _projectDirInfo;
    public DirectoryInfo ProjectDirInfo
    {
        get
        {
            if (this._projectDirInfo != null) return this._projectDirInfo;

            var targetDirName = this.TargetFileInfo.DirectoryName;

            if (targetDirName == null || !MSBuildProject.TryFindVSProject(targetDirName, out var projectDirName, out var projectFileName))
            {
                throw new DirectoryNotFoundException("Project directory not found!");
            }

            this._projectDirInfo = new(projectDirName);
            this._projectFileInfo ??= new(projectFileName);

            return this._projectDirInfo;

        }
        set => this._projectDirInfo = value;
    }

    #endregion

    #region VersioningDirInfo

    public DirectoryInfo VersioningDirInfo => CreateDirectoryInfo(this.ProjectDirInfo, ".versioning");

    #endregion

    #region CacheBaseDirInfo

    public DirectoryInfo CacheBaseDirInfo => CreateDirectoryInfo(this.VersioningDirInfo, "cache", this.GitHash.Length > 8 ? this.GitHash.Substring(0, 8) : this.GitHash);

    #endregion

    #region CacheDirInfo

    public DirectoryInfo CacheDirInfo
    {
        get
        {
            var dirInfo = CreateDirectoryInfo(this.CacheBaseDirInfo, this.TargetFramework, this.TargetPlatform);
            if (dirInfo.Exists) return dirInfo;

            this.AddToGitIgnore("Versioning cache", "**/.[Vv]ersioning/[Cc]ache/");

            return dirInfo;
        }
    }

    #endregion

    #region ProjectRefDirInfo

    /// <summary>
    /// “Directory exists” is the indicator that we are using version.bin as the project reference,  otherwise nuget is used as the reference.
    /// </summary>
    public DirectoryInfo ProjectRefDirInfo => CreateDirectoryInfo(this.VersioningDirInfo, "ref", this.TargetFramework, this.TargetPlatform);

    #endregion

    #endregion

    #region private members

    private void AddToGitIgnore(string description, string ignorePattern)
    {
        var gitIgnorePath = Path.Combine(this.GitRepositoryDirInfo.FullName, ".gitignore");

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

    private static DirectoryInfo CreateDirectoryInfo(FileSystemInfo directoryInfo, params string[] paths)
    {
        var pathList = new List<string> { directoryInfo.FullName };
        pathList.AddRange(paths);
        return new(Path.Combine(pathList.Where(x => !string.IsNullOrEmpty(x)).ToArray()));
    }

    #endregion
}