using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class NuGetTests
{
    [Fact]
    public async void TestDownload()
    {
        await NuGetHelper.DownloadPackageAsync("Newtonsoft.Json");
        await NuGetHelper.DownloadPackageAsync("Oleander.Extensions.Logging.Abstractions");

    }
}