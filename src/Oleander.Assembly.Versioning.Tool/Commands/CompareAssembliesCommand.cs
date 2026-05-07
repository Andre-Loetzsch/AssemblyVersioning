using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;
using System.CommandLine;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal class CompareAssembliesCommand : CompareAssembliesCommandBase
{
    public CompareAssembliesCommand(ILogger logger, CompareAssembliesTool tool) : base(logger, tool, "compare", "Compares the public API of two assemblies")
    {
        var target1FileOption = new Target1FileOption().AcceptExistingOnly();
        var target2FileOption = new Target2FileOption().AcceptExistingOnly();
        var outputFormatOption = new OutputFormatOption();

        this.Options.Add(target1FileOption);    
        this.Options.Add(target2FileOption);
        this.Options.Add(outputFormatOption);

        this.SetAction(parseResult =>
        {
            Task.FromResult(this.CompareAssemblies(
                parseResult.GetRequiredValue(target1FileOption), 
                parseResult.GetRequiredValue(target2FileOption),
                parseResult.GetRequiredValue(outputFormatOption)));

        });
    }
}