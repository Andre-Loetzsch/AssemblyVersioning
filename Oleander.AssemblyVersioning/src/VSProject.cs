using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

namespace Versioning;

// ReSharper disable once ClassNeverInstantiated.Global
// ReSharper disable once InconsistentNaming
internal class VSProject
{

    private readonly ProjectRootElement _projectRootElement;
    private readonly ILogger _logger;
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

        //foreach (var property in this._projectRootElement.Properties)
        //{
        //    Console.WriteLine($"Name: {property.Name} - Value: {property.Value}");
        //}

    }

    public bool IsDotnetCoreProject { get; set; }





    public string? AssemblyVersion
    {
        get
        {
            var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "AssemblyVersion");
            return property?.Value ?? null;
        }

        set
        {
            var property = this._projectRootElement.Properties.FirstOrDefault(x => x.Name == "AssemblyVersion") ?? 
                           this._projectRootElement.AddProperty("AssemblyVersion", value);

            this._hasChanges = property.Value != value;
            property.Value = value;
        }
    }

    public void SaveChanges()
    {
        if (this._hasChanges)
        {
            this._projectRootElement.Save();
            this._hasChanges = false;

            return;
        }

    }




    // ReSharper disable once InconsistentNaming
    public static bool TryFindVSProject(string startDirectory, out string projectDirName, out string projectFileName)
    {
        projectFileName = string.Empty;
        projectDirName = string.Empty;

        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            var fileInfo = parentDir.GetFiles("*.csproj").MinBy(x => x.FullName);

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


    public static bool TryFindGitRepositoryDirName(string? startDirectory, out string gitRepositoryDirName)
    {
        gitRepositoryDirName = string.Empty;
        if (string.IsNullOrEmpty(startDirectory)) return false;
        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            if (parentDir.GetDirectories(".git").Any())
            {
                gitRepositoryDirName = parentDir.FullName;
                return true;
            }

            parentDir = parentDir.Parent;
        }

        return false;
    }
}