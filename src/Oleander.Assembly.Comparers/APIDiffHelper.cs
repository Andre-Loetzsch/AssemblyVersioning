using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core;
using Oleander.Assembly.Comparers.Core.Comparers;

namespace Oleander.Assembly.Comparers
{
    public static class APIDiffHelper
    {
        public static IMetadataDiffItem GetAPIDifferences(string oldAssemblyPath, string newAssemblyPath)
        {
            if (oldAssemblyPath == null || newAssemblyPath == null) return null;

            var oldAssembly = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(oldAssemblyPath);
            var newAssembly = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(newAssemblyPath);

            if (oldAssembly == null || newAssembly == null) return null;

            return GetAPIDifferences(oldAssembly, newAssembly);
        }

        public static void ClearCache()
        {
            GlobalAssemblyResolver.Instance.ClearCache();
        }

        private static IMetadataDiffItem<AssemblyDefinition> GetAPIDifferences(AssemblyDefinition oldAssembly, AssemblyDefinition newAssembly)
        {
            return new AssemblyComparer(oldAssembly, newAssembly).Compare();
        }
    }
}
