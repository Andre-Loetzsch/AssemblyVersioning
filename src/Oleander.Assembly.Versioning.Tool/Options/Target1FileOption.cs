using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class Target1FileOption : Option<FileInfo>
{
    public Target1FileOption() : base("--target1", "-t1")
    {
        this.Description = "The first target assembly to compare";
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}