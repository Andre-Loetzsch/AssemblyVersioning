using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class SimulationTests
{
    [Fact]
    public void TestAddPrivateMethod()
    {
        var result = TestRunner.RunSimulation("addPrivateMethod").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddPublicMethod()
    {
        var result = TestRunner.RunSimulation("addPublicMethod").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestGitChanges()
    {
        var result = TestRunner.RunSimulation("gitChanges").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 0, 1, 0), result[1]);
    }

    [Fact]
    public void TestAddInterface()
    {
        var result = TestRunner.RunSimulation("addInterface").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestModifyInterface()
    {
        var result = TestRunner.RunSimulation("modifyInterface").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestRemovePublicMethod()
    {
        var result = TestRunner.RunSimulation("removePublicMethod").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestChangeNamespace()
    {
        var result = TestRunner.RunSimulation("changeNamespace").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddParameterToPublicMethod()
    {
        var result = TestRunner.RunSimulation("addParameterToPublicMethod").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestAddAndRemovePublicMethod()
    {
        var result = TestRunner.RunSimulation("addAndRemovePublicMethod").ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(2, 0, 0, 0), result[2]);
    }

    [Fact]
    public void TestAddAndRemovePublicMethodWithoutCommit()
    {
        var result = TestRunner.RunSimulation("addAndRemovePublicMethodWithoutCommit").ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(1, 0, 0, 0), result[2]);
    }

    [Fact]
    public void TestAddAnInterfaceToAClass()
    {
        var result = TestRunner.RunSimulation("addInterfaceToClass").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
    }

    [Fact]
    public void TestRemoveInterfaceFromClass()
    {
        var result = TestRunner.RunSimulation("removeInterfaceFromClass").ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(2, 0, 0, 0), result[1]);
    }

    [Fact]
    public void TestModifyEnum()
    {
        var result = TestRunner.RunSimulation("modifyEnum").ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(new(1, 0, 0, 0), result[0]);
        Assert.Equal(new(1, 1, 0, 0), result[1]);
        Assert.Equal(new(2, 0, 0, 0), result[2]);

    }
}