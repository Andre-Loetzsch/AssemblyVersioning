namespace Versioning.ExternalProcesses;

public class GitGetStatus : ExternalProcess
{
    public GitGetStatus()
        : base("git", "status")
    {
    }
}