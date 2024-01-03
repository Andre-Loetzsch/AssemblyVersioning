namespace Oleander.Assembly.Versioning.ExternalProcesses;

internal class GitGetHash(string workingDirectory) : 
    ExternalProcess("git", "describe --long --always --exclude=* --abbrev=40", workingDirectory);