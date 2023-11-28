using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class ProjectDirOption : Option<DirectoryInfo>
{
    public ProjectDirOption() : base(name: "--project-dir", description: "The project directory")
    {
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete));
    }
}