using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Assemblies;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
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
