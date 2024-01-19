using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.DiffItems
{
    abstract class BaseMemberDiffItem<T> : BaseDiffItem<T> where T: class, IMemberDefinition
    {
        public BaseMemberDiffItem(T oldMember, T newMember, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldMember, newMember, declarationDiffs, childrenDiffs)
        {
        }

        protected override string GetElementShortName(T element)
        {
            string type;
            element.GetMemberTypeAndName(out type, out var name);
            return name;
        }
    }
}
