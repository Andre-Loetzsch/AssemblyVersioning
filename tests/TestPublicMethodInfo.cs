using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class TestPublicMethodInfo
{
    [Fact]
    public void AddNewPublicMethod()
    {

        Helper.CopyAndBuildProject("v1.0.0.0", "122ce329", new []{""});
        Helper.CopyAndBuildProject("v1.0.1.0", "122ce486", new[] { "Class1.sc" });


    }



}