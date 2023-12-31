﻿using Oleander.Assembly.Comparers.Cecil.Extensions;
/*Telerik Authorship*/
using AssemblyPathName = System.Collections.Generic.KeyValuePair<Oleander.Assembly.Comparers.Cecil.AssemblyResolver.AssemblyStrongNameExtended, string>;

namespace Oleander.Assembly.Comparers.Cecil.AssemblyResolver
{
    internal class AssemblyPathResolver
    {
        private readonly AssemblyPathResolverCache pathRepository;
        private readonly ReaderParameters readerParameters;
        private ITargetPlatformResolver targetPlatformResolver;

        public AssemblyPathResolver(AssemblyPathResolverCache pathRepository, ReaderParameters readerParameters, ITargetPlatformResolver targetPlatformResolver)
        {
            this.pathRepository = pathRepository;
            this.readerParameters = readerParameters;
            this.targetPlatformResolver = targetPlatformResolver;
        }

        public void AddToAssemblyCache(string filePath, TargetArchitecture architecture)
        {
            AssemblyName assemblyName;
            if (this.TryGetAssemblyNameDefinition(filePath, true, architecture, out assemblyName))
            {
                /*Telerik Authorship*/
                ModuleDefinition module = AssemblyDefinition.ReadAssembly(filePath, this.readerParameters).MainModule;

                /*Telerik Authorship*/
                // Call GetTargetPlatform method to resolve assembly target platform and put it in resolver cache
                this.targetPlatformResolver.GetTargetPlatform(filePath, module);

                /*Telerik Authorship*/
                SpecialTypeAssembly special = module.IsReferenceAssembly() ? SpecialTypeAssembly.Reference : SpecialTypeAssembly.None;
                AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(assemblyName.FullName, assemblyName.TargetArchitecture, special);
                if (!this.pathRepository.AssemblyPathName.ContainsKey(assemblyKey))
                {
                    this.CheckFileExistence(assemblyName, filePath, true, false);

                    this.RemoveFromUnresolvedCache(assemblyKey);
                }
            }
        }

        /*Telerik Authorship*/
        internal string GetAssemblyPath(AssemblyName assemblyName, AssemblyStrongNameExtended assemblyKey)
        {
            if (this.IsFailedAssembly(assemblyKey))
            {
                return string.Empty;
            }
            string filePath = string.Empty;

            if (string.IsNullOrEmpty(filePath))
            {
                IEnumerable<string> filePaths = this.GetAssemblyPaths(assemblyName, assemblyKey);

                filePath = filePaths.FirstOrDefault() ?? string.Empty;

                if (string.IsNullOrEmpty(filePath))
                {
                    foreach (string currentFilePath in filePaths)
                    {
                        if (!string.IsNullOrEmpty(currentFilePath))
                        {
                            filePath = currentFilePath;
                            break;
                        }
                    }
                }

                if (this.CheckFoundedAssembly(assemblyName, filePath))
                {
                    return filePath;
                }
            }
            return filePath;
        }

        public bool CheckFileExistence(AssemblyName assemblyName, string searchPattern, bool caching, bool checkForBaseDir, bool checkForArchitectPlatfrom = true)
        {
            AssemblyName assemblyNameFromStorage;
            if (this.TryGetAssemblyNameDefinition(searchPattern, caching, assemblyName.TargetArchitecture, out assemblyNameFromStorage, checkForArchitectPlatfrom))
            {
                var areEquals = /*Telerik Authorship*/AssemblyNameComparer.AreVersionEquals(assemblyNameFromStorage.Version, assemblyName.Version)
                                && /*Telerik Authorship*/AssemblyNameComparer.ArePublicKeyEquals(assemblyNameFromStorage.PublicKeyToken, assemblyName.PublicKeyToken)
                                && assemblyName.TargetArchitecture.CanReference(assemblyNameFromStorage.TargetArchitecture)
                                && (!checkForBaseDir || this.AreDefaultDirEqual(assemblyName, assemblyNameFromStorage));                
                if (areEquals && caching)
                {
                    /*Telerik Authorship*/
                    ModuleDefinition module = AssemblyDefinition.ReadAssembly(searchPattern, this.readerParameters).MainModule;
                    SpecialTypeAssembly special = module.IsReferenceAssembly() ? SpecialTypeAssembly.Reference : SpecialTypeAssembly.None;
                    AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(assemblyName.FullName, assemblyName.TargetArchitecture, special);
                    this.pathRepository.AssemblyPathName.Add(assemblyKey, searchPattern);
                    if (!this.pathRepository.AssemblyPathArchitecture.ContainsKey(searchPattern))
                    {
                        TargetArchitecture architecture = module.GetModuleArchitecture();
                        this.pathRepository.AssemblyPathArchitecture.Add(new KeyValuePair<string, TargetArchitecture>(searchPattern, architecture));
                    }
                }
                return areEquals;
            }
            return false;
        }

