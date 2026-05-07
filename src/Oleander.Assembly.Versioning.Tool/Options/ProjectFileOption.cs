using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class ProjectFileOption : Option<FileInfo>
{
    public ProjectFileOption() : base(name: "--project", "-p")
    {
        this.Description = "The project file for which the version is to be calculated";
        this.Validators.Add(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.AddError($"Invalid project file: '{fullName}'");
            }
        });

        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.csproj"));
    }
}

