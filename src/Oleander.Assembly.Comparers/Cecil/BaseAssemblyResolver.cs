//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Text;
using Oleander.Assembly.Comparers.Cecil.AssemblyResolver;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Extensions;
/*Telerik Authorship*/
/*Telerik Authorship*/
/*Telerik Authorship*/
/*Telerik Authorship*/

/*Telerik Authorship*/

namespace Oleander.Assembly.Comparers.Cecil {
	public delegate AssemblyDefinition AssemblyResolveEventHandler(object sender, /*Telerik Authorship*/ AssemblyResolveEventArgs args);

	/*Telerik Authorship*/
	public delegate void AssemblyDefinitionFailureEventHandler(object sender, Exception ex);

	public sealed class AssemblyResolveEventArgs : EventArgs {

		readonly AssemblyNameReference reference;
		/*Telerik Authorship*/
		private readonly TargetArchitecture architecture;

		public AssemblyNameReference AssemblyReference {
			get { return this.reference; }
		}

		/*Telerik Authorship*/
		public TargetArchitecture Architecture
		{
			get { return this.architecture; }
		}

		public AssemblyResolveEventArgs(AssemblyNameReference reference, /*Telerik Authorship*/ TargetArchitecture architecture)
		{
			this.reference = reference;

			/*Telerik Authorship*/
			this.architecture = architecture;
		}
	}
	

	/*Telerik Authorship*/
	[DoNotPrune]
	[DoNotObfuscateType]
	public abstract class BaseAssemblyResolver : IAssemblyResolver
	{
		static readonly bool on_mono = Type.GetType ("Mono.Runtime") != null;

		/*Telerik Authorship*/
		public static readonly object Locker = new object();

		/*Telerik Authorship*/
		protected readonly List<string> directories;
		protected HashSet<DirectoryAssemblyInfo> directoryAssemblies;
		protected readonly Dictionary<AssemblyStrongNameExtended, List<AssemblyDefinition>> resolvedAssemblies;
		protected readonly Dictionary<string, AssemblyDefinition> filePathToAssemblyDefinitionCache;
		protected readonly IList<string> userDefinedAssemblies;
		private readonly HashSet<string> resolvableExtensionsSet;
		private readonly string[] architectureStrings;
        /*Telerik Authorship*/
        private readonly ITargetPlatformResolver targetPlatformResolver;
        /*Telerik Authorship*/
        private readonly ReaderParameters readerParameters;
        /*Telerik Authorship*/
        private readonly AssemblyPathResolver assemblyPathResolver;
        /*Telerik Authorship*/
        private readonly ReaderWriterLockSlim resolveLock;
        /*Telerik Authorship*/
        private readonly ReaderWriterLockSlim directoriesLock;
        /*Telerik Authorship*/
        private readonly ReaderWriterLockSlim directoryAssembliesLock;


        Collection<string> gac_paths;

		/*Telerik Authorship*/
		protected BaseAssemblyResolver(AssemblyPathResolverCache pathRespository, ITargetPlatformResolver targetPlatformResolver)
		{
            this.directories = new List<string>();
            this.resolvedAssemblies = new Dictionary<AssemblyStrongNameExtended, List<AssemblyDefinition>>();
            this.filePathToAssemblyDefinitionCache = new Dictionary<string, AssemblyDefinition>();
            this.userDefinedAssemblies = new List<string>();
            this.resolvableExtensionsSet = new HashSet<string>(SystemInformation.ResolvableExtensions);
            this.architectureStrings = this.GetArchitectureStrings();
            /*Telerik Authorship*/
            this.directoryAssemblies = new HashSet<DirectoryAssemblyInfo>();

            /*Telerik Authorship*/
            this.targetPlatformResolver = targetPlatformResolver;
            /*Telerik Authorship*/
            this.readerParameters = new ReaderParameters(this);
            /*Telerik Authorship*/
            this.assemblyPathResolver = new AssemblyPathResolver(pathRespository, /*Telerik Authorship*/ this.readerParameters, /*Telerik Authorship*/ this.targetPlatformResolver);

            /*Telerik Authorship*/
            this.resolveLock = new ReaderWriterLockSlim();
            /*Telerik Authorship*/
            this.directoriesLock = new ReaderWriterLockSlim();
            /*Telerik Authorship*/
            this.directoryAssembliesLock = new ReaderWriterLockSlim();

        }

		/*Telerik Authorship*/
		private string[] GetArchitectureStrings()
		{
			List<string> result = new List<string>();
			foreach (TargetArchitecture architecture in Enum.GetValues(typeof(TargetArchitecture)))
			{
				result.Add(architecture.ToString());
			}
			return result.ToArray();
		}
        
