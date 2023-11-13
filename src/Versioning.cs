using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Oleander.AssemblyVersioning.ExternalProcesses;

namespace Oleander.AssemblyVersioning;

public class Versioning
{
    private const string projectVersionInfoFileName = ".versionInfo";
    private string _targetFileName = string.Empty;
    private string _projectDirName = string.Empty;
    private string _projectFileName = string.Empty;
    private string _gitRepositoryDirName = string.Empty;

    private readonly string _currentDirectory = Directory.GetCurrentDirectory();

    #region UpdateAssemblyVersion

    public VersioningResult UpdateAssemblyVersion(string targetFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = string.Empty;
        this._projectFileName = string.Empty;
        this._gitRepositoryDirName = string.Empty;

        var updateResult = new VersioningResult();

        if (!File.Exists(targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        var targetDir = Path.GetDirectoryName(targetFileName);

        if (targetDir == null || !Directory.Exists(targetDir))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetDirNotExist;
            return updateResult;
        }

        if (!VSProject.TryFindVSProject(targetDir, out var projectDirName, out var projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = string.Empty;

        var updateResult = new VersioningResult();

        if (!TryFindGitRepositoryDirName(projectDirName, out var gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        this._gitRepositoryDirName = gitRepositoryDirName;

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    public VersioningResult UpdateAssemblyVersion(string targetFileName, string projectDirName, string projectFileName, string gitRepositoryDirName)
    {
        this._targetFileName = targetFileName;
        this._projectDirName = projectDirName;
        this._projectFileName = projectFileName;
        this._gitRepositoryDirName = gitRepositoryDirName;

        var updateResult = new VersioningResult();

        if (!File.Exists(this._targetFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.TargetFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._projectDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectDirNotExist;
            return updateResult;
        }

        if (!File.Exists(this._projectFileName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.ProjectFileNotExist;
            return updateResult;
        }

        if (!Directory.Exists(this._gitRepositoryDirName))
        {
            updateResult.ErrorCode = VersioningErrorCodes.GitRepositoryDirNotExist;
            return updateResult;
        }

        return this.PrivateUpdateAssemblyVersion();
    }

    #endregion

    #region protected virtual

    protected virtual bool TryGetGitHash(out ExternalProcessResult result, [MaybeNullWhen(false)] out string hash)
    {
        hash = null;
        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitGetHash().Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput)) return false;

        hash = result.StandardOutput.Trim();

        return true;
    }

    protected virtual bool TryGetGitChanges(string gitHash, out ExternalProcessResult result, [MaybeNullWhen(false)] out string[] gitChanges)
    {
        gitChanges = null;

        Directory.SetCurrentDirectory(this._gitRepositoryDirName);

        result = new GitDiffNameOnly(gitHash).Start();

        Directory.SetCurrentDirectory(this._currentDirectory);

        if (result.ExitCode != 0) return false;
        if (string.IsNullOrEmpty(result.StandardOutput))
        {
            gitChanges = Array.Empty<string>();
            return true;
        }

        gitChanges = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        return true;
    }

    protected virtual string[] GetGitDiffFiler()
    {
        if (Directory.Exists(this._projectDirName) &&
            File.Exists(Path.Combine(this._projectDirName, ".gitdiff")))
        {
            return File.ReadAllLines(Path.Combine(this._projectDirName, ".gitdiff"));
        }

        return new[] { ".cs", ".xaml" };
    }

    #endregion

    #region internal / private members

    internal static Version CalculateVersion(Version version, bool increaseMajor, bool increaseMinor, bool increaseBuild, bool increaseRevision)
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

    private VersioningResult PrivateUpdateAssemblyVersion()
    {
        var updateResult = new VersioningResult();

        if (!this.TryGetGitHash(out var result, out var gitHash))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitHashFailed;
            return updateResult;
        }

        if (!this.TryGetGitChanges(gitHash, out result, out var gitChanges))
        {
            updateResult.ExternalProcessResult = result;
            updateResult.ErrorCode = VersioningErrorCodes.GetGitDiffNameOnlyFailed;
            return updateResult;
        }

        var refVersionFileContent = this.GetRefVersionFileContent(gitHash).ToList();
        var assemblyVersion = this.GetAssemblyVersionFromProjectFile();
        var refVersion = TryGetRefVersion(refVersionFileContent, out var v) ? v : assemblyVersion;
        var assembly = CreateAssembly(new FileInfo(this._targetFileName));
        var currentVersionList = CreateRefInfos(assembly).ToList();
        var currentVersionTempList = currentVersionList.ToList();
        var increaseMajor = false;

        RemoveRefVersion(refVersionFileContent);

        foreach (var line in refVersionFileContent)
        {
            if (currentVersionTempList.Remove(line)) continue;
            increaseMajor = true;
        }

        var increaseMinor = refVersionFileContent.Count > 0 && currentVersionTempList.Count > 0;
        var increaseBuild = this.IncreaseBuild(gitChanges);

        updateResult.CalculatedVersion = CalculateVersion(refVersion, increaseMajor, increaseMinor, increaseBuild, false);

        this.CreateRefVersionFileIfNotExist(gitHash,
                this.WriteProjectVersionInfoFile(currentVersionList, assemblyVersion > updateResult.CalculatedVersion ? assemblyVersion : updateResult.CalculatedVersion));

        if (assemblyVersion > updateResult.CalculatedVersion) return updateResult;

        WriteVersionFile(this._projectFileName, updateResult.CalculatedVersion);

        return updateResult;
    }

    private IEnumerable<string> GetRefVersionFileContent(string gitHash)
    {
        var versioningDir = Path.Combine(this._projectDirName, ".versioning");
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var refVersionInfoFileName = Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName), $".{gitHash}.versionInfo"));

        if (File.Exists(refVersionInfoFileName)) return File.ReadLines(refVersionInfoFileName);

        var defaultRefVersionInfoFileName = Path.Combine(this._projectDirName, projectVersionInfoFileName);

        if (!File.Exists(defaultRefVersionInfoFileName)) return Enumerable.Empty<string>();
        File.Copy(defaultRefVersionInfoFileName, refVersionInfoFileName);
        return File.ReadLines(refVersionInfoFileName);

    }

    private void CreateRefVersionFileIfNotExist(string gitHash, IEnumerable<string> fileContent)
    {
        var versioningDir = Path.Combine(this._projectDirName, ".versioning");
        if (!Directory.Exists(versioningDir)) Directory.CreateDirectory(versioningDir);

        var refVersionInfoFileName = Path.Combine(versioningDir, string.Concat(Path.GetFileName(this._targetFileName), $".{gitHash}.versionInfo"));

        if (!File.Exists(refVersionInfoFileName)) File.WriteAllLines(refVersionInfoFileName, fileContent);
    }

    private static bool TryGetRefVersion(IEnumerable<string> refVersionFileContent, [MaybeNullWhen(false)] out Version version)
    {
        version = null;
        var firstLine = refVersionFileContent.FirstOrDefault();
        return firstLine != null && Version.TryParse(firstLine, out version);
    }

    private static void RemoveRefVersion(IList<string> refVersionFileContent)
    {
        var firstLine = refVersionFileContent.FirstOrDefault();

        if (firstLine != null && Version.TryParse(firstLine, out _))
        {
            refVersionFileContent.RemoveAt(0);
        }
    }

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

    private IEnumerable<string> WriteProjectVersionInfoFile(IList<string> fileContent, Version currentVersion)
    {
        var defaultRefVersionInfoFileName = Path.Combine(this._projectDirName, projectVersionInfoFileName);

        fileContent.Insert(0, currentVersion.ToString());
        File.WriteAllLines(defaultRefVersionInfoFileName, fileContent);
        return fileContent;
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

    private static Assembly CreateAssembly(FileInfo assemblyFileInfo)
    {
        var assemblyFile = assemblyFileInfo.FullName;

        AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
        {
            var fileName = args.Name.Split(", ", StringSplitOptions.RemoveEmptyEntries).First();
            var directoryName = assemblyFileInfo.DirectoryName;

            if (directoryName == null) return null;

            fileName = Directory.GetFiles(directoryName, $"{fileName}.*").FirstOrDefault();

            return fileName == null ? null : Assembly.Load(File.ReadAllBytes(fileName));
        };

        return Assembly.LoadFile(assemblyFile);
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

    internal static IEnumerable<string> CreateRefInfo(Type type)
    {
        if (!type.IsPublic) return Enumerable.Empty<string>();

        var result = new List<string> { $"type:{type.FullName}:{type.BaseType?.FullName}:{type.IsAbstract}:{type.IsInterface}:{type.IsEnum}" };

        result.Sort();

        if (type.IsEnum)
        {
            var hasFlag = type.GetCustomAttributes(true).OfType<FlagsAttribute>().Any();

            var enumNames = Enum.GetNames(type);
            var enumValues = GetEnumValues(type).ToArray();

            result.AddRange(enumNames.Select((t, i) => $"enum:{type.FullName}:{hasFlag}:{t}:{enumValues[i]}"));

            return result;
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
        var enumUnderlyingType = Enum.GetUnderlyingType(enumType);
        var enumValues = Enum.GetValues(enumType);

        for (var i = 0; i < enumValues.Length; i++)
        {
            var value = enumValues.GetValue(i);
            if (value == null) continue;
            yield return Convert.ChangeType(value, enumUnderlyingType);
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