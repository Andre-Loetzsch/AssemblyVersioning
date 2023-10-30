namespace Oleander.AssemblyVersioning.ExternalProcesses;

public class GitGetStatus : ExternalProcess
{
    public GitGetStatus()
        : base("git", "status")
    {
    }
}