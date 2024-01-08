using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Properties
{
    class PropertyDiffItem : BaseMemberDiffItem<PropertyDefinition>
    {
        public PropertyDiffItem(PropertyDefinition oldProperty, PropertyDefinition newProperty, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldProperty, newProperty, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Property; }
        }
    }
}
