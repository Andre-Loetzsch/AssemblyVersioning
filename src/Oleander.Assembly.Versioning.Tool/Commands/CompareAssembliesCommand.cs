using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal class CompareAssembliesCommand : CompareAssembliesCommandBase
{
    public CompareAssembliesCommand(ILogger logger, CompareAssembliesTool tool) : base(logger, tool, "compare", "Compares the public API of two assemblies")
    {
        var target1FileOption = new Target1FileOption().ExistingOnly();
        var target2FileOption = new Target2FileOption().ExistingOnly();
        var outputFormatOption = new OutputFormatOption();

        this.AddOption(target1FileOption);
        this.AddOption(target2FileOption);
        this.AddOption(outputFormatOption);

        this.SetHandler((target1File, target2File, outputFormat) =>
            Task.FromResult(this.CompareAssemblies(target1File, target2File, outputFormat)), target1FileOption, target2FileOption, outputFormatOption);
    }
}