using Xunit;

namespace Oleander.AssemblyVersioning.Test;

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



}