using JustAssembly.Core;
using SysAssembly = System.Reflection.Assembly;

namespace Oleander.Assembly.Comparator;

public class AssemblyComparison
{
    private readonly FileSystemInfo? _refAssembly;
    private readonly FileSystemInfo? _newAssembly;
    private readonly IMetadataDiffItem? _diffItem;


    public AssemblyComparison(FileSystemInfo? refAssembly, FileSystemInfo? newAssembly)
    {
        if (refAssembly is not { Exists: true }) return;
        if (newAssembly is not { Exists: true }) return;

        this._refAssembly = refAssembly;
        this._newAssembly = newAssembly;

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
            if (this._diffItem == null) return this.CheckEnumsForBreakingChange(this._refAssembly, this._newAssembly);
            if (this._diffItem.IsBreakingChange) return VersionChange.Major;

            VersionChange retValue;

            var differences = this._diffItem.ChildrenDiffs.Concat(this._diffItem.DeclarationDiffs).ToList();

            if (!differences.Any()) retValue = VersionChange.Build;
            if (differences.Any(diff => diff.DiffType == DiffType.Deleted)) retValue = VersionChange.Major;

            retValue = differences.Any(diff => diff.DiffType is DiffType.Modified or DiffType.New) ?
                VersionChange.Minor :
                VersionChange.Build;

            var enumVersionChange = this.CheckEnumsForBreakingChange(this._refAssembly, this._newAssembly);
            return retValue > enumVersionChange ? retValue: enumVersionChange;
        }
    }

    private VersionChange CheckEnumsForBreakingChange(FileSystemInfo? refAssemblyFile, FileSystemInfo? newAssemblyFile)
    {
        if (refAssemblyFile is not { Exists: true } || newAssemblyFile is not { Exists: true }) return VersionChange.None;

        var refAssemblyEnums = SysAssembly.Load(File.ReadAllBytes(refAssemblyFile.FullName)).ExportedTypes.Where(x => x.IsEnum);
        var newAssemblyEnums = SysAssembly.Load(File.ReadAllBytes(newAssemblyFile.FullName)).ExportedTypes.Where(x => x.IsEnum);
        var refList = new List<string>();
        var newList = new List<string>();

        foreach (var enumType in refAssemblyEnums)
        {
            var hasFlag = enumType.GetCustomAttributes(true).OfType<FlagsAttribute>().Any();
            var enumNames = Enum.GetNames(enumType);
            var enumValues = GetEnumValues(enumType).ToArray();

            refList.AddRange(enumNames.Select((t, i) => $"enum:{enumType.FullName}:{hasFlag}:{t}:{enumValues[i]}"));
        }

        foreach (var enumType in newAssemblyEnums)
        {
            var hasFlag = enumType.GetCustomAttributes(true).OfType<FlagsAttribute>().Any();
            var enumNames = Enum.GetNames(enumType);
            var enumValues = GetEnumValues(enumType).ToArray();

            newList.AddRange(enumNames.Select((t, i) => $"enum:{enumType.FullName}:{hasFlag}:{t}:{enumValues[i]}"));
        }

        if (refList.Any(item => !newList.Remove(item)))
        {
            return VersionChange.Major;
        }
       
        return newList.Any() ? VersionChange.Minor : VersionChange.None;
    }


    private static IEnumerable<object> GetEnumValues(Type enumType)
    {
        var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
        var enumValues = Enum.GetValues(enumType);

        for (var i = 0; i < enumValues.Length; i++)
        {
            var value = enumValues.GetValue(i);
            if (value == null) continue;
            yield return Convert.ChangeType(value, enumUnderlyingType);
        }
    }







}