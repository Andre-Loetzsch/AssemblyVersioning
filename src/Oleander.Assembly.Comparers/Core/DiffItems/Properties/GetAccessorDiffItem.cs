using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Properties
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
