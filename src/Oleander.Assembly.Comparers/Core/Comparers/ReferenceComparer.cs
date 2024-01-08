using Mono.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.References;

namespace Oleander.Assembly.Comparers.Core.Comparers
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
        }

        protected override int CompareElements(AssemblyNameReference x, AssemblyNameReference y)
        {
            //return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);
            var result = string.Compare(x.Name, y.Name, StringComparison.Ordinal);
            if (result != 0) return result; 

            if (x.Version.Major > y.Version.Major) return 1;
            if (x.Version.Major < y.Version.Major) return -1;

            return 0;
        }

        protected override bool IsAPIElement(AssemblyNameReference element)
        {
            return true;
        }


    }
}
