using System.Diagnostics.CodeAnalysis;
using Oleander.Assembly.Versioning.ExternalProcesses;

namespace Oleander.Assembly.Versioning.Tests;

internal class TestVersioning : Versioning
{
    protected override bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        result = new ExternalProcessResult("test.exe", "get git-changes");
        gitChanges = this.GitChanges.ToArray();
        return true;
    }

    protected override bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string hash)
    {
        result = new ExternalProcessResult("test.exe", "get git-hash");
        hash = this.GitHash;
        return !string.IsNullOrEmpty(this.GitHash);
    }

    public List<string> GitChanges { get; } = new List<string>();

    public string? GitHash { get; set; } 
}