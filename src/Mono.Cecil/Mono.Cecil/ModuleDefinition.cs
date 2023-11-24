//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using SR = System.Reflection;
/*Telerik Authorship*/
using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;
using Mono.Collections.Generic;

namespace Mono.Cecil {

	public enum ReadingMode {
		Immediate = 1,
		Deferred = 2,
	}

	public sealed class ReaderParameters {

		ReadingMode reading_mode;
		IAssemblyResolver assembly_resolver;
		IMetadataResolver metadata_resolver;
		Stream symbol_stream;
		ISymbolReaderProvider symbol_reader_provider;
		bool read_symbols;

		public ReadingMode ReadingMode {
			get { return this.reading_mode; }
			set { this.reading_mode = value; }
		}

		public IAssemblyResolver AssemblyResolver {
			get { return this.assembly_resolver; }
			set { this.assembly_resolver = value; }
		}

		public IMetadataResolver MetadataResolver {
			get { return this.metadata_resolver; }
			set { this.metadata_resolver = value; }
		}

		public Stream SymbolStream {
			get { return this.symbol_stream; }
			set { this.symbol_stream = value; }
		}

		public ISymbolReaderProvider SymbolReaderProvider {
			get { return this.symbol_reader_provider; }
			set { this.symbol_reader_provider = value; }
		}

		public bool ReadSymbols {
			get { return this.read_symbols; }
			set { this.read_symbols = value; }
		}

		/*Telerik Authorship*/
		public ReaderParameters (IAssemblyResolver resolver)
			: this (resolver, ReadingMode.Deferred)
		{
		}

		/*Telerik Authorship*/
		public ReaderParameters (IAssemblyResolver resolver, ReadingMode readingMode)
		{
			/*Telerik Authorship*/
			if (resolver == null)
			{
				throw new ArgumentNullException("resolver");
			}
			this.assembly_resolver = resolver;

			this.reading_mode = readingMode;
		}
	}

#if !READ_ONLY

	public sealed class ModuleParameters {

		ModuleKind kind;
		TargetRuntime runtime;
		TargetArchitecture architecture;
		IAssemblyResolver assembly_resolver;
		IMetadataResolver metadata_resolver;

		public ModuleKind Kind {
			get { return this.kind; }
			set { this.kind = value; }
		}

		public TargetRuntime Runtime {
			get { return this.runtime; }
			set { this.runtime = value; }
		}

		public TargetArchitecture Architecture {
			get { return this.architecture; }
			set { this.architecture = value; }
		}

		public IAssemblyResolver AssemblyResolver {
			get { return this.assembly_resolver; }
			set { this.assembly_resolver = value; }
		}

		public IMetadataResolver MetadataResolver {
			get { return this.metadata_resolver; }
			set { this.metadata_resolver = value; }
		}

		public ModuleParameters ()
		{
			this.kind = ModuleKind.Dll;
			this.Runtime = GetCurrentRuntime ();
			this.architecture = TargetArchitecture.I386;
		}

		static TargetRuntime GetCurrentRuntime ()
		{
#if !CF
			return typeof (object).Assembly.ImageRuntimeVersion.ParseRuntime ();
#else
			var corlib_version = typeof (object).Assembly.GetName ().Version;
			switch (corlib_version.Major) {
			case 1:
				return corlib_version.Minor == 0
					? TargetRuntime.Net_1_0
					: TargetRuntime.Net_1_1;
			case 2:
				return TargetRuntime.Net_2_0;
			case 4:
				return TargetRuntime.Net_4_0;
			default:
				throw new NotSupportedException ();
			}
#endif
		}
	}

	public sealed class WriterParameters {

		Stream symbol_stream;
		ISymbolWriterProvider symbol_writer_provider;
		bool write_symbols;
#if !SILVERLIGHT && !CF
		SR.StrongNameKeyPair key_pair;
#endif
		public Stream SymbolStream {
			get { return this.symbol_stream; }
			set { this.symbol_stream = value; }
		}

		public ISymbolWriterProvider SymbolWriterProvider {
			get { return this.symbol_writer_provider; }
			set { this.symbol_writer_provider = value; }
		}

