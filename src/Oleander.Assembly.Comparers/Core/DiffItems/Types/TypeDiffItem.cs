using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Types
{
    class TypeDiffItem : BaseDiffItem<TypeDefinition>
    {
        public TypeDiffItem(TypeDefinition oldType, TypeDefinition newType, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldType, newType, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => MetadataType.Type;

        public override bool IsBreakingChange
        {
            get
            {
                if (base.ChildrenDiffs.Any() &&
                    this.OldElement != null && this.OldElement.IsInterface &&
                    this.NewElement != null && this.NewElement.IsInterface) return true;

                return base.IsBreakingChange;
            }
        }

        protected override string GetElementShortName(TypeDefinition typeDef)
        {
            return typeDef.FullName;
        }
    }
}
