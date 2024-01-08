using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Properties
{
    class GetAccessorDiffItem : BaseDiffItem<MethodDefinition>
    {
        public GetAccessorDiffItem(PropertyDefinition oldProperty, PropertyDefinition newProperty, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldProperty.GetMethod, newProperty.GetMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Method; }
        }

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "get";
        }
    }
}