		public bool WriteSymbols {
			get { return this.write_symbols; }
			set { this.write_symbols = value; }
		}
#if !SILVERLIGHT && !CF
		public SR.StrongNameKeyPair StrongNameKeyPair {
			get { return this.key_pair; }
			set { this.key_pair = value; }
		}
#endif

		/*Telerik Authorship*/
		public Dictionary<MethodDefinition, Dictionary<VariableDefinition, string>> MethodsVariableDefinitionToNameMap { get; set; }
	}

#endif

	public sealed class ModuleDefinition : ModuleReference, ICustomAttributeProvider {

		internal Image Image;
		internal MetadataSystem MetadataSystem;
		internal ReadingMode ReadingMode;
		internal ISymbolReaderProvider SymbolReaderProvider;

		internal ISymbolReader symbol_reader;
		internal IAssemblyResolver assembly_resolver;
		internal IMetadataResolver metadata_resolver;
		internal TypeSystem type_system;

		readonly MetadataReader reader;
		readonly string fq_name;

		internal string runtime_version;
		internal ModuleKind kind;
		TargetRuntime runtime;
		TargetArchitecture architecture;
		ModuleAttributes attributes;
		ModuleCharacteristics characteristics;
		Guid mvid;

		internal AssemblyDefinition assembly;
		MethodDefinition entry_point;

#if !READ_ONLY
		MetadataImporter importer;
#endif
		Collection<CustomAttribute> custom_attributes;
		Collection<AssemblyNameReference> references;
		Collection<ModuleReference> modules;
		Collection<Resource> resources;
		Collection<ExportedType> exported_types;
		TypeDefinitionCollection types;

		public bool IsMain {
			get { return this.kind != ModuleKind.NetModule; }
		}

		/*TelerikAuthorship*/
		public string FilePath 
		{
			get 
			{
				return this.Image.FileName;
			}
		}

		public ModuleKind Kind {
			get { return this.kind; }
			set { this.kind = value; }
		}

		public TargetRuntime Runtime {
			get { return this.runtime; }
			set {
                this.runtime = value;
                this.runtime_version = this.runtime.RuntimeVersionString ();
			}
		}

		public string RuntimeVersion {
			get { return this.runtime_version; }
			set {
                this.runtime_version = value;
                this.runtime = this.runtime_version.ParseRuntime ();
			}
		}

		public TargetArchitecture Architecture {
			get { return this.architecture; }
			set { this.architecture = value; }
		}

		public ModuleAttributes Attributes {
			get { return this.attributes; }
			set { this.attributes = value; }
		}

		public ModuleCharacteristics Characteristics {
			get { return this.characteristics; }
			set { this.characteristics = value; }
		}

		public string FullyQualifiedName {
			get { return this.fq_name; }
		}

		/*Telerik Authorship*/
		string directoryPath;
		public string ModuleDirectoryPath
		{
			get
			{
				return this.directoryPath ?? (this.directoryPath = Path.GetDirectoryName(this.fq_name));
			}
		}

		public Guid Mvid {
			get { return this.mvid; }
			set { this.mvid = value; }
		}

		internal bool HasImage {
			get { return this.Image != null; }
		}

		public bool HasSymbols {
			get { return this.symbol_reader != null; }
		}

		public ISymbolReader SymbolReader {
			get { return this.symbol_reader; }
		}

		public override MetadataScopeType MetadataScopeType {
			get { return MetadataScopeType.ModuleDefinition; }
		}

		public AssemblyDefinition Assembly {
			get { return this.assembly; }
			/*Telerik Authorship*/
			set { this.assembly = value; }
		}

#if !READ_ONLY
		internal MetadataImporter MetadataImporter {
			get {
				if (this.importer == null)
					Interlocked.CompareExchange (ref this.importer, new MetadataImporter (this), null);

				return this.importer;
			}
		}
#endif

		public IAssemblyResolver AssemblyResolver {
			get {
				if (this.assembly_resolver == null)
					Interlocked.CompareExchange (ref this.assembly_resolver, /*Telerik Authorship*/ GlobalAssemblyResolver.Instance, null);

				return this.assembly_resolver;
			}
		}

