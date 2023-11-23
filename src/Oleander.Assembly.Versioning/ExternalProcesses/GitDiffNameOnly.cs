namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitDiffNameOnly : ExternalProcess
{
    public GitDiffNameOnly(string sha, string workingDirectory)
        : base("git", $"diff --name-only {sha}", workingDirectory)
    {
    }
}