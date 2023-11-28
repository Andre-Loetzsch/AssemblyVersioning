using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal class CompareAssemblyCommand : CommandBase
{
    public CompareAssemblyCommand(ILogger logger, AssemblyVersioningTool tool) : base(logger, tool, "compare", "Compares the public API of two assemblies")
    {
        var target1FileOption = new Target1FileOption().ExistingOnly();
        var target2FileOption = new Target2FileOption().ExistingOnly();

        this.AddOption(target1FileOption);
        this.AddOption(target2FileOption);

        this.SetHandler((target1File, target2File) =>
            Task.FromResult(this.CompareAssemblies(target1File, target2File)), target1FileOption, target2FileOption);
    }
}