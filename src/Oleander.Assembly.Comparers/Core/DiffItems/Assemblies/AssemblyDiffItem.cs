﻿using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Assemblies
{
    class AssemblyDiffItem : BaseDiffItem<AssemblyDefinition>
    {
        public AssemblyDiffItem(AssemblyDefinition oldAssembly, AssemblyDefinition newAssembly,
            IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem<ModuleDefinition>> childrenDiffs)
            : base(oldAssembly, newAssembly, declarationDiffs, childrenDiffs)
        {
        }

        public override MetadataType MetadataType => MetadataType.Assembly;

        protected override string GetElementShortName(AssemblyDefinition element)
        {
            return element.FullName;
        }
    }
}
