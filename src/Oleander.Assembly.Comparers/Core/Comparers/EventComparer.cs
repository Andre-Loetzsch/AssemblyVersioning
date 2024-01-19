﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.Comparers.Accessors;
using Oleander.Assembly.Comparers.Core.DiffItems.Events;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class EventComparer : BaseDiffComparer<EventDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(EventDefinition element)
        {
            return new EventDiffItem(element, null, null, null);
        }

        protected override IDiffItem GenerateDiffItem(EventDefinition oldElement, EventDefinition newElement)
        {
            IEnumerable<IDiffItem> declarationDiffs = new CustomAttributeComparer().GetMultipleDifferences(oldElement.CustomAttributes, newElement.CustomAttributes);
            IEnumerable<IMetadataDiffItem<MethodDefinition>> childrenDiffs = this.GenerateAccessorDifferences(oldElement, newElement);

            // TODO compare event args!
            if (declarationDiffs.IsEmpty() && childrenDiffs.IsEmpty())
            {
                return null;
            }

            return new EventDiffItem(oldElement, newElement, declarationDiffs, childrenDiffs);
        }

        private IEnumerable<IMetadataDiffItem<MethodDefinition>> GenerateAccessorDifferences(EventDefinition oldEvent, EventDefinition newEvent)
        {
            List<IMetadataDiffItem<MethodDefinition>> result = new List<IMetadataDiffItem<MethodDefinition>>(2);

            IMetadataDiffItem<MethodDefinition> addAccessorDiffItem = new AddAccessorComparer(oldEvent, newEvent).GenerateAccessorDiffItem();
            if (addAccessorDiffItem != null)
            {
                result.Add(addAccessorDiffItem);
            }

            IMetadataDiffItem<MethodDefinition> removeAccessorDiffItem = new RemoveAccessorComparer(oldEvent, newEvent).GenerateAccessorDiffItem();
            if(removeAccessorDiffItem != null)
            {
                result.Add(removeAccessorDiffItem);
            }

            return result;
        }

        protected override IDiffItem GetNewDiffItem(EventDefinition element)
        {
            return new EventDiffItem(null, element, null, null);
        }

        protected override int CompareElements(EventDefinition x, EventDefinition y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(EventDefinition element)
        {
            return element.AddMethod != null && element.AddMethod.IsAPIDefinition() ||
                element.RemoveMethod != null && element.RemoveMethod.IsAPIDefinition();
           
        }

        protected override bool IsIgnored(EventDefinition element)
        {
            return APIDiffHelper.InternalApiIgnore != null && 
                   APIDiffHelper.InternalApiIgnore($"Event:{element.DeclaringType.FullName}.{element.Name}");
        }
    }
}
