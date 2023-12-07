//using NuGet.Common;
//using NuGet.Configuration;
//using NuGet.Packaging;
//using NuGet.Packaging.Core;
//using NuGet.Protocol;
//using NuGet.Protocol.Core.Types;
//using NuGet.Versioning;

using System.IO.Compression;
using System.Xml.Linq;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using NuGet.Packaging;
using NuGet.Packaging.Signing;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging.Core;


namespace Oleander.Assembly.Versioning;

public class NuGetHelper
{
    public static async Task DownloadPackageAsync(string packageId)
    {
        #region DownloadPackage
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

        using var packageReader = new PackageArchiveReader(packageStream);

        var packageFileName = $"{packageId}.{latestPackageVersion}.nupkg";
        var packageDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages");

        if (!Directory.Exists(packageDir)) Directory.CreateDirectory(packageDir);

        var packageFilePath = Path.Combine(packageDir, packageFileName);

        File.WriteAllBytes(packageFilePath, packageStream.ToArray());



        var packagePathResolver = new PackagePathResolver(Path.GetFullPath(packageDir));

        var installedPath = packagePathResolver.GetInstalledPath(new PackageIdentity(packageId, packageVersion));





        //var settings = Settings.LoadDefaultSettings(root: null);
        //var packageExtractionContext = new PackageExtractionContext(
        //    PackageSaveMode.Nuspec | PackageSaveMode.Files | PackageSaveMode.Nupkg,
        //    XmlDocFileSaveMode.None, 
        //    ClientPolicyContext.GetClientPolicy(settings, logger),
        //    logger);


        //packageExtractionContext.PackageSaveMode = PackageSaveMode.Files;

        UnZip(Path.Combine(packageDir, Path.GetFileNameWithoutExtension(packageFilePath)), packageFilePath);


        #endregion
    }

    private static void UnZip(string directory, string packageZip)
    {

        if (directory == null)
            throw new ArgumentNullException(nameof(directory));

        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);


        //var packageZip = Directory.EnumerateFiles(directory, "*.nupkg").First();

        XDocument document;

        using (var file = File.OpenRead(packageZip))


        using (var archive = new ZipArchive(file, ZipArchiveMode.Read, false))
        {
            var entry = archive.Entries.First(e => System.IO.Path.GetExtension(e.FullName) == ".nuspec");
            using (var nuspec = entry.Open())
                document = XDocument.Load(nuspec);


            foreach (var zipEntry in archive.Entries)
            {
                var destinationPath = Path.GetFullPath(Path.Combine(directory, zipEntry.FullName));
                var destinationDir = Path.GetDirectoryName(destinationPath);

                if (destinationDir != null && !Directory.Exists(destinationDir)) Directory.CreateDirectory(destinationDir);

                zipEntry.ExtractToFile(destinationPath, true);
            }

        }






        if (document == null)
            throw new Exception($"No nuspec found in {packageZip}");

        var ns = document.Root.Name.Namespace;
        var metadata = document.Root.Element(ns + "metadata");

        if (metadata == null)
            throw new Exception($"No metadata found in nuspec document in {packageZip}");

        var id = metadata.Element(ns + "id").Value.Trim();
        var version = metadata.Element(ns + "version").Value.Trim();
        var path = directory;

        var libFolder = System.IO.Path.Combine(path, "lib");
        var toolsFolder = System.IO.Path.Combine(path, "tools");

        IEnumerable<string> EnumDirs(string dir)
        {
            if (!Directory.Exists(dir)) return Enumerable.Empty<string>();
            return Directory.EnumerateDirectories(dir);
        }

        var FrameworkVersions =
            EnumDirs(libFolder).Concat(EnumDirs(toolsFolder))
                .Select(d => NuGetFramework.ParseFolder(System.IO.Path.GetFileName(d)))
                .ToList();


    }

}