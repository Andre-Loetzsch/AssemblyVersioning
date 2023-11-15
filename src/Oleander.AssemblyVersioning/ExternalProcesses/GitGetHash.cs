namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitGetHash : ExternalProcess
{
    public GitGetHash()
        : base("git", "describe --long --always --exclude=* --abbrev=40")
    {
    }
}