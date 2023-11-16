using JustAssembly.Core;

namespace Oleander.Assembly.Comparator;

public class AssemblyComparison
{
    private readonly IMetadataDiffItem? _diffItem;

    public AssemblyComparison(FileSystemInfo? refAssembly, FileSystemInfo? newAssembly)
    {
        if (refAssembly is not { Exists: true }) return;
        if (newAssembly is not { Exists: true }) return;
        
        this._diffItem = APIDiffHelper.GetAPIDifferences(refAssembly.FullName, newAssembly.FullName);
    }

    public string? ToXml()
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




    //public static Version CalculateVersion(Version version, VersionChange versionChange)
    //{
    //    var versionAsList = new List<int> {version.Major, version.Minor, version.Build, version.Revision};

    //    switch (versionChange)
    //    {
    //        case VersionChange.Major:
    //            versionAsList[0] = version.Major + 1; 
    //            break;
    //        case VersionChange.Minor:
    //            versionAsList[1] = version.Minor + 1;
    //            break;
    //        case VersionChange.Build:
    //            versionAsList[2] = version.Build + 1;
    //            break;
    //        case VersionChange.Revision:
    //            versionAsList[3] = version.Revision + 1;
    //            break;
    //        case VersionChange.None:
    //            break;
    //        default:
    //            throw new ArgumentOutOfRangeException(nameof(versionChange), versionChange, null);
    //    }

    //    if (version.Major == 0)         // beta
    //    {
    //        versionAsList.Insert(0, 0); // alpha

    //        if (version.Minor == 0)
    //        {
    //            versionAsList.Insert(0, 0);
    //        }
    //    }

    //    return new Version(versionAsList[0], versionAsList[1], versionAsList[2], versionAsList[3]);
    //}



}

