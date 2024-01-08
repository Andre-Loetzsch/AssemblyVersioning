using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Fields
{
    class FieldDiffItem : BaseMemberDiffItem<FieldDefinition>
    {
        public FieldDiffItem(FieldDefinition oldField, FieldDefinition newField, IEnumerable<IDiffItem> declarationDiffs) 
            : base(oldField, newField, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Field; }
        }
    }
}