        /*Telerik Authorship*/
        private void InvalidateDirectoryAssemblyCache()
        {
            this.DoWithWriteLock(this.directoryAssembliesLock, () =>
            {
                this.directoryAssemblies = new HashSet<DirectoryAssemblyInfo>(this.GetDirectoryAssemblies().ToList());
            });
        }

        /*Telerik Authorship*/
        public void AddSearchDirectory(string directory)
		{
            string directoryInLowercase = directory.ToLowerInvariant();

            if (this.DoWithReadLock(this.directoriesLock, () => { return this.directories.Contains(directoryInLowercase); }))
            {
                return;
            }
            
            if (Directory.Exists(directory))
            {
                bool isAdded = this.DoWithWriteLock(this.directoriesLock, () =>
                {
                    // Double check for ensuring the entry is not added in the meantime.
                    if (this.directories.Contains(directoryInLowercase))
                    {
                        return false;
                    }

                    this.directories.Add(directoryInLowercase);

                    return true;
                });

                if (isAdded)
                {
                    this.InvalidateDirectoryAssemblyCache();
                }
            }
		}

		/*Telerik Authorship*/
		public void RemoveSearchDirectory(string directory)
        {
            /*Telerik Authorship*/
            this.DoWithWriteLock(this.directoriesLock, () =>
            {
                this.directories.Remove(directory);
            });
		}

		/*Telerik Authorship*/
		public string[] GetSearchDirectories()
        {
            /*Telerik Authorship*/
            return this.DoWithReadLock(this.directoriesLock, () =>
            {
                var directories = new string[this.directories.Count];
                Array.Copy(this.directories.ToArray(), directories, directories.Length);
                return directories;
            });
		}


		public event AssemblyResolveEventHandler ResolveFailure;

		/*Telerik Authorship*/
		public event AssemblyDefinitionFailureEventHandler AssemblyDefinitionFailure = delegate { };

		/*Telerik Authorship*/
		private AssemblyDefinition GetAssembly(string file, ReaderParameters parameters)
		{
			if (parameters.AssemblyResolver == null)
				parameters.AssemblyResolver = this;

			return ModuleDefinition.ReadModule (file, parameters).Assembly;
		}

		/*Telerik Authorship*/
		private IEnumerable<DirectoryAssemblyInfo> GetDirectoryAssemblies()
        {
            /*Telerik Authorship*/
            return this.DoWithReadLock(this.directoriesLock, () =>
            {
                List<DirectoryAssemblyInfo> result = new List<DirectoryAssemblyInfo>();
                foreach (string directory in this.directories)
                {
                    if (!Directory.Exists(directory))
                    {
                        continue;
                    }
                    foreach (string extension in SystemInformation.ResolvableExtensions)
                    {
                        foreach (string file in Directory.GetFiles(directory, "*" + extension))
                        {
                            if (this.resolvableExtensionsSet.Contains(Path.GetExtension(file)) && file.Length < 260)
                            //Check is added because of the behaviour of Directory.GetFiles
                            {
                                result.Add(this.CreateDirectoryAssemblyInfo(file));
                            }
                        }
                    }
                }

                return result;
            });
		}

		/*Telerik Authorship*/
		private DirectoryAssemblyInfo CreateDirectoryAssemblyInfo(string file)
		{
			return new DirectoryAssemblyInfo(file, Path.GetFileNameWithoutExtension(file).ToLowerInvariant()) { Dir = Path.GetDirectoryName(file) };
		}

		/*Telerik Authorship*/
		public virtual AssemblyDefinition Resolve(string fullName, ReaderParameters parameters, TargetArchitecture platform, SpecialTypeAssembly special, bool bubbleToUserIfFailed = true)
		{
			if (fullName == null)
			{
				throw new ArgumentNullException("fullName");
			}

			return this.Resolve(AssemblyNameReference.Parse(fullName), string.Empty, parameters, platform, special, bubbleToUserIfFailed);
		}

		/*Telerik Authorship*/
		public virtual AssemblyDefinition Resolve(AssemblyNameReference name, string path, TargetArchitecture architecture, SpecialTypeAssembly special, bool bubbleToUserIfFailed = true)
        {
			AssemblyDefinition assemblyDefinition = this.Resolve(name, path, new ReaderParameters(this), architecture, special, bubbleToUserIfFailed);

			return assemblyDefinition;
		}

		/*Telerik Authorship*/
		public virtual AssemblyDefinition Resolve(AssemblyNameReference name, string path, TargetArchitecture architecture, SpecialTypeAssembly special, bool addToFailedCache, bool bubbleToUserIfFailed = true)
        {
			AssemblyDefinition assemblyDefinition = this.Resolve(name, path, new ReaderParameters(this), architecture, special, bubbleToUserIfFailed, addToFailedCache);

			return assemblyDefinition;
		}

