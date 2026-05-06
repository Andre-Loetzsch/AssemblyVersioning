using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal class UpdateAssemblyVersionCommand : UpdateAssemblyVersionCommandBase
{
    public UpdateAssemblyVersionCommand(ILogger logger, AssemblyVersioningTool tool) 
        : base(logger, tool, "update", "Compares the public API of two assemblies and updates the calculated version in the project file")
    {
        var targetFileOption = new TargetFileOption().AcceptExistingOnly();
        var projectDirOption = new ProjectDirOption();
        var projFileOption = new ProjectFileOption();
        var gitDirOption = new GitDirOption();
        
        this.Options.Add(targetFileOption);
        this.Options.Add(projectDirOption);
        this.Options.Add(projFileOption);
        this.Options.Add(gitDirOption);

        this.SetAction(parseResult =>
        {
            Task.FromResult(this.UpdateAssemblyVersion(
                parseResult.GetRequiredValue(targetFileOption),
                parseResult.GetRequiredValue(projectDirOption),
                parseResult.GetRequiredValue(projFileOption),
                parseResult.GetRequiredValue(gitDirOption)));

        });
    }
}