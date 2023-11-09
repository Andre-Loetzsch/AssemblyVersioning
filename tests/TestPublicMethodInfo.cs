using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class TestPublicMethodInfo
{
    [Fact]
    public void AddNewPublicMethod()
    {



        var result = TestRunner.RunSimulation("simulation1").ToList();
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