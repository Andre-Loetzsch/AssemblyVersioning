using Mono.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Events
{
    class RemoveAccessorDiffItem : BaseMemberDiffItem<MethodDefinition>
    {
        public RemoveAccessorDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldEvent.RemoveMethod, newEvent.RemoveMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Method; }
        }

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "remove";
        }
    }
}
