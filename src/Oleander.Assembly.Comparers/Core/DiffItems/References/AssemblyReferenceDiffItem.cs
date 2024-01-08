using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.References
{
    class AssemblyReferenceDiffItem : BaseDiffItem<AssemblyNameReference>
    {
        public AssemblyReferenceDiffItem(AssemblyNameReference oldReference, AssemblyNameReference newReference, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldReference, newReference, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.AssemblyReference; }
        }

        protected override string GetElementShortName(AssemblyNameReference element)
        {
            return element.FullName;
        }

        public override bool IsBreakingChange => false;
    }
}
