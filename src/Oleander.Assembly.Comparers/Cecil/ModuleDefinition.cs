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
using Mono.Cecil.PE;
using Oleander.Assembly.Comparers.Cecil.Cil;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Mono.Cecil
{

    public enum ReadingMode
    {
        Immediate = 1,
        Deferred = 2,
    }

    public sealed class ReaderParameters
    {

        ReadingMode reading_mode;
        IAssemblyResolver assembly_resolver;
        IMetadataResolver metadata_resolver;
        Stream symbol_stream;
        ISymbolReaderProvider symbol_reader_provider;
        bool read_symbols;

        public ReadingMode ReadingMode
        {
            get { return this.reading_mode; }
            set { this.reading_mode = value; }
        }

        public IAssemblyResolver AssemblyResolver
        {
            get { return this.assembly_resolver; }
            set { this.assembly_resolver = value; }
        }

        public IMetadataResolver MetadataResolver
        {
            get { return this.metadata_resolver; }
            set { this.metadata_resolver = value; }
        }

        public Stream SymbolStream
        {
            get { return this.symbol_stream; }
            set { this.symbol_stream = value; }
        }

        public ISymbolReaderProvider SymbolReaderProvider
        {
            get { return this.symbol_reader_provider; }
            set { this.symbol_reader_provider = value; }
        }

        public bool ReadSymbols
        {
            get { return this.read_symbols; }
            set { this.read_symbols = value; }
        }

        /*Telerik Authorship*/
        public ReaderParameters(IAssemblyResolver resolver)
            : this(resolver, ReadingMode.Deferred)
        {
        }

        /*Telerik Authorship*/
        public ReaderParameters(IAssemblyResolver resolver, ReadingMode readingMode)
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



    public sealed class ModuleDefinition : ModuleReference, ICustomAttributeProvider
    {

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
        Collection<CustomAttribute> custom_attributes;
        Collection<AssemblyNameReference> references;
        Collection<ModuleReference> modules;
        Collection<Resource> resources;
        Collection<ExportedType> exported_types;
        TypeDefinitionCollection types;

        public bool IsMain
        {
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

        public ModuleKind Kind
        {
            get { return this.kind; }
            set { this.kind = value; }
        }

        public TargetRuntime Runtime
        {
            get { return this.runtime; }
            set
            {
                this.runtime = value;
                this.runtime_version = this.runtime.RuntimeVersionString();
            }
        }

        public string RuntimeVersion
        {
            get { return this.runtime_version; }
            set
            {
                this.runtime_version = value;
                this.runtime = this.runtime_version.ParseRuntime();
            }
        }

        public TargetArchitecture Architecture
        {
            get { return this.architecture; }
            set { this.architecture = value; }
        }

        public ModuleAttributes Attributes
        {
            get { return this.attributes; }
            set { this.attributes = value; }
        }

        public ModuleCharacteristics Characteristics
        {
            get { return this.characteristics; }
            set { this.characteristics = value; }
        }

        public string FullyQualifiedName
        {
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

        public Guid Mvid
        {
            get { return this.mvid; }
            set { this.mvid = value; }
        }

        internal bool HasImage
        {
            get { return this.Image != null; }
        }

        public bool HasSymbols
        {
            get { return this.symbol_reader != null; }
        }

        public ISymbolReader SymbolReader
        {
            get { return this.symbol_reader; }
        }

        public override MetadataScopeType MetadataScopeType
        {
            get { return MetadataScopeType.ModuleDefinition; }
        }

        public AssemblyDefinition Assembly
        {
            get { return this.assembly; }
            /*Telerik Authorship*/
            set { this.assembly = value; }
        }

        public IAssemblyResolver AssemblyResolver
        {
            get
            {
                if (this.assembly_resolver == null)
                    Interlocked.CompareExchange(ref this.assembly_resolver, /*Telerik Authorship*/ GlobalAssemblyResolver.Instance, null);

                return this.assembly_resolver;
            }
        }

        public IMetadataResolver MetadataResolver
        {
            get
            {
                if (this.metadata_resolver == null)
                    Interlocked.CompareExchange(ref this.metadata_resolver, new MetadataResolver(this.AssemblyResolver), null);

                return this.metadata_resolver;
            }
        }

        public TypeSystem TypeSystem
        {
            get
            {
                if (this.type_system == null)
                    Interlocked.CompareExchange(ref this.type_system, TypeSystem.CreateTypeSystem(this), null);

                return this.type_system;
            }
        }

        public bool HasAssemblyReferences
        {
            get
            {
                if (this.references != null)
                    return this.references.Count > 0;

                return this.HasImage && this.Image.HasTable(Table.AssemblyRef);
            }
        }

        public Collection<AssemblyNameReference> AssemblyReferences
        {
            get
            {
                if (this.references != null) return this.references;
                if (this.HasImage) return this.Read(ref this.references, this, (_, r) => r.ReadAssemblyReferences());

                return this.references = new Collection<AssemblyNameReference>();
            }
        }

        public bool HasModuleReferences
        {
            get
            {
                if (this.modules != null) return this.modules.Count > 0;
                return this.HasImage && this.Image.HasTable(Table.ModuleRef);
            }
        }

        public Collection<ModuleReference> ModuleReferences
        {
            get
            {
                if (this.modules != null) return this.modules;
                if (this.HasImage) return this.Read(ref this.modules, this, (_, r) => r.ReadModuleReferences());
                return this.modules = new Collection<ModuleReference>();
            }
        }

        public bool HasResources
        {
            get
            {
                if (this.resources != null)
                    return this.resources.Count > 0;

                if (this.HasImage)
                    return this.Image.HasTable(Table.ManifestResource) || this.Read(this, (_, r) => r.HasFileResource());

                return false;
            }
        }

        public Collection<Resource> Resources
        {
            get
            {
                if (this.resources != null)
                    return this.resources;

                if (this.HasImage)
                    return this.Read(ref this.resources, this, (_, r) => r.ReadResources());

                return this.resources = new Collection<Resource>();
            }
        }

        /*Telerik Authorship*/
        private bool? _hasCustomAttributes;
        public bool HasCustomAttributes
        {
            get
            {
                if (this.custom_attributes != null)
                    return this.custom_attributes.Count > 0;

                /*Telerik Authorship*/
                if (this._hasCustomAttributes != null)
                    return this._hasCustomAttributes == true;

                /*Telerik Authorship*/
                return this.GetHasCustomAttributes(ref this._hasCustomAttributes, this);
            }
        }

        public Collection<CustomAttribute> CustomAttributes => this.custom_attributes ?? (this.GetCustomAttributes(ref this.custom_attributes, this));

        public bool HasTypes
        {
            get
            {
                if (this.types != null)
                    return this.types.Count > 0;

                return this.HasImage && this.Image.HasTable(Table.TypeDef);
            }
        }

        public Collection<TypeDefinition> Types
        {
            get
            {
                if (this.types != null)
                    return this.types;

                if (this.HasImage)
                    return this.Read(ref this.types, this, (_, r) => r.ReadTypes());

                return this.types = new TypeDefinitionCollection(this);
            }
        }

        /*Telerik Authorship*/
        private ICollection<TypeDefinition> _allTypes;

        /*Telerik Authorship*/
        private readonly object allTypesLock = new object();

        /*Telerik Authorship*/
        public ICollection<TypeDefinition> AllTypes
        {
            get
            {
                lock (this.allTypesLock)
                {
                    if (this._allTypes != null)
                    {
                        return this._allTypes;
                    }

                    this._allTypes = this.GetAllTypes();

                    return this._allTypes;
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


        public bool HasExportedTypes
        {
            get
            {
                if (this.exported_types != null)
                    return this.exported_types.Count > 0;

                return this.HasImage && this.Image.HasTable(Table.ExportedType);
            }
        }

        public Collection<ExportedType> ExportedTypes
        {
            get
            {
                if (this.exported_types != null)
                    return this.exported_types;

                if (this.HasImage)
                    return this.Read(ref this.exported_types, this, (_, r) => r.ReadExportedTypes());

                return this.exported_types = new Collection<ExportedType>();
            }
        }

        public MethodDefinition EntryPoint
        {
            get
            {
                if (this.entry_point != null)
                    return this.entry_point;

                if (this.HasImage)
                    return this.Read(ref this.entry_point, this, (_, r) => r.ReadEntryPoint());

                return this.entry_point = null;
            }
            set { this.entry_point = value; }
        }

        internal ModuleDefinition(/*Telerik Authorship*/ IAssemblyResolver resolver)
        {
            this.MetadataSystem = new MetadataSystem();
            this.token = new MetadataToken(TokenType.Module, 1);

            /*Telerik Authorship*/
            this.assembly_resolver = resolver;
        }

        internal ModuleDefinition(Image image, /*Telerik Authorship*/ IAssemblyResolver resolver)
            : this(/*Telerik Authorship*/ resolver)
        {
            this.Image = image;
            this.kind = image.Kind;
            this.RuntimeVersion = image.RuntimeVersion;
            this.architecture = image.Architecture;
            this.attributes = image.Attributes;
            this.characteristics = image.Characteristics;
            this.fq_name = image.FileName;

            this.reader = new MetadataReader(this);
        }

        public bool HasTypeReference(string fullName)
        {
            return this.HasTypeReference(string.Empty, fullName);
        }

        public bool HasTypeReference(string scope, string fullName)
        {
            CheckFullName(fullName);

            if (!this.HasImage)
                return false;

            return this.GetTypeReference(scope, fullName) != null;
        }

        public bool TryGetTypeReference(string fullName, out TypeReference type)
        {
            return this.TryGetTypeReference(string.Empty, fullName, out type);
        }

        public bool TryGetTypeReference(string scope, string fullName, out TypeReference type)
        {
            CheckFullName(fullName);

            if (!this.HasImage)
            {
                type = null;
                return false;
            }

            return (type = this.GetTypeReference(scope, fullName)) != null;
        }

        TypeReference GetTypeReference(string scope, string fullname)
        {
            return this.Read(new Row<string, string>(scope, fullname), (row, r) => r.GetTypeReference(row.Col1, row.Col2));
        }

        public IEnumerable<TypeReference> GetTypeReferences()
        {
            if (!this.HasImage)
                return Empty<TypeReference>.Array;

            return this.Read(this, (_, reader) => reader.GetTypeReferences());
        }

        public IEnumerable<MemberReference> GetMemberReferences()
        {
            if (!this.HasImage)
                return Empty<MemberReference>.Array;

            return this.Read(this, (_, reader) => reader.GetMemberReferences());
        }

        public TypeReference GetType(string fullName, bool runtimeName)
        {
            return runtimeName
                ? TypeParser.ParseType(this, fullName)
                : this.GetType(fullName);
        }

        public TypeDefinition GetType(string fullName)
        {
            CheckFullName(fullName);

            var position = fullName.IndexOf('/');
            if (position > 0)
                return this.GetNestedType(fullName);

            return ((TypeDefinitionCollection)this.Types).GetType(fullName);
        }

        public TypeDefinition GetType(string @namespace, string name)
        {
            Mixin.CheckName(name);

            return ((TypeDefinitionCollection)this.Types).GetType(@namespace ?? string.Empty, name);
        }

        public IEnumerable<TypeDefinition> GetTypes()
        {
            return GetTypes(this.Types);
        }

        static IEnumerable<TypeDefinition> GetTypes(Collection<TypeDefinition> types)
        {
            for (int i = 0; i < types.Count; i++)
            {
                var type = types[i];

                yield return type;

                if (!type.HasNestedTypes)
                    continue;

                foreach (var nested in GetTypes(type.NestedTypes))
                    yield return nested;
            }
        }

        static void CheckFullName(string fullName)
        {
            if (fullName == null)
                throw new ArgumentNullException(nameof(fullName));
            if (fullName.Length == 0)
                throw new ArgumentException();
        }

        TypeDefinition GetNestedType(string fullname)
        {
            var names = fullname.Split('/');
            var type = this.GetType(names[0]);

            if (type == null)
                return null;

            for (int i = 1; i < names.Length; i++)
            {
                var nested_type = type.GetNestedType(names[i]);
                if (nested_type == null)
                    return null;

                type = nested_type;
            }

            return type;
        }

        internal FieldDefinition Resolve(FieldReference field)
        {
            return this.MetadataResolver.Resolve(field);
        }

        internal MethodDefinition Resolve(MethodReference method)
        {
            return this.MetadataResolver.Resolve(method);
        }

        internal TypeDefinition Resolve(TypeReference type)
        {
            return this.MetadataResolver.Resolve(type);
        }

        /*Telerik Authorship*/
        internal TypeDefinition Resolve(TypeReference type, ICollection<string> visitedDlls)
        {
            return ((MetadataResolver)this.MetadataResolver).Resolve(type, visitedDlls);
        }



        public IMetadataTokenProvider LookupToken(int token)
        {
            return this.LookupToken(new MetadataToken((uint)token));
        }

        public IMetadataTokenProvider LookupToken(MetadataToken token)
        {
            return this.Read(token, (t, reader) => reader.LookupToken(t));
        }

        readonly object module_lock = new object();

        internal object SyncRoot
        {
            get { return this.module_lock; }
        }

        internal TRet Read<TItem, TRet>(TItem item, Func<TItem, MetadataReader, TRet> read)
        {
            lock (this.module_lock)
            {
                var position = this.reader.position;
                var context = this.reader.context;

                var ret = read(item, this.reader);

                this.reader.position = position;
                this.reader.context = context;

                return ret;
            }
        }

        internal TRet Read<TItem, TRet>(ref TRet variable, TItem item, Func<TItem, MetadataReader, TRet> read) /*Telerik Authorship*/ // where TRet : class
        {
            lock (this.module_lock)
            {
                if (variable != null)
                    return variable;

                var position = this.reader.position;
                var context = this.reader.context;

                var ret = read(item, this.reader);

                this.reader.position = position;
                this.reader.context = context;

                return variable = ret;
            }
        }

        public bool HasDebugHeader
        {
            get { return this.Image != null && !this.Image.Debug.IsZero; }
        }

        public ImageDebugDirectory GetDebugHeader(out byte[] header)
        {
            if (!this.HasDebugHeader)
                throw new InvalidOperationException();

            return this.Image.GetDebugHeader(out header);
        }

        void ProcessDebugHeader()
        {
            if (!this.HasDebugHeader)
                return;

            byte[] header;
            var directory = this.GetDebugHeader(out header);

            if (!this.symbol_reader.ProcessDebugHeader(directory, header))
            {
                /*Telerik Authorship*/
                //throw new InvalidOperationException ();
                return;
            }
        }


        public void ReadSymbols()
        {
            if (string.IsNullOrEmpty(this.fq_name))
            {
                /*Telerik Authorship*/
                //throw new InvalidOperationException ();
                return;
            }

            var provider = SymbolProvider.GetPlatformReaderProvider();
            if (provider == null)
                throw new InvalidOperationException();

            this.ReadSymbols(provider.GetSymbolReader(this, this.fq_name));
        }

        public void ReadSymbols(ISymbolReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            this.symbol_reader = reader;

            this.ProcessDebugHeader();
        }


        public static ModuleDefinition ReadModule(string fileName, ReaderParameters parameters)
        {
            using (var stream = GetFileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return ReadModule(stream, parameters);
            }
        }

        static void CheckStream(object stream)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");
        }

        public static ModuleDefinition ReadModule(Stream stream, ReaderParameters parameters)
        {
            CheckStream(stream);
            if (!stream.CanRead || !stream.CanSeek)
                throw new ArgumentException();
            Mixin.CheckParameters(parameters);

            return ModuleReader.CreateModuleFrom(
                ImageReader.ReadImageFrom(stream),
                parameters);
        }

        static Stream GetFileStream(string fileName, FileMode mode, FileAccess access, FileShare share)
        {
            if (fileName == null)
                throw new ArgumentNullException("fileName");
            if (fileName.Length == 0)
                throw new ArgumentException();

            return new FileStream(fileName, mode, access, share);
        }
    }

    static partial class Mixin
    {

        public static void CheckParameters(object parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException("parameters");
        }

        public static bool HasImage(this ModuleDefinition self)
        {
            return self != null && self.HasImage;
        }

        public static bool IsCorlib(this ModuleDefinition module)
        {
            if (module.Assembly == null)
                return false;

            return module.Assembly.Name.Name == "mscorlib";
        }

        public static string GetFullyQualifiedName(this Stream self)
        {
            return string.Empty;
        }

        public static TargetRuntime ParseRuntime(this string self)
        {
            switch (self[1])
            {
                case '1':
                    return self[3] == '0'
                        ? TargetRuntime.Net_1_0
                        : TargetRuntime.Net_1_1;
                case '2':
                    return TargetRuntime.Net_2_0;
                case '4':
                default:
                    return TargetRuntime.Net_4_0;
            }
        }

        public static string RuntimeVersionString(this TargetRuntime runtime)
        {
            switch (runtime)
            {
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