		public IMetadataResolver MetadataResolver {
			get {
				if (this.metadata_resolver == null)
					Interlocked.CompareExchange (ref this.metadata_resolver, new MetadataResolver (this.AssemblyResolver), null);

				return this.metadata_resolver;
			}
		}

		public TypeSystem TypeSystem {
			get {
				if (this.type_system == null)
					Interlocked.CompareExchange (ref this.type_system, TypeSystem.CreateTypeSystem (this), null);

				return this.type_system;
			}
		}

		public bool HasAssemblyReferences {
			get {
				if (this.references != null)
					return this.references.Count > 0;

				return this.HasImage && this.Image.HasTable (Table.AssemblyRef);
			}
		}

		public Collection<AssemblyNameReference> AssemblyReferences {
			get {
				if (this.references != null)
					return this.references;

				if (this.HasImage)
					return this.Read (ref this.references, this, (_, reader) => reader.ReadAssemblyReferences ());

				return this.references = new Collection<AssemblyNameReference> ();
			}
		}

		public bool HasModuleReferences {
			get {
				if (this.modules != null)
					return this.modules.Count > 0;

				return this.HasImage && this.Image.HasTable (Table.ModuleRef);
			}
		}

		public Collection<ModuleReference> ModuleReferences {
			get {
				if (this.modules != null)
					return this.modules;

				if (this.HasImage)
					return this.Read (ref this.modules, this, (_, reader) => reader.ReadModuleReferences ());

				return this.modules = new Collection<ModuleReference> ();
			}
		}

		public bool HasResources {
			get {
				if (this.resources != null)
					return this.resources.Count > 0;

				if (this.HasImage)
					return this.Image.HasTable (Table.ManifestResource) || this.Read (this, (_, reader) => reader.HasFileResource ());

				return false;
			}
		}

		public Collection<Resource> Resources {
			get {
				if (this.resources != null)
					return this.resources;

				if (this.HasImage)
					return this.Read (ref this.resources, this, (_, reader) => reader.ReadResources ());

				return this.resources = new Collection<Resource> ();
			}
		}

