namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitGetHash : ExternalProcess
{
    public GitGetHash(string workingDirectory)
        : base("git", "describe --long --always --exclude=* --abbrev=40", workingDirectory)
    {
    }
}