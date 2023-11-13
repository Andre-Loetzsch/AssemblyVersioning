using Microsoft.VisualBasic;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

internal static class TestRunner
{
    public static IEnumerable<Version> RunSimulation(string simulationName)
    {
        var gitDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", ".git");
        var simulationSourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", simulationName);
        var simulationTargetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", "deploy", simulationName);

        if (!Directory.Exists(simulationSourceDir)) throw new DirectoryNotFoundException($"Directory '{simulationSourceDir}' not found!");
        if (Directory.Exists(simulationTargetDir)) Directory.Delete(simulationTargetDir, true);

        if (!Directory.Exists(gitDir)) Directory.CreateDirectory(gitDir);

        Directory.CreateDirectory(simulationTargetDir);

        foreach (var file in Directory.GetFiles(simulationSourceDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(file, file.Replace(simulationSourceDir, simulationTargetDir), true);
        }

        if (!Helper.TryFindCsProject(simulationTargetDir, out var projectDirName, out var projectFileName))
        {
            throw new Exception($"Simulation '{simulationName}' not found!");
        }
        
        var gitHashFileName = Path.Combine(projectDirName, ".gitHash");
        var gitChangesFileName = Path.Combine(projectDirName, ".gitChanges");
        var versioning = new TestVersioning();

        foreach (var directory in Directory.GetDirectories(simulationSourceDir).Select(x => new DirectoryInfo(x)))
        {
            Helper.CopyFilesRecursively(directory.FullName, simulationTargetDir);

            var outDir = Path.Combine(projectDirName, "out", directory.Name);
            var targetPath = Path.Combine(outDir, string.Concat(Path.GetFileNameWithoutExtension(projectFileName), ".dll"));

            Helper.DotnetBuild(projectFileName, outDir);

            if (File.Exists(gitHashFileName))
            {
                versioning.GitHash = File.ReadAllLines(gitHashFileName).FirstOrDefault();
                //File.Delete(gitHashFileName);
            }

            if (File.Exists(gitChangesFileName))
            {
                versioning.GitChanges.AddRange(File.ReadAllLines(gitChangesFileName));
                File.Delete(gitChangesFileName);
            }

            var result = versioning.UpdateAssemblyVersion(targetPath);
            Assert.Equal(VersioningErrorCodes.Success, result.ErrorCode);
           
            if (Helper.TryGetVersionFromProjectFile(projectFileName, out var version)) yield return version;
        }
    }
}