        public void ClearCache()
        {
            this.targetPlatformResolver.ClearCache();
            this.pathRepository.Clear();
        }

        private bool CheckFoundedAssembly(AssemblyName assemblyName, string filePath)
        {
            bool checkFileExistence = this.CheckFileExistence(assemblyName, filePath, false, false);
            return checkFileExistence;
        }
        
        /*Telerik Authorship*/
        internal bool TryGetAssemblyPathsFromCache(AssemblyName sourceAssemblyName, AssemblyStrongNameExtended assemblyKey, out IEnumerable<string> filePaths)
        {
            filePaths = Enumerable.Empty<string>();

            if (this.pathRepository.AssemblyPathName.ContainsKey(assemblyKey))
            {
                /*Telerik Authorship*/
                List<string> targets = this.pathRepository.AssemblyPathName.Where(a => a.Key == assemblyKey)
                                                        .Select(a => a.Value)
                                                        .ToList();

                if (sourceAssemblyName.HasDefaultDir && targets.Any(d => Path.GetDirectoryName(d) == sourceAssemblyName.DefaultDir))
                {
                    filePaths = targets.Where(d => Path.GetDirectoryName(d) == sourceAssemblyName.DefaultDir);

                    return true;
                }
                else
                {
                    /*Telerik Authorship*/
                    string targetAssembly = this.pathRepository.AssemblyPathName
                                                             .Where(a => this.pathRepository.AssemblyPathArchitecture.ContainsKey(a.Value) && this.pathRepository.AssemblyPathArchitecture[a.Value].CanReference(sourceAssemblyName.TargetArchitecture))
                                                             .FirstOrDefault(a => a.Key == assemblyKey).Value;
                    if (targetAssembly != null)
                    {
                        filePaths = new string[] { targetAssembly };

                        return true;
                    }
                }
            }
            return false;
        }

        /*Telerik Authorship*/
        public IEnumerable<string> GetAssemblyPaths(AssemblyName sourceAssemblyName, AssemblyStrongNameExtended assemblyKey)
        {
            IEnumerable<string> results;

            if (this.TryGetAssemblyPathsFromCache(sourceAssemblyName, assemblyKey, out results))
            {
                return results;
            }
            if (this.IsFailedAssembly(assemblyKey))
            {
                return Enumerable.Empty<string>();
            }
			var platforms = new List<TargetPlatform>
			{
				TargetPlatform.CLR_4,
				TargetPlatform.CLR_2_3,
				TargetPlatform.Silverlight,
				TargetPlatform.WindowsPhone,
				TargetPlatform.WindowsCE,
				TargetPlatform.CLR_1,
				TargetPlatform.WinRT,
				TargetPlatform.NetCore
            };
            var result = new List<string>();
            foreach (TargetPlatform platform in platforms)
            {
                IEnumerable<string> assemblyLocations = this.GetAssemblyLocationsByPlatform(sourceAssemblyName, platform);

                result.AddRange(assemblyLocations);
            }
            return result;
        }

