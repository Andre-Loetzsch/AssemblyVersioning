using Microsoft.Extensions.Logging;
using Oleander.Assembly.Comparers;
using Oleander.Assembly.Versioning.Tool.OutputFormats;

namespace Oleander.Assembly.Versioning.Tool;

internal class CompareAssembliesTool(ILogger<CompareAssembliesTool> logger)
{
    public int CompareAssemblies(FileInfo target1, FileInfo target2, IOutputFormat outputFormat)
    {
        if (!target1.Exists) return -1;
        if (!target2.Exists) return -2;

        var assemblyComparison = new AssemblyComparison(target1, target2, true);

        logger.LogInformation("recommended version change: {versionChange}", assemblyComparison.VersionChange);

        var output = outputFormat.Format(assemblyComparison);
        logger.LogInformation("Result: {output}", output);

        if (!string.IsNullOrEmpty(output)) Console.WriteLine(output);

        return 0;
    }
}
