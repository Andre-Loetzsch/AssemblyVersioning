using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class TestPublicMethodInfo
{
    [Fact]
    public void AddNewPublicMethod()
    {

        Helper.CopyAndBuildProject("v1.0.0.0");

    }


        
}