		/*Telerik Authorship*/
		private AssemblyDefinition Resolve(AssemblyNameReference name, string defaultPath, ReaderParameters parameters, TargetArchitecture architecture, SpecialTypeAssembly special, bool bubbleToUserIfFailed, bool addToFailedCache = true)
        {
            if (!string.IsNullOrEmpty(defaultPath))
            {
                this.AddSearchDirectory(defaultPath);
            }

            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (parameters == null)
            {
                parameters = new ReaderParameters(this);
            }

            /*Telerik Authorship*/
            AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(name.FullName, architecture, special);
            AssemblyName assemblyName = new AssemblyName(name.Name, name.FullName, name.Version, name.PublicKey) { TargetArchitecture = architecture };
            AssemblyDefinition assembly = null;

            /*Telerik Authorship*/
            bool isResolved = this.DoWithReadLock(this.resolveLock, () =>
            {
                if (this.assemblyPathResolver.IsFailedAssembly(assemblyKey))
                {
                    assembly = null;

                    return true;
                }

                assembly = this.GetFromResolvedAssemblies(assemblyName, special);
                return assembly != null;
            });

            if (isResolved)
            {
                return assembly;
            }

            /*Telerik Authorship*/
            return this.DoWithWriteLock(this.resolveLock, () =>
            {
                // Double checks to ensure that the entry is not added in the meantime.
                if (this.assemblyPathResolver.IsFailedAssembly(assemblyKey))
                {
                    return null;
                }

                assembly = this.GetFromResolvedAssemblies(assemblyName, special);
                if (assembly != null)
                {
                    return assembly;
                }

                /*Telerik Authorship*/
                // This code has been added by Mono.Cecil 0.9.6. It has been commented, because retargetable references should be further
                // researched and handled appropriately across the application. TP item N. 323383
                //if (name.IsRetargetable)
                //{
                //	// if the reference is retargetable, zero it
                //	name = new AssemblyNameReference(name.Name, new Version(0, 0, 0, 0))
                //	{
                //		PublicKeyToken = Empty<byte>.Array,
                //	};
                //}

                assembly = this.SearchDirectory(name, parameters, architecture, defaultPath) ?? this.TryGetTargetAssembly(name, parameters, architecture, assemblyKey);

                if (assembly != null)
                {
                    if (!this.filePathToAssemblyDefinitionCache.ContainsKey(assembly.MainModule.FilePath))
                    {
                        this.AddToResolvedAssemblies(assembly);
                    }
                    return assembly;
                }
                assembly = this.GetTargetAssembly(name, parameters, architecture);
                if (assembly != null)
                {
                    if (!this.filePathToAssemblyDefinitionCache.ContainsKey(assembly.MainModule.FilePath))
                    {
                        this.AddToResolvedAssemblies(assembly);
                    }
                    return assembly;
                }
                if (bubbleToUserIfFailed)
                {
                    return this.UserSpecifiedAssembly(name, architecture, assemblyKey);
                }
                else if (addToFailedCache)
                {
                    this.assemblyPathResolver.AddToUnresolvedCache(assemblyKey);
                }
                return null;
            });
		}

        /*Telerik Authorship*/
        private AssemblyDefinition GetFromResolvedAssemblies(AssemblyName assemblyName, SpecialTypeAssembly special)
		{
			foreach (string architectureString in this.GetReferencableArchitectures(assemblyName))
			{
				List<AssemblyDefinition> assemblyList;
                AssemblyStrongNameExtended extendedStrongName = new AssemblyStrongNameExtended(assemblyName.FullName, (TargetArchitecture)Enum.Parse(typeof(TargetArchitecture), architectureString), special);
                if (this.TryGetResolvedAssembly(extendedStrongName, out assemblyList))
				{
					return assemblyList[0];
				}
			}

			return null;
		}

		/*Telerik Authorship*/
		protected virtual bool TryGetResolvedAssembly(AssemblyStrongNameExtended key, out List<AssemblyDefinition> assemblyList)
		{
			return this.resolvedAssemblies.TryGetValue(key, out assemblyList);
		}

		/*Telerik Authorship*/
		private string[] GetReferencableArchitectures(AssemblyName assemblyName)
		{
			if (assemblyName.TargetArchitecture != TargetArchitecture.AnyCPU)
			{
				return new string[] { "AnyCPU", assemblyName.TargetArchitecture.ToString() };
			}
			return this.architectureStrings;
		}

