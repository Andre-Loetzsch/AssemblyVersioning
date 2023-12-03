using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System.Linq;

namespace Oleander.Assembly.Versioning;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once InconsistentNaming
internal class VSProject
{
    private readonly ProjectRootElement _projectRootElement;
    //private readonly ILogger _logger;
    private bool _hasChanges;

    public VSProject(string projectFileName)
    {

        if (!File.Exists(projectFileName))
        {
            throw new FileNotFoundException("Project file not found!", projectFileName);
        }

        this._projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        this.IsDotnetCoreProject = !string.IsNullOrEmpty(this._projectRootElement.Sdk) &&
                                   string.IsNullOrEmpty(this._projectRootElement.ToolsVersion);

    }

    public bool IsDotnetCoreProject { get; set; }

    public string? AssemblyVersion
    {
        get
        {
            return this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "AssemblyVersion")?.Value;
        }

        set
        {
            var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "AssemblyVersion");

            if (property == null)
            {
                this._projectRootElement.AddProperty("AssemblyVersion", value);
                this._hasChanges = true;
                return;
            }

            this._hasChanges = this._hasChanges || property.Value != value;
            property.Value = value;
        }
    }

    public string? SourceRevisionId
    {
        get
        {
            return this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "SourceRevisionId")?.Value;
        }

        set
        {
            var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "SourceRevisionId");

            if (property == null)
            {
                this._projectRootElement.AddProperty("SourceRevisionId", value);
                this._hasChanges = true;
                return;
            }

            this._hasChanges = this._hasChanges || property.Value != value;
            property.Value = value;
        }
    }

    public string? VersionSuffix
    {
        get
        {
            return this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "VersionSuffix")?.Value;
        }

        set
        {
            var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "VersionSuffix");

            if (property == null)
            {
                this._projectRootElement.AddProperty("VersionSuffix", value);
                this._hasChanges = true;
                return;
            }

            this._hasChanges = this._hasChanges || property.Value != value;
            property.Value = value;
        }
    }

    public void CreateFileVersionIfNotExist()
    {
        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "FileVersion");
        if (property != null) return;
        
        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty("FileVersion", "$(AssemblyVersion)");
            return;
        }

        this._projectRootElement.AddProperty("FileVersion", "$(AssemblyVersion)");
        this._hasChanges = true;
    }

    public void CreateInformationalVersionIfNotExist()
    {
        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "InformationalVersion");
        if (property != null) return;

        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty("InformationalVersion", "$(AssemblyVersion)");
            return;
        }

        this._projectRootElement.AddProperty("InformationalVersion", "$(AssemblyVersion)");
        this._hasChanges = true;
    }

    public void CreateVersionIfNotExist()
    {
        var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "Version");
        if (property != null) return;

        var propertyGroup = this._projectRootElement.PropertyGroups.FirstOrDefault(x => x.Properties.Any(y => y.Name == "AssemblyVersion"));

        if (propertyGroup != null)
        {
            propertyGroup.AddProperty("Version", "$(AssemblyVersion)-$(VersionSuffix)").Condition = "'$(VersionSuffix)' != ''";
            propertyGroup.AddProperty("Version", "$(AssemblyVersion)").Condition = "'$(VersionSuffix)' == ''";
            return;
        }


        this._projectRootElement.AddProperty("Version", "$(AssemblyVersion)-$(VersionSuffix)").Condition = "'$(VersionSuffix)' != ''";
        this._projectRootElement.AddProperty("Version", "$(AssemblyVersion)").Condition = "'$(VersionSuffix)' == ''";

        this._hasChanges = true;
    }

    public void SaveChanges()
    {
        if (!this._hasChanges) return;
        this._projectRootElement.Save();
        this._hasChanges = false;
    }

    // ReSharper disable once InconsistentNaming
    public static bool TryFindVSProject(string startDirectory, out string projectDirName, out string projectFileName)
    {
        projectFileName = string.Empty;
        projectDirName = string.Empty;

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

}