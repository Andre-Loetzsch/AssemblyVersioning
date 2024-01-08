using Oleander.Assembly.Comparers.Core;

namespace JustAssembly.Core.DiffItems.Enums
{
    class EnumFieldDiffItem : BaseDiffItem<EnumFieldDefinition>
    {
        public EnumFieldDiffItem(EnumFieldDefinition oldEnum, EnumFieldDefinition newEnum, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldEnum, newEnum, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => MetadataType.Field;


        protected override string GetElementShortName(EnumFieldDefinition enumDef)
        {
            return $"{enumDef.Name}={enumDef.Value} ({enumDef.FieldTypeName})";
        }
    }
}