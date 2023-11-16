using Oleander.Assembly.Comparator;

namespace Oleander.Assembly.Comparer.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

            var oldAssembly = new FileInfo(@"D:\dev\git\oleander\AssemblyVersioning\src\Oleander.Assembly.Versioning\bin\Debug\Oleander.Assembly.Versioning.dll");
            var newAssembly = new FileInfo(@"D:\dev\git\oleander\AssemblyVersioning\src\Oleander.Assembly.Versioning\bin\Debug\net7.0\Oleander.Assembly.Versioning.dll");
          

            Assert.Equal(VersionChange.Minor, new AssemblyComparison(oldAssembly, newAssembly).VersionChange);
            Assert.Equal(VersionChange.Major, new AssemblyComparison(newAssembly, oldAssembly).VersionChange);

        }
    }
}