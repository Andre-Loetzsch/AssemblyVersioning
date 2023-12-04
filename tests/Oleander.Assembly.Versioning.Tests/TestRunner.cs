using Xunit;

namespace Oleander.Assembly.Versioning.Tests;

internal class TestRunner
{

    public TestRunner(string simulationName)
    {
        this.SimulationName = simulationName;
        this.GitDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", ".git");
        this.SimulationSourceDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", simulationName);
        this.SimulationTargetDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Simulations", "deploy", simulationName);

        if (!Directory.Exists(this.SimulationSourceDir)) throw new DirectoryNotFoundException($"Directory '{this.SimulationSourceDir}' not found!");
        if (Directory.Exists(this.SimulationTargetDir)) Directory.Delete(this.SimulationTargetDir, true);

        if (!Directory.Exists(this.GitDir)) Directory.CreateDirectory(this.GitDir);

        Directory.CreateDirectory(this.SimulationTargetDir);

        foreach (var file in Directory.GetFiles(this.SimulationSourceDir, "*.*", SearchOption.TopDirectoryOnly))
        {
            File.Copy(file, file.Replace(this.SimulationSourceDir, this.SimulationTargetDir), true);
        }
    }


    public string SimulationName { get; }

    public string GitDir { get; }

    public string SimulationSourceDir { get; }
    public string SimulationTargetDir { get; }


    public IEnumerable<Version> RunSimulation()
    {
        if (!Helper.TryFindCsProject(this.SimulationTargetDir, out var projectDirName, out var projectFileName))
        {
            throw new Exception($"Simulation '{this.SimulationName}' not found!");
        }

        var gitHashFileName = Path.Combine(projectDirName, ".gitHash");
        var gitChangesFileName = Path.Combine(projectDirName, ".gitChanges");
        var versioning = new TestVersioning();

        foreach (var directory in Directory.GetDirectories(this.SimulationSourceDir).Order().Select(x => new DirectoryInfo(x)))
        {
            Helper.CopyFilesRecursively(directory.FullName, this.SimulationTargetDir);

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

    public MSBuildProject CreateMSBuildProject()
    {
        if (!Helper.TryFindCsProject(this.SimulationTargetDir, out _, out var projectFileName))
        {
            throw new Exception($"Simulation '{this.SimulationName}' not found!");
        }

        foreach (var directory in Directory.GetDirectories(this.SimulationSourceDir).Order().Select(x => new DirectoryInfo(x)))
        {
            Helper.CopyFilesRecursively(directory.FullName, Path.Combine(this.SimulationTargetDir, directory.Name));
        }

        return new MSBuildProject(projectFileName);
    }
}