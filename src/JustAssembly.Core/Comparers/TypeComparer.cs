using System.Collections.Generic;
using System.Linq;
using JustAssembly.Core.DiffItems.Common;
using JustAssembly.Core.DiffItems.Enums;
using JustAssembly.Core.DiffItems.Types;
using JustAssembly.Core.Extensions;
using Mono.Cecil;


namespace JustAssembly.Core.Comparers
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
                    CheckVisibility(oldElement, newElement)
                );

            IEnumerable<IDiffItem> childrenDiffs =
                EnumerableExtensions.ConcatAll<IDiffItem>(
                    GetFieldDifferences(oldElement, newElement),
                    GetPropertyDifferences(oldElement, newElement),
                    GetMethodDifferences(oldElement, newElement),
                    GetEventDifferences(oldElement, newElement),
                    GetNestedTypeDiffs(oldElement, newElement),
                    GetEnumTypeValueDiffs(oldElement, newElement)
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
            return x.FullName.CompareTo(y.FullName);
        }

        protected override bool IsAPIElement(TypeDefinition element)
        {
            return element.IsPublic || element.IsNestedPublic || element.IsNestedFamily || element.IsNestedFamilyOrAssembly;
        }
    }
}
