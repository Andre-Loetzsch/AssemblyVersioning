namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitDiffNameOnly(string sha, string workingDirectory) : 
    ExternalProcess("git", $"--no-pager diff --name-only {sha}", workingDirectory);