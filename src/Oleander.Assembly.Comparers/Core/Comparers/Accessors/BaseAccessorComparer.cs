using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.DiffItems.Common;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.Comparers.Accessors
{
    abstract class BaseAccessorComparer<T>
    {
        protected readonly T oldElement;
        protected readonly T newElement;

        public BaseAccessorComparer(T oldElement, T newElement)
        {
            this.oldElement = oldElement;
            this.newElement = newElement;
        }

        protected abstract MethodDefinition SelectAccessor(T element);

        protected abstract IMetadataDiffItem<MethodDefinition> CreateAccessorDiffItem(IEnumerable<IDiffItem> declarationDiffs);

        public IMetadataDiffItem<MethodDefinition> GenerateAccessorDiffItem()
        {
            MethodDefinition oldAccessor = this.SelectAccessor(this.oldElement);
            MethodDefinition newAccessor = this.SelectAccessor(this.newElement);

            if (oldAccessor == null && newAccessor == null)
            {
                return null;
            }

            if (oldAccessor == null && newAccessor != null ||
                oldAccessor != null && newAccessor == null)
            {
                return (oldAccessor ?? newAccessor).IsAPIDefinition() ? this.CreateAccessorDiffItem(null) : null;
            }

            if (!oldAccessor.IsAPIDefinition() && !newAccessor.IsAPIDefinition())
            {
                return null;
            }

            IEnumerable<IDiffItem> declarationDiffs = new MethodComparer().GetDeclarationDiffs(oldAccessor, newAccessor).Where(item => !(item is MemberTypeDiffItem));
            if (declarationDiffs.IsEmpty())
            {
                return null;
            }

            return this.CreateAccessorDiffItem(declarationDiffs);
        }
    }
}
