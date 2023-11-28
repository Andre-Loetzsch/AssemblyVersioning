using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class TargetFileOption : Option<FileInfo>
{
    public TargetFileOption() : base(name: "--target", description: "The target assembly")
    {
        this.AddAlias("-t");
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}