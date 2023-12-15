using JustAssembly.Core;
using Mono.Cecil;
using Mono.Cecil.AssemblyResolver;

namespace Oleander.Assembly.Comparator;

public class AssemblyComparison
{
    private readonly IMetadataDiffItem _diffItem;

    public AssemblyComparison(FileSystemInfo refAssembly, FileSystemInfo newAssembly, bool clearCache)
    {
        if (refAssembly is not { Exists: true }) return;
        if (newAssembly is not { Exists: true }) return;

        var tempRefAssembly = Path.GetTempFileName();
        var tempNewAssembly = Path.GetTempFileName();

        File.Copy(refAssembly.FullName, tempRefAssembly, true);
        File.Copy(newAssembly.FullName, tempNewAssembly, true);

        if (clearCache) TargetPlatformResolver.Instance.ResolverCache.Clear();

        this._diffItem = APIDiffHelper.GetAPIDifferences(tempRefAssembly, tempNewAssembly);

        File.Delete(tempRefAssembly);
        File.Delete(tempNewAssembly);
    }

    public string ToXml()
    {
        return this._diffItem?.ToXml();
    }

    public VersionChange VersionChange
    {
        get
        {
            if (this._diffItem == null) return VersionChange.None;
            if (this._diffItem.IsBreakingChange) return VersionChange.Major;

            var differences = this._diffItem.ChildrenDiffs.Concat(this._diffItem.DeclarationDiffs).ToList();

            if (!differences.Any()) return VersionChange.Build;
            if (differences.Any(diff => diff.DiffType == DiffType.Deleted)) return VersionChange.Major;

            return differences.Any(diff => diff.DiffType is DiffType.Modified or DiffType.New) ?
                VersionChange.Minor :
                VersionChange.Build;
        }
    }
}
