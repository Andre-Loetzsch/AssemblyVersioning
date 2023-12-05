using JustAssembly.Core.DiffItems.References;
using Mono.Cecil;

namespace JustAssembly.Core.Comparers
{
    class ReferenceComparer : BaseDiffComparer<AssemblyNameReference>
    {
        protected override IDiffItem GetMissingDiffItem(AssemblyNameReference element)
        {
            return new AssemblyReferenceDiffItem(element, null, null);
        }

        protected override IDiffItem GenerateDiffItem(AssemblyNameReference oldElement, AssemblyNameReference newElement)
        {
            return null;
        }

        protected override IDiffItem GetNewDiffItem(AssemblyNameReference element)
        {
            return new AssemblyReferenceDiffItem(null, element, null);
            //return null;
        }

        protected override int CompareElements(AssemblyNameReference x, AssemblyNameReference y)
        {
            //return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);

        }

        protected override bool IsAPIElement(AssemblyNameReference element)
        {
            return true;
        }
    }
}
