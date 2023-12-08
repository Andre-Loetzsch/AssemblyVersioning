using System.Runtime.Versioning;
using NuGet.Frameworks;

namespace Oleander.Assembly.Versioning;

public class AssemblyFrameworkInfo
{
    public AssemblyFrameworkInfo(SysAssembly assembly)
    {
        this.TargetFramework = GetTargetFramework(assembly);
        this.TargetPlatform = GetTargetPlatform(assembly);
        this.ImageRuntimeVersion = assembly.ImageRuntimeVersion;

        if (string.IsNullOrEmpty(this.TargetFramework)) return;
        this.FrameworkName = new FrameworkName(this.TargetFramework);
        this.NuGetFramework = NuGetFramework.ParseFrameworkName(this.FrameworkName.FullName, new DefaultFrameworkNameProvider());
        this.ShortFolderName = this.NuGetFramework.GetShortFolderName();
    }

    public string? TargetFramework { get; }

    public string? TargetPlatform { get; }

    public string ImageRuntimeVersion { get; }

    public FrameworkName? FrameworkName { get; }
    
    public NuGetFramework? NuGetFramework { get; }

    public string? ShortFolderName { get; }


    private static string? GetTargetFramework(SysAssembly assembly)
    {
        string? targetFrameworkAttributeValue = null;

        var targetFrameworkAttributeAttributeData = assembly.CustomAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetFrameworkAttribute");

        if (targetFrameworkAttributeAttributeData is { ConstructorArguments.Count: > 0 })
        {
            targetFrameworkAttributeValue = targetFrameworkAttributeAttributeData.ConstructorArguments[0].Value as string;
        }

        return targetFrameworkAttributeValue;
    }

    private static string? GetTargetPlatform(SysAssembly assembly)
    {
        string? targetPlatformAttributeValue = null;

        var targetPlatformAttributeData = assembly.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.Versioning.TargetPlatformAttribute");

        if (targetPlatformAttributeData is { ConstructorArguments.Count: > 0 })
        {
            targetPlatformAttributeValue = targetPlatformAttributeData.ConstructorArguments[0].Value as string;
        }

        return targetPlatformAttributeValue;
    }
}