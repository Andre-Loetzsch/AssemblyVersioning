using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal abstract class CompareAssembliesCommandBase(ILogger logger, CompareAssembliesTool tool, string name, string description)
    : Command(name, description)
{
    protected int CompareAssemblies(FileInfo target1FileInfo, FileInfo target2FileInfo)
    {
        try
        {
            return tool.CompareAssemblies(target1FileInfo, target2FileInfo);
        }
        catch (Exception ex)
        {
            MSBuildLogFormatter.CreateMSBuildError("AVT-2", ex.Message, "assembly-versioning");
            logger.LogError("{exception}", ex.GetAllMessages());
            return -1;
        }
    }
}