		/*Telerik Authorship*/
		private bool? hasCustomAttributes;
		public bool HasCustomAttributes {
			get {
				if (this.custom_attributes != null)
					return this.custom_attributes.Count > 0;

				/*Telerik Authorship*/
				if (this.hasCustomAttributes != null)
					return this.hasCustomAttributes == true;

				/*Telerik Authorship*/
				return this.GetHasCustomAttributes (ref this.hasCustomAttributes, this);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this)); }
		}

		public bool HasTypes {
			get {
				if (this.types != null)
					return this.types.Count > 0;

				return this.HasImage && this.Image.HasTable (Table.TypeDef);
			}
		}

		public Collection<TypeDefinition> Types {
			get {
				if (this.types != null)
					return this.types;

				if (this.HasImage)
					return this.Read (ref this.types, this, (_, reader) => reader.ReadTypes ());

				return this.types = new TypeDefinitionCollection (this);
			}
		}

		/*Telerik Authorship*/
		private ICollection<TypeDefinition> allTypes;

		/*Telerik Authorship*/
		private object allTypesLock = new object();

		/*Telerik Authorship*/
		public ICollection<TypeDefinition> AllTypes
		{
			get
			{
				lock (this.allTypesLock)
				{
					if (this.allTypes != null)
					{
						return this.allTypes;
					}

                    this.allTypes = this.GetAllTypes();

					return this.allTypes;
				}
			}
		}

		/*Telerik Authorship*/
		private ICollection<TypeDefinition> GetAllTypes()
		{
			List<TypeDefinition> result = new List<TypeDefinition>();

			foreach (TypeDefinition type in this.Types)
			{
				result.AddRange(this.GetTypeAndNestedTypes(type));
			}

			return result;
		}

		/*Telerik Authorship*/
		private ICollection<TypeDefinition> GetTypeAndNestedTypes(TypeDefinition type)
		{
			List<TypeDefinition> result = new List<TypeDefinition>();

			result.Add(type);

			if (type.HasNestedTypes)
			{
				foreach (TypeDefinition nestedType in type.NestedTypes)
				{
					result.AddRange(this.GetTypeAndNestedTypes(nestedType));
				}
			}

			return result;
		}


		public bool HasExportedTypes {
			get {
				if (this.exported_types != null)
					return this.exported_types.Count > 0;

				return this.HasImage && this.Image.HasTable (Table.ExportedType);
			}
		}

		public Collection<ExportedType> ExportedTypes {
			get {
				if (this.exported_types != null)
					return this.exported_types;

				if (this.HasImage)
					return this.Read (ref this.exported_types, this, (_, reader) => reader.ReadExportedTypes ());

				return this.exported_types = new Collection<ExportedType> ();
			}
		}

		public MethodDefinition EntryPoint {
			get {
				if (this.entry_point != null)
					return this.entry_point;

				if (this.HasImage)
					return this.Read (ref this.entry_point, this, (_, reader) => reader.ReadEntryPoint ());

				return this.entry_point = null;
			}
			set { this.entry_point = value; }
		}

		internal ModuleDefinition (/*Telerik Authorship*/ IAssemblyResolver resolver)
		{
			this.MetadataSystem = new MetadataSystem ();
			this.token = new MetadataToken (TokenType.Module, 1);
			
			/*Telerik Authorship*/
			this.assembly_resolver = resolver;
		}

		internal ModuleDefinition (Image image, /*Telerik Authorship*/ IAssemblyResolver resolver)
			: this(/*Telerik Authorship*/ resolver)
		{
			this.Image = image;
			this.kind = image.Kind;
			this.RuntimeVersion = image.RuntimeVersion;
			this.architecture = image.Architecture;
			this.attributes = image.Attributes;
			this.characteristics = image.Characteristics;
			this.fq_name = image.FileName;

			this.reader = new MetadataReader (this);
		}

		public bool HasTypeReference (string fullName)
		{
			return this.HasTypeReference (string.Empty, fullName);
		}

		public bool HasTypeReference (string scope, string fullName)
		{
			CheckFullName (fullName);

			if (!this.HasImage)
				return false;

			return this.GetTypeReference (scope, fullName) != null;
		}

		public bool TryGetTypeReference (string fullName, out TypeReference type)
		{
			return this.TryGetTypeReference (string.Empty, fullName, out type);
		}

		public bool TryGetTypeReference (string scope, string fullName, out TypeReference type)
		{
			CheckFullName (fullName);

			if (!this.HasImage) {
				type = null;
				return false;
			}

			return (type = this.GetTypeReference (scope, fullName)) != null;
		}

		TypeReference GetTypeReference (string scope, string fullname)
		{
			return this.Read (new Row<string, string> (scope, fullname), (row, reader) => reader.GetTypeReference (row.Col1, row.Col2));
		}

		public IEnumerable<TypeReference> GetTypeReferences ()
		{
			if (!this.HasImage)
				return Empty<TypeReference>.Array;

			return this.Read (this, (_, reader) => reader.GetTypeReferences ());
		}

		public IEnumerable<MemberReference> GetMemberReferences ()
		{
			if (!this.HasImage)
				return Empty<MemberReference>.Array;

			return this.Read (this, (_, reader) => reader.GetMemberReferences ());
		}

		public TypeReference GetType (string fullName, bool runtimeName)
		{
			return runtimeName
				? TypeParser.ParseType (this, fullName)
				: this.GetType (fullName);
		}

		public TypeDefinition GetType (string fullName)
		{
			CheckFullName (fullName);

			var position = fullName.IndexOf ('/');
			if (position > 0)
				return this.GetNestedType (fullName);

			return ((TypeDefinitionCollection) this.Types).GetType (fullName);
		}

		public TypeDefinition GetType (string @namespace, string name)
		{
			Mixin.CheckName (name);

			return ((TypeDefinitionCollection) this.Types).GetType (@namespace ?? string.Empty, name);
		}

		public IEnumerable<TypeDefinition> GetTypes ()
		{
			return GetTypes (this.Types);
		}

		static IEnumerable<TypeDefinition> GetTypes (Collection<TypeDefinition> types)
		{
			for (int i = 0; i < types.Count; i++) {
				var type = types [i];

				yield return type;

				if (!type.HasNestedTypes)
					continue;

				foreach (var nested in GetTypes (type.NestedTypes))
					yield return nested;
			}
		}

		static void CheckFullName (string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fullName.Length == 0)
				throw new ArgumentException ();
		}

		TypeDefinition GetNestedType (string fullname)
		{
			var names = fullname.Split ('/');
			var type = this.GetType (names [0]);

			if (type == null)
				return null;

			for (int i = 1; i < names.Length; i++) {
				var nested_type = type.GetNestedType (names [i]);
				if (nested_type == null)
					return null;

				type = nested_type;
			}

			return type;
		}

		internal FieldDefinition Resolve (FieldReference field)
		{
			return this.MetadataResolver.Resolve (field);
		}

		internal MethodDefinition Resolve (MethodReference method)
		{
			return this.MetadataResolver.Resolve (method);
		}

		internal TypeDefinition Resolve (TypeReference type)
		{
			return this.MetadataResolver.Resolve (type);
		}

		/*Telerik Authorship*/
		internal TypeDefinition Resolve(TypeReference type, ICollection<string> visitedDlls)
		{
			return ((MetadataResolver)this.MetadataResolver).Resolve(type, visitedDlls);
		}

