using Newtonsoft.Json.Linq;
using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class AssemblyInfoTests
{
    [Fact]
    public void TestAssemblyInfoFileAttributeValue()
    {
        var runner = new TestRunner("readAssemblyInfo");
        var project = runner.CreateMSBuildProject();

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("AssemblyVersion", out var value));
        Assert.Equal("4.8.23271.11207", value);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("AssemblyFileVersion", out value));
        Assert.True(project.TrySetAssemblyInfoFileAttributeValue("AssemblyFileVersion", "4.8.23271.11207"));
        Assert.Equal("4.8.23271.0", value);


        Assert.False(project.TryGetAssemblyInfoFileAttributeValue("InformationalVersion", out _));
        Assert.True(project.TrySetAssemblyInfoFileAttributeValue("InformationalVersion", "4.8.23271.11207+5bacf07eccc6ec731abfea0e6fb758160e844333"));

        project.SaveChanges();

        project = new MSBuildProject(project.ProjectFileName);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("AssemblyFileVersion", out value));
        Assert.Equal("4.8.23271.11207", value);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("InformationalVersion", out value));
        Assert.Equal("4.8.23271.11207+5bacf07eccc6ec731abfea0e6fb758160e844333", value);
    }

    [Fact]
    public void TestUpdateAssemblyInfoFile()
    {
        var runner = new TestRunner("readAssemblyInfo");
        var project = runner.CreateMSBuildProject();

        project.AssemblyVersion = "4.2.345.1";
        project.VersionSuffix = "dev";
        project.SourceRevisionId = "5bacf07eccc6ec731abfea0e6fb758160e844123";

        project.SaveChanges();

        project = new MSBuildProject(project.ProjectFileName);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("AssemblyVersion", out var value));
        Assert.Equal("4.2.345.1", value);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("AssemblyFileVersion", out value));
        Assert.Equal("4.2.345.1", value);

        Assert.True(project.TryGetAssemblyInfoFileAttributeValue("InformationalVersion", out value));
        Assert.Equal("4.2.345.1+dev+5bacf07eccc6ec731abfea0e6fb758160e844123", value);
    }
}