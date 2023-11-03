using System.Reflection;
using Oleander.AssemblyVersioning.ExternalProcesses;

namespace Oleander.AssemblyVersioning;

internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 1) return -1;

        var targetPath = args[0];
        if (!File.Exists(targetPath)) return -1;

        var targetDir = Path.GetDirectoryName(targetPath);

        if (!Directory.Exists(targetDir)) return -1;
        if (!VSProject.TryFindVSProject(targetDir, out var projectDirName, out var projectFileName)) return -1;
        if (!VSProject.TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName)) return -1;

        Directory.SetCurrentDirectory(gitRepositoryDirName);

        var result = new GitGetHash().Start();
        if (result.ExitCode != 0) return result.ExitCode;
        if (string.IsNullOrEmpty(result.StandardOutput)) return -1;

        var gitHash = result.StandardOutput.Trim();

        result = new GitDiffNameOnly(gitHash).Start();
        if (result.ExitCode != 0) return result.ExitCode;

        var increaseMajor = false;
        var increaseRevision = false;
        var gitDiffExtensionList = new List<string> { ".cs", ".xaml" };
        var gitDiffFileExtensionPath = Path.Combine(projectDirName, ".gitdiff");

        if (File.Exists(gitDiffFileExtensionPath))
        {
            gitDiffExtensionList = File.ReadAllLines(gitDiffFileExtensionPath).ToList();
        }
        else
        {
            //File.WriteAllLines(gitDiffFileExtensionPath, gitDiffExtensionList);
        }

        var projectFiles = Directory.GetFiles(projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => gitDiffExtensionList.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x.Substring(gitRepositoryDirName.Length + 1).ToLower());

        var increaseBuild = result.StandardOutput != null && projectFiles.Any(projectFile => result.StandardOutput.ToLower().Contains(projectFile.Replace('\\', '/')));
        var versioningDir = Path.Combine(projectDirName, ".versioning");

        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var defaultRefVersionInfoFileName = Path.Combine(projectDirName, ".versionInfo");
        var refVersionInfoFileName = Path.Combine(versioningDir, string.Concat(Path.GetFileName(targetPath), $".{gitHash}.versionInfo"));
        var fileContent = CreateRefInfos(CreateAssembly(new FileInfo(targetPath))).ToList();
        var vsProject = new VSProject(projectFileName);
        var projectFileAssemblyVersion = vsProject.AssemblyVersion;

        if (projectFileAssemblyVersion == null)
        {
            projectFileAssemblyVersion = "0.0.0.0";
            vsProject.AssemblyVersion = projectFileAssemblyVersion;
            vsProject.SaveChanges();
        }

        fileContent.Insert(0, projectFileAssemblyVersion);

        if (!File.Exists(refVersionInfoFileName))
        {
            if (File.Exists(defaultRefVersionInfoFileName))
            {
                File.Copy(defaultRefVersionInfoFileName, refVersionInfoFileName);
            }
            else
            {
                File.WriteAllLines(refVersionInfoFileName, fileContent);
            }
        }

        var refList = File.ReadAllLines(refVersionInfoFileName).ToList();
        var refVersion = refList.First().Split('.', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var v) ? v : 0).ToList();

        while (refVersion.Count < 4)
        {
            refVersion.Add(0);
        }

        var major = refVersion[0];
        var minor = refVersion[1];
        var build = refVersion[2];
        var revision = refVersion[3];
        var assembly = CreateAssembly(new FileInfo(targetPath));
        var currentList = CreateRefInfos(assembly).ToList();

        for (var i = 1; i < refList.Count; i++)
        {
            var line = refList[i];
            if (currentList.Remove(line)) continue;
            increaseMajor = true;
        }

        var increaseMinor = currentList.Count > 0;
        var calculateVersion = CalculateVersion(major, minor, build, revision, increaseMajor, increaseMinor, increaseBuild, increaseRevision);
        var assemblyVersion = new Version(projectFileAssemblyVersion);
        var lastCalculateVersion = new Version(File.Exists(defaultRefVersionInfoFileName) ? 
            File.ReadAllLines(defaultRefVersionInfoFileName).FirstOrDefault() ?? calculateVersion.ToString() : 
            calculateVersion.ToString());

        fileContent[0] = calculateVersion.ToString();
        File.WriteAllLines(defaultRefVersionInfoFileName, fileContent);

        if (lastCalculateVersion < assemblyVersion) return 0;
        if (calculateVersion == assemblyVersion) return 0;

        WriteVersionFile(projectFileName, calculateVersion);
        return 0;
    }


    private static Version CalculateVersion(int major, int minor, int build, int revision,
        bool increaseMajor, bool increaseMinor, bool increaseBuild, bool increaseRevision)
    {
        // beta version
        if (increaseMajor && major == 0)
        {
            increaseMajor = false;
            increaseMinor = true;
        }
        // alpha version
        if (increaseMinor && major == 0 && minor == 0)
        {
            increaseMinor = false;
            increaseBuild = true;
        }

        if (increaseMajor)
        {
            major++;
            minor = 0;
            build = 0;
            revision = 0;
            increaseMinor = false;
            increaseBuild = false;
            increaseRevision = false;
        }

        if (increaseMinor)
        {
            minor++;
            build = 0;
            revision = 0;
            increaseBuild = false;
            increaseRevision = false;
        }

        if (increaseBuild)
        {
            build++;
            revision = 0;
            increaseRevision = false;
        }

        if (increaseRevision)
        {
            revision++;
        }

        return new Version(major, minor, build, revision);
    }


    private static Assembly CreateAssembly(FileInfo assemblyFileInfo)
    {
        var assemblyFile = assemblyFileInfo.FullName;

        AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
        {
            var fileName = args.Name.Split(", ", StringSplitOptions.RemoveEmptyEntries).First();
            var directoryName = assemblyFileInfo.DirectoryName;

            if (directoryName == null) return null;

            fileName = Directory.GetFiles(directoryName, $"{fileName}.*").FirstOrDefault();

            return fileName == null ? null : Assembly.Load(File.ReadAllBytes(fileName));
        };

        return Assembly.LoadFile(assemblyFile);
    }


    private static Version GetAssemblyVersion(Assembly assembly)
    {
        return assembly.GetName().Version ?? new Version(0, 0, 0, 0);
    }


    private static IEnumerable<string> CreateRefInfos(Assembly assembly)
    {
        //yield return GetAssemblyVersion(assembly).ToString();

        foreach (var result in assembly.Modules.Select(CreateRefInfo))
        {
            yield return result;
        }

        foreach (var exportedType in assembly.ExportedTypes)
        {
            foreach (var result in CreateRefInfo(exportedType))
            {
                yield return result;
            }
        }

        foreach (var assemblyName in assembly.GetReferencedAssemblies())
        {
            yield return $"referencedAssembly:{assemblyName.FullName}";
        }



        //foreach (var exportedType in assembly.GetCustomAttributesData())
        //{
        //    foreach (var result in CreateRefInfo(exportedType))
        //    {
        //        yield return result;
        //    }
        //}







        //result.Add($"{major}.{minor}.{build}.{revision}");
        //result.AddRange(assembly.Modules.Select(CreateModuleRefInfo));

        //foreach (var exportedType in assembly.ExportedTypes)
        //{
        //    result.AddRange(CreateTypeRefInfo(exportedType));
        //}

        //return result;
    }


    private static string CreateRefInfo(Module module)
    {
        return $"module:{module.Name}";
    }

    private static IEnumerable<string> CreateRefInfo(Type type)
    {
        var result = new List<string>
        {
            type.IsInterface ? 
                $"interface:{type.FullName}:{type.BaseType?.FullName}" : 
                $"type:{type.FullName}:{type.BaseType?.FullName}:{type.IsAbstract}"
        };

        result.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(CreateRefInfo));
        result.AddRange(type.GetMethods(BindingFlags.Static | BindingFlags.Public).Select(CreateRefInfo));

        result.Sort();

        return type.IsInterface ?
            new []{ string.Join('|', result) } :
            result.Where(x => !string.IsNullOrEmpty(x));
    }

    private static string CreateRefInfo(MethodInfo methodInfo)
    {
        if (methodInfo.DeclaringType == typeof(object)) return string.Empty;
        var parameters = string.Join(':', methodInfo.GetParameters().Select(CreateRefInfo));
        var genericArguments = string.Join(':', methodInfo.GetGenericArguments().Select(CreateRefInfo));

        return $"methodInfo:{methodInfo.DeclaringType}.{methodInfo.Name}:{methodInfo.ReturnType.FullName}:{methodInfo.IsAbstract}:{methodInfo.IsVirtual}:{methodInfo.IsStatic}:{parameters}:{genericArguments}";
    }


    private static string CreateRefInfo(ParameterInfo parameterInfo)
    {
        return $"{{parameterInfo:{parameterInfo.ParameterType}:{parameterInfo.Position}:{parameterInfo.IsOut}:{parameterInfo.IsIn}:{parameterInfo.IsOptional}:{parameterInfo.HasDefaultValue}:{parameterInfo.DefaultValue}}}";
    }

    private static string CreateRefInfo(Attribute attribute)
    {
        return $"{{attribute:{attribute.IsDefaultAttribute()}}}";
    }

    private static string CreateRefInfo(CustomAttributeData customAttributeData)
    {
        return $"{{customAttributeData:{customAttributeData.AttributeType}:{customAttributeData.Constructor}}}";
    }

    private static string CreateRefInfo(ConstructorInfo constructorInfo)
    {
        if (constructorInfo.IsPrivate) return string.Empty;

        return $"{{constructorInfo:{constructorInfo}:{constructorInfo.IsStatic}}}";
    }


    private static string CreateRefInfo(MemberInfo memberInfo)
    {
        return $"{{memberInfo:{memberInfo}}}";
    }




    private static void WriteVersionFile(string projectFileName, Version assemblyVersion)
    {
        var vsProject = new VSProject(projectFileName)
        {
            AssemblyVersion = assemblyVersion.ToString()
        };

        vsProject.SaveChanges();
    }


}