#if !READ_ONLY

		static void CheckType (object type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		}

		static void CheckField (object field)
		{
			if (field == null)
				throw new ArgumentNullException ("field");
		}

		static void CheckMethod (object method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");
		}

		static void CheckContext (IGenericParameterProvider context, ModuleDefinition module)
		{
			if (context == null)
				return;

			if (context.Module != module)
				throw new ArgumentException ();
		}

		static ImportGenericContext GenericContextFor (IGenericParameterProvider context)
		{
			return context != null ? new ImportGenericContext (context) : default (ImportGenericContext);
		}

#if !CF

		public TypeReference Import (Type type)
		{
			return this.Import (type, null);
		}

		public TypeReference Import (Type type, IGenericParameterProvider context)
		{
			CheckType (type);
			CheckContext (context, this);

			return this.MetadataImporter.ImportType (
				type,
				GenericContextFor (context),
				context != null ? ImportGenericKind.Open : ImportGenericKind.Definition);
		}

		public FieldReference Import (SR.FieldInfo field)
		{
			return this.Import (field, null);
		}

		public FieldReference Import (SR.FieldInfo field, IGenericParameterProvider context)
		{
			CheckField (field);
			CheckContext (context, this);

			return this.MetadataImporter.ImportField (field, GenericContextFor (context));
		}

		public MethodReference Import (SR.MethodBase method)
		{
			CheckMethod (method);

			return this.MetadataImporter.ImportMethod (method, default (ImportGenericContext), ImportGenericKind.Definition);
		}

		public MethodReference Import (SR.MethodBase method, IGenericParameterProvider context)
		{
			CheckMethod (method);
			CheckContext (context, this);

			return this.MetadataImporter.ImportMethod (method,
				GenericContextFor (context),
				context != null ? ImportGenericKind.Open : ImportGenericKind.Definition);
		}
#endif

		public TypeReference Import (TypeReference type)
		{
			CheckType (type);

			if (type.Module == this)
				return type;

			return this.MetadataImporter.ImportType (type, default (ImportGenericContext));
		}

		public TypeReference Import (TypeReference type, IGenericParameterProvider context)
		{
			CheckType (type);

			if (type.Module == this)
				return type;

			CheckContext (context, this);

			return this.MetadataImporter.ImportType (type, GenericContextFor (context));
		}

		public FieldReference Import (FieldReference field)
		{
			CheckField (field);

			if (field.Module == this)
				return field;

			return this.MetadataImporter.ImportField (field, default (ImportGenericContext));
		}

		public FieldReference Import (FieldReference field, IGenericParameterProvider context)
		{
			CheckField (field);

			if (field.Module == this)
				return field;

			CheckContext (context, this);

			return this.MetadataImporter.ImportField (field, GenericContextFor (context));
		}

		public MethodReference Import (MethodReference method)
		{
			return this.Import (method, null);
		}

		public MethodReference Import (MethodReference method, IGenericParameterProvider context)
		{
			CheckMethod (method);

			if (method.Module == this)
				return method;

			CheckContext (context, this);

			return this.MetadataImporter.ImportMethod (method, GenericContextFor (context));
		}

