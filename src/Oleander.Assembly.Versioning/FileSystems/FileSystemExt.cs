namespace Oleander.Assembly.Versioning.FileSystems;

internal static class FileSystemExt
{
    public static bool CreateDirectoryIfNotExist(this DirectoryInfo dirInfo)
    {
        if (dirInfo.Exists) return false;
        dirInfo.Create();
        return true;
    }

    public static bool DeleteDirectoryIfExist(this DirectoryInfo dirInfo)
    {
        if (!dirInfo.Exists) return false;
        dirInfo.Delete(true);
        return true;
    }

    public static bool CreateDirectoryIfNotExist(this FileInfo fileInfo)
    {
        var dirName = fileInfo.DirectoryName ??
                      throw new ArgumentException($"Directory name cannot be null! (FileInfo='{fileInfo.FullName}')");

        if (Directory.Exists(dirName)) return false;
        Directory.CreateDirectory(dirName);
        return true;
    }

    public static bool DeleteFileIfExist(this FileInfo fileInfo)
    {
        if (!fileInfo.Exists) return false;
        fileInfo.Delete();
        return true;
    }
}