using System.Runtime.Versioning;
using NuGet.Frameworks;
using Oleander.Assembly.Comparers.Cecil;

namespace Oleander.Assembly.Versioning;

internal class AssemblyFrameworkInfo
{
    public AssemblyFrameworkInfo(string assemblyLocation)
    {
        var assemblyDefinition = GlobalAssemblyResolver.Instance.GetAssemblyDefinition(assemblyLocation);

        if (assemblyDefinition == null) return;

        this.CouldResolved = true;
        this.TargetFramework = assemblyDefinition.TargetFrameworkAttributeValue ?? "unknown";
        this.TargetPlatform = assemblyDefinition.TargetPlatformAttributeValue ?? "any";
        this.Version = assemblyDefinition.Name.Version;

        if (this.TargetFramework == null) return;
        this.FrameworkName = new FrameworkName(this.TargetFramework);
        this.NuGetFramework = NuGetFramework.ParseFrameworkName(this.FrameworkName.FullName, new DefaultFrameworkNameProvider());
        this.FrameworkShortFolderName = this.NuGetFramework.GetShortFolderName();
    }

    public bool CouldResolved { get; }

    public Version Version { get; }

    public string? TargetFramework { get; } 

    public string? TargetPlatform { get; }

    public FrameworkName? FrameworkName { get; }
    
    public NuGetFramework? NuGetFramework { get; }

    public string? FrameworkShortFolderName { get; }
}