#endif

		public IMetadataTokenProvider LookupToken (int token)
		{
			return this.LookupToken (new MetadataToken ((uint) token));
		}

		public IMetadataTokenProvider LookupToken (MetadataToken token)
		{
			return this.Read (token, (t, reader) => reader.LookupToken (t));
		}

		readonly object module_lock = new object();

		internal object SyncRoot {
			get { return this.module_lock; }
		}

		internal TRet Read<TItem, TRet> (TItem item, Func<TItem, MetadataReader, TRet> read)
		{
			lock (this.module_lock) {
				var position = this.reader.position;
				var context = this.reader.context;

				var ret = read (item, this.reader);

                this.reader.position = position;
                this.reader.context = context;

				return ret;
			}
		}

		internal TRet Read<TItem, TRet> (ref TRet variable, TItem item, Func<TItem, MetadataReader, TRet> read) /*Telerik Authorship*/ // where TRet : class
		{
			lock (this.module_lock) {
				if (variable != null)
					return variable;

				var position = this.reader.position;
				var context = this.reader.context;

				var ret = read (item, this.reader);

                this.reader.position = position;
                this.reader.context = context;

				return variable = ret;
			}
		}

		public bool HasDebugHeader {
			get { return this.Image != null && !this.Image.Debug.IsZero; }
		}

		public ImageDebugDirectory GetDebugHeader (out byte [] header)
		{
			if (!this.HasDebugHeader)
				throw new InvalidOperationException ();

			return this.Image.GetDebugHeader (out header);
		}

		void ProcessDebugHeader ()
		{
			if (!this.HasDebugHeader)
				return;

			byte [] header;
			var directory = this.GetDebugHeader (out header);

			if (!this.symbol_reader.ProcessDebugHeader (directory, header))
			{
				/*Telerik Authorship*/
				//throw new InvalidOperationException ();
				return;
			}
		}

#if !READ_ONLY

		public static ModuleDefinition CreateModule (string name, ModuleKind kind)
		{
			return CreateModule (name, new ModuleParameters { Kind = kind });
		}

		public static ModuleDefinition CreateModule (string name, ModuleParameters parameters)
		{
			Mixin.CheckName (name);
			Mixin.CheckParameters (parameters);

			var module = new ModuleDefinition (GlobalAssemblyResolver.Instance) {
				Name = name,
				kind = parameters.Kind,
				Runtime = parameters.Runtime,
				architecture = parameters.Architecture,
				mvid = Guid.NewGuid (),
				Attributes = ModuleAttributes.ILOnly,
				Characteristics = (ModuleCharacteristics) 0x8540,
			};

			if (parameters.AssemblyResolver != null)
				module.assembly_resolver = parameters.AssemblyResolver;

			if (parameters.MetadataResolver != null)
				module.metadata_resolver = parameters.MetadataResolver;

			if (parameters.Kind != ModuleKind.NetModule) {
				var assembly = new AssemblyDefinition ();
				module.assembly = assembly;
				module.assembly.Name = CreateAssemblyName (name);
				assembly.main_module = module;
			}

			module.Types.Add (new TypeDefinition (string.Empty, "<Module>", TypeAttributes.NotPublic));

			return module;
		}

		static AssemblyNameDefinition CreateAssemblyName (string name)
		{
			if (name.EndsWith (".dll") || name.EndsWith (".exe"))
				name = name.Substring (0, name.Length - 4);

			return new AssemblyNameDefinition (name, new Version (0, 0, 0, 0));
		}

