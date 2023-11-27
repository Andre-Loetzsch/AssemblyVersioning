using Mono.Cecil;

namespace JustAssembly.Core.DiffItems.Enums
{
    class EnumFieldDefinition : IMetadataTokenProvider
    {
        public EnumFieldDefinition(string fieldTypeName, string name, object value)
        {
            this.FieldTypeName = fieldTypeName;
            this.Name = name;
            this.Value = value;
        }

        public MetadataToken MetadataToken { get; set; }
      
        public string FieldTypeName { get; }
        public string Name { get; }
        public object Value { get; }
    }
}