		/*Telerik Authorship*/
		private AssemblyDefinition UserSpecifiedAssembly(AssemblyNameReference name, TargetArchitecture architecture, AssemblyStrongNameExtended assemblyKey)
		{
			//If not in denied assemblies cache
			if (this.assemblyPathResolver.IsFailedAssembly(assemblyKey))
			{
				return null;
			}
			if (this.ResolveFailure != null)
			{
				AssemblyDefinition assembly = this.ResolveFailure(this, new AssemblyResolveEventArgs(name, architecture));
				if (assembly != null)
				{
					if (!this.filePathToAssemblyDefinitionCache.ContainsKey(assembly.MainModule.FilePath))
					{
                        this.AddToResolvedAssemblies(assembly);
					}
					if (!this.userDefinedAssemblies.Contains(assembly.MainModule.FilePath))
					{
                        this.userDefinedAssemblies.Add(assembly.MainModule.FilePath);
					}

                    this.RemoveFromFailedAssemblies(assemblyKey);

					return assembly;
				}
			}

            this.assemblyPathResolver.AddToUnresolvedCache(assemblyKey);
			return null;
		}

		/*Telerik Authorship*/
		private AssemblyDefinition SearchDirectory(AssemblyNameReference name, ReaderParameters parameters, TargetArchitecture architecture, string defaultPath)
        {
            /*Telerik Authorship*/
            return this.DoWithReadLock(this.directoryAssembliesLock, () =>
            {
                var defaultLocations = this.directoryAssemblies.Where(d => d.Dir.Equals(defaultPath, StringComparison.OrdinalIgnoreCase));

                AssemblyDefinition ad;

                if (this.TrySearchDirectory(name, parameters, architecture, defaultLocations, out ad))
                {
                    return ad;
                }
                var notDefaultLocations = this.directoryAssemblies.Except(defaultLocations);

                if (this.TrySearchDirectory(name, parameters, architecture, notDefaultLocations, out ad))
                {
                    return ad;
                }
                return null;
            });
		}

		/*Telerik Authorship*/
		private bool TrySearchDirectory(AssemblyNameReference name, ReaderParameters parameters, TargetArchitecture architecture, IEnumerable<DirectoryAssemblyInfo> targetDirs, out AssemblyDefinition assemblyDefinition)
		{
			assemblyDefinition = null;

			string lowerName = name.Name.ToLowerInvariant();

			foreach (DirectoryAssemblyInfo directoryAssembly in targetDirs)
			{
				string assemblyPath = directoryAssembly.FullFileName;
				if (directoryAssembly.FileNameWithoutExtension == lowerName)
				{
					AssemblyName assName = new AssemblyName(name.Name, name.FullName, name.Version, name.PublicKeyToken) { TargetArchitecture = architecture };
					bool sameVersion = this.assemblyPathResolver.CheckFileExistence(assName, assemblyPath, false, false, checkForArchitectPlatfrom: true);
					if (sameVersion)
					{
						assemblyDefinition = this.GetAssembly(directoryAssembly.FullFileName, parameters);

						return true;
					}
				}
			}
			return false;
		}

		static bool IsZero (Version version)
		{
			return version == null || (version.Major == 0 && version.Minor == 0 && version.Build == 0 && version.Revision == 0);
		}

		AssemblyDefinition GetCorlib (AssemblyNameReference reference, ReaderParameters parameters)
		{
			var version = reference.Version;
			var corlib = typeof (object).Assembly.GetName ();

			if (corlib.Version == version || IsZero (version))
				return this.GetAssembly (typeof (object).Module.FullyQualifiedName, parameters);

			var path = Directory.GetParent (
				Directory.GetParent (
					typeof (object).Module.FullyQualifiedName).FullName
				).FullName;

			if (on_mono) {
				if (version.Major == 1)
					path = Path.Combine (path, "1.0");
				else if (version.Major == 2) {
					if (version.MajorRevision == 5)
						path = Path.Combine (path, "2.1");
					else
						path = Path.Combine (path, "2.0");
				} else if (version.Major == 4)
					path = Path.Combine (path, "4.0");
				else
					throw new NotSupportedException ("Version not supported: " + version);
			} else {
				switch (version.Major) {
				case 1:
					if (version.MajorRevision == 3300)
						path = Path.Combine (path, "v1.0.3705");
					else
						path = Path.Combine (path, "v1.0.5000.0");
					break;
				case 2:
					path = Path.Combine (path, "v2.0.50727");
					break;
				case 4:
					path = Path.Combine (path, "v4.0.30319");
					break;
				default:
					throw new NotSupportedException ("Version not supported: " + version);
				}
			}

			var file = Path.Combine (path, "mscorlib.dll");
			if (File.Exists (file))
				return this.GetAssembly (file, parameters);

			return null;
		}

		static Collection<string> GetGacPaths ()
		{
			if (on_mono)
				return GetDefaultMonoGacPaths ();

			var paths = new Collection<string> (2);
			var windir = Environment.GetEnvironmentVariable ("WINDIR");
			if (windir == null)
				return paths;

			paths.Add (Path.Combine (windir, "assembly"));
			paths.Add (Path.Combine (windir, Path.Combine ("Microsoft.NET", "assembly")));
			return paths;
		}

