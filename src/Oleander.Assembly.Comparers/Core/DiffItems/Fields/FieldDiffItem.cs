using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Fields
{
    class FieldDiffItem : BaseMemberDiffItem<FieldDefinition>
    {
        public FieldDiffItem(FieldDefinition oldField, FieldDefinition newField, IEnumerable<IDiffItem> declarationDiffs) 
            : base(oldField, newField, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType => MetadataType.Field;
    }
}
