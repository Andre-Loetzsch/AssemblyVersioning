using System.IO.Compression;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Frameworks;
using NuGet.Configuration;
using NuGet.Common;

namespace Oleander.Assembly.Versioning;

internal class NuGetDownLoader : IDisposable
{
    private readonly INuGetLogger _logger = NullLogger.Instance;
    private readonly SourceCacheContext _sourceCacheContext = new() { IgnoreFailedSources = true };

    public NuGetDownLoader(string targetName)
    {
        this.TargetName = targetName;
    }

    public NuGetDownLoader(string targetName, INuGetLogger logger)
    {
        this._logger = logger;
        this.TargetName = targetName;
    }

    public string TargetName { get; set; }

    public async Task<IReadOnlyList<Tuple<SourceRepository, NuGetVersion>>> GetAllVersionsAsync(IEnumerable<SourceRepository> sources, string packageId, CancellationToken cancellationToken)
    {
        var result = new List<Tuple<SourceRepository, NuGetVersion>>();

        foreach (var source in sources)
        {
            try
            {
                var resource = await source.GetResourceAsync<MetadataResource>(cancellationToken);
                var versions = await resource.GetVersions(packageId, true, false, this._sourceCacheContext, this._logger, cancellationToken);

                foreach (var version in versions)
                {
                    if (result.Any(x => x.Item2.Version == version.Version)) continue;
                    result.Add(new(source, version));
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError($"Get versions failed! {ex.GetType()} : {ex.Message} PackageId={packageId}, Source name={source.PackageSource.Name}, Source={source.PackageSource.Source}");
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
        var resource = await source.GetResourceAsync<FindPackageByIdResource>(cancellationToken);
        using var packageStream = new MemoryStream();
        await resource.CopyNupkgToStreamAsync(packageId, packageVersion, packageStream, this._sourceCacheContext, this._logger, cancellationToken);

        this.UnZipAssemblies(packageStream, outDir);

        return true;
    }

    private void UnZipAssemblies(Stream packageStream, string outDir)
    {
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, false);

        foreach (var zipEntry in archive.Entries
                     .Where(x => string.Equals(x.Name, this.TargetName, StringComparison.OrdinalIgnoreCase)))
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
                        // ReSharper disable once UseIndexFromEndExpression
                        var nugetFramework = NuGetFramework.ParseFrameworkName(zipEntryPathItems[zipEntryPathItems.Length -2], new DefaultFrameworkNameProvider());

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
            catch (Exception ex)
            {
                this._logger.LogError($"{ex.GetType()}: {ex}");
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