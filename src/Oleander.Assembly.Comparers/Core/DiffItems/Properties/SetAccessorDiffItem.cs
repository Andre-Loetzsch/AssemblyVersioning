using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Properties
{
    class SetAccessorDiffItem : BaseDiffItem<MethodDefinition>
    {
        public SetAccessorDiffItem(PropertyDefinition oldProperty, PropertyDefinition newProperty, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldProperty.SetMethod, newProperty.SetMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Method; }
        }

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "set";
        }
    }
}
