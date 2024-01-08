using System.Xml;
using Mono.Cecil;
using Oleander.Assembly.Comparers.Core.Extensions;

namespace Oleander.Assembly.Comparers.Core.DiffItems
{
    abstract class BaseDiffItem(DiffType diffType) : IDiffItem
    {
        public DiffType DiffType { get; } = diffType;

        public string ToXml()
        {
            using StringWriter stringWriter = new StringWriter();
            using (var xmlWriter = new XmlTextWriter(stringWriter))
            {
                xmlWriter.Formatting = Formatting.Indented;
                this.ToXml(xmlWriter);
            }

            return stringWriter.ToString();
        }

        protected abstract string GetXmlInfoString();

        internal virtual void ToXml(XmlWriter writer)
        {
            writer.WriteStartElement("DiffItem");
            writer.WriteAttributeString("DiffType", this.DiffType.ToString());
            writer.WriteString(this.GetXmlInfoString());
            writer.WriteEndElement();
        }

        public abstract bool IsBreakingChange { get; }
    }

    abstract class BaseDiffItem<T>(T oldElement, T newElement, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
        : BaseDiffItem(newElement == null ? DiffType.Deleted : (oldElement == null ? DiffType.New : DiffType.Modified)), IMetadataDiffItem<T>
        where T : class, IMetadataTokenProvider
    {
        public T OldElement { get; } = oldElement;

        public T NewElement { get; } = newElement;

        public abstract MetadataType MetadataType { get; }

        public uint OldTokenID => this.OldElement.MetadataToken.ToUInt32();

        public uint NewTokenID => this.NewElement.MetadataToken.ToUInt32();

        public IEnumerable<IDiffItem> DeclarationDiffs { get; } = declarationDiffs?.ToList() ?? Enumerable.Empty<IDiffItem>();

        public IEnumerable<IMetadataDiffItem> ChildrenDiffs { get; } = childrenDiffs?.ToList() ?? Enumerable.Empty<IMetadataDiffItem>();

        protected T GetElement()
        {
            return this.NewElement ?? this.OldElement;
        }

        protected abstract string GetElementShortName(T element);

        internal override void ToXml(XmlWriter writer)
        {
            writer.WriteStartElement(this.MetadataType.ToString());
            writer.WriteAttributeString("Name", this.GetElementShortName(this.GetElement()));
            writer.WriteAttributeString("DiffType", this.DiffType.ToString());

            if (!this.DeclarationDiffs.IsEmpty())
            {
                writer.WriteStartElement("DeclarationDiffs");
                foreach (var item in this.DeclarationDiffs.Cast<BaseDiffItem>())
                {
                    item.ToXml(writer);
                }
                writer.WriteEndElement();
            }

            foreach (var item in this.ChildrenDiffs.Cast<BaseDiffItem>())
            {
                item.ToXml(writer);
            }

            writer.WriteEndElement();
        }

        protected override string GetXmlInfoString()
        {
            throw new NotSupportedException();
        }

        private bool? _isBreakingChange;
        public override bool IsBreakingChange
        {
            get
            {
                if (this._isBreakingChange != null) return this._isBreakingChange.Value;
                
                if(this.DiffType != DiffType.Modified)
                {
                    this._isBreakingChange = this.DiffType == DiffType.Deleted;
                }
                else
                {
                    this._isBreakingChange = EnumerableExtensions.ConcatAll(this.DeclarationDiffs, this.ChildrenDiffs).Any(item => item.IsBreakingChange);
                }
               
                return this._isBreakingChange.Value;
            }
        }
    }
}
