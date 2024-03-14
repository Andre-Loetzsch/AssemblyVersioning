using Oleander.Assembly.Comparers;

namespace Oleander.Assembly.Versioning.Tool.OutputFormats;

internal interface IOutputFormat
{
    string Format(AssemblyComparison assemblyComparison);
}