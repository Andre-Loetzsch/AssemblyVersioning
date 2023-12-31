﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Properties;

namespace Oleander.Assembly.Comparers.Core.Comparers.Accessors
{
    class SetAccessorComparer : BaseAccessorComparer<PropertyDefinition>
    {
        public SetAccessorComparer(PropertyDefinition oldProperty, PropertyDefinition newProperty)
            : base(oldProperty, newProperty)
        {
        }

        protected override MethodDefinition SelectAccessor(PropertyDefinition element)
        {
            return element.SetMethod;
        }

        protected override IMetadataDiffItem<MethodDefinition> CreateAccessorDiffItem(IEnumerable<IDiffItem> declarationDiffs)
        {
            return new SetAccessorDiffItem(this.oldElement, this.newElement, declarationDiffs);
        }
    }
}
