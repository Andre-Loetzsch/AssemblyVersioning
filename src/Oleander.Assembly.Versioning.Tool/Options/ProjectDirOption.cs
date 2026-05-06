using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class ProjectDirOption : Option<DirectoryInfo>
{
    public ProjectDirOption() : base(name: "--project-dir")
    {
        this.Description = "The project directory";
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete));
    }
}