#endif

		public void ReadSymbols ()
		{
			if (string.IsNullOrEmpty (this.fq_name))
			{
				/*Telerik Authorship*/
				//throw new InvalidOperationException ();
				return;
			}

			var provider = SymbolProvider.GetPlatformReaderProvider ();
			if (provider == null)
				throw new InvalidOperationException ();

            this.ReadSymbols (provider.GetSymbolReader (this, this.fq_name));
		}

		public void ReadSymbols (ISymbolReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException ("reader");

            this.symbol_reader = reader;

            this.ProcessDebugHeader ();
		}

		/*Telerik Authorship*/
		//public static ModuleDefinition ReadModule (string fileName)
		//{
		//    return ReadModule (fileName, new ReaderParameters (ReadingMode.Deferred));
		//}

		/*Telerik Authorship*/
		//public static ModuleDefinition ReadModule (Stream stream)
		//{
		//    return ReadModule (stream, new ReaderParameters (ReadingMode.Deferred));
		//}

		public static ModuleDefinition ReadModule (string fileName, ReaderParameters parameters)
		{
			using (var stream = GetFileStream (fileName, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				return ReadModule (stream, parameters);
			}
		}

		static void CheckStream (object stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
		}

		public static ModuleDefinition ReadModule (Stream stream, ReaderParameters parameters)
		{
			CheckStream (stream);
			if (!stream.CanRead || !stream.CanSeek)
				throw new ArgumentException ();
			Mixin.CheckParameters (parameters);

			return ModuleReader.CreateModuleFrom (
				ImageReader.ReadImageFrom (stream),
				parameters);
		}

		static Stream GetFileStream (string fileName, FileMode mode, FileAccess access, FileShare share)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			if (fileName.Length == 0)
				throw new ArgumentException ();

			return new FileStream (fileName, mode, access, share);
		}

#if !READ_ONLY

		public void Write (string fileName)
		{
            this.Write (fileName, new WriterParameters ());
		}

		public void Write (Stream stream)
		{
            this.Write (stream, new WriterParameters ());
		}

		public void Write (string fileName, WriterParameters parameters)
		{
			using (var stream = GetFileStream (fileName, FileMode.Create, FileAccess.ReadWrite, FileShare.None)) {
                this.Write (stream, parameters);
			}
		}

		public void Write (Stream stream, WriterParameters parameters)
		{
			CheckStream (stream);
			if (!stream.CanWrite || !stream.CanSeek)
				throw new ArgumentException ();
			Mixin.CheckParameters (parameters);

			ModuleWriter.WriteModuleTo (this, stream, parameters);
		}

#endif

	}

	static partial class Mixin {

		public static void CheckParameters (object parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException ("parameters");
		}

		public static bool HasImage (this ModuleDefinition self)
		{
			return self != null && self.HasImage;
		}

		public static bool IsCorlib (this ModuleDefinition module)
		{
			if (module.Assembly == null)
				return false;

			return module.Assembly.Name.Name == "mscorlib";
		}

		public static string GetFullyQualifiedName (this Stream self)
		{
#if !SILVERLIGHT
			var file_stream = self as FileStream;
			if (file_stream == null)
				return string.Empty;

			return Path.GetFullPath (file_stream.Name);
#else
			return string.Empty;
#endif
		}

		public static TargetRuntime ParseRuntime (this string self)
		{
			switch (self [1]) {
			case '1':
				return self [3] == '0'
					? TargetRuntime.Net_1_0
					: TargetRuntime.Net_1_1;
			case '2':
				return TargetRuntime.Net_2_0;
			case '4':
			default:
				return TargetRuntime.Net_4_0;
			}
		}

		public static string RuntimeVersionString (this TargetRuntime runtime)
		{
			switch (runtime) {
			case TargetRuntime.Net_1_0:
				return "v1.0.3705";
			case TargetRuntime.Net_1_1:
				return "v1.1.4322";
			case TargetRuntime.Net_2_0:
				return "v2.0.50727";
			case TargetRuntime.Net_4_0:
			default:
				return "v4.0.30319";
			}
		}
	}
}
