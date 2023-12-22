namespace Oleander.Assembly.Versioning;

internal class VersioningDirectories(string projectDir, string gitHash)
{
    private readonly string _gitHash = gitHash;

    public string ProjectDir { get; } = projectDir;

    public string VersioningDir => CreateDirectoryIfNotExist(this.ProjectDir, "versioning");

    public string CacheBaseDir => CreateDirectoryIfNotExist(this.VersioningDir, "cache");
    public string CacheDir => CreateDirectoryIfNotExist(this.CacheBaseDir, this._gitHash);

    public string ProjectRefDir => CreateDirectoryIfNotExist(this.VersioningDir, "ref");

    public string TargetFramework { get; set; } = string.Empty; 
    public string TargetPlatform { get; set; } = string.Empty;
    public string TargetCacheDir => CreateDirectoryIfNotExist(this.CacheDir, this.TargetFramework, this.TargetPlatform);



    private static string CreateDirectoryIfNotExist(params string[] paths)
    {
        if (paths.Length == 0) return string.Empty;
        var dir = Path.Combine(paths.Where(x => !string.IsNullOrEmpty(x)).ToArray());
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }


}