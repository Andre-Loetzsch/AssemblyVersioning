using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Events
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
