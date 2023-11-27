using Mono.Cecil;
using JustAssembly.Core.Extensions;
using JustAssembly.Core.DiffItems.Assemblies;

namespace JustAssembly.Core.Comparers
{
    internal class AssemblyComparer(AssemblyDefinition oldAssembly, AssemblyDefinition newAssembly)
    {
        public IMetadataDiffItem<AssemblyDefinition> Compare()
        {
            var declarationDiffs = new CustomAttributeComparer().GetMultipleDifferences(oldAssembly.CustomAttributes, newAssembly.CustomAttributes).ToList();
            var childrenDiffs = new ModuleComparer().GetMultipleDifferences(oldAssembly.Modules, newAssembly.Modules).ToList();
            
            if (declarationDiffs.IsEmpty() && childrenDiffs.IsEmpty())
            {
                return null;
            }
            return new AssemblyDiffItem(oldAssembly, newAssembly, declarationDiffs, childrenDiffs.Cast<IMetadataDiffItem<ModuleDefinition>>());
        }
    }
}
