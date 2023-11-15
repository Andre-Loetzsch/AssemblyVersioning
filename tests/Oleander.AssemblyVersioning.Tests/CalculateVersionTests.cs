using Oleander.Assembly.Versioning;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class CalculateVersionTests
{
    [Fact]
    public void TestVersionIncreaseRevision()
    {
        var expectedVersion = new Version(1, 0, 0, 1);
        var currentVersion = new Version(1, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));

        expectedVersion = new(1, 0, 0, 2);
        currentVersion = new(1, 0, 0, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));

        expectedVersion = new(1, 1, 1, 2);
        currentVersion = new(1, 1, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, false, true));
    }

    [Fact]
    public void TestVersionIncreaseBuild()
    {
        var expectedVersion = new Version(1, 0, 1, 0);
        var currentVersion = new Version(1, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));

        expectedVersion = new Version(1, 0, 2, 0);
        currentVersion = new Version(1, 0, 1, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));

        expectedVersion = new Version(1, 0, 2, 0);
        currentVersion = new Version(1, 0, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, false, true, true));
    }

    [Fact]
    public void TestVersionIncreaseMinor()
    {
        var expectedVersion = new Version(1, 1, 0, 0);
        var currentVersion = new Version(1, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));

        expectedVersion = new Version(1, 3, 0, 0);
        currentVersion = new Version(1, 2, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));

        expectedVersion = new Version(1, 3, 0, 0);
        currentVersion = new Version(1, 2, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, false, true, true, true));
    }

    [Fact]
    public void TestVersionIncreaseMajor()
    {
        var expectedVersion = new Version(2, 0, 0, 0);
        var currentVersion = new Version(1, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));

        expectedVersion = new Version(3, 0, 0, 0);
        currentVersion = new Version(2, 0, 0, 0);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));

        expectedVersion = new Version(3, 0, 0, 0);
        currentVersion = new Version(2, 1, 1, 1);

        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, false, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, false, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, false));
        Assert.Equal(expectedVersion, Versioning.CalculateVersion(currentVersion, true, true, true, true));
    }

}