namespace Oleander.AssemblyVersioning.ExternalProcesses;

public class GitGetHash : ExternalProcess
{
    public GitGetHash()
        : base("git", "describe --long --always --exclude=* --abbrev=8")
    {
    }
}