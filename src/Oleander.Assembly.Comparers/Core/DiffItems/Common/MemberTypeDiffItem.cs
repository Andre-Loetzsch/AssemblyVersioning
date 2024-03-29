﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Common
{
    class MemberTypeDiffItem : BaseDiffItem
    {
        private readonly IMemberDefinition oldMember;
        private readonly IMemberDefinition newMember;

        public MemberTypeDiffItem(IMemberDefinition oldMember, IMemberDefinition newMember)
            :base(DiffType.Modified)
        {
            this.oldMember = oldMember;
            this.newMember = newMember;
        }

        protected override string GetXmlInfoString()
        {
            string name;

            string oldType;
            this.oldMember.GetMemberTypeAndName(out oldType, out name);

            string newType;
            this.newMember.GetMemberTypeAndName(out newType, out name);

            return $"Member type changed from {oldType} to {newType}.";
        }

        public override bool IsBreakingChange => true;
    }
}