        private IEnumerable<string> GetAssemblyLocationsByPlatform(AssemblyName assemblyName, TargetPlatform runtime)
        {
            var result = new List<string>();
            switch (runtime)
            {
                case TargetPlatform.CLR_1:
                    result.AddRange(this.ResolveClr1(assemblyName));
                    break;
                case TargetPlatform.CLR_2_3:
                case TargetPlatform.CLR_4:
                    result.AddRange(this.ResolveClr(assemblyName, runtime));
                    break;

                case TargetPlatform.WindowsCE:
                    result.AddRange(this.ResolveCompact(assemblyName));
                    break;

                case TargetPlatform.Silverlight:
                case TargetPlatform.WindowsPhone:
                    result.AddRange(this.ResolveSilverlightPaths(assemblyName));
                    result.AddRange(this.ResolveWP(assemblyName));
                    break;

                case TargetPlatform.WinRT:
                    result.AddRange(this.ResolveWinRTMetadata(assemblyName));
                    result.AddRange(this.ResolveUWPReferences(assemblyName));
                    break;
				case TargetPlatform.NetCore:
					result.AddRange(this.ResolveNetCoreReferences(assemblyName));
					break;

			}

            /*Telerik Authorship*/
            this.targetPlatformResolver.AddPartCacheResult(result, runtime);

            return result;
        }

        // AddPartCacheResult is moved to TargetPlatformResolver

		/*Telerik Authorship*/
		private IEnumerable<string> ResolveNetCoreReferences(AssemblyName assemblyName)
		{
			List<string> targetDirectories = null;

			foreach (var ver in assemblyName.SupportedVersions(TargetPlatform.NetCore))
			{
				targetDirectories = new List<string>();

				if (Directory.Exists(SystemInformation.NetCoreX64SharedAssemblies))
				{
                    targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.NetCoreX64SharedAssemblies, ver + "*"));
                }

