using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.Options;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal class UpdateAssemblyVersionCommand : UpdateAssemblyVersionCommandBase
{
    public UpdateAssemblyVersionCommand(ILogger logger, AssemblyVersioningTool tool) : base(logger, tool, "update", "Compares the public API of two assemblies and updates the calculated version in the project file")
    {
        var targetFileOption = new TargetFileOption().ExistingOnly();
        var projectDirOption = new ProjectDirOption();
        var projFileOption = new ProjectFileOption();
        var gitDirOption = new GitDirOption();

        this.AddOption(targetFileOption);
        this.AddOption(projectDirOption);
        this.AddOption(projFileOption);
        this.AddOption(gitDirOption);

        this.SetHandler((targetFile, projectDir, projFile, gitDir) =>
            Task.FromResult(this.UpdateAssemblyVersion(targetFile, projectDir, projFile, gitDir)), targetFileOption, projectDirOption, projFileOption, gitDirOption);
    }
}