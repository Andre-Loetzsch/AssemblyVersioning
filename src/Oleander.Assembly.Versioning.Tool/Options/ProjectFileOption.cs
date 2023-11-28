using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class ProjectFileOption : Option<FileInfo>
{
    public ProjectFileOption() : base(name: "--project", description: "The project file for which the version is to be calculated")
    {
        this.AddAlias("-p");

        this.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.ErrorMessage = $"Invalid project file: '{fullName}'";
            }
        });

        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.csproj"));
    }
}

