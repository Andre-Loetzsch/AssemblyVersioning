using Mono.Cecil;

namespace Oleander.Assembly.Comparers.Cecil.AssemblyResolver
{
    public interface IAssemblyFrameworkResolver
    {
        FrameworkVersion GetFrameworkVersionForModule(ModuleDefinition moduleDef);

        bool IsCLR4Assembly(ModuleDefinition module);
    }
}
