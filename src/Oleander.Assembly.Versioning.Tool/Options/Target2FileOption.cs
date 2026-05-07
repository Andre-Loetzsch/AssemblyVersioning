using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class Target2FileOption : Option<FileInfo>
{
    public Target2FileOption() : base(name: "--target2", "-t2")
    {
        this.Description = "The second target assembly to compare";
      
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.dll"));
        this.CompletionSources.Add(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.exe"));
    }
}