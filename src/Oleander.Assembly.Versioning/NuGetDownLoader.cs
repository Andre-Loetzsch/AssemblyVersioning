using System.IO.Compression;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;

namespace Oleander.Assembly.Versioning;

public class NuGetDownLoader
{
    public static async Task<bool> DownloadPackageAsync(string packageId, string outDir)
    {
        return await DownloadPackageAsync(packageId, outDir, "https://api.nuget.org/v3/index.json");
    }

    public static async Task<bool> DownloadPackageAsync(string packageId, string outDir, string packageSource)
    {
        var logger = NuGet.Common.NullLogger.Instance;
        var cancellationToken = CancellationToken.None;
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3(packageSource);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var versions = (await resource.GetAllVersionsAsync(packageId, cache, logger, cancellationToken)).ToList();
        var latestPackageVersion = versions.FirstOrDefault(x => x.Version == versions.Max(x1 => x1.Version));

        if (latestPackageVersion == null) return false;

        var packageVersion = new NuGetVersion(latestPackageVersion);

        using var packageStream = new MemoryStream();
        await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, cache, logger, cancellationToken);

        UnZipAssemblies(packageStream, outDir);

        return true;
    }


    private static void UnZipAssemblies(Stream packageStream, string outDir)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, false);

        foreach (var zipEntry in archive.Entries)
        {
            var fileExtension = Path.GetExtension(zipEntry.FullName).ToLower();
            if (fileExtension != ".dll" && fileExtension != ".exe") continue;

            using var stream = zipEntry.Open();
            var ms = new MemoryStream();

            stream.CopyTo(ms);

            try
            {
                var buffer = ms.ToArray();
                var assembly = SysAssembly.Load(buffer);
                var pathItemsList = new List<string>();
                var assemblyInfo = new AssemblyFrameworkInfo(assembly);
                var shortFolderName = assemblyInfo.ShortFolderName;

                if (shortFolderName != null) pathItemsList.Add(shortFolderName);
                if (assemblyInfo.TargetPlatform != null) pathItemsList.Add(assemblyInfo.TargetPlatform);

                if (!pathItemsList.Any())
                {
                    var zipEntryPathItems = zipEntry.FullName.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                    if (zipEntryPathItems.Length > 1)
                    {
                        var nugetFramework = NuGetFramework.ParseFrameworkName(zipEntryPathItems[zipEntryPathItems.Length - 2], new DefaultFrameworkNameProvider());

                        pathItemsList.Add(nugetFramework.Framework);

                        if (nugetFramework.HasPlatform)
                        {
                            pathItemsList.Add(nugetFramework.Platform);
                        }
                    }
                }

                pathItemsList.Insert(0, outDir);
                var libDir = Path.Combine(pathItemsList.ToArray());

                if (!Directory.Exists(libDir)) Directory.CreateDirectory(libDir);
                var path = Path.Combine(libDir, zipEntry.Name);

                File.WriteAllBytes(path, buffer);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}