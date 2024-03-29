﻿using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Properties
{
    class PropertyDiffItem : BaseMemberDiffItem<PropertyDefinition>
    {
        public PropertyDiffItem(PropertyDefinition oldProperty, PropertyDefinition newProperty, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldProperty, newProperty, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => MetadataType.Property;
    }
}
