using Microsoft.Extensions.Logging;
using Oleander.Assembly.Comparator;

namespace Oleander.Assembly.Versioning.Tool;

internal class CompareAssembliesTool(ILogger<CompareAssembliesTool> logger)
{
    public int CompareAssemblies(FileInfo target1, FileInfo target2)
    {
        if (!target1.Exists) return -1;
        if (!target2.Exists) return -2;

        var assemblyComparison = new AssemblyComparison(target1, target2);

        logger.LogInformation("recommended version change: {versionChange}", assemblyComparison.VersionChange);

        var xml = assemblyComparison.ToXml();

        if (xml != null) logger.LogInformation("{xml}", xml);

        return 0;
    }
}
