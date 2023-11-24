using System;
using System.Linq;
using JustDecompile.External.JustAssembly;
using Mono.Cecil;
using Mono.Cecil.Extensions;

namespace JustAssembly.Core.Extensions
{
    static class MemberDefinitionExtensions
    {
        private const string Separator = " : ";

        public static void GetMemberTypeAndName(this IMemberDefinition self, out string type, out string name)
        {
            TypeDefinition declaringType = self.DeclaringType;
            ModuleDefinition module = declaringType.Module;
            string assemblyFilePath = module.Assembly.MainModule.FilePath;
            
            
            string nameWithType =
                Decompiler.GetMemberName(assemblyFilePath, module.MetadataToken.ToUInt32(), declaringType.MetadataToken.ToUInt32(), self.MetadataToken.ToUInt32(), SupportedLanguage.CSharp);


            var name1 = self.FullName.Contains(":") && self is MethodDefinition ? self.FullName.Split(":").Last() : self.Name;
            string nameWithType1 = $"{name1} : {self.GetReturnType().Name}";


            nameWithType1 = nameWithType1.Replace("System.", string.Empty);



            if (nameWithType != nameWithType1) throw new Exception($"{nameWithType} != {nameWithType1}");


            int index = nameWithType.IndexOf(Separator);
            if (index == -1)
            {
                // The member is constructor, hense it has no type.
                name = nameWithType;
                type = null;
            }
            else
            {
                name = nameWithType.Substring(0, index);
                type = nameWithType.Substring(index + Separator.Length);
            }
        }





        //public static TypeReference GetReturnType(this IMemberDefinition memberDefinition)
        //{
        //    if (memberDefinition is MethodDefinition)
        //    {
        //        return ((MethodDefinition)memberDefinition).FixedReturnType;
        //    }
        //    if (memberDefinition is PropertyDefinition)
        //    {
        //        return ((PropertyDefinition)memberDefinition).PropertyType;
        //    }
        //    if (memberDefinition is FieldDefinition)
        //    {
        //        return ((FieldDefinition)memberDefinition).FieldType;
        //    }
        //    return null;
        //}
    }
}
