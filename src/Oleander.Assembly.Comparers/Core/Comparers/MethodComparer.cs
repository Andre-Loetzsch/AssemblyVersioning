using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Common;
using Oleander.Assembly.Comparers.Core.DiffItems.Methods;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class MethodComparer : BaseDiffComparer<MethodDefinition>
    {
        protected override IDiffItem GetMissingDiffItem(MethodDefinition element)
        {
            return new MethodDiffItem(element, null, null);
        }

        protected override IDiffItem GenerateDiffItem(MethodDefinition oldElement, MethodDefinition newElement)
        {
            IEnumerable<IDiffItem> declarationDiffs = this.GetDeclarationDiffsChecked(oldElement, newElement);

            if (declarationDiffs.IsEmpty())
            {
                return null;
            }
            return new MethodDiffItem(oldElement, newElement, declarationDiffs);
        }

        public IEnumerable<IDiffItem> GetDeclarationDiffs(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            if (oldMethod == null)
            {
                throw new ArgumentNullException("oldMethod");
            }

            if (newMethod == null)
            {
                throw new ArgumentNullException("newMethod");
            }

            return this.GetDeclarationDiffsChecked(oldMethod, newMethod);
        }

        private IEnumerable<IDiffItem> GetDeclarationDiffsChecked(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            IEnumerable<IDiffItem> attributeDifferences = new CustomAttributeComparer().GetMultipleDifferences(oldMethod.CustomAttributes, newMethod.CustomAttributes);
            IEnumerable<IDiffItem> reducedVisibilityDiff = this.CheckVisibility(oldMethod, newMethod);
            IEnumerable<IDiffItem> virtualFlagDiff = this.CheckVirtualFlag(oldMethod, newMethod);
            IEnumerable<IDiffItem> staticFlagDiff = this.CheckStaticFlag(oldMethod, newMethod);
            IEnumerable<IDiffItem> returnTypeDiff = this.GetReturnTypeDifference(oldMethod, newMethod);
            IEnumerable<IDiffItem> parameterNameDifferences = this.GetParameterNameDiffs(oldMethod, newMethod);

            IEnumerable<IDiffItem> declarationDiffs =
                EnumerableExtensions.ConcatAll(
                    attributeDifferences,
                    reducedVisibilityDiff,
                    virtualFlagDiff,
                    staticFlagDiff,
                    returnTypeDiff,
                    parameterNameDifferences
                );

            return declarationDiffs;
        }

        private IEnumerable<IDiffItem> CheckVisibility(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            int result = VisibilityComparer.CompareVisibilityDefinitions(oldMethod, newMethod);
            if (result != 0)
            {
                yield return new VisibilityChangedDiffItem(result < 0);
            }
        }

        private IEnumerable<IDiffItem> CheckVirtualFlag(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            if (oldMethod.IsVirtual != newMethod.IsVirtual)
            {
                yield return new VirtualFlagChangedDiffItem(newMethod.IsVirtual);
            }
        }

        private IEnumerable<IDiffItem> CheckStaticFlag(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            if (oldMethod.IsStatic != newMethod.IsStatic)
            {
                yield return new StaticFlagChangedDiffItem(newMethod.IsStatic);
            }
        }

        private IEnumerable<IDiffItem> GetReturnTypeDifference(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            if (oldMethod.ReturnType.FullName != newMethod.ReturnType.FullName)
            {
                yield return new MemberTypeDiffItem(oldMethod, newMethod);
            }
        }

        private IEnumerable<IDiffItem> GetParameterNameDiffs(MethodDefinition oldMethod, MethodDefinition newMethod)
        {
            List<IDiffItem> result = new List<IDiffItem>();
            for (int i = 0; i < oldMethod.Parameters.Count; i++)
            {
                ParameterDefinition oldParameter = oldMethod.Parameters[i];
                ParameterDefinition newParameter = newMethod.Parameters[i];
                if (oldParameter.Name != newParameter.Name)
                {
                    result.Add(new ParameterDiffItem(oldParameter, newParameter));
                }
            }

            return result;
        }

        protected override IDiffItem GetNewDiffItem(MethodDefinition element)
        {
            return new MethodDiffItem(null, element, null);
        }

        protected override int CompareElements(MethodDefinition x, MethodDefinition y)
        {
            return x.GetSignature().CompareTo(y.GetSignature());
        }

        protected override bool IsAPIElement(MethodDefinition element)
        {
            return element.IsAPIDefinition();
        }
    }
}
