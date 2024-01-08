using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Events
{
    class EventDiffItem : BaseMemberDiffItem<EventDefinition>
    {
        public EventDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            :base(oldEvent, newEvent, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Event; }
        }
    }
}
