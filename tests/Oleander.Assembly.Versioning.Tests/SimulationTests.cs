using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class SimulationTests
{
    [Fact]
    public void TestAddPrivateMethod()
    {
        var result = new TestRunner("addPrivateMethod").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddPublicMethod()
    {
        var result = new TestRunner("addPublicMethod").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestGitChangesBuild()
    {
        var result = new TestRunner("gitChangesBuild").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 0, 1, 0), result[1]);
    }

    [Fact]
    public void TestGitChangesRevision()
    {
        var result = new TestRunner("gitChangesRevision").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 0, 0, 1), result[1]);
    }

    [Fact]
    public void TestAddInterface()
    {
        var result = new TestRunner("addInterface").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestModifyInterface()
    {
        var result = new TestRunner("modifyInterface").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestRemovePublicMethod()
    {
        var result = new TestRunner("removePublicMethod").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestChangeNamespace()
    {
        var result = new TestRunner("changeNamespace").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddParameterToPublicMethod()
    {
        var result = new TestRunner("addParameterToPublicMethod").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddAndRemovePublicMethod()
    {
        var result = new TestRunner("addAndRemovePublicMethod").RunSimulation().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(2, 0, 0, 0), result[2]);
    }

    [Fact]
    public void TestAddAndRemovePublicMethodWithoutCommit()
    {
        var result = new TestRunner("addAndRemovePublicMethodWithoutCommit").RunSimulation().ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(1, 0, 0, 0), result[2]);
    }

    [Fact]
    public void TestAddAnInterfaceToAClass()
    {
        var result = new TestRunner("addInterfaceToClass").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestRemoveInterfaceFromClass()
    {
        var result = new TestRunner("removeInterfaceFromClass").RunSimulation().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestModifyEnum()
    {
        var result = new TestRunner("modifyEnum").RunSimulation().ToList();

        Assert.Equal(5, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(2, 0, 0, 0), result[2]);
        Assert.Equal(new(2, 1, 0, 0), result[3]);
        Assert.Equal(new(3, 0, 0, 0), result[4]);
    }
}