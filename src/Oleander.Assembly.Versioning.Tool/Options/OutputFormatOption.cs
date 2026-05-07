using System.CommandLine;
using Oleander.Assembly.Versioning.Tool.OutputFormats;

namespace Oleander.Assembly.Versioning.Tool.Options;

internal class OutputFormatOption : Option<OutputFormat>
{
    public OutputFormatOption() : base(name: "--output", "-o")
    {
        this.Description = "Output format";
    }
}