		static Collection<string> GetDefaultMonoGacPaths ()
		{
			var paths = new Collection<string> (1);
			var gac = GetCurrentMonoGac ();
			if (gac != null)
				paths.Add (gac);

			var gac_paths_env = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
			if (string.IsNullOrEmpty (gac_paths_env))
				return paths;

			var prefixes = gac_paths_env.Split (Path.PathSeparator);
			foreach (var prefix in prefixes) {
				if (string.IsNullOrEmpty (prefix))
					continue;

				var gac_path = Path.Combine (Path.Combine (Path.Combine (prefix, "lib"), "mono"), "gac");
				if (Directory.Exists (gac_path) && !paths.Contains (gac))
					paths.Add (gac_path);
			}

			return paths;
		}

		static string GetCurrentMonoGac ()
		{
			return Path.Combine (
				Directory.GetParent (
					Path.GetDirectoryName (typeof (object).Module.FullyQualifiedName)).FullName,
				"gac");
		}

		AssemblyDefinition GetAssemblyInGac (AssemblyNameReference reference, ReaderParameters parameters)
		{
			if (reference.PublicKeyToken == null || reference.PublicKeyToken.Length == 0)
				return null;

			if (this.gac_paths == null) this.gac_paths = GetGacPaths ();

			if (on_mono)
				return this.GetAssemblyInMonoGac (reference, parameters);

			return this.GetAssemblyInNetGac (reference, parameters);
		}

		AssemblyDefinition GetAssemblyInMonoGac (AssemblyNameReference reference, ReaderParameters parameters)
		{
			for (int i = 0; i < this.gac_paths.Count; i++) {
				var gac_path = this.gac_paths [i];
				var file = GetAssemblyFile (reference, string.Empty, gac_path);
				if (File.Exists (file))
					return this.GetAssembly (file, parameters);
			}

			return null;
		}

		AssemblyDefinition GetAssemblyInNetGac (AssemblyNameReference reference, ReaderParameters parameters)
		{
			var gacs = new [] { "GAC_MSIL", "GAC_32", "GAC_64", "GAC" };
			var prefixes = new [] { string.Empty, "v4.0_" };

			for (int i = 0; i < 2; i++) {
				for (int j = 0; j < gacs.Length; j++) {
					var gac = Path.Combine (this.gac_paths [i], gacs [j]);
					var file = GetAssemblyFile (reference, prefixes [i], gac);
					if (Directory.Exists (gac) && File.Exists (file))
						return this.GetAssembly (file, parameters);
				}
			}

			return null;
		}

		static string GetAssemblyFile (AssemblyNameReference reference, string prefix, string gac)
		{
			var gac_folder = new StringBuilder ()
				.Append (prefix)
				.Append (reference.Version)
				.Append ("__");

			for (int i = 0; i < reference.PublicKeyToken.Length; i++)
				gac_folder.Append (reference.PublicKeyToken [i].ToString ("x2"));

			return Path.Combine (
				Path.Combine (
					Path.Combine (gac, reference.Name), gac_folder.ToString ()),
				reference.Name + ".dll");
		}


#region  /*Telerik Authorship*/
		AssemblyDefinition GetTargetAssembly(AssemblyNameReference reference, ReaderParameters parameters, TargetArchitecture architecture)
		{
			if (reference == null)
			{
				return null;
			}
			var assemblyName = new AssemblyName(reference.Name,
												reference.FullName,
												reference.Version,
												reference.PublicKeyToken) { TargetArchitecture = architecture };            

            AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(assemblyName.FullName, architecture, SpecialTypeAssembly.None);
            IEnumerable<string> filePaths = this.assemblyPathResolver.GetAssemblyPaths(assemblyName, assemblyKey);

			return this.GetTargetAssembly(filePaths, parameters, architecture);
		}

		AssemblyDefinition TryGetTargetAssembly(AssemblyNameReference reference, ReaderParameters parameters, TargetArchitecture architecture, AssemblyStrongNameExtended assemblyKey)
		{
			if (reference == null)
			{
				return null;
			}
			var assemblyName = new AssemblyName(reference.Name,
												reference.FullName,
												reference.Version,
												reference.PublicKeyToken) { TargetArchitecture = architecture };
			IEnumerable<string> filePaths;
			if (this.assemblyPathResolver.TryGetAssemblyPathsFromCache(assemblyName, assemblyKey, out filePaths))
			{
				return this.GetTargetAssembly(filePaths, parameters, architecture);
			}
			return null;
		}

