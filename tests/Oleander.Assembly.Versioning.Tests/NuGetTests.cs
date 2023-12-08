using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class NuGetTests
{
    [Fact]
    public async void TestDownload()
    {
        await NuGetDownLoader.DownloadPackageAsync("Newtonsoft.Json", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages"));
        await NuGetDownLoader.DownloadPackageAsync("Oleander.Extensions.Logging.Abstractions", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "packages"));

    }
}