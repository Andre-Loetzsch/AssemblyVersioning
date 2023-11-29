using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class Target2FileOption : Option<FileInfo>
{
    public Target2FileOption() : base(name: "--target2", description: "the second target assembly to compare")
    {
        this.AddAlias("-t2");
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}