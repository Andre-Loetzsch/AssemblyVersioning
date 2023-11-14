using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class CalculateAlphaVersionTests
{
    [Fact]
    public void TestAlphaVersionIncreaseRevision()
    {
        var expectedVersion = new Version(0, 0, 0, 1);
        var currentVersion = new Version(0, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));

        expectedVersion = new(0, 0, 1, 1);
        currentVersion = new(0, 0, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));
    }

    [Fact]
    public void TestAlphaVersionIncreaseBuild()
    {
        var expectedVersion = new Version(0, 0, 1, 0);
        var currentVersion = new Version(0, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));

        expectedVersion = new Version(0, 0, 2, 0);
        currentVersion = new Version(0, 0, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));
    }

    [Fact]
    public void TestAlphaVersionIncreaseMinor()
    {
        var expectedVersion = new Version(0, 0, 1, 0);
        var currentVersion = new Version(0, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));

        expectedVersion = new Version(0, 0, 2, 0);
        currentVersion = new Version(0, 0, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));
    }

    [Fact]
    public void TestAlphaVersionIncreaseMajor()
    {
        var expectedVersion = new Version(0, 0, 1, 0);
        var currentVersion = new Version(0, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));

        expectedVersion = new Version(0, 0, 2, 0);
        currentVersion = new Version(0, 0, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));
    }

}