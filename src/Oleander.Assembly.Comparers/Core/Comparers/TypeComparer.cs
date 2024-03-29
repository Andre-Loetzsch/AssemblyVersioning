﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Common;
using Oleander.Assembly.Comparers.Core.DiffItems.Enums;
using Oleander.Assembly.Comparers.Core.DiffItems.Types;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class TypeComparer : BaseDiffComparer<TypeDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(TypeDefinition element)
        {
            return new TypeDiffItem(element, null, null, null);
        }

        protected override IDiffItem GenerateDiffItem(TypeDefinition oldElement, TypeDefinition newElement)
        {
            IEnumerable<IDiffItem> attributeDiffs = new CustomAttributeComparer().GetMultipleDifferences(oldElement.CustomAttributes, newElement.CustomAttributes);

            IEnumerable<IDiffItem> declarationDiffs =
                EnumerableExtensions.ConcatAll<IDiffItem>(
                    attributeDiffs,
                    this.CheckVisibility(oldElement, newElement)
                );

            IEnumerable<IDiffItem> childrenDiffs =
                EnumerableExtensions.ConcatAll<IDiffItem>(
                    this.GetFieldDifferences(oldElement, newElement),
                    this.GetPropertyDifferences(oldElement, newElement),
                    this.GetMethodDifferences(oldElement, newElement),
                    this.GetEventDifferences(oldElement, newElement),
                    this.GetNestedTypeDiffs(oldElement, newElement),
                    this.GetEnumTypeValueDiffs(oldElement, newElement)
                );

            if (declarationDiffs.IsEmpty() && childrenDiffs.IsEmpty())
            {
                return null;
            }
            return new TypeDiffItem(oldElement, newElement, declarationDiffs, childrenDiffs.Cast<IMetadataDiffItem>());
        }

        private IEnumerable<IDiffItem> CheckVisibility(TypeDefinition oldType, TypeDefinition newType)
        {
            int result = VisibilityComparer.CompareTypes(oldType, newType);
            if (result != 0)
            {
                yield return new VisibilityChangedDiffItem(result < 0);
            }
        }

        private IEnumerable<IDiffItem> GetMethodDifferences(TypeDefinition oldType, TypeDefinition newType)
        {
            return new MethodComparer().GetMultipleDifferences(oldType.Methods.Where(IsNotAccessor), newType.Methods.Where(IsNotAccessor));
        }

        private IEnumerable<IDiffItem> GetFieldDifferences(TypeDefinition oldType, TypeDefinition newType)
        {
            List<IDiffItem> result = new List<IDiffItem>(new FieldComparer().GetMultipleDifferences(oldType.Fields, newType.Fields));
            return result;
        }

        private IEnumerable<IDiffItem> GetPropertyDifferences(TypeDefinition oldType, TypeDefinition newType)
        {
            return new PropertyComparer().GetMultipleDifferences(oldType.Properties, newType.Properties);
        }

        private IEnumerable<IDiffItem> GetEventDifferences(TypeDefinition oldType, TypeDefinition newType)
        {
            return new EventComparer().GetMultipleDifferences(oldType.Events, newType.Events);
        }

        private IEnumerable<IDiffItem> GetNestedTypeDiffs(TypeDefinition oldType, TypeDefinition newType)
        {
            return new TypeComparer().GetMultipleDifferences(oldType.NestedTypes, newType.NestedTypes);
        }

        private IEnumerable<IDiffItem> GetEnumTypeValueDiffs(TypeDefinition oldType, TypeDefinition newType)
        {
            if (!oldType.IsEnum) return Enumerable.Empty<IDiffItem>();
            if (!newType.IsEnum) return Enumerable.Empty<IDiffItem>();
            if (oldType.Fields.Count != newType.Fields.Count) return Enumerable.Empty<IDiffItem>();

            var result = new List<IDiffItem>();
            var fieldTypeNameIsEquals = oldType.Fields[0].FieldType.Name == newType.Fields[0].FieldType.Name;

            for (var i = 1; i < oldType.Fields.Count; i++)
            {
                var oldField = oldType.Fields[i];
                var newField = newType.Fields.FirstOrDefault(x => x.Name == oldField.Name);
                if (newField == null) continue;
                if (fieldTypeNameIsEquals && oldField.Constant?.Value?.ToString() == newField.Constant?.Value.ToString()) continue;

                var oldDef = new EnumFieldDefinition(oldType.Fields[0].FieldType.Name, oldField.Name, oldField.Constant?.Value);
                var newDef = new EnumFieldDefinition(newType.Fields[0].FieldType.Name, newField.Name, newField.Constant?.Value);

                result.Add(new EnumFieldDiffItem(oldDef, null, null, null));
                result.Add(new EnumFieldDiffItem(null, newDef, null, null));
            }

            return result;

        }

        private static bool IsNotAccessor(MethodDefinition methodDef)
        {
            return !methodDef.IsGetter && !methodDef.IsSetter && !methodDef.IsAddOn && !methodDef.IsRemoveOn;
        }

        protected override IDiffItem GetNewDiffItem(TypeDefinition element)
        {
            return new TypeDiffItem(null, element, null, null);
        }

        protected override int CompareElements(TypeDefinition x, TypeDefinition y)
        {
            return string.Compare(x.FullName, y.FullName, StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(TypeDefinition element)
        {
            return element.IsPublic || element.IsNestedPublic || element.IsNestedFamily || element.IsNestedFamilyOrAssembly;
        }

        protected override bool IsIgnored(TypeDefinition element)
        {
            return APIDiffHelper.InternalApiIgnore != null && APIDiffHelper.InternalApiIgnore($"Type:{element.FullName}");
        }
    }
}
