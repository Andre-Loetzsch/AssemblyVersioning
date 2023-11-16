using System.Reflection;
using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

public class CreateRefInfoTests
{
    [Fact]
    public void TestCreateRefInfoEnums()
    {
        var result = Versioning.CreateRefInfo(typeof(BindingFlags));



    }
}