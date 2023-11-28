using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class GitDirOption : Option<DirectoryInfo>
{
    public GitDirOption() : base(name: "--git-dir", description: "The git repository directory")
    {
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete));
    }
}