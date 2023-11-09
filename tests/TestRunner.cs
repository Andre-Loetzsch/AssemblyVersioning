using Xunit;

namespace Oleander.AssemblyVersioning.Test;

internal static class TestRunner
{
    public static IEnumerable<Version> RunSimulation(string simulationName)
    {
        var simulationSourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", simulationName);
        var simulationTargetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", simulationName, "deploy");

        if (Directory.Exists(simulationTargetDir)) Directory.Delete(simulationTargetDir, true);
        if (!Directory.Exists(simulationSourceDir)) throw new DirectoryNotFoundException($"Directory '{simulationSourceDir}' not found!");

        foreach (var directory in Directory.GetDirectories(simulationSourceDir))
        {
            Helper.CopyFilesRecursively(directory, simulationTargetDir);

            foreach (var file in Directory.GetFiles(simulationTargetDir, "*.txt", SearchOption.AllDirectories))
            {
                File.Move(file, file[..^4], true);
            }

            if (!Helper.TryFindCsProject(simulationTargetDir, out var projectDirName, out var projectFileName))
            {
                throw new Exception($"Simulation '{simulationName}' not found!");
            }

            var outDir = Path.Combine(projectDirName, "out");

            Helper.DotnetBuild(Path.Combine(projectDirName, Path.GetFileName(projectFileName)), outDir);
            
            var gitHashFileName = Path.Combine(projectDirName, ".gitHash");
            var gitChangesFileName = Path.Combine(projectDirName, ".gitChanges");
            var targetPath = Path.Combine(outDir, string.Concat(Path.GetFileNameWithoutExtension(projectFileName), ".dll"));
            var versioning = new TestVersioning();

            if (File.Exists(gitHashFileName))
            {
                versioning.GitHash = File.ReadAllLines(gitHashFileName).FirstOrDefault();
            }

            if (File.Exists(gitChangesFileName))
            {
                versioning.GitChanges.AddRange(File.ReadAllLines(gitChangesFileName));
            }

            var result = versioning.UpdateAssemblyVersion(targetPath);
            Assert.Equal(VersioningErrorCodes.Success, result.ErrorCode);
           
            if (result.CalculatedVersion != null) yield return result.CalculatedVersion;
        }
    }
}