		private AssemblyDefinition GetTargetAssembly(IEnumerable<string> filePaths, ReaderParameters parameters, TargetArchitecture architecture)
		{
			foreach (string path in filePaths)
			{
				if (!string.IsNullOrEmpty(path))
				{
					AssemblyDefinition assembly = this.GetAssembly(path, parameters);
					if (assembly.MainModule.GetModuleArchitecture().CanReference(architecture))
					{
						return assembly;
					}
				}
			}
			return null;
		}

        /*Telerik Authorship*/
        public string ResolveAssemblyPath(string strongName, SpecialTypeAssembly special)
		{
			AssemblyNameReference nameRef = AssemblyNameReference.Parse(strongName);
			TargetArchitecture architecture = this.GetArchitectureFromStrongName(strongName);

			AssemblyName assemblyName = new AssemblyName(nameRef.Name,
												nameRef.FullName,
												nameRef.Version,
												nameRef.PublicKeyToken) { TargetArchitecture = architecture };
            
            AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(assemblyName.FullName, architecture, special);
            IEnumerable<string> files = this.assemblyPathResolver.GetAssemblyPaths(assemblyName, assemblyKey);

			foreach (string file in files)
			{
				if (this.GetAssemblyDefinition(file).MainModule.GetModuleArchitecture().CanReference(architecture))
				{
					return file;
				}
			}
			return string.Empty;
		}

		private TargetArchitecture GetArchitectureFromStrongName(string strongName)
		{
			string[] parts = strongName.Split(new string[] { ", " }, StringSplitOptions.None);

			string architectureString = string.Empty;
			foreach (var part in parts)
			{
				if (part.StartsWith("Architecture="))
				{
					architectureString = part.Split('=')[1].ToLowerInvariant();
				}
			}

			switch (architectureString)
			{
				case "amd64":
					return TargetArchitecture.AMD64;
				case "ia64":
					return TargetArchitecture.IA64;
				default:
					return TargetArchitecture.I386;
			}
		}

        /*Telerik Authorship*/
        public virtual void AddToAssemblyCache(string filePath, TargetArchitecture platform, bool storeAssemblyDefInCahce = false)
		{
            this.assemblyPathResolver.AddToAssemblyCache(filePath, platform);

            this.AddSearchDirectory(Path.GetDirectoryName(filePath));
			if (storeAssemblyDefInCahce && !this.filePathToAssemblyDefinitionCache.ContainsKey(filePath))
			{
				AssemblyDefinition assemblyDef = this.LoadAssemblyDefinition(filePath, new ReaderParameters(this), loadPdb: true);
				if (assemblyDef != null)
                {
                    string strongName = assemblyDef.MainModule.Name;
                    TargetArchitecture architecture = assemblyDef.MainModule.Architecture;
                    SpecialTypeAssembly special = assemblyDef.MainModule.IsReferenceAssembly() ? SpecialTypeAssembly.Reference : SpecialTypeAssembly.None;

                    AssemblyStrongNameExtended assemblyKey = new AssemblyStrongNameExtended(strongName, architecture, special);

                    this.AddToResolvedAssemblies(assemblyDef);
                    this.assemblyPathResolver.RemoveFromUnresolvedCache(assemblyKey);
                }
            }
        }

        /*Telerik Authorship*/
        public virtual string FindAssemblyPath(AssemblyName assemblyName, string fallbackDir, AssemblyStrongNameExtended assemblyKey, bool bubbleToUserIfFailed = true)
		{
			if (this.assemblyPathResolver.IsFailedAssembly(assemblyKey))
			{
				return null;
			}

			AssemblyDefinition resolvedAssembly = this.GetFromResolvedAssemblies(assemblyName, SpecialTypeAssembly.None);
			if (resolvedAssembly != null)
			{
				return resolvedAssembly.MainModule.FilePath;
			}

			var assemblyNameRef = new AssemblyNameReference(assemblyName.Name, assemblyName.Version) { PublicKeyToken = assemblyName.PublicKeyToken };

			IEnumerable<string> results;
			if (this.assemblyPathResolver.TryGetAssemblyPathsFromCache(assemblyName, assemblyKey, out results))
			{
				return results.FirstOrDefault();
			}
			AssemblyDefinition assemblyDefinition = this.SearchDirectory(assemblyNameRef, new ReaderParameters(this), assemblyName.TargetArchitecture, fallbackDir);

			if (assemblyDefinition != null)
			{
                this.assemblyPathResolver.AddToAssemblyPathNameCache(assemblyName, assemblyDefinition.MainModule.FilePath);

				return assemblyDefinition.MainModule.FilePath;
			}

            string result = this.assemblyPathResolver.GetAssemblyPath(assemblyName, assemblyKey);

	
			if (!string.IsNullOrWhiteSpace(result))
			{
				return result;
			}
			if (bubbleToUserIfFailed)
			{
				assemblyDefinition = this.UserSpecifiedAssembly(assemblyNameRef, assemblyName.TargetArchitecture, assemblyKey);
			}
			if (assemblyDefinition != null)
			{
				string filePath = assemblyDefinition.MainModule.FilePath;

				this.AddSearchDirectory(Path.GetDirectoryName(filePath));

				return filePath;
			}

			return result;
		}

