using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Methods
{
    class MethodDiffItem : BaseMemberDiffItem<MethodDefinition>
    {
        public MethodDiffItem(MethodDefinition oldMethod, MethodDefinition newMethod, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldMethod, newMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Method; }
        }
    }
}
