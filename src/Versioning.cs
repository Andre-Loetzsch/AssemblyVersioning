using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Oleander.AssemblyVersioning.ExternalProcesses;

namespace Oleander.AssemblyVersioning;

internal class Versioning
{
    private readonly string _targetFileName;
    private readonly string _projectDirName;
    private readonly string _projectFileName;
    private readonly string _gitRepositoryDirName;

    private readonly string _currentDirectory = Directory.GetCurrentDirectory();


    public VersioningErrorCodes ErrorCode { get; private set; }

    public Version? CalculatedVersion { get; private set; }

    public ExternalProcessResult? ExternalProcessResult { get; private set; }


    #region constructors

    public Versioning(string targetFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = string.Empty;
        this._projectFileName = string.Empty;
        this._gitRepositoryDirName = string.Empty;

        if (!File.Exists(targetFileName))
        {
            this.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return;
        }

        var targetDir = Path.GetDirectoryName(targetFileName);

        if (targetDir == null || Directory.Exists(targetDir))
        {
            this.ErrorCode = VersioningErrorCodes.TargetDirNotExist;
            return;
        }

        if (!VSProject.TryFindVSProject(targetDir, out var projectDirName, out var projectFileName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return;
        }

        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            this.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return;
        }
        
        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            this.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return;
        }

        if (!File.Exists(this._projectFileName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            this.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return;
        }
    }

