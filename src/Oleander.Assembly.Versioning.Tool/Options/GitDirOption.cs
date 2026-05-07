using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class GitDirOption : Option<DirectoryInfo>
{
    public GitDirOption() : base("--git-dir")
    {
        this.Description = "The git repository directory";
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete));
    }
}