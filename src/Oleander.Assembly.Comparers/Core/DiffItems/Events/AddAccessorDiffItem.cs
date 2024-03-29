﻿using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Events
{
    class AddAccessorDiffItem : BaseMemberDiffItem<MethodDefinition>
    {
        public AddAccessorDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldEvent.AddMethod, newEvent.AddMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType => MetadataType.Method;

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "add";
        }
    }
}
