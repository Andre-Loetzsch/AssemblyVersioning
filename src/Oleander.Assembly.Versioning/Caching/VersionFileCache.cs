using Oleander.Assembly.Versioning.FileSystems;

namespace Oleander.Assembly.Versioning.Caching
{
    internal class VersionFileCache(FileInfo cacheFileInfo)
    {
        public FileInfo CacheFileInfo { get; } = cacheFileInfo;
        public Version RefVersion { get; set; } = new Version(0, 0, 0, 0);
        public Version LastUsedVersion { get; set; } = new Version(0, 0, 0, 0);
        public Version DesiredVersion { get; set; } = new Version(0, 0, 0, 0);

        public void Write()
        {
            var fileContent = new string[3];

            fileContent[0] = this.RefVersion.ToString();
            fileContent[1] = this.LastUsedVersion.ToString();
            fileContent[2] = this.DesiredVersion.ToString();
            
            File.WriteAllLines(this.CacheFileInfo.FullName, fileContent);
        }

        public void Read()
        {
            var fileContent = File.ReadAllLines(this.CacheFileInfo.FullName);

            for (var i = 0; i < fileContent.Length; i++)
            {
                if (!Version.TryParse(fileContent[i], out var version)) continue;

                switch (i)
                {
                    case 0:
                        this.RefVersion = version;
                        break;
                    case 1:
                        this.LastUsedVersion = version;
                        break;
                    case 2:
                        this.DesiredVersion = version;
                        break;
                }
            }
        }
    }
}
