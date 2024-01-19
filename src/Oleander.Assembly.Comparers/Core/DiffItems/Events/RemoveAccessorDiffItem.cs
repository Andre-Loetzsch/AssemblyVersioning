using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Events
{
    internal class RemoveAccessorDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs)
        : BaseMemberDiffItem<MethodDefinition>(oldEvent.RemoveMethod, newEvent.RemoveMethod, declarationDiffs, null)
    {
        public override MetadataType MetadataType => MetadataType.Method;

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "remove";
        }
    }
}
