using System.Collections.Generic;
using System.Linq;
using JustDecompile.External.JustAssembly;
using Mono.Cecil;

namespace JustAssembly.Core.DiffItems.Types
{
    class TypeDiffItem : BaseDiffItem<TypeDefinition>
    {
        public TypeDiffItem(TypeDefinition oldType, TypeDefinition newType, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem> childrenDiffs)
            : base(oldType, newType, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => Core.MetadataType.Type;

        public override bool IsBreakingChange
        {
            get
            {
                if (base.ChildrenDiffs.Any() &&
                    this.OldElement != null && this.OldElement.IsInterface &&
                    this.NewElement != null && this.NewElement.IsInterface) return true;

                return base.IsBreakingChange;
            }
        }


        protected override string GetElementShortName(TypeDefinition typeDef)
        {
            var retVal1 = typeDef.FullName;
            var retVal2 = typeDef.Namespace + "." + Decompiler.GetTypeName(typeDef.Module.FilePath, typeDef.Module.MetadataToken.ToUInt32(), typeDef.MetadataToken.ToUInt32(), SupportedLanguage.CSharp);


            if (retVal1 != retVal2) throw new Exception($"{retVal1} != {retVal2}");

            return retVal1;
        }
    }
}
