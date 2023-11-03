namespace Oleander.AssemblyVersioning.ExternalProcesses;

public class GitDiffNameOnly : ExternalProcess
{
    public GitDiffNameOnly(string sha)
        : base("git", $"diff --name-only {sha}")
    {
    }
}