		public virtual TargetPlatform GetTargetPlatform(string assemblyFilePath)
		{
            /*Telerik Authorship*/
            return this.targetPlatformResolver.GetTargetPlatform(assemblyFilePath, this);
		}

		/*Telerik Authorship*/
		public virtual void ClearCache()
        {
            /*Telerik Authorship*/
            this.DoWithWriteLock(this.directoriesLock, () =>
            {
                this.directories.Clear();
            });

            /*Telerik Authorship*/
            this.DoWithWriteLock(this.directoryAssembliesLock, () =>
            {
                this.directoryAssemblies.Clear();
            });

            this.ClearResolvedAssembliesCache();

			this.filePathToAssemblyDefinitionCache.Clear();

			this.assemblyPathResolver.ClearCache();
		}

		/*Telerik Authorship*/
		protected virtual void ClearResolvedAssembliesCache()
		{
			this.resolvedAssemblies.Clear();
		}

		public void RemoveFromAssemblyCache(string fileName)
		{
            this.assemblyPathResolver.RemoveFromAssemblyCache(fileName);

			AssemblyDefinition assemblyDef;
			if (this.filePathToAssemblyDefinitionCache.TryGetValue(fileName, out assemblyDef))
            {
                /*Telerik Authorship*/
                AssemblyStrongNameExtended assemblyKey = this.GetAssemblyKey(assemblyDef);
				List<AssemblyDefinition> assemblyDefinitions;
				if (this.TryGetResolvedAssembly(assemblyKey, out assemblyDefinitions))
				{
					assemblyDefinitions.Remove(assemblyDef);
					if (assemblyDefinitions.Count == 0)
					{
                        this.RemoveFromResolvedAssemblies(assemblyKey);
					}
				}

                this.filePathToAssemblyDefinitionCache.Remove(fileName);
			}
		}

		/*Telerik Authorship*/
		protected virtual void RemoveFromResolvedAssemblies(AssemblyStrongNameExtended assemblyKey)
		{
            this.resolvedAssemblies.Remove(assemblyKey);
		}

        /*Telerik Authorship*/
        public void RemoveFromFailedAssemblies(AssemblyStrongNameExtended assemblyName)
		{
            this.assemblyPathResolver.RemoveFromUnresolvedCache(assemblyName);
		}

		public AssemblyDefinition GetAssemblyDefinition(string filePath)
		{
			if (string.IsNullOrWhiteSpace(filePath))
			{
				return null;
			}
			////NOTE: Need to get the full name as filePath can express a relative path - (start from cmd with retalive file args).
			string fullFilePathName = Path.GetFullPath(filePath);

			if (this.filePathToAssemblyDefinitionCache.ContainsKey(fullFilePathName))
			{
				AssemblyDefinition assemblyDefinition = this.filePathToAssemblyDefinitionCache[fullFilePathName];

				return assemblyDefinition;
			}
			else
			{
				AssemblyDefinition assemblyDef = this.LoadAssemblyDefinition(fullFilePathName, new ReaderParameters(this), loadPdb: true);

				if (assemblyDef == null)
				{
					return null;
				}

                this.AddToResolvedAssemblies(assemblyDef, filePath);
				return assemblyDef;
			}
		}

        private void AddToResolvedAssemblies(AssemblyDefinition assemblyDef)
        {
            this.AddToResolvedAssemblies(assemblyDef, assemblyDef.MainModule.FilePath);
        }

        private void AddToResolvedAssemblies(AssemblyDefinition assemblyDef, string key)
		{
            if (string.IsNullOrEmpty(key))
            {
				key = Guid.NewGuid().ToString();
            }

			if (this.filePathToAssemblyDefinitionCache.ContainsKey(key)) return;
            this.filePathToAssemblyDefinitionCache.Add(key, assemblyDef);

            /*Telerik Authorship*/
            AssemblyStrongNameExtended assemblyKey = this.GetAssemblyKey(assemblyDef);
			List<AssemblyDefinition> assemblyList;
			if (!this.TryGetResolvedAssembly(assemblyKey, out assemblyList))
			{
				assemblyList = new List<AssemblyDefinition>();
                this.AddToResolvedAssembliesInternal(assemblyKey, assemblyList);
			}

			assemblyList.Add(assemblyDef);
		}

		/*Telerik Authorship*/
		protected virtual void AddToResolvedAssembliesInternal(AssemblyStrongNameExtended assemblyKey, List<AssemblyDefinition> assemblyList)
		{
            this.resolvedAssemblies.Add(assemblyKey, assemblyList);
		}

