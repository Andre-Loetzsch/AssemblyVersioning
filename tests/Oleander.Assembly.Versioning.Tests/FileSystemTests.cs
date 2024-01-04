using Oleander.Assembly.Versioning.FileSystems;
using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class FileSystemTests
{
    [Fact]
    public void TestAddToGitIgnoreFile()
    {
        var testPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileSystemTest");
        var gitignoreFileName = Path.Combine(testPath, ".gitignore");

        if (Directory.Exists(testPath)) Directory.Delete(testPath, true);
        Directory.CreateDirectory(testPath);

        File.WriteAllLines(gitignoreFileName, Enumerable.Empty<string>());

        var fileSystem = new FileSystem(new NullLogger())
        {
            GitHash = "122ce327",
            TargetPlatform = "any",
            GitRepositoryDirInfo = new(testPath),
            ProjectDirInfo = new(testPath)
        };

        fileSystem.CacheDirInfo.DeleteDirectoryIfExist();
        fileSystem.CacheDirInfo.CreateDirectoryIfNotExist();

        Assert.Contains("**/.[Vv]ersioning/[Cc]ache/", File.ReadAllLines(gitignoreFileName));

        Directory.Delete(testPath, true);
    }
}