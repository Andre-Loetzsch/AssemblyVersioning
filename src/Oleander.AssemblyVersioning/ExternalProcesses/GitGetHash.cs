using System.Runtime.CompilerServices;

namespace Oleander.AssemblyVersioning.ExternalProcesses;

internal class GitGetHash : ExternalProcess
{
    public GitGetHash()
        : base("git", "describe --long --always --exclude=* --abbrev=40")
    {
    }
}