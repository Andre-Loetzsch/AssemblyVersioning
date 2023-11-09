using System.ComponentModel;
using System.Diagnostics;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

internal static class Helper
{
    public static bool TryFindCsProject(string startDirectory, out string projectDirName, out string projectFileName)
    {
        return VSProject.TryFindVSProject(startDirectory, out projectDirName, out projectFileName);
    }

    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);

        //Now Create all of the directories
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }

    public static void DotnetBuild(string projectFileName, string outPath)
    {
        var p = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                FileName = "dotnet",
                Arguments = $"build {projectFileName} -c Debug -o {outPath}",
                CreateNoWindow = true,
                ErrorDialog = false
            }
        };

        if (!p.Start())
        {
            throw new Win32Exception("The process did not start!");
        }

        if (!p.WaitForExit(30000))
        {
            p.Kill();
            throw new Win32Exception("The process did not exit!");
        }

        if (p.ExitCode == 0) return;
        var error = p.StandardError.ReadToEnd();
        throw new Win32Exception(p.ExitCode, error);
    }
}