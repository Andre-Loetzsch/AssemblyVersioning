using JustAssembly.Core.Extensions;
using Mono.Cecil;
using Oleander.Assembly.Comparers.Core;

namespace JustAssembly.Core.DiffItems
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
            string name;
            element.GetMemberTypeAndName(out type, out name);
            return name;
        }
    }
}
