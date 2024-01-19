
namespace Oleander.Assembly.Comparers.Core.DiffItems.Common
{
    class StaticFlagChangedDiffItem : BaseDiffItem
    {
        private readonly bool _isNewMemberStatic;

        public StaticFlagChangedDiffItem(bool isNewMemberStatic)
            :base(DiffType.Modified)
        {
            this._isNewMemberStatic = isNewMemberStatic;
        }

        protected override string GetXmlInfoString()
        {
            return $"Member changed to {(this._isNewMemberStatic ? "static" : "instance")}.";
        }

        public override bool IsBreakingChange => true;
    }
}