    public Versioning(string targetFileName, string projectDirName, string projectFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = string.Empty;

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            this.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return;
        }
      
        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            this.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return;
        }

        if (!File.Exists(this._projectFileName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            this.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return;
        }
    }

    public Versioning(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            this.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return;
        }

        if (!File.Exists(this._projectFileName))
        {
            this.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            this.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return;
        }
    }

    #endregion

    public void CalculateAssemblyVersion()
    {
        if (!this.TryGetGitHash(out var result, out var gitHash))
        {
            this.ExternalProcessResult = result;
            this.ErrorCode = VersioningErrorCodes.GetGitHashFailed;
            return;
        }

        if (!this.TryGetGitChanges(gitHash, out result, out var gitChanges))
        {
            this.ExternalProcessResult = result;
            this.ErrorCode = VersioningErrorCodes.GetGitDiffNameOnlyFailed;
            return;
        }

        var increaseMajor = false;
        var increaseRevision = false;
        var increaseBuild = this.IncreaseBuild(gitChanges);
        var versioningDir = Path.Combine(this._projectDirName, ".versioning");

        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var refVersionInfoFileName = Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName), $".{gitHash}.versionInfo"));
        var defaultRefVersionInfoFileName = Path.Combine(this._projectDirName, ".versionInfo");
        var projectAssemblyVersion = this.GetAssemblyVersionFromProjectFile();
        var fileContent = CreateRefInfos(CreateAssembly(new FileInfo(this._targetFileName))).ToList();


        fileContent.Insert(0, projectAssemblyVersion.ToString());

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
        var refVersion = refList.Any() && Version.TryParse(refList[0], out var rv) ? rv : new Version(0, 0, 0, 0);
        var assembly = CreateAssembly(new FileInfo(this._targetFileName));
        var currentList = CreateRefInfos(assembly).ToList();

        for (var i = 1; i < refList.Count; i++)
        {
            var line = refList[i];
            if (currentList.Remove(line)) continue;
            increaseMajor = true;
        }

        var increaseMinor = currentList.Count > 0;
        var calculateVersion = CalculateVersion(refVersion, increaseMajor, increaseMinor, increaseBuild, increaseRevision);
        var assemblyVersion = projectAssemblyVersion;
        var lastCalculateVersion = new Version(File.Exists(defaultRefVersionInfoFileName) ? File.ReadAllLines(defaultRefVersionInfoFileName).FirstOrDefault() ?? calculateVersion.ToString() : calculateVersion.ToString());

        fileContent[0] = calculateVersion.ToString();
        File.WriteAllLines(defaultRefVersionInfoFileName, fileContent);

        if (lastCalculateVersion < assemblyVersion) return;
        if (calculateVersion == assemblyVersion) return;

        WriteVersionFile(this._projectFileName, calculateVersion);
    }


    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string hash)
    {
        hash = null;
        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitGetHash().Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        hash = result.StandardOutput;

        return true;
    }

    protected virtual bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        gitChanges = null;

        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitDiffNameOnly(gitHash).Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        gitChanges = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return true;
    }

    protected virtual string[]  GetGitDiffFiler()
    {
        if (Directory.Exists(this._projectDirName) || 
            File.Exists(Path.Combine(this._projectDirName, ".gitdiff")))
        {
            return File.ReadAllLines(Path.Combine(this._projectDirName, ".gitdiff"));
        }

        return new [] { ".cs", ".xaml" };
    }

    #endregion


    #region private members

    private bool IncreaseBuild(IEnumerable<string> gitChanges)
    {
        var gitDiffFilter = this.GetGitDiffFiler();
        var projectFiles = Directory.GetFiles(this._projectDirName, "*.*", SearchOption.AllDirectories)
            .Where(x => gitDiffFilter.Contains(Path.GetExtension(x).ToLower()))
            .Select(x => x[(this._gitRepositoryDirName.Length + 1)..].Replace('\\', '/'));

        return projectFiles.Any(
            projectFile => gitChanges.Any(x => string.Equals(x, projectFile, StringComparison.InvariantCultureIgnoreCase)));

    }

    private Version GetAssemblyVersionFromProjectFile()
    {
        var vsProject = new VSProject(this._projectFileName);
        var projectFileAssemblyVersion = vsProject.AssemblyVersion;

        if (projectFileAssemblyVersion != null &&
            Version.TryParse(projectFileAssemblyVersion, out var version)) return version;

        version = new Version(0, 0, 0, 0);
        vsProject.AssemblyVersion = version.ToString();
        vsProject.SaveChanges();

        return version;
    }

    private static bool TryFindGitRepositoryDirName(string? startDirectory, out string gitRepositoryDirName)
    {
        gitRepositoryDirName = string.Empty;
        if (string.IsNullOrEmpty(startDirectory)) return false;
        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            if (parentDir.GetDirectories(".git").Any())
            {
                gitRepositoryDirName = parentDir.FullName;
                return true;
            }

            parentDir = parentDir.Parent;
        }

        return false;
    }

    private static Version CalculateVersion(Version version, bool increaseMajor, bool increaseMinor, bool increaseBuild, bool increaseRevision)
    {

        var major = version.Major;
        var minor = version.Minor;
        var build = version.Build;
        var revision = version.Revision;


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
        var result = new List<string> { $"type:{type.FullName}:{type.BaseType?.FullName}:{type.IsAbstract}:{type.IsInterface}:{type.IsEnum}" };

        result.Sort();

        if (type.IsEnum)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var b = type.GetMethod("HasFlag", BindingFlags.Instance | BindingFlags.Public);
            var values = type.GetEnumValues();

            var names = Enum.GetNames(type);
            var values2 = Enum.GetValues(type);
            var value3 = GetEnumValues(type).ToList();

        }
        else
        {
            result.AddRange(type.GetMethods(BindingFlags.Instance | BindingFlags.Public).Select(CreateRefInfo));
            result.AddRange(type.GetMethods(BindingFlags.Static | BindingFlags.Public).Select(CreateRefInfo));
            result.AddRange(type.GetInterfaces().Select(@interface => $"type:{type.FullName}:interface:{@interface.FullName}, {@interface.Assembly.GetName().Name}"));
        }


        return type.IsInterface || type.IsEnum ? new[] { string.Join('|', result) } : result.Where(x => !string.IsNullOrEmpty(x));
    }

    private static string CreateRefInfo(MethodInfo methodInfo)
    {
        if (methodInfo.DeclaringType == typeof(object)) return string.Empty;
        var parameters = string.Join(':', methodInfo.GetParameters().Select(CreateRefInfo));
        var genericArguments = string.Join(':', methodInfo.GetGenericArguments().Select(CreateRefInfo));

        return $"methodInfo:{methodInfo.DeclaringType}.{methodInfo.Name}:{methodInfo.ReturnType.FullName}:{methodInfo.IsAbstract}:{methodInfo.IsStatic}:{parameters}:{genericArguments}";
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

    private static IEnumerable<object> GetEnumValues(Type enumType)
    {

        System.Type enumUnderlyingType = System.Enum.GetUnderlyingType(enumType);
        System.Array enumValues = System.Enum.GetValues(enumType);

        for (int i = 0; i < enumValues.Length; i++)
        {
            // Retrieve the value of the ith enum item.
            object value = enumValues.GetValue(i);

            // Convert the value to its underlying type (int, byte, long, ...)
            yield return System.Convert.ChangeType(value, enumUnderlyingType);

        }
    }

    private static void WriteVersionFile(string projectFileName, Version assemblyVersion)
    {
        var vsProject = new VSProject(projectFileName)
        {
            AssemblyVersion = assemblyVersion.ToString()
        };

        vsProject.SaveChanges();
    }

    #endregion
}