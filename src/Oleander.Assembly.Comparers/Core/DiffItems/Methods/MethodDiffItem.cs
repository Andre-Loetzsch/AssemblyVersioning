﻿using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Methods
{
    class MethodDiffItem : BaseMemberDiffItem<MethodDefinition>
    {
        public MethodDiffItem(MethodDefinition oldMethod, MethodDefinition newMethod, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldMethod, newMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType => MetadataType.Method;
    }
}
