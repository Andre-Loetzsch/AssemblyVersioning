using System.IO;
using Xunit;

namespace Oleander.AssemblyVersioning.Test;

internal static class TestRunner
{
    public static IEnumerable<Version> RunSimulation(string simulationName)
    {
        var simulationSourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", simulationName);
        var simulationTargetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", "deploy", simulationName);

        if (!Directory.Exists(simulationSourceDir)) throw new DirectoryNotFoundException($"Directory '{simulationSourceDir}' not found!");
        if (Directory.Exists(simulationTargetDir)) Directory.Delete(simulationTargetDir, true);

        Directory.CreateDirectory(simulationTargetDir);

        foreach (var file in Directory.GetFiles(simulationSourceDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            if (file.EndsWith(".txt"))
            {
                File.Copy(file, file[..^4].Replace(simulationSourceDir, simulationTargetDir), true);
                continue;
            }

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

            foreach (var file in Directory.GetFiles(simulationTargetDir, "*.txt", SearchOption.AllDirectories))
            {
                var newFileName = file[..^4];

                if (File.Exists(newFileName))
                {
                    File.WriteAllText(newFileName, File.ReadAllText(file));
                    File.Delete(file);
                    continue;
                }

                File.Move(file, newFileName, true);
            }

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
           
            if (result.CalculatedVersion != null) yield return result.CalculatedVersion;
        }
    }
}