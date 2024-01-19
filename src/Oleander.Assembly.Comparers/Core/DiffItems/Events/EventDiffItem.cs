using Oleander.Assembly.Comparers.Cecil;
using System.Xml.Linq;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Events
{
    class EventDiffItem : BaseMemberDiffItem<EventDefinition>
    {
        public EventDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            :base(oldEvent, newEvent, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => MetadataType.Event;

        protected override string GetElementShortName(EventDefinition element)
        {
            return $"{element.Name}({element.EventType.FullName.Replace("<", "[").Replace(">", "]")})".Replace("System.", string.Empty);
        }
    }
}
