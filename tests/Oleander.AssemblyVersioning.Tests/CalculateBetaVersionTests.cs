using Oleander.Assembly.Versioning;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class CalculateBetaVersionTests
{
    [Fact]
    public void TestBetaVersionIncreaseRevision()
    {
        var expectedVersion = new Version(0, 1, 0, 1);
        var currentVersion = new Version(0, 1, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));

        expectedVersion = new(0, 1, 0, 1);
        currentVersion = new(0, 1, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));

        expectedVersion = new(0, 1, 1, 1);
        currentVersion = new(0, 1, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));
    }

    [Fact]
    public void TestBetaVersionIncreaseBuild()
    {
        var expectedVersion = new Version(0, 1, 1, 0);
        var currentVersion = new Version(0, 1, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));

        expectedVersion = new Version(0, 1, 2, 0);
        currentVersion = new Version(0, 1, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));

        expectedVersion = new Version(0, 1, 2, 0);
        currentVersion = new Version(0, 1, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));
    }

    [Fact]
    public void TestBetaVersionIncreaseMinor()
    {
        var expectedVersion = new Version(0, 2, 0, 0);
        var currentVersion = new Version(0, 1, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));

        expectedVersion = new Version(0, 3, 0, 0);
        currentVersion = new Version(0, 2, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));

        expectedVersion = new Version(0, 3, 0, 0);
        currentVersion = new Version(0, 2, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));
    }

    [Fact]
    public void TestBetaVersionIncreaseMajor()
    {
        var expectedVersion = new Version(0, 2, 0, 0);
        var currentVersion = new Version(0, 1, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));

        expectedVersion = new Version(0, 3, 0, 0);
        currentVersion = new Version(0, 2, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));

        expectedVersion = new Version(0, 3, 0, 0);
        currentVersion = new Version(0, 2, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));
    }

}