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
            IEnumerable<IDiffItem> declarationDiffs = EnumerableExtensions.ConcatAll(
                    new CustomAttributeComparer().GetMultipleDifferences(oldElement.CustomAttributes, newElement.CustomAttributes),
                    this.GetReturnTypeDifference(oldElement, newElement));
            
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
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(PropertyDefinition element)
        {
            return element.GetMethod != null && element.GetMethod.IsAPIDefinition() ||
                element.SetMethod != null && element.SetMethod.IsAPIDefinition();

        }

        protected override bool IsIgnored(PropertyDefinition element)
        {
            return APIDiffHelper.InternalApiIgnore != null &&
                   APIDiffHelper.InternalApiIgnore($"Property:{element.DeclaringType.FullName}.{element.Name}");
        }
    }
}
