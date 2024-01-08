using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.Extensions
{
    static class MemberDefinitionExtensions
    {
        public static void GetMemberTypeAndName(this IMemberDefinition self, out string type, out string name)
        {
            name = self.FullName.Contains(':') && self is MethodDefinition ? self.FullName.Split(':').Last() : self.Name;
            if (name.Contains("System.")) name = name.Replace("System.", string.Empty);
            type = self.GetReturnType()?.Name ?? string.Empty;
        }

        public static TypeReference GetReturnType(this IMemberDefinition memberDefinition)
        {
            if (memberDefinition is MethodDefinition methodDefinition)
            {
                return methodDefinition.FixedReturnType;
            }
            if (memberDefinition is PropertyDefinition propertyDefinition)
            {
                return propertyDefinition.PropertyType;
            }
            if (memberDefinition is FieldDefinition fieldDefinition)
            {
                return fieldDefinition.FieldType;
            }

            return null;
        }
    }
}
