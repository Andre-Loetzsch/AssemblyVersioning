using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Events;

namespace Oleander.Assembly.Comparers.Core.Comparers.Accessors
{
    class AddAccessorComparer : BaseAccessorComparer<EventDefinition>
    {
        public AddAccessorComparer(EventDefinition oldEvent, EventDefinition newEvent)
            : base(oldEvent, newEvent)
        {
        }

        protected override MethodDefinition SelectAccessor(EventDefinition element)
        {
            return element.AddMethod;
        }

        protected override IMetadataDiffItem<MethodDefinition> CreateAccessorDiffItem(IEnumerable<IDiffItem> declarationDiffs)
        {
            return new AddAccessorDiffItem(this.oldElement, this.newElement, declarationDiffs);
        }
    }
}
