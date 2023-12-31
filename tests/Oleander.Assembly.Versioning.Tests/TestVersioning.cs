﻿using Oleander.Assembly.Versioning.ExternalProcesses;

namespace Oleander.Assembly.Versioning.Tests;

internal class TestVersioning() : Versioning(new NullLogger())
{
    protected override bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, out string[] gitChanges)
    {
        result = new ExternalProcessResult("test.exe", "get git-changes");
        gitChanges = this.GitChanges.ToArray();
        return true;
    }

    protected override bool TryGetGitHash(out ExternalProcessResult result, out string gitHash)
    {
        result = new ExternalProcessResult("test.exe", "get git-hash");
        gitHash = this.GitHash ?? string.Empty;

        return !string.IsNullOrEmpty(this.GitHash);
    }


    //protected override string[] GetGitDiffFilter()
    //{
    //    return base.GetGitDiffFilter();
    //}

    protected override bool TryDownloadNugetPackage(string outDir)
    {
        return false;
    }

    public List<string> GitChanges { get; } = new List<string>();

    public string? GitHash { get; set; } 
}

