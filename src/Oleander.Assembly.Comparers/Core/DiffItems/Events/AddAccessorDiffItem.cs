using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;
using MetadataType = Oleander.Assembly.Comparers.Core.MetadataType;

namespace JustAssembly.Core.DiffItems.Events
{
    class AddAccessorDiffItem : BaseMemberDiffItem<MethodDefinition>
    {
        public AddAccessorDiffItem(EventDefinition oldEvent, EventDefinition newEvent, IEnumerable<IDiffItem> declarationDiffs)
            : base(oldEvent.AddMethod, newEvent.AddMethod, declarationDiffs, null)
        {
        }

        public override MetadataType MetadataType
        {
            get { return MetadataType.Method; }
        }

        protected override string GetElementShortName(MethodDefinition element)
        {
            return "add";
        }
    }
}
