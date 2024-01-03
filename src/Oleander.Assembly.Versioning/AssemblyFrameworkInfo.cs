using System.Runtime.Versioning;
using Mono.Cecil;
using NuGet.Frameworks;

namespace Oleander.Assembly.Versioning;

internal class AssemblyFrameworkInfo
{
    public AssemblyFrameworkInfo(string assemblyLocation)
    {
        var assemblyDefinition = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(assemblyLocation);

        this.TargetFramework = assemblyDefinition.TargetFrameworkAttributeValue;
        this.TargetPlatform = assemblyDefinition.TargetPlatformAttributeValue ?? "any";
        this.Version = assemblyDefinition.Name.Version;

        if (this.TargetFramework == null) return;
        this.FrameworkName = new FrameworkName(this.TargetFramework);
        this.NuGetFramework = NuGetFramework.ParseFrameworkName(this.FrameworkName.FullName, new DefaultFrameworkNameProvider());
        this.FrameworkShortFolderName = this.NuGetFramework.GetShortFolderName();
    }

    public Version Version { get; }

    public string? TargetFramework { get; } 

    public string? TargetPlatform { get; }

    public FrameworkName? FrameworkName { get; }
    
    public NuGetFramework? NuGetFramework { get; }

    public string? FrameworkShortFolderName { get; }
}