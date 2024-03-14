using System.CommandLine;
using Microsoft.Extensions.Logging;
using Oleander.Assembly.Versioning.Tool.OutputFormats;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.Assembly.Versioning.Tool.Commands;

internal abstract class CompareAssembliesCommandBase(ILogger logger, CompareAssembliesTool tool, string name, string description)
    : Command(name, description)
{
    protected int CompareAssemblies(FileInfo target1FileInfo, FileInfo target2FileInfo, OutputFormat outputFormat)
    {
        try
        {
            return tool.CompareAssemblies(target1FileInfo, target2FileInfo, GetOutputFormat(outputFormat));
        }
        catch (Exception ex)
        {
            MSBuildLogFormatter.CreateMSBuildError("AVT-2", ex.Message, "assembly-versioning");
            logger.LogError("{exception}", ex.GetAllMessages());
            return -1;
        }
    }

    private static IOutputFormat GetOutputFormat(OutputFormat outputFormat)
    {
        return outputFormat switch
        {
            OutputFormat.Asciidoc => new AsciiOutputFormat(),
            OutputFormat.Markdown => new MarkdownOutputFormat(),
            _ => new XmlOutputFormat()
        };
    }
}