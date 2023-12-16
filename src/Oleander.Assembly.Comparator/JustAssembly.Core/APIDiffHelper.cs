using JustAssembly.Core.Comparers;
using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;

namespace JustAssembly.Core
{
    public static class APIDiffHelper
    {
        public static IMetadataDiffItem GetAPIDifferences(string oldAssemblyPath, string newAssemblyPath)
        {
            if (oldAssemblyPath == null || newAssemblyPath == null)
            {
                return null;
            }

            //var resolver = new DefaultAssemblyResolver(new AssemblyPathResolverCache());



            AssemblyDefinition oldAssembly = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(oldAssemblyPath);
            AssemblyDefinition newAssembly = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(newAssemblyPath);




            if (oldAssembly == null || newAssembly == null)
            {
                return null;
            }

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
