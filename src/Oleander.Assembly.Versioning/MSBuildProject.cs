using System.Diagnostics.CodeAnalysis;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Oleander.Assembly.Versioning;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once InconsistentNaming
internal class MSBuildProject
{
    private readonly ProjectRootElement _projectRootElement;

    private readonly List<string> _assemblyInfoContent;
    //private readonly ILogger _logger;
    private bool _projectFileChanged;
    private bool _assemblyInfoFileChanged;
    private readonly Dictionary<string, string> _projectProperties = new();

    public MSBuildProject(string projectFileName)
    {
        if (!File.Exists(projectFileName))
        {
            throw new FileNotFoundException("Project file not found!", projectFileName);
        }

        this.ProjectFileName = projectFileName;
        //this._projectRootElement = ProjectRootElement.Open(
        //    this.ProjectFileName, 
        //    ProjectCollection.GlobalProjectCollection,
        //    preserveFormatting: true);

        this._projectRootElement = ProjectRootElement.Open(
            this.ProjectFileName,
            new ProjectCollection(),
            preserveFormatting: true);


        this.IsDotnetCoreProject = !string.IsNullOrEmpty(this._projectRootElement.Sdk) &&
                                   string.IsNullOrEmpty(this._projectRootElement.ToolsVersion);

        this._assemblyInfoContent = this.AssemblyInfoExist ?
            new(File.ReadAllLines(this.AssemblyInfoPath)) :
            new List<string>();
    }

    public string ProjectFileName { get; }

    public bool IsDotnetCoreProject { get; set; }

    public string? AssemblyVersion
    {
        get => this.GetAttributeValue("AssemblyVersion");
        set => this.SetAttributeValue("AssemblyVersion", value);
    }

    public string? SourceRevisionId
    {
        get => this.GetAttributeValue("SourceRevisionId");
        set => this.SetAttributeValue("SourceRevisionId", value);
    }

    public string? VersionSuffix
    {
        get => this.GetAttributeValue("VersionSuffix");
        set => this.SetAttributeValue("VersionSuffix", value);
    }

    public bool IsPackable => 
        this.GetAttributeValue("IsPackable") is null or "true";

    public string? PackageId
    {
        get
        {
            var packageId = this.GetAttributeValue("PackageId") ?? string.Empty;
            var mSBuildProjectName = this.GetAttributeValue("MSBuildProjectName") ?? Path.GetFileNameWithoutExtension(this.ProjectFileName);

            packageId = packageId.Replace("$(MSBuildProjectName)", mSBuildProjectName);
            if (!string.IsNullOrEmpty(packageId)) return packageId;

            var assemblyName = this.GetAttributeValue("AssemblyName") ?? string.Empty;
            assemblyName = assemblyName.Replace("$(MSBuildProjectName)", mSBuildProjectName);

            if (!string.IsNullOrEmpty(assemblyName)) return assemblyName;
            if (!string.IsNullOrEmpty(mSBuildProjectName)) return mSBuildProjectName;

            return Path.GetFileNameWithoutExtension(this.ProjectFileName);
        }
    }

    public string? PackageSource => this.GetAttributeValue("PackageSource");

