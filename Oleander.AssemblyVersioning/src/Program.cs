using System.Reflection;
using Versioning.ExternalProcesses;

namespace Versioning;

internal class Program
{
    static int Main(string[] args)
    {

        var dir = AppDomain.CurrentDomain.BaseDirectory;

        if (args.Length == 1)
        {
            dir = Path.GetDirectoryName(args[0]);
        }

        if (dir == null) return -1;

        if (!VSProject.TryFindVSProject(dir, out var projectDirName, out var projectFileName)) return -1;
        if (!VSProject.TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName)) return -1;

        Directory.SetCurrentDirectory(gitRepositoryDirName);

        var result = new GitGetStatus().Start();
        if (result.ExitCode != 0) return result.ExitCode;


        var increaseMajor = false;
        var increaseRevision = false;
        var extensionList = new List<string> { ".xaml" };
        var projectFiles = Directory.GetFiles(projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => extensionList.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x.Substring(gitRepositoryDirName.Length + 1).ToLower());

        var increaseBuild = projectFiles.Any(projectFile => result.StandardOutput.ToLower().Contains(projectFile.Replace('\\', '/')));

        result = new GitGetHash().Start();
        if (result.ExitCode != 0) return result.ExitCode;

        var versioningPath = Path.Combine(projectDirName, ".versioning", result.StandardOutput.Trim());
        var assemblyName = Path.GetFileNameWithoutExtension(projectFileName);
        var assemblyFile = Directory.GetFiles(Path.Combine(projectDirName, "bin", "Debug"), $"{assemblyName}.dll", SearchOption.AllDirectories).FirstOrDefault() ??
                           Directory.GetFiles(Path.Combine(projectDirName, "bin", "Debug"), $"{assemblyName}.exe", SearchOption.AllDirectories).FirstOrDefault();

        if (assemblyFile == null) return -1;

        var refVersionInfoFileName = Path.Combine(versioningPath, string.Concat(Path.GetFileName(assemblyFile), ".versionInfo.txt"));

        if (!File.Exists(refVersionInfoFileName))
        {
            Directory.CreateDirectory(versioningPath);
            File.WriteAllLines(refVersionInfoFileName, CreateRefInfos(CreateAssembly(new FileInfo(assemblyFile))));
            return 0;
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
        var assembly = CreateAssembly(new FileInfo(assemblyFile));

        var currentList = CreateRefInfos(assembly).ToList();

        foreach (var line in refList)
        {
            if (currentList.Remove(line)) continue;
            increaseMajor = true;
        }

        var increaseMinor = currentList.Count > 0;

        if (new Version(major, minor, build, revision) < GetAssemblyVersion(assembly)) return 0;

        var calculateVersion = CalculateVersion(major, minor, build, revision, increaseMajor, increaseMinor, increaseBuild, increaseRevision);
        WriteVersionFile(projectFileName, calculateVersion);
        return 0;

    }


    private static Version CalculateVersion(int major, int minor, int build, int revision, 
        bool increaseMajor, bool  increaseMinor, bool increaseBuild, bool increaseRevision)
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




    private static string CompareAssemblies(string refAssemblyFile, string assemblyFile)
    {

        var refAssembly = Assembly.LoadFile(refAssemblyFile);
        var assembly = Assembly.LoadFile(assemblyFile);
        var refAssemblyVersion = refAssembly.GetName().Version;
        var major = refAssemblyVersion?.Major ?? 0;
        var minor = refAssemblyVersion?.Minor ?? 0;
        var build = refAssemblyVersion?.Build ?? 0;
        var revision = refAssemblyVersion?.Revision ?? 0;





        return $"{major}.{minor}.{build}.{revision}";
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
        yield return GetAssemblyVersion(assembly).ToString();

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
            yield return assemblyName.FullName;
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
        var result = new List<string> { $"type:{type.FullName}:{type.BaseType?.FullName}:{type.IsAbstract}" };
        result.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(CreateRefInfo));
        result.AddRange(type.GetMethods(BindingFlags.Static | BindingFlags.Public).Select(CreateRefInfo));

        return result;
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
