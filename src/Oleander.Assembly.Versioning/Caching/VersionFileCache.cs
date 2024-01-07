namespace Oleander.Assembly.Versioning.Caching
{
    internal class VersionFileCache(FileInfo cacheFileInfo)
    {
        public FileInfo CacheFileInfo { get; } = cacheFileInfo;
        public Version RefVersion { get; set; } = new Version();
        public Version LastCalculatedVersion { get; set; } = new Version();
        public Version ProjectFileVersion { get; set; } = new Version();

        public void Write()
        {
            var fileContent = new string[3];

            fileContent[0] = this.RefVersion.ToString();
            fileContent[1] = this.LastCalculatedVersion.ToString();
            fileContent[2] = this.ProjectFileVersion.ToString();

            File.WriteAllLines(this.CacheFileInfo.FullName, fileContent);
        }

        public void Read()
        {
            var fileContent = File.ReadAllLines(this.CacheFileInfo.FullName);

            for (var i = 0; i < fileContent.Length; i++)
            {
                if (!Version.TryParse(fileContent[i],out var version)) continue;

                switch (i)
                {
                    case 0:
                        this.RefVersion = version; 
                        break;
                    case 1:
                        this.LastCalculatedVersion = version;
                        break;
                    case 2:
                        this.ProjectFileVersion = version;
                        break;
                }
            }
        }
    }
}
