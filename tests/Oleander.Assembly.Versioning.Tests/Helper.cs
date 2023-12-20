using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Oleander.Assembly.Versioning.Tests;

internal static class Helper
{
    public static bool TryFindCsProject(string startDirectory, [MaybeNullWhen(false)] out string projectDirName, [MaybeNullWhen(false)] out string projectFileName)
    {
        return MSBuildProject.TryFindVSProject(startDirectory, out projectDirName, out projectFileName);
    }

    public static bool TryGetVersionFromProjectFile(string projectFileName, [MaybeNullWhen(false)] out Version version)
    {
        version = null;
        var project = new MSBuildProject(projectFileName);
        return project.AssemblyVersion != null && Version.TryParse(project.AssemblyVersion, out version);
    }

    public static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        if (!Directory.Exists(targetPath)) Directory.CreateDirectory(targetPath);
       
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        foreach (var sourceFile in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            var targetFile = sourceFile.Replace(sourcePath, targetPath);

            if (File.Exists(targetFile))
            {
                File.WriteAllText(targetFile, File.ReadAllText(sourceFile));
                continue;
            }

            File.Copy(sourceFile, targetFile, true);
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


        var error = p.StandardError.ReadToEnd();
        var msg = p.StandardOutput.ReadToEnd();


        if (!p.WaitForExit(30000))
        {
            p.Kill();
            throw new Win32Exception("The process did not exit!");
        }

        if (p.ExitCode == 0) return;

        error = string.IsNullOrEmpty(error) ? msg : error;
        throw new Win32Exception(p.ExitCode, error);
    }
}