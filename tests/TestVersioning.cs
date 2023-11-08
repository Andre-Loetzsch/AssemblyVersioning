using Oleander.AssemblyVersioning.ExternalProcesses;
using System.Diagnostics.CodeAnalysis;

namespace Oleander.AssemblyVersioning.Test;

internal class TestVersioning : Versioning
{
    public TestVersioning(string targetFileName) : base(targetFileName)
    {
    }

    public TestVersioning(string targetFileName, string projectDirName, string projectFileName) : 
        base(targetFileName, projectDirName, projectFileName)
    {
    }

    public TestVersioning(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName) : 
        base(targetFileName, projectDirName, projectFileName, gitRepositoryDirName)
    {
    }

    protected override bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        result = new ExternalProcessResult("test.exe", "get git-changes");
        gitChanges = this.GitChanges.ToArray();
        return this.GitChanges.Count > 0;
    }

    protected override bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string hash)
    {
        result = new ExternalProcessResult("test.exe", "get git-hash");
        hash = this.GitHash;
        return string.IsNullOrEmpty(this.GitHash);
    }

    public List<string> GitChanges { get; } = new List<string>();

    public string? GitHash { get; set; } 
}