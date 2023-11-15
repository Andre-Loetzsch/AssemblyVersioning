namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitDiffNameOnly : ExternalProcess
{
    public GitDiffNameOnly(string sha)
        : base("git", $"diff --name-only {sha}")
    {
    }
}