using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal abstract class CommandBase(ILogger logger, AssemblyVersioningTool tool, string name, string description)
    : Command(name, description)
{
    protected int UpdateAssemblyVersion(FileInfo targetFileInfo, DirectoryInfo? projectDirInfo, FileInfo? projectFileInfo, DirectoryInfo? gitRepositoryDirInfo)
    {
        try
        {
            if (projectDirInfo != null && projectFileInfo != null && gitRepositoryDirInfo != null)
            {
                return tool.UpdateAssemblyVersion(targetFileInfo, projectDirInfo, projectFileInfo, gitRepositoryDirInfo);
            }

            if (projectDirInfo != null && projectFileInfo != null)
            {
                return tool.UpdateAssemblyVersion(targetFileInfo, projectDirInfo, projectFileInfo);
            }

            return projectFileInfo != null ?
                tool.UpdateAssemblyVersion(targetFileInfo, projectFileInfo) : 
                tool.UpdateAssemblyVersion(targetFileInfo);
        }
        catch (Exception ex)
        {
            MSBuildLogFormatter.CreateMSBuildError("AVT-1", ex.Message, "assembly-versioning");
            logger.LogError("{exception}", ex.GetAllMessages());
            return -1;
        }
    }

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