				if (Directory.Exists(SystemInformation.NetCoreX86SharedAssemblies))
				{
                    targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.NetCoreX86SharedAssemblies, ver + "*"));
                }

				foreach (string dirVersions in targetDirectories)
				{
					string searchPattern = string.Format("{0}\\{1}.dll", dirVersions, assemblyName.Name);
					if (this.CheckFileExistence(assemblyName, searchPattern, true, true))
					{
						yield return searchPattern;
					}
				}
			}
		}

        private IEnumerable<string> ResolveSilverlightPaths(AssemblyName assemblyName)
        {
            List<string> result = new List<string>();

            string runtime = this.ResolveSilverlightRuntimePath(assemblyName, SystemInformation.SILVERLIGHT_RUNTIME);

            string runtime64 = this.ResolveSilverlightRuntimePath(assemblyName, SystemInformation.SILVERLIGHT_RUNTIME_64);

            IEnumerable<string> @default = this.ResolveSilverlightPath(assemblyName, SystemInformation.SILVERLIGHT_DEFAULT);

            IEnumerable<string> sdk = this.ResolveSilverlightPath(assemblyName, SystemInformation.SILVERLIGHT_SDK);

            if (string.IsNullOrEmpty(runtime) == false)
            {
                result.Add(runtime);
            }
            result.AddRange(@default);

            result.AddRange(sdk);
            /*Telerik Authorship*/
            if (string.IsNullOrEmpty(runtime64) == false)
            {
                result.Add(runtime64);
            }
            return result;
        }

        private string ResolveSilverlightRuntimePath(AssemblyName assemblyName, string path)
        {
            string searchPattern = string.Format(path, assemblyName.Name);
            if (this.CheckFileExistence(assemblyName, searchPattern, true, true))
            {
                return searchPattern;
            }
            return string.Empty;
        }

        private IEnumerable<string> ResolveSilverlightPath(AssemblyName assemblyName, string path)
        {
            foreach (var version in assemblyName.SupportedVersions(TargetPlatform.Silverlight))
            {
                string searchPattern = string.Format(path, SystemInformation.ProgramFilesX86, version, assemblyName.Name);
                if (this.CheckFileExistence(assemblyName, searchPattern, true, true))
                {
                    yield return searchPattern;
                }
            }
        }

        private IEnumerable<string> ResolveCompact(AssemblyName assemblyName)
        {
            foreach (var ver in assemblyName.SupportedVersions(TargetPlatform.WindowsCE))
            {
                var searchPattern = string.Format(SystemInformation.COMPACT_FRAMEWORK, SystemInformation.ProgramFilesX86, ver, assemblyName.Name);
                if (this.CheckFileExistence(assemblyName, searchPattern, true, false))
                {
                    yield return searchPattern;
                }
            }
        }

        private IEnumerable<string> ResolveWP(AssemblyName assemblyName)
        {
            foreach (var version in assemblyName.SupportedVersions(TargetPlatform.WindowsPhone))
            {
                string windowsPhoneDir = string.Format(@"{0}\Reference Assemblies\Microsoft\Framework\Silverlight\{1}\Profile\",
                                                       SystemInformation.ProgramFilesX86,
                                                       version);
                if (Directory.Exists(windowsPhoneDir))
                {
                    foreach (string dirVersions in Directory.GetDirectories(windowsPhoneDir, "WindowsPhone*."))
                    {
                        string searchPattern = string.Format("{0}\\{1}.dll", dirVersions, assemblyName.Name);
                        if (this.CheckFileExistence(assemblyName, searchPattern, true, true))
                        {
                            yield return searchPattern;
                        }
                    }
                }
            }
        }

        private IEnumerable<string> ResolveClr(AssemblyName assemblyName, TargetPlatform clrRuntime)
        {
            var result = new List<string>();

            string versionMajor = string.Format("v{0}*", (clrRuntime == TargetPlatform.CLR_4 ? 4 : 2));

            var targetDirectories = new List<string>();
            if (Directory.Exists(SystemInformation.CLR_Default_32))
            {
                targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.CLR_Default_32, versionMajor));
            }
            if (Directory.Exists(SystemInformation.CLR_Default_64))
            {
                targetDirectories.AddRange(Directory.GetDirectories(SystemInformation.CLR_Default_64, versionMajor));
            }
            foreach (var dir in targetDirectories)
            {
                foreach (string extension in SystemInformation.ResolvableExtensions)
                {
                    string filePath = dir + "\\" + assemblyName.Name + extension;
                    if (this.CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        result.Add(filePath);
                    }
                }
            }
            var searchPattern = clrRuntime == TargetPlatform.CLR_4 ? SystemInformation.CLR_4 : SystemInformation.CLR;

            foreach (var targetRuntime in assemblyName.SupportedVersions(clrRuntime))
            {
                foreach (var extension in SystemInformation.ResolvableExtensions)
                {
                    string filePath = string.Format(searchPattern,
                                                    SystemInformation.WindowsPath,
                                                    targetRuntime,
                                                    assemblyName.Name,
                                                    assemblyName.ParentDirectory(clrRuntime),
                                                    assemblyName.Name,
                                                    extension);
                    if (this.CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        result.Add(filePath);
                    }
                }
            }
            return result;
        }

        private IEnumerable<string> ResolveClr1(AssemblyName assemblyName)
        {
            string path = Path.Combine(SystemInformation.WindowsPath, "Microsoft.NET\\Framework");
            if (assemblyName.Version.Major != 1)
            {
                yield break;
            }

            foreach (string extension in SystemInformation.ResolvableExtensions)
            {
                string[] files = Directory.GetFiles(path, assemblyName.Name + extension, SearchOption.AllDirectories);
                foreach (string filePath in files)
                {
                    if (this.CheckFileExistence(assemblyName, filePath, true, false))
                    {
                        yield return filePath;
                    }
                }
            }
        }

        private IEnumerable<string> ResolveWinRTMetadata(AssemblyName assemblyName)
        {
            string filePath = string.Format(@"{0}\{1}.winmd", SystemInformation.WINRT_METADATA, assemblyName.Name);
            if (this.CheckFileExistence(assemblyName, filePath, true, false))
            {
                yield return filePath;
            }

            if (!Directory.Exists(SystemInformation.WINDOWS_WINMD_LOCATION))
            {
                yield break;
            }

            foreach (string foundFile in Directory.GetFiles(SystemInformation.WINDOWS_WINMD_LOCATION, assemblyName.Name + ".winmd", SearchOption.AllDirectories))
            {
                if (this.CheckFileExistence(assemblyName, foundFile, true, false))
                {
                    yield return foundFile;
                }
            }
        }

        private IEnumerable<string> ResolveUWPReferences(AssemblyName assemblyName)
        {
            string fileName = string.Format("{0}.winmd", assemblyName.Name);
            string filePath = Path.Combine(SystemInformation.UWP_REFERENCES, assemblyName.Name, assemblyName.Version.ToString(), fileName);
            if (this.CheckFileExistence(assemblyName, filePath, true, false))
            {
                yield return filePath;
            }
        }

        private bool AreDefaultDirEqual(AssemblyName assemblyName, AssemblyName assemblyNameFromStorage)
        {
            if (!assemblyName.HasDefaultDir && !assemblyNameFromStorage.HasDefaultDir)
            {
                return true;
            }
            if (assemblyName.HasDefaultDir && assemblyNameFromStorage.HasDefaultDir)
            {
                if (File.Exists(Path.Combine(assemblyName.DefaultDir, assemblyName.Name + ".dll"))
                    && File.Exists(Path.Combine(assemblyNameFromStorage.DefaultDir, assemblyNameFromStorage.Name + ".dll")))
                {
                    return assemblyName.DefaultDir == assemblyNameFromStorage.DefaultDir;
                }
                return true;
            }
            return true;
        }

        private bool IsZero(Version version)
        {
            return version == null || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0);
        }

        private bool TryGetAssemblyNameDefinition(string assemblyFilePath,
                                                         bool caching,
                                                         TargetArchitecture architecture,
                                                         out AssemblyName assemblyName,
                                                         bool checkForArchitectPlatfrom = true)
        {
            assemblyName = null;
            if (this.pathRepository.AssemblyNameDefinition.ContainsKey(assemblyFilePath))
            {
                assemblyName = this.pathRepository.AssemblyNameDefinition[assemblyFilePath];
                if (!checkForArchitectPlatfrom)
                {
                    return true;
                }
                else if (assemblyName.TargetArchitecture == architecture)
                {
                    return true;
                }
            }
            if ((caching || assemblyName == null) && File.Exists(assemblyFilePath))
            {
                var moduleDef = ModuleDefinition.ReadModule(assemblyFilePath, this.readerParameters);
                if (moduleDef != null && moduleDef.Assembly != null)
                {
                    AssemblyDefinition assemblyDef = moduleDef.Assembly;
                    assemblyName = new AssemblyName(moduleDef.Name,
                                                    assemblyDef.FullName,
                                                    assemblyDef.Name.Version,
                                                    assemblyDef.Name.PublicKeyToken,
                                                    Path.GetDirectoryName(assemblyFilePath)) { TargetArchitecture = moduleDef.GetModuleArchitecture() };
                    this.pathRepository.AssemblyNameDefinition[assemblyFilePath] = assemblyName;
                    return true;
                }
            }
            return false;
        }

        internal void RemoveFromAssemblyCache(string fileName)
        {
            int index = this.pathRepository.AssemblyPathName.FindIndex(p => p.Value == fileName);
            if (index != -1)
            {
                this.pathRepository.AssemblyPathName.RemoveAt(index);
            }
            /*Telerik Authorship*/
            if (this.targetPlatformResolver.ResolverCache.AssemblyPathToTargetPlatform.ContainsKey(fileName))
            {
                /*Telerik Authorship*/
                this.targetPlatformResolver.ResolverCache.AssemblyPathToTargetPlatform.Remove(fileName);
            }
            if (this.pathRepository.AssemblyNameDefinition.ContainsKey(fileName))
            {
                this.pathRepository.AssemblyNameDefinition.Remove(fileName);
            }
            if (this.pathRepository.AssemblyPathArchitecture.ContainsKey(fileName))
            {
                this.pathRepository.AssemblyPathArchitecture.Remove(fileName);
            }
        }

        internal void AddToAssemblyPathNameCache(AssemblyName assemblyName, string filePath)
        {
            /*Telerik Authorship*/
            ModuleDefinition module = AssemblyDefinition.ReadAssembly(filePath, this.readerParameters).MainModule;
            SpecialTypeAssembly special = module.IsReferenceAssembly() ? SpecialTypeAssembly.Reference : SpecialTypeAssembly.None;
            AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(assemblyName.FullName, assemblyName.TargetArchitecture, special);
            this.pathRepository.AssemblyPathName.Add(assemblyKey, filePath);

            if (!this.pathRepository.AssemblyPathArchitecture.ContainsKey(filePath))
            {
                this.pathRepository.AssemblyPathArchitecture.Add(new KeyValuePair<string, TargetArchitecture>(filePath, assemblyName.TargetArchitecture));
            }
        }

        #region failed cache
        /*Telerik Authorship*/
        internal void AddToUnresolvedCache(AssemblyStrongNameExtended fullName)
        {
            if (!this.pathRepository.AssemblyFaildedResolverCache.Contains(fullName))
            {
                this.pathRepository.AssemblyFaildedResolverCache.Add(fullName);
            }
        }
        
        /*Telerik Authorship*/
        internal void RemoveFromUnresolvedCache(AssemblyStrongNameExtended fullName)
        {
            if (this.pathRepository.AssemblyFaildedResolverCache.Contains(fullName))
            {
                this.pathRepository.AssemblyFaildedResolverCache.Remove(fullName);
            }
        }

        /*Telerik Authorship*/
        internal bool IsFailedAssembly(AssemblyStrongNameExtended fullName)
        {
            return this.pathRepository.AssemblyFaildedResolverCache.Contains(fullName);
        }

        /*Telerik Authorship*/
        internal IClonableCollection<AssemblyStrongNameExtended> GetAssemblyFailedResolvedCache()
        {
            return this.pathRepository.AssemblyFaildedResolverCache;
        }

        /*Telerik Authorship*/
        internal void SetFailedAssemblyCache(IList<AssemblyStrongNameExtended> list)
        {
            foreach (AssemblyStrongNameExtended assemblyKey in list)
            {
                this.AddToUnresolvedCache(assemblyKey);
            }
        }

        internal void ClearAssemblyFailedResolverCache()
        {
            this.pathRepository.AssemblyFaildedResolverCache.Clear();
        }
        #endregion
    }

    public static class Extensions
    {
        /*Telerik Authorship*/
        internal static bool ContainsKey(this IList<AssemblyPathName> collection, AssemblyStrongNameExtended assemblyKey)
        {
            return collection.Any(a => a.Key == assemblyKey);
        }

        /*Telerik Authorship*/
        internal static void Add(this IList<AssemblyPathName> collection, AssemblyStrongNameExtended assemblyKey, string value)
        {
            collection.Add(new AssemblyPathName(assemblyKey, value));
        }

        public static bool IsReferenceAssembly(this ModuleDefinition moduleDef)
        {
            if (moduleDef == null || moduleDef.Assembly == null || moduleDef.Assembly.CustomAttributes == null)
            {
                return false;
            }
            foreach (var attribute in moduleDef.Assembly.CustomAttributes)
            {
                if (attribute.AttributeType.Name == "ReferenceAssemblyAttribute" && attribute.AttributeType.Namespace == "System.Runtime.CompilerServices")
                {
                    return true;
                }
            }
            return false;
        }
    }
}

