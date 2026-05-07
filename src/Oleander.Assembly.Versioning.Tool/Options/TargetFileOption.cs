using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class TargetFileOption : Option<FileInfo>
{
    public TargetFileOption() : base(name: "--target", "-t")
    {
        this.Description = "The target assembly";
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}