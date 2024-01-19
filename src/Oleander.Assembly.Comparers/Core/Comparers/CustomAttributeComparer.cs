using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Attributes;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers
{
    class CustomAttributeComparer : BaseDiffComparer<CustomAttribute>
    {
        protected override IDiffItem GetMissingDiffItem(CustomAttribute element)
        {
            return new CustomAttributeDiffItem(element, null);
        }

        protected override IDiffItem GenerateDiffItem(CustomAttribute oldElement, CustomAttribute newElement)
        {
            return null;
        }

        protected override IDiffItem GetNewDiffItem(CustomAttribute element)
        {
            return new CustomAttributeDiffItem(null, element);
        }

        protected override int CompareElements(CustomAttribute x, CustomAttribute y)
        {
            return string.Compare(x.Constructor.GetSignature(), y.Constructor.GetSignature(), StringComparison.Ordinal);
        }

        protected override bool IsAPIElement(CustomAttribute element)
        {
            return true;
        }

        protected override bool IsIgnored(CustomAttribute element)
        {
            var signature = element.Constructor.GetSignature();

            if (signature.StartsWith("System.Diagnostics.Debugg")) return true;
            if (signature.StartsWith("System.Runtime.CompilerServices.RefSafetyRulesAttribute")) return true;

            return APIDiffHelper.InternalApiIgnore != null &&
                   APIDiffHelper.InternalApiIgnore($"CustomAttribute:{signature.Replace("::.", ".").Replace("::", ".")}");
        }
    }
}
