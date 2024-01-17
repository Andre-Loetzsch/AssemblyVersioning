using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Modules;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class ModuleComparer : BaseDiffComparer<ModuleDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(ModuleDefinition element)
        {
            return new ModuleDiffItem(element, null, null, null);
        }

        protected override IDiffItem GenerateDiffItem(ModuleDefinition oldElement, ModuleDefinition newElement)
        {
            var declarationDiffs = EnumerableExtensions.ConcatAll(
                GetCustomAttributeDiffs(oldElement, newElement), GetReferenceDiffs(oldElement, newElement)).ToList();

            var childrenDiffs = GetTypeDiffs(oldElement, newElement).ToList();

            if (declarationDiffs.IsEmpty() && childrenDiffs.IsEmpty()) return null;

            return new ModuleDiffItem(oldElement, newElement, declarationDiffs, childrenDiffs.Cast<IMetadataDiffItem<TypeDefinition>>());
        }

        private static IEnumerable<IDiffItem> GetCustomAttributeDiffs(ModuleDefinition oldModule, ModuleDefinition newModule)
        {
            return new CustomAttributeComparer().GetMultipleDifferences(oldModule.CustomAttributes, newModule.CustomAttributes);
        }

        private static IEnumerable<IDiffItem> GetReferenceDiffs(ModuleDefinition oldModule, ModuleDefinition newModule)
        {
            return new ReferenceComparer().GetMultipleDifferences(oldModule.AssemblyReferences, newModule.AssemblyReferences);
        }

        private static IEnumerable<IDiffItem> GetTypeDiffs(ModuleDefinition oldModule, ModuleDefinition newModule)
        {
            return new TypeComparer().GetMultipleDifferences(oldModule.Types, newModule.Types);
        }

        protected override IDiffItem GetNewDiffItem(ModuleDefinition element)
        {
            return new ModuleDiffItem(null, element, null, null);
        }

        protected override int CompareElements(ModuleDefinition x, ModuleDefinition y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(ModuleDefinition element)
        {
            if (element.Types.All(type => !type.IsPublic)) return false;

            return APIDiffHelper.InternalApiIgnore == null ||
                   APIDiffHelper.InternalApiIgnore($"{nameof(ModuleDefinition)}:{element.Name}");
        }
    }
}
