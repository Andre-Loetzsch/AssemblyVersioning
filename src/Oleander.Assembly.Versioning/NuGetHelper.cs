using System.IO.Compression;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;

namespace Oleander.Assembly.Versioning;

public class NuGetHelper
{
    public static async Task DownloadPackageAsync(string packageId)
    {
        var logger = NuGet.Common.NullLogger.Instance;
        var cancellationToken = CancellationToken.None;
        var cache = new SourceCacheContext();
        var repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        var versions = (await resource.GetAllVersionsAsync(packageId, cache, logger, cancellationToken)).ToList();

        var latestPackageVersion = versions.FirstOrDefault(x => x.Version == versions.Max(x1 => x1.Version));

        if (latestPackageVersion == null) return;

        var packageVersion = new NuGetVersion(latestPackageVersion);

        using var packageStream = new MemoryStream();
        await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, cache, logger, cancellationToken);

        UnZipStream(packageStream, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages"));
    }

    private static void UnZipStream(Stream packageStream, string outDir)
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

                var split = Versioning.GetTargetFrameworkPlatformName(assembly).Split(new [] {'-'}, StringSplitOptions.RemoveEmptyEntries).ToList();

                if (!split.Any())
                {
                    var pathItems = zipEntry.FullName.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                    if (pathItems.Length > 1)
                    {
                        var nugetFramework = NuGetFramework.ParseFrameworkName(pathItems[pathItems.Length - 2], new DefaultFrameworkNameProvider());
                        
                        split.Add(nugetFramework.Framework);

                        if (nugetFramework.HasPlatform)
                        {
                            split.Add(nugetFramework.Platform);
                        }
                    }
                }


                split.Insert(0, outDir);
                var libDir = Path.Combine(split.ToArray());

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