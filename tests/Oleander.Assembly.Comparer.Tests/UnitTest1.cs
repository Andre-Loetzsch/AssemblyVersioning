using Oleander.Assembly.Comparator;

namespace Oleander.Assembly.Comparer.Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Test1()
        {

            var oldAssembly = new FileInfo(@"C:\dev\git\oleander\AssemblyVersioning\lib\JustAssembly.Core.dll");
            var newAssembly = new FileInfo(@"C:\dev\git\oleander\AssemblyVersioning\src\JustAssembly.Core\bin\Debug\JustAssembly.Core.dll");
          

            Assert.Equal(VersionChange.Minor, new AssemblyComparison(oldAssembly, newAssembly).VersionChange);
            Assert.Equal(VersionChange.Major, new AssemblyComparison(newAssembly, oldAssembly).VersionChange);

        }
    }
}