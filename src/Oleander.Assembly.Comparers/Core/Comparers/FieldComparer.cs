﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Common;
using Oleander.Assembly.Comparers.Core.DiffItems.Fields;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class FieldComparer : BaseDiffComparer<FieldDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(FieldDefinition element)
        {
            return new FieldDiffItem(element, null, null);
        }

        protected override IDiffItem GenerateDiffItem(FieldDefinition oldElement, FieldDefinition newElement)
        {
            IEnumerable<IDiffItem> attributeDiffs = new CustomAttributeComparer().GetMultipleDifferences(oldElement.CustomAttributes, newElement.CustomAttributes);
            IEnumerable<IDiffItem> fieldTypeDiffs = this.GetFieldTypeDiff(oldElement, newElement);

            IEnumerable<IDiffItem> declarationDiffs =
                EnumerableExtensions.ConcatAll(
                    attributeDiffs,
                    this.CheckVisibility(oldElement, newElement),
                    this.CheckStaticFlag(oldElement, newElement),
                    fieldTypeDiffs
                );

            if(declarationDiffs.IsEmpty())
            {
                return null;
            }
            return new FieldDiffItem(oldElement, newElement, declarationDiffs);
        }

        private IEnumerable<IDiffItem> CheckVisibility(FieldDefinition oldField, FieldDefinition newField)
        {
            int result = VisibilityComparer.CompareVisibilityDefinitions(oldField, newField);
            if (result != 0)
            {
                yield return new VisibilityChangedDiffItem(result < 0);
            }
        }

        private IEnumerable<IDiffItem> CheckStaticFlag(FieldDefinition oldField, FieldDefinition newField)
        {
            if (oldField.IsStatic != newField.IsStatic)
            {
                yield return new StaticFlagChangedDiffItem(newField.IsStatic);
            }
        }

        private IEnumerable<IDiffItem> GetFieldTypeDiff(FieldDefinition oldField, FieldDefinition newField)
        {
            if (oldField.FieldType.FullName != newField.FieldType.FullName)
            {
                yield return new MemberTypeDiffItem(oldField, newField);
            }
        }

        protected override IDiffItem GetNewDiffItem(FieldDefinition element)
        {
            return new FieldDiffItem(null, element, null);
        }

        protected override int CompareElements(FieldDefinition x, FieldDefinition y)
        {
            return string.Compare(x.Name, y.Name, StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(FieldDefinition element)
        {
            return element.IsAPIDefinition();
        }

        protected override bool IsIgnored(FieldDefinition element)
        {
            var name = element.FullName.Replace(":", string.Empty);
            var declaringType = element.DeclaringType.FullName;

            if (name.StartsWith(declaringType)) name = name.Substring(declaringType.Length).Trim();
            return APIDiffHelper.InternalApiIgnore != null && APIDiffHelper.InternalApiIgnore($"Field:{name}");
        }
    }
}