    public bool UseAssemblyInfoFile
    {
        get
        {
            return !this.IsDotnetCoreProject ||
                   this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "GenerateAssemblyInfo")?.Value?.ToLower() == "false";
        }
    }

    public void SaveChanges()
    {
        if (this.UseAssemblyInfoFile)
        {
            var assemblyVersion = this.AssemblyVersion;
           
            this.TrySetAssemblyInfoFileAttributeValue("AssemblyVersion", assemblyVersion);
            this.TrySetAssemblyInfoFileAttributeValue("AssemblyFileVersion", assemblyVersion);

            if (!this._assemblyInfoFileChanged) return;
            File.WriteAllLines(this.AssemblyInfoPath, this._assemblyInfoContent);
            this._assemblyInfoFileChanged = false;

            return;
        }

        this.CreateFileVersionIfNotExist();
        this.CreateVersionIfNotExist();

        if (!this._projectFileChanged) return;
        this._projectRootElement.Save();
        this._projectFileChanged = false;
    }

    public bool TryGetAssemblyInfoFileAttributeValue(string attribute, [MaybeNullWhen(false)] out string value)
    {
        value = null;

        if (!this.AssemblyInfoExist) return false;
        var line = this._assemblyInfoContent.FirstOrDefault(x => x.Contains(attribute));
        if (line == null) return false;
        var indexOf = line.IndexOf(attribute, StringComparison.InvariantCulture);
        if (indexOf == -1) return false;

        indexOf += attribute.Length;
        value = line.Substring(indexOf);
        indexOf = value.IndexOf("\"", StringComparison.InvariantCulture);

        if (indexOf == -1) return false;
        value = value.Substring(indexOf + 1);
        indexOf = value.IndexOf("\"", StringComparison.InvariantCulture);

        if (indexOf == -1) return false;
        value = value.Substring(0, indexOf);
        return true;
    }

    public bool TrySetAssemblyInfoFileAttributeValue(string attribute, string? value)
    {
        if (!this.AssemblyInfoExist) return false;
        var line = this._assemblyInfoContent.FirstOrDefault(x => x.Contains(attribute));

        if (line == null)
        {
            if (value == null) return true;

            this._assemblyInfoContent.Add($"[assembly: {attribute}(\"{value}\")]");
            this._assemblyInfoFileChanged = true;
            return true;
        }

        var lineIdx = this._assemblyInfoContent.IndexOf(line);
        var startIdx = line.IndexOf(attribute, StringComparison.InvariantCulture);

        if (startIdx == -1 || lineIdx == -1) return false;

        var newValue = $"[assembly: {attribute}(\"{value}\")]";

        this._assemblyInfoFileChanged = this._assemblyInfoFileChanged || this._assemblyInfoContent[lineIdx] != newValue;
        this._assemblyInfoContent[lineIdx] = newValue;
        return true;
    }

    public bool AssemblyInfoExist => File.Exists(this.AssemblyInfoPath);

    private string? _assemblyInfoPath;
    public string AssemblyInfoPath
    {
        get
        {
            if (this._assemblyInfoPath != null) return this._assemblyInfoPath;

            this._assemblyInfoPath =  Path.Combine(
                Path.GetDirectoryName(this._projectRootElement.FullPath) ?? string.Empty, "Properties", "AssemblyInfo.cs");
            return this._assemblyInfoPath;
        }
        set => this._assemblyInfoPath = value;
    }

    // ReSharper disable once InconsistentNaming
    public static bool TryFindVSProject(string startDirectory, 
        [MaybeNullWhen(false)] out string projectDirName, [MaybeNullWhen(false)] out string projectFileName)
    {
        projectFileName = null;
        projectDirName = null;

        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir is { Exists: true })
        {
            //var fileInfo = parentDir.GetFiles("*.csproj").MinBy(x => x.FullName);
            var fileInfo = parentDir.GetFiles("*.csproj").FirstOrDefault();

            if (fileInfo != null)
            {
                projectDirName = parentDir.FullName;
                projectFileName = fileInfo.FullName;
                return true;
            }

            parentDir = parentDir.Parent;
        }

        return false;
    }

    private void CreateFileVersionIfNotExist()
    {
        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "FileVersion");
        if (property != null) return;

        this._projectFileChanged = true;

        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty("FileVersion", "$(AssemblyVersion)");
            return;
        }

        this._projectRootElement.AddProperty("FileVersion", "$(AssemblyVersion)");
    }

    private void CreateVersionIfNotExist()
    {
        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "Version");
        if (property != null) return;

        this._projectFileChanged = true;

        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty("Version", "$(AssemblyVersion)-$(VersionSuffix)").Condition = "'$(VersionSuffix)' != ''";
            propertyGroup.AddProperty("Version", "$(AssemblyVersion)").Condition = "'$(VersionSuffix)' == ''";
            return;
        }

        this._projectRootElement.AddProperty("Version", "$(AssemblyVersion)-$(VersionSuffix)").Condition = "'$(VersionSuffix)' != ''";
        this._projectRootElement.AddProperty("Version", "$(AssemblyVersion)").Condition = "'$(VersionSuffix)' == ''";
    }

    private string? GetAttributeValue(string attribute)
    {
        if (this._projectProperties.TryGetValue(attribute, out var value)) return value;
        value = this.TryGetAssemblyInfoFileAttributeValue(attribute, out value) ?
            value : this._projectRootElement.Properties.FirstOrDefault(x => x.Name == attribute)?.Value;

        this._projectProperties[attribute] = value ?? string.Empty;
        return value;
    }

    private void SetAttributeValue(string attribute, string? value)
    {
        if (value == null)
        {
            this._projectProperties.Remove(attribute);
        }
        else
        {
            this._projectProperties[attribute] = value;
        }

        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == attribute);

        if (property != null)
        {
            this._projectFileChanged = this._projectFileChanged || property.Value != value;
            property.Value = value;
            return;
        }

        this._projectFileChanged = true;

        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty(attribute, value);
            return;
        }

        this._projectRootElement.AddProperty(attribute, value);
    }
}