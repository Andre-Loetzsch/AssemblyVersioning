using Oleander.Assembly.Versioning.NuGet;
using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class NuGetTests
{
    [Fact]
    public async Task TestDownload()
    {
        var packageId = "Oleander.Assembly.Versioning.Tool";//"Newtonsoft.Json";
        var outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages");
        using var nuGetDownLoader1 = new NuGetDownLoader(new NuGetLogger(new NullLogger()), "Oleander.Assembly.Versioning.Tool.dll");
        var sources = NuGetDownLoader.GetNuGetConfigSources();
        var versions = await nuGetDownLoader1.GetAllVersionsAsync(sources, packageId, CancellationToken.None);

        if (!versions.Any()) return;

        var (source, version) = versions.First(x => x.Item2.Version == versions.Max(x1 => x1.Item2.Version));
        Assert.True(await nuGetDownLoader1.DownloadPackageAsync(source, packageId, version, outDir, CancellationToken.None));

        packageId = "Oleander.Extensions.Logging.Abstractions";
        using var nuGetDownLoader2 = new NuGetDownLoader(new NuGetLogger(new NullLogger()), "Oleander.Extensions.Logging.Abstractions.dll");
        versions = await nuGetDownLoader2.GetAllVersionsAsync(sources, packageId, CancellationToken.None);

        if (!versions.Any()) return;

        (source, version) = versions.First(x => x.Item2.Version == versions.Max(x1 => x1.Item2.Version));
        Assert.True(await nuGetDownLoader2.DownloadPackageAsync(source, packageId, version, outDir, CancellationToken.None));
    }
}