using System.Reflection;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

public class CreateRefInfoTests
{
    [Fact]
    public void TestCreateRefInfoEnums()
    {
        var result = Versioning.CreateRefInfo(typeof(BindingFlags));



    }
}