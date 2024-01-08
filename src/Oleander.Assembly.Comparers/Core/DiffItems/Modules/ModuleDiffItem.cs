﻿using Mono.Cecil;

namespace Oleander.Assembly.Comparers.Core.DiffItems.Modules
{
    class ModuleDiffItem : BaseDiffItem<ModuleDefinition>
    {
        public ModuleDiffItem(ModuleDefinition oldModule, ModuleDefinition newModule, IEnumerable<IDiffItem> declarationDiffs, IEnumerable<IMetadataDiffItem<TypeDefinition>> diffChildren)
            : base(oldModule, newModule, declarationDiffs, diffChildren)
        {
        }

        public override MetadataType MetadataType => MetadataType.Module;

        protected override string GetElementShortName(ModuleDefinition element)
        {
            return element.Name;
        }
    }
}
