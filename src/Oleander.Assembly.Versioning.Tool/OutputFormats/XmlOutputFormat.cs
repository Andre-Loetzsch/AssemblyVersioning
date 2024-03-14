using Oleander.Assembly.Comparers;

namespace Oleander.Assembly.Versioning.Tool.OutputFormats;

internal class XmlOutputFormat : IOutputFormat
{
    public string Format(AssemblyComparison assemblyComparison)
    {
        return assemblyComparison.ToXml();
    }
}