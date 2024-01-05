using System.IO.Compression;
using Microsoft.Extensions.Logging;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;
using NuGet.Configuration;

namespace Oleander.Assembly.Versioning.NuGet;

internal class NuGetDownLoader(NuGetLogger logger, string targetName) : IDisposable
{
    private readonly SourceCacheContext _sourceCacheContext = new() { IgnoreFailedSources = true };

    public async Task<IReadOnlyList<Tuple<SourceRepository, NuGetVersion>>> GetAllVersionsAsync(IEnumerable<SourceRepository> sources, string packageId, CancellationToken cancellationToken)
    {
        var result = new List<Tuple<SourceRepository, NuGetVersion>>();

        foreach (var source in sources)
        {
            try
            {
                logger.LogInformation("Get version: PackageId={packageId}, Source name={packageName}, Source={packageSource}",
                     packageId, source.PackageSource.Name, source.PackageSource.Source);

                var resource = await source.GetResourceAsync<MetadataResource>(cancellationToken);
                var versions = await resource.GetVersions(packageId, true, false, this._sourceCacheContext, logger, cancellationToken);

                foreach (var version in versions)
                {
                    if (result.Any(x => x.Item2.Version == version.Version)) continue;
                    result.Add(new(source, version));
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Get versions failed! ({type} : {message}) PackageId={packageId}, Source name={packageName}, Source={packageSource}",
                    ex.GetType(), ex.Message, packageId, source.PackageSource.Name, source.PackageSource.Source);
            }
        }

        return result.ToArray();
    }

    public IReadOnlyList<SourceRepository> GetNuGetConfigSources()
    {
        var settings = Settings.LoadDefaultSettings(root: null);
        var sourceRepositoryProvider = new SourceRepositoryProvider(
            new PackageSourceProvider(settings), Repository.Provider.GetCoreV3());

        return sourceRepositoryProvider.GetRepositories()
            .Where(x => x.PackageSource.IsEnabled).ToArray();
    }

    public async Task<bool> DownloadPackageAsync(SourceRepository source, string packageId, NuGetVersion packageVersion, string outDir, CancellationToken cancellationToken)
    {
        logger.LogInformation("Download package: PackageId={packageId}, Source name={packageName}, Source={packageSource}, Version={version}",
            packageId, source.PackageSource.Name, source.PackageSource.Source, packageVersion.Version);

        var resource = await source.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        using var packageStream = new MemoryStream();
        await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, this._sourceCacheContext, logger, cancellationToken);

        return this.UnZipAssemblies(packageStream, outDir);
    }

    private bool UnZipAssemblies(Stream packageStream, string outDir)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, false);
        var filteredZipEntries = archive.Entries
            .Where(x => string.Equals(x.Name, targetName, StringComparison.OrdinalIgnoreCase)).ToList();

        if (!filteredZipEntries.Any())
        {
            logger.LogWarning("Package does not contain any content with target name '{targetName}'!", targetName);
            return false;
        }

        foreach (var zipEntry in filteredZipEntries)
        {
            using var stream = zipEntry.Open();
            var ms = new MemoryStream();

            stream.CopyTo(ms);

            try
            {
                var tempFilename = Path.GetTempFileName();

                File.WriteAllBytes(tempFilename, ms.ToArray());

                var pathItemsList = new List<string>();
                var assemblyInfo = new AssemblyFrameworkInfo(tempFilename);
                var shortFolderName = assemblyInfo.FrameworkShortFolderName;

                if (shortFolderName != null) pathItemsList.Add(shortFolderName);

                pathItemsList.Add(string.IsNullOrEmpty(assemblyInfo.TargetPlatform)
                    ? "any"
                    : assemblyInfo.TargetPlatform);

                if (!pathItemsList.Any())
                {
                    var zipEntryPathItems = zipEntry.FullName.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                    if (zipEntryPathItems.Length > 1)
                    {
                        // ReSharper disable once UseIndexFromEndExpression
                        var nugetFramework = NuGetFramework.ParseFrameworkName(zipEntryPathItems[zipEntryPathItems.Length - 2], new DefaultFrameworkNameProvider());

                        logger.LogInformation("NuGet framework '{nugetFramework}' parsed from zip full name.", nugetFramework.Framework);

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

                this.MoveTempFileToTarget(tempFilename, path, zipEntry.Name);
            }
            catch (Exception ex)
            {
                logger.LogError("Unzip assemblies failed! ({type} : {message})", ex.GetType(), ex.Message);
                throw;
            }
        }

        return true;
    }

    private void MoveTempFileToTarget(string tempFilename, string targetPath, string zipEntryName)
    {
        var fileInfo = new FileInfo(targetPath);

        if (fileInfo.Exists)
        {
            if ((DateTime.Now - fileInfo.CreationTime).TotalSeconds > 10)
            {
                File.Delete(targetPath);
                File.Move(tempFilename, targetPath);
                logger.LogInformation("Unzip '{zipEntryName}' entry to override file '{path}'.", zipEntryName, targetPath);
            }
            else
            {
                logger.LogInformation("Skip unzipping the file '{path}' because the file is up-to-date.", targetPath);
            }
        }
        else
        {
            try
            {
                File.Move(tempFilename, targetPath);
                logger.LogInformation("Unzip '{zipEntryName}' entry to file '{path}'.", zipEntryName, targetPath);
            }
            catch (IOException)
            {
                fileInfo = new(targetPath);

                if (!fileInfo.Exists) throw;
                logger.LogInformation("Skip unzipping the file '{path}' because the file is up-to-date.", targetPath);
            }
        }
    }


    public void Dispose()
    {
        this._sourceCacheContext.Dispose();
        GC.SuppressFinalize(this);
    }

    ~NuGetDownLoader()
    {
        this._sourceCacheContext.Dispose();
    }
}