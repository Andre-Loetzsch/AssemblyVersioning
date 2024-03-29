﻿using Oleander.Assembly.Comparers.Cecil;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Attributes
{
    class CustomAttributeDiffItem : BaseDiffItem
    {
        private readonly CustomAttribute attribute;

        public CustomAttributeDiffItem(CustomAttribute oldAttribute, CustomAttribute newAttribute)
            : base(oldAttribute == null ? DiffType.New : DiffType.Deleted)
        {
            if (oldAttribute != null && newAttribute != null)
            {
                throw new InvalidOperationException();
            }

            this.attribute = oldAttribute ?? newAttribute;
        }

        protected override string GetXmlInfoString()
        {
            throw new NotSupportedException();
        }

        internal override void ToXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("CustomAttribute");
            writer.WriteAttributeString("Name", this.attribute.Constructor.GetSignature());
            writer.WriteAttributeString("DiffType", this.DiffType.ToString());
            writer.WriteEndElement();
        }

        public override bool IsBreakingChange => this.DiffType == DiffType.Deleted;
    }
}