		public AssemblyDefinition LoadAssemblyDefinition(string filePath, ReaderParameters parameters, bool loadPdb)
		{
			try
			{
				if (loadPdb)
				{
					SetSymbolStore(filePath, parameters);
				}
				return ModuleDefinition.ReadModule(filePath, parameters).Assembly;
			}
			catch (Exception ex)
			{
                if (loadPdb && (ex.GetType().FullName == "Microsoft.Cci.Pdb.PdbException" /*Telerik Authorship*/|| ex.GetType().FullName == "Microsoft.Cci.Pdb.PdbDebugException"))
                {
                    //// NOTE: There is no other way to catch a PdbException as it is internal!

                    var exception = new Exception(string.Format("Failed reading {0}\\{1}.pdb", Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath)), ex);
                    this.AssemblyDefinitionFailure(this, exception);
                
					parameters.ReadSymbols = false;

					return this.LoadAssemblyDefinition(filePath, parameters, false);
				}
                else if (ex.Message == "Magic is wrong.")
                {
                    parameters.ReadSymbols = false;
                    return this.LoadAssemblyDefinition(filePath, parameters, false);
                }
				else
				{
					var exception = new Exception(filePath, ex);

                    this.AssemblyDefinitionFailure(this, exception);
				}
				return null;
			}
			finally
			{
				if (parameters.SymbolStream != null)
				{
					parameters.SymbolStream.Dispose();
				}
			}
		}

		private static void SetSymbolStore(string fileName, ReaderParameters p)
		{
			// search for pdb in same directory as dll
			string pdbName = Path.ChangeExtension(fileName, ".pdb");

			if (File.Exists(pdbName))
			{
				try
				{
					p.ReadSymbols = true;
					p.SymbolStream = File.OpenRead(pdbName);
				}
				catch (Exception)
				{
				}
			}
			// TODO : include microsoft symbol store.
		}

        /*Telerik Authorship*/
        public void SetNotResolvedAssembliesForCurrentSession(IList<AssemblyStrongNameExtended> list)
		{
            this.assemblyPathResolver.SetFailedAssemblyCache(list);
		}

		public void AddResolvedAssembly(string filePath)
		{
            this.AddSearchDirectory(Path.GetDirectoryName(filePath));

			if (!this.userDefinedAssemblies.Contains(filePath))
			{
                this.userDefinedAssemblies.Add(filePath);
			}
		}

        /*Telerik Authorship*/
        public IEnumerable<AssemblyStrongNameExtended> GetNotResolvedAssemblyNames()
		{
			return this.assemblyPathResolver.GetAssemblyFailedResolvedCache();
		}

		public IEnumerable<string> GetUserDefiniedAssemblies()
		{
			return this.userDefinedAssemblies;
		}

		public void ClearAssemblyFailedResolverCache()
		{
            this.assemblyPathResolver.ClearAssemblyFailedResolverCache();
		}
        
		/*Telerik Authorship*/
		private AssemblyStrongNameExtended GetAssemblyKey(AssemblyDefinition assemblyDefinition)
		{
			ModuleDefinition moduleDefinition = assemblyDefinition.MainModule;

            SpecialTypeAssembly special = moduleDefinition.IsReferenceAssembly() ? SpecialTypeAssembly.Reference : SpecialTypeAssembly.None;

            return new AssemblyStrongNameExtended(assemblyDefinition.FullName, moduleDefinition.GetModuleArchitecture(), special);
        }

        /*Telerik Authorship*/
        private void DoWithReadLock(ReaderWriterLockSlim locker, Action action)
        {
            locker.EnterReadLock();
            try
            {
                action();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /*Telerik Authorship*/
        private T DoWithReadLock<T>(ReaderWriterLockSlim locker, Func<T> func)
        {
            locker.EnterReadLock();
            try
            {
                return func();
            }
            finally
            {
                locker.ExitReadLock();
            }
        }

        /*Telerik Authorship*/
        private void DoWithWriteLock(ReaderWriterLockSlim locker, Action action)
        {
            locker.EnterWriteLock();
            try
            {
                action();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        /*Telerik Authorship*/
        private T DoWithWriteLock<T>(ReaderWriterLockSlim locker, Func<T> func)
        {
            locker.EnterWriteLock();
            try
            {
                return func();
            }
            finally
            {
                locker.ExitWriteLock();
            }
        }

        protected class DirectoryAssemblyInfo
		{
			public string FullFileName { get; set; }
			public string FileNameWithoutExtension { get; set; }
			public string Dir { get; set; }

			public DirectoryAssemblyInfo(string fullFileName, string fileNameWithoutExtension)
			{
				this.FullFileName = fullFileName;
				this.FileNameWithoutExtension = fileNameWithoutExtension;
			}
		}
#endregion
	}
}
