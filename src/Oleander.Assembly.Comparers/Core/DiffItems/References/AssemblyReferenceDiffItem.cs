using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.References
{
    class AssemblyReferenceDiffItem : BaseDiffItem<AssemblyNameReference>
    {
        public AssemblyReferenceDiffItem(AssemblyNameReference oldReference, AssemblyNameReference newReference, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldReference, newReference, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType => MetadataType.AssemblyReference;

        protected override string GetElementShortName(AssemblyNameReference element)
        {
            return element.FullName;
        }

        public override bool IsBreakingChange => false;
    }
}
