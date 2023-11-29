using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class Target1FileOption : Option<FileInfo>
{
    public Target1FileOption() : base(name: "--target1", description: "the first target assembly to compare")
    {
        this.AddAlias("-t1");
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}