using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.Comparers.Accessors;
using Oleander.Assembly.Comparers.Core.DiffItems.Common;
using Oleander.Assembly.Comparers.Core.DiffItems.Properties;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class PropertyComparer : BaseDiffComparer<PropertyDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(PropertyDefinition element)
        {
            return new PropertyDiffItem(element, null, null, null);
        }

        protected override IDiffItem GenerateDiffItem(PropertyDefinition oldElement, PropertyDefinition newElement)
        {
            IEnumerable<IDiffItem> declarationDiffs =
                EnumerableExtensions.ConcatAll(
                    new CustomAttributeComparer().GetMultipleDifferences(oldElement.CustomAttributes, newElement.CustomAttributes),
                    this.GetReturnTypeDifference(oldElement, newElement)
                    );
            IEnumerable<IMetadataDiffItem<MethodDefinition>> childrenDiffs = this.GenerateAccessorDifferences(oldElement, newElement);

            if (declarationDiffs.IsEmpty() && childrenDiffs.IsEmpty())
            {
                return null;
            }

            return new PropertyDiffItem(oldElement, newElement, declarationDiffs, childrenDiffs);
        }

        private IEnumerable<IDiffItem> GetReturnTypeDifference(PropertyDefinition oldProperty, PropertyDefinition newProperty)
        {
            if (oldProperty.PropertyType.FullName != newProperty.PropertyType.FullName)
            {
                yield return new MemberTypeDiffItem(oldProperty, newProperty);
            }
        }

        private IEnumerable<IMetadataDiffItem<MethodDefinition>> GenerateAccessorDifferences(PropertyDefinition oldProperty, PropertyDefinition newProperty)
        {
            List<IMetadataDiffItem<MethodDefinition>> result = new List<IMetadataDiffItem<MethodDefinition>>(2);

            IMetadataDiffItem<MethodDefinition> getAccessorDiffItem = new GetAccessorComparer(oldProperty, newProperty).GenerateAccessorDiffItem();
            if (getAccessorDiffItem != null)
            {
                result.Add(getAccessorDiffItem);
            }

            IMetadataDiffItem<MethodDefinition> setAccessorDiffItem = new SetAccessorComparer(oldProperty, newProperty).GenerateAccessorDiffItem();
            if (setAccessorDiffItem != null)
            {
                result.Add(setAccessorDiffItem);
            }

            return result;
        }

        protected override IDiffItem GetNewDiffItem(PropertyDefinition element)
        {
            return new PropertyDiffItem(null, element, null, null);
        }

        protected override int CompareElements(PropertyDefinition x, PropertyDefinition y)
        {
            return x.Name.CompareTo(y.Name);
        }

        protected override bool IsAPIElement(PropertyDefinition element)
        {
            var isApi = element.GetMethod != null && element.GetMethod.IsAPIDefinition() ||
                element.SetMethod != null && element.SetMethod.IsAPIDefinition();

            if (!isApi) return false;

            element.GetMemberTypeAndName(out _, out var name);

            return APIDiffHelper.InternalApiIgnore == null ||
                   APIDiffHelper.InternalApiIgnore($"{nameof(PropertyDefinition)}:{name}");

        }
    }
}
