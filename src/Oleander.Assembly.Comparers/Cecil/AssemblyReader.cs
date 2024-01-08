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
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using RVA = System.UInt32;
/*Telerik Authorship*/
using Mono.Cecil.Mono.Cecil;
using Oleander.Assembly.Comparers.Cecil.Cil;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Mono.Cecil {

	abstract class ModuleReader {

		readonly protected Image image;
		readonly protected ModuleDefinition module;

		/*Telerik Authorship*/
		protected ModuleReader (Image image, ReaderParameters parameters)
		{
			this.image = image;
			this.module = new ModuleDefinition (image, /*Telerik Authorship*/ parameters.AssemblyResolver);
			this.module.ReadingMode = /*Telerik Authorship*/ parameters.ReadingMode;
		}

		protected abstract void ReadModule ();

		protected void ReadModuleManifest (MetadataReader reader)
		{
			reader.Populate (this.module);

            this.ReadAssembly (reader);
		}

		void ReadAssembly (MetadataReader reader)
		{
			var name = reader.ReadAssemblyNameDefinition ();
			if (name == null) {
                this.module.kind = ModuleKind.NetModule;
				return;
			}

			var assembly = new AssemblyDefinition ();
			assembly.Name = name;

            this.module.assembly = assembly;
			assembly.MainModule = this.module;
		}

		public static ModuleDefinition CreateModuleFrom (Image image, ReaderParameters parameters)
		{
			/*Telerik Authorship*/
			var reader = CreateModuleReader (image, parameters);
			var module = reader.module;

			if (parameters.AssemblyResolver != null)
				module.assembly_resolver = parameters.AssemblyResolver;

			if (parameters.MetadataResolver != null)
				module.metadata_resolver = parameters.MetadataResolver;

			reader.ReadModule ();

			ReadSymbols (module, parameters);

			return module;
		}

		static void ReadSymbols (ModuleDefinition module, ReaderParameters parameters)
		{
			/*Telerik Authorship*/
			if (module.Kind == ModuleKind.NetModule && parameters.SymbolStream == null && !File.Exists(Path.ChangeExtension(module.FullyQualifiedName, ".pdb")))
			{
				return;
			}

			var symbol_reader_provider = parameters.SymbolReaderProvider;

			if (symbol_reader_provider == null && parameters.ReadSymbols)
				symbol_reader_provider = SymbolProvider.GetPlatformReaderProvider ();

			if (symbol_reader_provider != null) {
				module.SymbolReaderProvider = symbol_reader_provider;

				var reader = parameters.SymbolStream != null
					? symbol_reader_provider.GetSymbolReader (module, parameters.SymbolStream)
					: symbol_reader_provider.GetSymbolReader (module, module.FullyQualifiedName);

				module.ReadSymbols (reader);
			}
		}

		static ModuleReader CreateModuleReader(Image image, /*Telerik Authorship*/ ReaderParameters parameters)
		{
			/*Telerik Authorship*/
			switch (parameters.ReadingMode)
			{
				case ReadingMode.Immediate:
				/*Telerik Authorship*/
				return new ImmediateModuleReader (image, parameters);
			case ReadingMode.Deferred:
				/*Telerik Authorship*/
				return new DeferredModuleReader (image, parameters);
			default:
				throw new ArgumentException ();
			}
		}
	}

	sealed class ImmediateModuleReader : ModuleReader {

		/*Telerik Authorship*/
		public ImmediateModuleReader(Image image, ReaderParameters parameters)
			: base(image, parameters)
		{
			if (parameters.ReadingMode != ReadingMode.Immediate)
			{
				throw new ArgumentException("Invalid reader parameters.");
			}
		}

		protected override void ReadModule ()
		{
			this.module.Read (this.module, (module, reader) => {
                this.ReadModuleManifest (reader);
				ReadModule (module);
				return module;
			});
		}

		public static void ReadModule (ModuleDefinition module)
		{
			if (module.HasAssemblyReferences)
				Read (module.AssemblyReferences);
			if (module.HasResources)
				Read (module.Resources);
			if (module.HasModuleReferences)
				Read (module.ModuleReferences);
			if (module.HasTypes)
				ReadTypes (module.Types);
			if (module.HasExportedTypes)
				Read (module.ExportedTypes);
			if (module.HasCustomAttributes)
				Read (module.CustomAttributes);

			var assembly = module.Assembly;
			if (assembly == null)
				return;

			if (assembly.HasCustomAttributes)
				ReadCustomAttributes (assembly);
			if (assembly.HasSecurityDeclarations)
				Read (assembly.SecurityDeclarations);
		}

		static void ReadTypes (Collection<TypeDefinition> types)
		{
			for (int i = 0; i < types.Count; i++)
				ReadType (types [i]);
		}

		static void ReadType (TypeDefinition type)
		{
			ReadGenericParameters (type);

			if (type.HasInterfaces)
				Read (type.Interfaces);

			if (type.HasNestedTypes)
				ReadTypes (type.NestedTypes);

			if (type.HasLayoutInfo)
				Read (type.ClassSize);

			if (type.HasFields)
				ReadFields (type);

			if (type.HasMethods)
				ReadMethods (type);

			if (type.HasProperties)
				ReadProperties (type);

			if (type.HasEvents)
				ReadEvents (type);

			ReadSecurityDeclarations (type);
			ReadCustomAttributes (type);
		}

		static void ReadGenericParameters (IGenericParameterProvider provider)
		{
			if (!provider.HasGenericParameters)
				return;

			var parameters = provider.GenericParameters;

			for (int i = 0; i < parameters.Count; i++) {
				var parameter = parameters [i];

				if (parameter.HasConstraints)
					Read (parameter.Constraints);

				ReadCustomAttributes (parameter);
			}
		}

		static void ReadSecurityDeclarations (ISecurityDeclarationProvider provider)
		{
			if (!provider.HasSecurityDeclarations)
				return;

			var security_declarations = provider.SecurityDeclarations;

			for (int i = 0; i < security_declarations.Count; i++) {
				var security_declaration = security_declarations [i];

				Read (security_declaration.SecurityAttributes);
			}
		}

		static void ReadCustomAttributes (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			var custom_attributes = provider.CustomAttributes;

			for (int i = 0; i < custom_attributes.Count; i++) {
				var custom_attribute = custom_attributes [i];

				Read (custom_attribute.ConstructorArguments);
			}
		}

		static void ReadFields (TypeDefinition type)
		{
			var fields = type.Fields;

			for (int i = 0; i < fields.Count; i++) {
				var field = fields [i];

				if (field.HasConstant)
					Read (field.Constant);

				if (field.HasLayoutInfo)
					Read (field.Offset);

				if (field.RVA > 0)
					Read (field.InitialValue);

				if (field.HasMarshalInfo)
					Read (field.MarshalInfo);

				ReadCustomAttributes (field);
			}
		}

		static void ReadMethods (TypeDefinition type)
		{
			var methods = type.Methods;

			for (int i = 0; i < methods.Count; i++) {
				var method = methods [i];

				ReadGenericParameters (method);

				if (method.HasParameters)
					ReadParameters (method);

				if (method.HasOverrides)
					Read (method.Overrides);

				if (method.IsPInvokeImpl)
					Read (method.PInvokeInfo);

				ReadSecurityDeclarations (method);
				ReadCustomAttributes (method);

				var return_type = method.MethodReturnType;
				if (return_type.HasConstant)
					Read (return_type.Constant);

				if (return_type.HasMarshalInfo)
					Read (return_type.MarshalInfo);

				ReadCustomAttributes (return_type);
			}
		}

		static void ReadParameters (MethodDefinition method)
		{
			var parameters = method.Parameters;

			for (int i = 0; i < parameters.Count; i++) {
				var parameter = parameters [i];

				if (parameter.HasConstant)
					Read (parameter.Constant);

				if (parameter.HasMarshalInfo)
					Read (parameter.MarshalInfo);

				ReadCustomAttributes (parameter);
			}
		}

		static void ReadProperties (TypeDefinition type)
		{
			var properties = type.Properties;

			for (int i = 0; i < properties.Count; i++) {
				var property = properties [i];

				Read (property.GetMethod);

				if (property.HasConstant)
					Read (property.Constant);

				ReadCustomAttributes (property);
			}
		}

		static void ReadEvents (TypeDefinition type)
		{
			var events = type.Events;

			for (int i = 0; i < events.Count; i++) {
				var @event = events [i];

				Read (@event.AddMethod);

				ReadCustomAttributes (@event);
			}
		}

		static void Read (object collection)
		{
		}
	}

	sealed class DeferredModuleReader : ModuleReader {

		/*Telerik Authorship*/
		public DeferredModuleReader(Image image, ReaderParameters parameters)
			: base(image, parameters)
		{
			if (parameters.ReadingMode != ReadingMode.Deferred)
			{
				throw new ArgumentException("Invalid reader parameters.");
			}
		}

		protected override void ReadModule ()
		{
			this.module.Read (this.module, (module, reader) => {
                this.ReadModuleManifest (reader);
				return module;
			});
		}
	}

	sealed class MetadataReader : ByteBuffer {

		readonly internal Image image;
		readonly internal ModuleDefinition module;
		readonly internal MetadataSystem metadata;

		internal IGenericContext context;
		internal CodeReader code;

		uint Position {
			get { return (uint) this.position; }
			set { this.position = (int) value; }
		}

		public MetadataReader (ModuleDefinition module)
			: base (module.Image.MetadataSection.Data)
		{
			this.image = module.Image;
			this.module = module;
			this.metadata = module.MetadataSystem;
			this.code = new CodeReader (this.image.MetadataSection, this);
		}

		int GetCodedIndexSize (CodedIndex index)
		{
			return this.image.GetCodedIndexSize (index);
		}

		uint ReadByIndexSize (int size)
		{
			if (size == 4)
				return this.ReadUInt32 ();
			else
				return this.ReadUInt16 ();
		}

		byte [] ReadBlob ()
		{
			var blob_heap = this.image.BlobHeap;
			if (blob_heap == null) {
                this.position += 2;
				return Empty<byte>.Array;
			}

			return blob_heap.Read (this.ReadBlobIndex ());
		}

		byte [] ReadBlob (uint signature)
		{
			var blob_heap = this.image.BlobHeap;
			if (blob_heap == null)
				return Empty<byte>.Array;

			return blob_heap.Read (signature);
		}

		uint ReadBlobIndex ()
		{
			var blob_heap = this.image.BlobHeap;
			return this.ReadByIndexSize (blob_heap != null ? blob_heap.IndexSize : 2);
		}

		string ReadString ()
		{
			return this.image.StringHeap.Read (this.ReadByIndexSize (this.image.StringHeap.IndexSize));
		}

		uint ReadStringIndex ()
		{
			return this.ReadByIndexSize (this.image.StringHeap.IndexSize);
		}

		uint ReadTableIndex (Table table)
		{
			return this.ReadByIndexSize (this.image.GetTableIndexSize (table));
		}

		MetadataToken ReadMetadataToken (CodedIndex index)
		{
			return index.GetMetadataToken (this.ReadByIndexSize (this.GetCodedIndexSize (index)));
		}

		int MoveTo (Table table)
		{
			var info = this.image.TableHeap [table];
			if (info.Length != 0) this.Position = info.Offset;

			return (int) info.Length;
		}

		bool MoveTo (Table table, uint row)
		{
			var info = this.image.TableHeap [table];
			var length = info.Length;
			if (length == 0 || row > length)
				return false;

            this.Position = info.Offset + (info.RowSize * (row - 1));
			return true;
		}

		public AssemblyNameDefinition ReadAssemblyNameDefinition ()
		{
			if (this.MoveTo (Table.Assembly) == 0)
				return null;

			var name = new AssemblyNameDefinition ();

			name.HashAlgorithm = (AssemblyHashAlgorithm)this.ReadUInt32 ();

            this.PopulateVersionAndFlags (name);

			name.PublicKey = this.ReadBlob ();

            this.PopulateNameAndCulture (name);

			return name;
		}

		public ModuleDefinition Populate (ModuleDefinition module)
		{
			if (this.MoveTo (Table.Module) == 0)
				return module;

            this.Advance (2); // Generation

			module.Name = this.ReadString ();
			module.Mvid = this.image.GuidHeap.Read (this.ReadByIndexSize (this.image.GuidHeap.IndexSize));

			return module;
		}

		void InitializeAssemblyReferences ()
		{
			if (this.metadata.AssemblyReferences != null)
				return;

			int length = this.MoveTo (Table.AssemblyRef);
			var references = this.metadata.AssemblyReferences = new AssemblyNameReference [length];

			for (uint i = 0; i < length; i++) {
				var reference = new AssemblyNameReference ();
				reference.token = new MetadataToken (TokenType.AssemblyRef, i + 1);

                this.PopulateVersionAndFlags (reference);

				var key_or_token = this.ReadBlob ();

				if (reference.HasPublicKey)
					reference.PublicKey = key_or_token;
				else
					reference.PublicKeyToken = key_or_token;

                this.PopulateNameAndCulture (reference);

				reference.Hash = this.ReadBlob ();

				references [i] = reference;
			}
		}

		public Collection<AssemblyNameReference> ReadAssemblyReferences ()
		{
            this.InitializeAssemblyReferences ();

			return new Collection<AssemblyNameReference> (this.metadata.AssemblyReferences);
		}

		public MethodDefinition ReadEntryPoint ()
		{
			if (this.module.Image.EntryPointToken == 0)
				return null;

			var token = new MetadataToken (this.module.Image.EntryPointToken);
			return this.GetMethodDefinition (token.RID);
		}

		/*Telerik Authorship*/
		public Collection<ModuleDefinition> ReadModules (IAssemblyResolver resolver)
		{
			var modules = new Collection<ModuleDefinition> (1);
			modules.Add (this.module);

			int length = this.MoveTo (Table.File);
			for (uint i = 1; i <= length; i++) {
				var attributes = (FileAttributes)this.ReadUInt32 ();
				var name = this.ReadString ();
                this.ReadBlobIndex ();

				if (attributes != FileAttributes.ContainsMetaData)
					continue;

				/*Telerik Authorship*/
				var parameters = new ReaderParameters(resolver) {
					ReadingMode = this.module.ReadingMode,
					SymbolReaderProvider = this.module.SymbolReaderProvider,
					/*Telerik Authorship*/
				};

				modules.Add (ModuleDefinition.ReadModule (this.GetModuleFileName (name), parameters));
			}

			return modules;
		}

		string GetModuleFileName (string name)
		{
			if (this.module.FullyQualifiedName == null)
				throw new NotSupportedException ();

			/*Telerik Authorship*/
			var path = this.module.ModuleDirectoryPath;
			return Path.Combine (path, name);
		}

		void InitializeModuleReferences ()
		{
			if (this.metadata.ModuleReferences != null)
				return;

			int length = this.MoveTo (Table.ModuleRef);
			var references = this.metadata.ModuleReferences = new ModuleReference [length];

			for (uint i = 0; i < length; i++) {
				var reference = new ModuleReference (this.ReadString ());
				reference.token = new MetadataToken (TokenType.ModuleRef, i + 1);

				references [i] = reference;
			}
		}

		public Collection<ModuleReference> ReadModuleReferences ()
		{
            this.InitializeModuleReferences ();

			return new Collection<ModuleReference> (this.metadata.ModuleReferences);
		}

		public bool HasFileResource ()
		{
			int length = this.MoveTo (Table.File);
			if (length == 0)
				return false;

			for (uint i = 1; i <= length; i++)
				if (this.ReadFileRecord (i).Col1 == FileAttributes.ContainsNoMetaData)
					return true;

			return false;
		}

		public Collection<Resource> ReadResources ()
		{
			int length = this.MoveTo (Table.ManifestResource);
			var resources = new Collection<Resource> (length);

			for (int i = 1; i <= length; i++) {
				var offset = this.ReadUInt32 ();
				var flags = (ManifestResourceAttributes)this.ReadUInt32 ();
				var name = this.ReadString ();
				var implementation = this.ReadMetadataToken (CodedIndex.Implementation);

				Resource resource;

				if (implementation.RID == 0) {
					resource = new EmbeddedResource (name, flags, offset, this);
				} else if (implementation.TokenType == TokenType.AssemblyRef) {
					resource = new AssemblyLinkedResource (name, flags) {
						Assembly = (AssemblyNameReference)this.GetTypeReferenceScope (implementation),
					};
				} else if (implementation.TokenType == TokenType.File) {
					var file_record = this.ReadFileRecord (implementation.RID);

					resource = new LinkedResource (name, flags) {
						File = file_record.Col2,
						hash = this.ReadBlob (file_record.Col3)
					};
				} else
					throw new NotSupportedException ();

				resources.Add (resource);
			}

			return resources;
		}

		Row<FileAttributes, string, uint> ReadFileRecord (uint rid)
		{
			var position = this.position;

			if (!this.MoveTo (Table.File, rid))
				throw new ArgumentException ();

			var record = new Row<FileAttributes, string, uint> (
				(FileAttributes)this.ReadUInt32 (), this.ReadString (), this.ReadBlobIndex ());

			this.position = position;

			return record;
		}

		public MemoryStream GetManagedResourceStream (uint offset)
		{
			var rva = this.image.Resources.VirtualAddress;
			var section = this.image.GetSectionAtVirtualAddress (rva);
			var position = (rva - section.VirtualAddress) + offset;
			var buffer = section.Data;

			var length = buffer [position]
				| (buffer [position + 1] << 8)
				| (buffer [position + 2] << 16)
				| (buffer [position + 3] << 24);

			return new MemoryStream (buffer, (int) position + 4, length);
		}

		void PopulateVersionAndFlags (AssemblyNameReference name)
		{
			name.Version = new Version (this.ReadUInt16 (), this.ReadUInt16 (), this.ReadUInt16 (), this.ReadUInt16 ());

			name.Attributes = (AssemblyAttributes)this.ReadUInt32 ();
		}

		void PopulateNameAndCulture (AssemblyNameReference name)
		{
			name.Name = this.ReadString ();
			name.Culture = this.ReadString ();
		}

		public TypeDefinitionCollection ReadTypes ()
		{
            this.InitializeTypeDefinitions ();
			var mtypes = this.metadata.Types;
			var type_count = mtypes.Length - this.metadata.NestedTypes.Count;
			var types = new TypeDefinitionCollection (this.module, type_count);

			for (int i = 0; i < mtypes.Length; i++) {
				var type = mtypes [i];
				if (IsNested (type.Attributes))
					continue;

				types.Add (type);
			}

			if (this.image.HasTable (Table.MethodPtr) || this.image.HasTable (Table.FieldPtr)) this.CompleteTypes ();

			return types;
		}

		void CompleteTypes ()
		{
			var types = this.metadata.Types;

			for (int i = 0; i < types.Length; i++) {
				var type = types [i];

				InitializeCollection (type.Fields);
				InitializeCollection (type.Methods);
			}
		}

		void InitializeTypeDefinitions ()
		{
			if (this.metadata.Types != null)
				return;

            this.InitializeNestedTypes ();
            this.InitializeFields ();
            this.InitializeMethods ();

			int length = this.MoveTo (Table.TypeDef);
			var types = this.metadata.Types = new TypeDefinition [length];

			for (uint i = 0; i < length; i++) {
				if (types [i] != null)
					continue;

				types [i] = this.ReadType (i + 1);
			}
		}

		static bool IsNested (TypeAttributes attributes)
		{
			switch (attributes & TypeAttributes.VisibilityMask) {
			case TypeAttributes.NestedAssembly:
			case TypeAttributes.NestedFamANDAssem:
			case TypeAttributes.NestedFamily:
			case TypeAttributes.NestedFamORAssem:
			case TypeAttributes.NestedPrivate:
			case TypeAttributes.NestedPublic:
				return true;
			default:
				return false;
			}
		}

		public bool HasNestedTypes (TypeDefinition type)
		{
			uint [] mapping;
            this.InitializeNestedTypes ();

			if (!this.metadata.TryGetNestedTypeMapping (type, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<TypeDefinition> ReadNestedTypes (TypeDefinition type)
		{
            this.InitializeNestedTypes ();
			uint [] mapping;
			if (!this.metadata.TryGetNestedTypeMapping (type, out mapping))
				return new MemberDefinitionCollection<TypeDefinition> (type);

			var nested_types = new MemberDefinitionCollection<TypeDefinition> (type, mapping.Length);

			for (int i = 0; i < mapping.Length; i++) {
				var nested_type = this.GetTypeDefinition (mapping [i]);

				if (nested_type != null)
					nested_types.Add (nested_type);
			}

            this.metadata.RemoveNestedTypeMapping (type);

			return nested_types;
		}

		void InitializeNestedTypes ()
		{
			if (this.metadata.NestedTypes != null)
				return;

			var length = this.MoveTo (Table.NestedClass);

            this.metadata.NestedTypes = new Dictionary<uint, uint []> (length);
            this.metadata.ReverseNestedTypes = new Dictionary<uint, uint> (length);

			if (length == 0)
				return;

			for (int i = 1; i <= length; i++) {
				var nested = this.ReadTableIndex (Table.TypeDef);
				var declaring = this.ReadTableIndex (Table.TypeDef);

                this.AddNestedMapping (declaring, nested);
			}
		}

		void AddNestedMapping (uint declaring, uint nested)
		{
            this.metadata.SetNestedTypeMapping (declaring, AddMapping (this.metadata.NestedTypes, declaring, nested));
            this.metadata.SetReverseNestedTypeMapping (nested, declaring);
		}

		static TValue [] AddMapping<TKey, TValue> (Dictionary<TKey, TValue []> cache, TKey key, TValue value)
		{
			TValue [] mapped;
			if (!cache.TryGetValue (key, out mapped)) {
				mapped = new [] { value };
				return mapped;
			}

			var new_mapped = new TValue [mapped.Length + 1];
			Array.Copy (mapped, new_mapped, mapped.Length);
			new_mapped [mapped.Length] = value;
			return new_mapped;
		}

		TypeDefinition ReadType (uint rid)
		{
			if (!this.MoveTo (Table.TypeDef, rid))
				return null;

			var attributes = (TypeAttributes)this.ReadUInt32 ();
			var name = this.ReadString ();
			var @namespace = this.ReadString ();
			var type = new TypeDefinition (@namespace, name, attributes);
			type.token = new MetadataToken (TokenType.TypeDef, rid);
			type.scope = this.module;
			type.module = this.module;

            this.metadata.AddTypeDefinition (type);

			this.context = type;

			type.BaseType = this.GetTypeDefOrRef (this.ReadMetadataToken (CodedIndex.TypeDefOrRef));

			type.fields_range = this.ReadFieldsRange (rid);
			type.methods_range = this.ReadMethodsRange (rid);

			if (IsNested (attributes))
				type.DeclaringType = this.GetNestedTypeDeclaringType (type);

			return type;
		}

		TypeDefinition GetNestedTypeDeclaringType (TypeDefinition type)
		{
			uint declaring_rid;
			if (!this.metadata.TryGetReverseNestedTypeMapping (type, out declaring_rid))
				return null;

            this.metadata.RemoveReverseNestedTypeMapping (type);
			return this.GetTypeDefinition (declaring_rid);
		}

		Range ReadFieldsRange (uint type_index)
		{
			return this.ReadListRange (type_index, Table.TypeDef, Table.Field);
		}

		Range ReadMethodsRange (uint type_index)
		{
			return this.ReadListRange (type_index, Table.TypeDef, Table.Method);
		}

		Range ReadListRange (uint current_index, Table current, Table target)
		{
			var list = new Range ();

			list.Start = this.ReadTableIndex (target);

			uint next_index;
			var current_table = this.image.TableHeap [current];

			if (current_index == current_table.Length)
				next_index = this.image.TableHeap [target].Length + 1;
			else {
				var position = this.Position;
                this.Position += (uint) (current_table.RowSize - this.image.GetTableIndexSize (target));
				next_index = this.ReadTableIndex (target);
                this.Position = position;
			}

			list.Length = next_index - list.Start;

			return list;
		}

		public Row<short, int> ReadTypeLayout (TypeDefinition type)
		{
            this.InitializeTypeLayouts ();
			Row<ushort, uint> class_layout;
			var rid = type.token.RID;
			if (!this.metadata.ClassLayouts.TryGetValue (rid, out class_layout))
				return new Row<short, int> (Mixin.NoDataMarker, Mixin.NoDataMarker);

			type.PackingSize = (short) class_layout.Col1;
			type.ClassSize = (int) class_layout.Col2;

            this.metadata.ClassLayouts.Remove (rid);

			return new Row<short, int> ((short) class_layout.Col1, (int) class_layout.Col2);
		}

		void InitializeTypeLayouts ()
		{
			if (this.metadata.ClassLayouts != null)
				return;

			int length = this.MoveTo (Table.ClassLayout);

			var class_layouts = this.metadata.ClassLayouts = new Dictionary<uint, Row<ushort, uint>> (length);

			for (uint i = 0; i < length; i++) {
				var packing_size = this.ReadUInt16 ();
				var class_size = this.ReadUInt32 ();

				var parent = this.ReadTableIndex (Table.TypeDef);

				class_layouts.Add (parent, new Row<ushort, uint> (packing_size, class_size));
			}
		}

		public TypeReference GetTypeDefOrRef (MetadataToken token)
		{
			return (TypeReference)this.LookupToken (token);
		}

		public TypeDefinition GetTypeDefinition (uint rid)
		{
            this.InitializeTypeDefinitions ();

			var type = this.metadata.GetTypeDefinition (rid);
			if (type != null)
				return type;

			return this.ReadTypeDefinition (rid);
		}

		TypeDefinition ReadTypeDefinition (uint rid)
		{
			if (!this.MoveTo (Table.TypeDef, rid))
				return null;

			return this.ReadType (rid);
		}

		void InitializeTypeReferences ()
		{
			if (this.metadata.TypeReferences != null)
				return;

            this.metadata.TypeReferences = new TypeReference [this.image.GetTableLength (Table.TypeRef)];
		}

		public TypeReference GetTypeReference (string scope, string full_name)
		{
            this.InitializeTypeReferences ();

			var length = this.metadata.TypeReferences.Length;

			for (uint i = 1; i <= length; i++) {
				var type = this.GetTypeReference (i);

				if (type.FullName != full_name)
					continue;

				if (string.IsNullOrEmpty (scope))
					return type;

				if (type.Scope.Name == scope)
					return type;
			}

			return null;
		}

		TypeReference GetTypeReference (uint rid)
		{
            this.InitializeTypeReferences ();

			var type = this.metadata.GetTypeReference (rid);
			if (type != null)
				return type;

			return this.ReadTypeReference (rid);
		}

		TypeReference ReadTypeReference (uint rid)
		{
			if (!this.MoveTo (Table.TypeRef, rid))
				return null;

			TypeReference declaring_type = null;
			IMetadataScope scope;

			var scope_token = this.ReadMetadataToken (CodedIndex.ResolutionScope);

			var name = this.ReadString ();
			var @namespace = this.ReadString ();

			var type = new TypeReference (
				@namespace,
				name, this.module,
				null);

			type.token = new MetadataToken (TokenType.TypeRef, rid);

            this.metadata.AddTypeReference (type);

			if (scope_token.TokenType == TokenType.TypeRef) {
				declaring_type = this.GetTypeDefOrRef (scope_token);

				scope = declaring_type != null
					? declaring_type.Scope
					: this.module;
			} else
				scope = this.GetTypeReferenceScope (scope_token);

			type.scope = scope;
			type.DeclaringType = declaring_type;

			MetadataSystem.TryProcessPrimitiveTypeReference (type);

			return type;
		}

		IMetadataScope GetTypeReferenceScope (MetadataToken scope)
		{
			if (scope.TokenType == TokenType.Module)
				return this.module;

			IMetadataScope[] scopes;

			switch (scope.TokenType) {
			case TokenType.AssemblyRef:
                this.InitializeAssemblyReferences ();
				scopes = this.metadata.AssemblyReferences;
				break;
			case TokenType.ModuleRef:
                this.InitializeModuleReferences ();
				scopes = this.metadata.ModuleReferences;
				break;
			default:
				throw new NotSupportedException ();
			}

			var index = scope.RID - 1;
			if (index < 0 || index >= scopes.Length)
				return null;

			return scopes [index];
		}

		public IEnumerable<TypeReference> GetTypeReferences ()
		{
            this.InitializeTypeReferences ();

			var length = this.image.GetTableLength (Table.TypeRef);

			var type_references = new TypeReference [length];

			for (uint i = 1; i <= length; i++)
				type_references [i - 1] = this.GetTypeReference (i);

			return type_references;
		}

		TypeReference GetTypeSpecification (uint rid)
		{
			if (!this.MoveTo (Table.TypeSpec, rid))
				return null;

			var reader = this.ReadSignature (this.ReadBlobIndex ());
			var type = reader.ReadTypeSignature ();
			if (type.token.RID == 0)
				type.token = new MetadataToken (TokenType.TypeSpec, rid);

			return type;
		}

		SignatureReader ReadSignature (uint signature)
		{
			return new SignatureReader (signature, this);
		}

		public bool HasInterfaces (TypeDefinition type)
		{
            this.InitializeInterfaces ();
			MetadataToken [] mapping;

			return this.metadata.TryGetInterfaceMapping (type, out mapping);
		}

		public Collection<TypeReference> ReadInterfaces (TypeDefinition type)
		{
            this.InitializeInterfaces ();
			MetadataToken [] mapping;

			if (!this.metadata.TryGetInterfaceMapping (type, out mapping))
				return new Collection<TypeReference> ();

			var interfaces = new Collection<TypeReference> (mapping.Length);

			this.context = type;

			for (int i = 0; i < mapping.Length; i++)
				interfaces.Add (this.GetTypeDefOrRef (mapping [i]));

            this.metadata.RemoveInterfaceMapping (type);

			return interfaces;
		}

		void InitializeInterfaces ()
		{
			if (this.metadata.Interfaces != null)
				return;

			int length = this.MoveTo (Table.InterfaceImpl);

            this.metadata.Interfaces = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 0; i < length; i++) {
				var type = this.ReadTableIndex (Table.TypeDef);
				var @interface = this.ReadMetadataToken (CodedIndex.TypeDefOrRef);

                this.AddInterfaceMapping (type, @interface);
			}
		}

		void AddInterfaceMapping (uint type, MetadataToken @interface)
		{
            this.metadata.SetInterfaceMapping (type, AddMapping (this.metadata.Interfaces, type, @interface));
		}

		public Collection<FieldDefinition> ReadFields (TypeDefinition type)
		{
			var fields_range = type.fields_range;
			if (fields_range.Length == 0)
				return new MemberDefinitionCollection<FieldDefinition> (type);

			var fields = new MemberDefinitionCollection<FieldDefinition> (type, (int) fields_range.Length);
			this.context = type;

			if (!this.MoveTo (Table.FieldPtr, fields_range.Start)) {
				if (!this.MoveTo (Table.Field, fields_range.Start))
					return fields;

				for (uint i = 0; i < fields_range.Length; i++) this.ReadField (fields_range.Start + i, fields);
			} else
                this.ReadPointers (Table.FieldPtr, Table.Field, fields_range, fields, this.ReadField);

			return fields;
		}

		/*Telerik Authorship*/
		FieldDefinition ReadField (uint field_rid)
		{
			var attributes = (FieldAttributes)this.ReadUInt16 ();
			var name = this.ReadString ();
			var signature = this.ReadBlobIndex ();

			/*Telerik Authorship*/
			TypeReference type = this.ReadFieldType(signature);
			if (type == null)
				return null;
			var field = new FieldDefinition (name, attributes, type);
			field.token = new MetadataToken (TokenType.Field, field_rid);
            this.metadata.AddFieldDefinition (field);

			if (IsDeleted (field))
				return null;
			/*Telerik Authorship*/
			return field;
		}

		/*Telerik Authorship*/
		void ReadField(uint field_rid, Collection<FieldDefinition> fields)
		{
			FieldDefinition field = this.ReadField(field_rid);
			if (field != null)
			{
				fields.Add(field);
			}
		}

		void InitializeFields ()
		{
			if (this.metadata.Fields != null)
				return;

            this.metadata.Fields = new FieldDefinition [this.image.GetTableLength (Table.Field)];
		}

		TypeReference ReadFieldType (uint signature)
		{
			var reader = this.ReadSignature (signature);

			const byte field_sig = 0x6;

			if (reader.ReadByte () != field_sig)
			{
				/*Telerik Authorship*/
				return null;
				// throw new NotSupportedException ();
			}

			/*Telerik Authorship*/
			try
			{
				return reader.ReadTypeSignature ();
			}
			catch
			{
				return null;
			}
		}

		public int ReadFieldRVA (FieldDefinition field)
		{
            this.InitializeFieldRVAs ();
			var rid = field.token.RID;

			RVA rva;
			if (!this.metadata.FieldRVAs.TryGetValue (rid, out rva))
				return 0;

			var size = GetFieldTypeSize (field.FieldType);

			if (size == 0 || rva == 0)
				return 0;

            this.metadata.FieldRVAs.Remove (rid);

			field.InitialValue = this.GetFieldInitializeValue (size, rva);

			return (int) rva;
		}

		byte [] GetFieldInitializeValue (int size, RVA rva)
		{
			var section = this.image.GetSectionAtVirtualAddress (rva);
			if (section == null)
				return Empty<byte>.Array;

			var value = new byte [size];
			Buffer.BlockCopy (section.Data, (int) (rva - section.VirtualAddress), value, 0, size);
			return value;
		}

		static int GetFieldTypeSize (TypeReference type)
		{
			int size = 0;

			switch (type.etype) {
			case ElementType.Boolean:
			case ElementType.U1:
			case ElementType.I1:
				size = 1;
				break;
			case ElementType.U2:
			case ElementType.I2:
			case ElementType.Char:
				size = 2;
				break;
			case ElementType.U4:
			case ElementType.I4:
			case ElementType.R4:
				size = 4;
				break;
			case ElementType.U8:
			case ElementType.I8:
			case ElementType.R8:
				size = 8;
				break;
			case ElementType.Ptr:
			case ElementType.FnPtr:
				size = IntPtr.Size;
				break;
			case ElementType.CModOpt:
			case ElementType.CModReqD:
				return GetFieldTypeSize (((IModifierType) type).ElementType);
			default:
				var field_type = type.Resolve ();
				if (field_type != null && field_type.HasLayoutInfo)
					size = field_type.ClassSize;

				break;
			}

			return size;
		}

		void InitializeFieldRVAs ()
		{
			if (this.metadata.FieldRVAs != null)
				return;

			int length = this.MoveTo (Table.FieldRVA);

			var field_rvas = this.metadata.FieldRVAs = new Dictionary<uint, uint> (length);

			for (int i = 0; i < length; i++) {
				var rva = this.ReadUInt32 ();
				var field = this.ReadTableIndex (Table.Field);

				field_rvas.Add (field, rva);
			}
		}

		public int ReadFieldLayout (FieldDefinition field)
		{
            this.InitializeFieldLayouts ();
			var rid = field.token.RID;
			uint offset;
			if (!this.metadata.FieldLayouts.TryGetValue (rid, out offset))
				return Mixin.NoDataMarker;

            this.metadata.FieldLayouts.Remove (rid);

			return (int) offset;
		}

		void InitializeFieldLayouts ()
		{
			if (this.metadata.FieldLayouts != null)
				return;

			int length = this.MoveTo (Table.FieldLayout);

			var field_layouts = this.metadata.FieldLayouts = new Dictionary<uint, uint> (length);

			for (int i = 0; i < length; i++) {
				var offset = this.ReadUInt32 ();
				var field = this.ReadTableIndex (Table.Field);

				field_layouts.Add (field, offset);
			}
		}

		public bool HasEvents (TypeDefinition type)
		{
            this.InitializeEvents ();

			Range range;
			if (!this.metadata.TryGetEventsRange (type, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<EventDefinition> ReadEvents (TypeDefinition type)
		{
            this.InitializeEvents ();
			Range range;

			if (!this.metadata.TryGetEventsRange (type, out range))
				return new MemberDefinitionCollection<EventDefinition> (type);

			var events = new MemberDefinitionCollection<EventDefinition> (type, (int) range.Length);

            this.metadata.RemoveEventsRange (type);

			if (range.Length == 0)
				return events;

			this.context = type;

			if (!this.MoveTo (Table.EventPtr, range.Start)) {
				if (!this.MoveTo (Table.Event, range.Start))
					return events;

				for (uint i = 0; i < range.Length; i++) this.ReadEvent (range.Start + i, events);
			} else
                this.ReadPointers (Table.EventPtr, Table.Event, range, events, this.ReadEvent);

			return events;
		}

		void ReadEvent (uint event_rid, Collection<EventDefinition> events)
		{
			var attributes = (EventAttributes)this.ReadUInt16 ();
			var name = this.ReadString ();
			var event_type = this.GetTypeDefOrRef (this.ReadMetadataToken (CodedIndex.TypeDefOrRef));

			var @event = new EventDefinition (name, attributes, event_type);
			@event.token = new MetadataToken (TokenType.Event, event_rid);

			if (IsDeleted (@event))
				return;

			events.Add (@event);
		}

		void InitializeEvents ()
		{
			if (this.metadata.Events != null)
				return;

			int length = this.MoveTo (Table.EventMap);

            this.metadata.Events = new Dictionary<uint, Range> (length);

			for (uint i = 1; i <= length; i++) {
				var type_rid = this.ReadTableIndex (Table.TypeDef);
				Range events_range = this.ReadEventsRange (i);
                this.metadata.AddEventsRange (type_rid, events_range);
			}
		}

		Range ReadEventsRange (uint rid)
		{
			return this.ReadListRange (rid, Table.EventMap, Table.Event);
		}

		public bool HasProperties (TypeDefinition type)
		{
            this.InitializeProperties ();

			Range range;
			if (!this.metadata.TryGetPropertiesRange (type, out range))
				return false;

			return range.Length > 0;
		}

		public Collection<PropertyDefinition> ReadProperties (TypeDefinition type)
		{
            this.InitializeProperties ();

			Range range;

			if (!this.metadata.TryGetPropertiesRange (type, out range))
				return new MemberDefinitionCollection<PropertyDefinition> (type);

            this.metadata.RemovePropertiesRange (type);

			var properties = new MemberDefinitionCollection<PropertyDefinition> (type, (int) range.Length);

			if (range.Length == 0)
				return properties;

			this.context = type;

			if (!this.MoveTo (Table.PropertyPtr, range.Start)) {
				if (!this.MoveTo (Table.Property, range.Start))
					return properties;
				for (uint i = 0; i < range.Length; i++) this.ReadProperty (range.Start + i, properties);
			} else
                this.ReadPointers (Table.PropertyPtr, Table.Property, range, properties, this.ReadProperty);

			return properties;
		}

		/*Telerik Authorship*/
		PropertyDefinition ReadProperty (uint property_rid)
		{
			var attributes = (PropertyAttributes)this.ReadUInt16 ();
			var name = this.ReadString ();
			var signature = this.ReadBlobIndex ();

			var reader = this.ReadSignature (signature);
			const byte property_signature = 0x8;

			var calling_convention = reader.ReadByte ();

			if ((calling_convention & property_signature) == 0)
			{
				/*Telerik Authorship*/
				return null;
				// throw new NotSupportedException ();
			}

			var has_this = (calling_convention & 0x20) != 0;

			reader.ReadCompressedUInt32 (); // count

			var property = new PropertyDefinition (name, attributes, reader.ReadTypeSignature ());
			property.HasThis = has_this;
			property.token = new MetadataToken (TokenType.Property, property_rid);

			if (IsDeleted (property))
				return null;

			/*Telerik Authorship*/
			return property;
		}

		/*Telerik Authorship*/
		void ReadProperty(uint property_rid, Collection<PropertyDefinition> properties)
		{
			PropertyDefinition property = this.ReadProperty(property_rid);
			if (property != null)
			{
				properties.Add(property);
			}
		}

		void InitializeProperties ()
		{
			if (this.metadata.Properties != null)
				return;

			int length = this.MoveTo (Table.PropertyMap);

            this.metadata.Properties = new Dictionary<uint, Range> (length);

			for (uint i = 1; i <= length; i++) {
				var type_rid = this.ReadTableIndex (Table.TypeDef);
				var properties_range = this.ReadPropertiesRange (i);
                this.metadata.AddPropertiesRange (type_rid, properties_range);
			}
		}

		Range ReadPropertiesRange (uint rid)
		{
			return this.ReadListRange (rid, Table.PropertyMap, Table.Property);
		}

		MethodSemanticsAttributes ReadMethodSemantics (MethodDefinition method)
		{
            this.InitializeMethodSemantics ();
			Row<MethodSemanticsAttributes, MetadataToken> row;
			if (!this.metadata.Semantics.TryGetValue (method.token.RID, out row))
				return MethodSemanticsAttributes.None;

			var type = method.DeclaringType;

			switch (row.Col1) {
			case MethodSemanticsAttributes.AddOn:
				/*Telerik Authorship*/
				EventDefinition @event1 = GetEvent(type, row.Col2);
				if (@event1 != null)
				{
					@event1.add_method = method;
				}
				break;
			case MethodSemanticsAttributes.Fire:
				/*Telerik Authorship*/
				EventDefinition @event2 = GetEvent(type, row.Col2);
				if (@event2 != null)
				{
					@event2.invoke_method = method;
				}
				break;
			case MethodSemanticsAttributes.RemoveOn:
				/*Telerik Authorship*/
				EventDefinition @event3 = GetEvent(type, row.Col2);
				if (@event3 != null)
				{
					@event3.remove_method = method;
				}
				break;
			case MethodSemanticsAttributes.Getter:
				/*Telerik Authorship*/
				PropertyDefinition property1 = GetProperty(type, row.Col2);
				if (property1 != null)
				{
					property1.get_method = method;
				}
				break;
			case MethodSemanticsAttributes.Setter:
				/*Telerik Authorship*/
				PropertyDefinition property2 = GetProperty(type, row.Col2);
				if (property2 != null)
				{
					property2.set_method = method;
				}
				break;
			case MethodSemanticsAttributes.Other:
				switch (row.Col2.TokenType) {
				case TokenType.Event: {
					var @event = GetEvent (type, row.Col2);
					/*Telerik Authorship*/
					if (@event != null)
					{
						if (@event.other_methods == null)
							@event.other_methods = new Collection<MethodDefinition> ();

						@event.other_methods.Add (method);
					}
					break;
				}
				case TokenType.Property: {
					var property = GetProperty (type, row.Col2);
					/*Telerik Authorship*/
					if (property != null)
					{
						if (property.other_methods == null)
							property.other_methods = new Collection<MethodDefinition> ();

						property.other_methods.Add (method);
					}
					break;
				}
				default:
					throw new NotSupportedException ();
				}
				break;
			default:
				throw new NotSupportedException ();
			}

			/*Telerik Authorship*/
			try
			{
                this.metadata.Semantics.Remove (method.token.RID);
			}
			catch
			{
			}

			return row.Col1;
		}

		static EventDefinition GetEvent (TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Event)
				throw new ArgumentException ();

			return GetMember (type.Events, token);
		}

		static PropertyDefinition GetProperty (TypeDefinition type, MetadataToken token)
		{
			if (token.TokenType != TokenType.Property)
				throw new ArgumentException ();

			return GetMember (type.Properties, token);
		}

		static TMember GetMember<TMember> (Collection<TMember> members, MetadataToken token) where TMember : IMemberDefinition
		{
			for (int i = 0; i < members.Count; i++) {
				var member = members [i];
				if (member.MetadataToken == token)
					return member;
			}

			/*Telerik Authorship*/
			return default(TMember);
			// throw new ArgumentException ();
		}

		void InitializeMethodSemantics ()
		{
			if (this.metadata.Semantics != null)
				return;

			int length = this.MoveTo (Table.MethodSemantics);

			var semantics = this.metadata.Semantics = new Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> (0);

			for (uint i = 0; i < length; i++) {
				var attributes = (MethodSemanticsAttributes)this.ReadUInt16 ();
				var method_rid = this.ReadTableIndex (Table.Method);
				var association = this.ReadMetadataToken (CodedIndex.HasSemantics);

				semantics [method_rid] = new Row<MethodSemanticsAttributes, MetadataToken> (attributes, association);
			}
		}

		public PropertyDefinition ReadMethods (PropertyDefinition property)
		{
            this.ReadAllSemantics (property.DeclaringType);
			return property;
		}

		public EventDefinition ReadMethods (EventDefinition @event)
		{
            this.ReadAllSemantics (@event.DeclaringType);
			return @event;
		}

		public MethodSemanticsAttributes ReadAllSemantics (MethodDefinition method)
		{
            this.ReadAllSemantics (method.DeclaringType);

			return method.SemanticsAttributes;
		}

		void ReadAllSemantics (TypeDefinition type)
		{
			var methods = type.Methods;
			for (int i = 0; i < methods.Count; i++) {
				var method = methods [i];
				if (method.sem_attrs_ready)
					continue;

				method.sem_attrs = this.ReadMethodSemantics (method);
				method.sem_attrs_ready = true;
			}
		}

		Range ReadParametersRange (uint method_rid)
		{
			return this.ReadListRange (method_rid, Table.Method, Table.Param);
		}

		public Collection<MethodDefinition> ReadMethods (TypeDefinition type)
		{
			var methods_range = type.methods_range;
			if (methods_range.Length == 0)
				return new MemberDefinitionCollection<MethodDefinition> (type);

			var methods = new MemberDefinitionCollection<MethodDefinition> (type, (int) methods_range.Length);
			if (!this.MoveTo (Table.MethodPtr, methods_range.Start)) {
				if (!this.MoveTo (Table.Method, methods_range.Start))
					return methods;

				for (uint i = 0; i < methods_range.Length; i++) this.ReadMethod (methods_range.Start + i, methods);
			} else
                this.ReadPointers (Table.MethodPtr, Table.Method, methods_range, methods, this.ReadMethod);

			return methods;
		}

		void ReadPointers<TMember> (Table ptr, Table table, Range range, Collection<TMember> members, Action<uint, Collection<TMember>> reader)
			where TMember : IMemberDefinition
		{
			for (uint i = 0; i < range.Length; i++) {
                this.MoveTo (ptr, range.Start + i);

				var rid = this.ReadTableIndex (table);
                this.MoveTo (table, rid);

				reader (rid, members);
			}
		}

		static bool IsDeleted (IMemberDefinition member)
		{
			return member.IsSpecialName && member.Name == "_Deleted";
		}

		void InitializeMethods ()
		{
			if (this.metadata.Methods != null)
				return;

            this.metadata.Methods = new MethodDefinition [this.image.GetTableLength (Table.Method)];
		}

		void ReadMethod (uint method_rid, Collection<MethodDefinition> methods)
		{
			var method = new MethodDefinition ();
			method.rva = this.ReadUInt32 ();
			method.ImplAttributes = (MethodImplAttributes)this.ReadUInt16 ();
			method.Attributes = (MethodAttributes)this.ReadUInt16 ();
			method.Name = this.ReadString ();
			method.token = new MetadataToken (TokenType.Method, method_rid);

			if (IsDeleted (method))
				return;

			methods.Add (method); // attach method

			var signature = this.ReadBlobIndex ();
			var param_range = this.ReadParametersRange (method_rid);

			this.context = method;

			/*Telerik Authorship*/
			try
			{
                this.ReadMethodSignature (signature, method);
                this.metadata.AddMethodDefinition (method);
			}
			catch
			{
				return;
			}

			if (param_range.Length == 0)
				return;

			var position = this.position;
            this.ReadParameters (method, param_range);
			this.position = position;
		}

		void ReadParameters (MethodDefinition method, Range param_range)
		{
			if (!this.MoveTo (Table.ParamPtr, param_range.Start)) {
				if (!this.MoveTo (Table.Param, param_range.Start))
					return;

				for (uint i = 0; i < param_range.Length; i++) this.ReadParameter (param_range.Start + i, method);
			} else
                this.ReadParameterPointers (method, param_range);
		}

		void ReadParameterPointers (MethodDefinition method, Range range)
		{
			for (uint i = 0; i < range.Length; i++) {
                this.MoveTo (Table.ParamPtr, range.Start + i);

				var rid = this.ReadTableIndex (Table.Param);

                this.MoveTo (Table.Param, rid);

                this.ReadParameter (rid, method);
			}
		}

		void ReadParameter (uint param_rid, MethodDefinition method)
		{
			var attributes = (ParameterAttributes)this.ReadUInt16 ();
			var sequence = this.ReadUInt16 ();
			var name = this.ReadString ();

			/*Telerik Authorship*/
			if ((sequence < 0) || ((sequence > 0) && (method.Parameters.Count <= sequence - 1)))
				return;

			var parameter = sequence == 0
				? method.MethodReturnType.Parameter
				: method.Parameters [sequence - 1];

			parameter.token = new MetadataToken (TokenType.Param, param_rid);
			parameter.Name = name;
			parameter.Attributes = attributes;
		}

		void ReadMethodSignature (uint signature, IMethodSignature method)
		{
			var reader = this.ReadSignature (signature);
			reader.ReadMethodSignature (method);
		}

		public PInvokeInfo ReadPInvokeInfo (MethodDefinition method)
		{
            this.InitializePInvokes ();
			Row<PInvokeAttributes, uint, uint> row;

			var rid = method.token.RID;

			if (!this.metadata.PInvokes.TryGetValue (rid, out row))
				return null;

            this.metadata.PInvokes.Remove (rid);

			return new PInvokeInfo (
				row.Col1, this.image.StringHeap.Read (row.Col2), this.module.ModuleReferences [(int) row.Col3 - 1]);
		}

		void InitializePInvokes ()
		{
			if (this.metadata.PInvokes != null)
				return;

			int length = this.MoveTo (Table.ImplMap);

			var pinvokes = this.metadata.PInvokes = new Dictionary<uint, Row<PInvokeAttributes, uint, uint>> (length);

			for (int i = 1; i <= length; i++) {
				var attributes = (PInvokeAttributes)this.ReadUInt16 ();
				var method = this.ReadMetadataToken (CodedIndex.MemberForwarded);
				var name = this.ReadStringIndex ();
				var scope = this.ReadTableIndex (Table.File);

				if (method.TokenType != TokenType.Method)
					continue;

				pinvokes.Add (method.RID, new Row<PInvokeAttributes, uint, uint> (attributes, name, scope));
			}
		}

		public bool HasGenericParameters (IGenericParameterProvider provider)
		{
            this.InitializeGenericParameters ();

			Range [] ranges;
			if (!this.metadata.TryGetGenericParameterRanges (provider, out ranges))
				return false;

			return RangesSize (ranges) > 0;
		}

		public Collection<GenericParameter> ReadGenericParameters (IGenericParameterProvider provider)
		{
            this.InitializeGenericParameters ();

			Range [] ranges;
			if (!this.metadata.TryGetGenericParameterRanges (provider, out ranges))
				return new GenericParameterCollection (provider);

            this.metadata.RemoveGenericParameterRange (provider);

			var generic_parameters = new GenericParameterCollection (provider, RangesSize (ranges));

			for (int i = 0; i < ranges.Length; i++) this.ReadGenericParametersRange (ranges [i], provider, generic_parameters);

			return generic_parameters;
		}

		void ReadGenericParametersRange (Range range, IGenericParameterProvider provider, GenericParameterCollection generic_parameters)
		{
			if (!this.MoveTo (Table.GenericParam, range.Start))
				return;

			for (uint i = 0; i < range.Length; i++) {
                this.ReadUInt16 (); // index
				var flags = (GenericParameterAttributes)this.ReadUInt16 ();
                this.ReadMetadataToken (CodedIndex.TypeOrMethodDef);
				var name = this.ReadString ();

				var parameter = new GenericParameter (name, provider);
				parameter.token = new MetadataToken (TokenType.GenericParam, range.Start + i);
				parameter.Attributes = flags;

				generic_parameters.Add (parameter);
			}
		}

		void InitializeGenericParameters ()
		{
			if (this.metadata.GenericParameters != null)
				return;

            this.metadata.GenericParameters = this.InitializeRanges (
				Table.GenericParam, () => {
                    this.Advance (4);
					var next = this.ReadMetadataToken (CodedIndex.TypeOrMethodDef);
                    this.ReadStringIndex ();
					return next;
			});
		}

		Dictionary<MetadataToken, Range []> InitializeRanges (Table table, Func<MetadataToken> get_next)
		{
			int length = this.MoveTo (table);
			var ranges = new Dictionary<MetadataToken, Range []> (length);

			if (length == 0)
				return ranges;

			MetadataToken owner = MetadataToken.Zero;
			Range range = new Range (1, 0);

			for (uint i = 1; i <= length; i++) {
				var next = get_next ();

				if (i == 1) {
					owner = next;
					range.Length++;
				} else if (next != owner) {
					AddRange (ranges, owner, range);
					range = new Range (i, 1);
					owner = next;
				} else
					range.Length++;
			}

			AddRange (ranges, owner, range);

			return ranges;
		}

		static void AddRange (Dictionary<MetadataToken, Range []> ranges, MetadataToken owner, Range range)
		{
			if (owner.RID == 0)
				return;

			Range [] slots;
			if (!ranges.TryGetValue (owner, out slots)) {
				ranges.Add (owner, new [] { range });
				return;
			}

			slots = slots.Resize (slots.Length + 1);
			slots [slots.Length - 1] = range;
			ranges [owner] = slots;
		}

		public bool HasGenericConstraints (GenericParameter generic_parameter)
		{
            this.InitializeGenericConstraints ();

			MetadataToken [] mapping;
			if (!this.metadata.TryGetGenericConstraintMapping (generic_parameter, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<TypeReference> ReadGenericConstraints (GenericParameter generic_parameter)
		{
            this.InitializeGenericConstraints ();

			MetadataToken [] mapping;
			if (!this.metadata.TryGetGenericConstraintMapping (generic_parameter, out mapping))
				return new Collection<TypeReference> ();

			var constraints = new Collection<TypeReference> (mapping.Length);

			this.context = (IGenericContext) generic_parameter.Owner;

			for (int i = 0; i < mapping.Length; i++)
				constraints.Add (this.GetTypeDefOrRef (mapping [i]));

            this.metadata.RemoveGenericConstraintMapping (generic_parameter);

			return constraints;
		}

		void InitializeGenericConstraints ()
		{
			if (this.metadata.GenericConstraints != null)
				return;

			var length = this.MoveTo (Table.GenericParamConstraint);

            this.metadata.GenericConstraints = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 1; i <= length; i++)
                this.AddGenericConstraintMapping (this.ReadTableIndex (Table.GenericParam), this.ReadMetadataToken (CodedIndex.TypeDefOrRef));
		}

		void AddGenericConstraintMapping (uint generic_parameter, MetadataToken constraint)
		{
            this.metadata.SetGenericConstraintMapping (
				generic_parameter,
				AddMapping (this.metadata.GenericConstraints, generic_parameter, constraint));
		}

		public bool HasOverrides (MethodDefinition method)
		{
            this.InitializeOverrides ();
			MetadataToken [] mapping;

			if (!this.metadata.TryGetOverrideMapping (method, out mapping))
				return false;

			return mapping.Length > 0;
		}

		public Collection<MethodReference> ReadOverrides (MethodDefinition method)
		{
            this.InitializeOverrides ();

			MetadataToken [] mapping;
			if (!this.metadata.TryGetOverrideMapping (method, out mapping))
				return new Collection<MethodReference> ();

			var overrides = new Collection<MethodReference> (mapping.Length);

			this.context = method;

			for (int i = 0; i < mapping.Length; i++)
				overrides.Add ((MethodReference)this.LookupToken (mapping [i]));

            this.metadata.RemoveOverrideMapping (method);

			return overrides;
		}

		void InitializeOverrides ()
		{
			if (this.metadata.Overrides != null)
				return;

			var length = this.MoveTo (Table.MethodImpl);

            this.metadata.Overrides = new Dictionary<uint, MetadataToken []> (length);

			for (int i = 1; i <= length; i++) {
                this.ReadTableIndex (Table.TypeDef);

				var method = this.ReadMetadataToken (CodedIndex.MethodDefOrRef);
				if (method.TokenType != TokenType.Method)
					throw new NotSupportedException ();

				var @override = this.ReadMetadataToken (CodedIndex.MethodDefOrRef);

                this.AddOverrideMapping (method.RID, @override);
			}
		}

		void AddOverrideMapping (uint method_rid, MetadataToken @override)
		{
            this.metadata.SetOverrideMapping (
				method_rid,
				AddMapping (this.metadata.Overrides, method_rid, @override));
		}

		public MethodBody ReadMethodBody (MethodDefinition method)
		{
			/*Telerik Authorship*/
			MethodBody result = this.code.ReadMethodBody (method);

			foreach (Instruction instruction in result.Instructions)
			{
				instruction.ContainingMethod = method;
			}

			/*Telerik Authorship*/
			foreach (VariableDefinition variable in result.Variables)
			{
				variable.ContainingMethod = method;
			}

			return result;
		}

		public CallSite ReadCallSite (MetadataToken token)
		{
			if (!this.MoveTo (Table.StandAloneSig, token.RID))
				return null;

			var signature = this.ReadBlobIndex ();

			var call_site = new CallSite ();

            this.ReadMethodSignature (signature, call_site);

			call_site.MetadataToken = token;

			return call_site;
		}

		public VariableDefinitionCollection ReadVariables (MetadataToken local_var_token)
		{
			if (!this.MoveTo (Table.StandAloneSig, local_var_token.RID))
				return null;

			var reader = this.ReadSignature (this.ReadBlobIndex ());
			const byte local_sig = 0x7;

			if (reader.ReadByte () != local_sig)
				throw new NotSupportedException ();

			var count = reader.ReadCompressedUInt32 ();
			if (count == 0)
				return null;

			var variables = new VariableDefinitionCollection ((int) count);

			for (int i = 0; i < count; i++)
				variables.Add (new VariableDefinition (reader.ReadTypeSignature ()));

			return variables;
		}

		public IMetadataTokenProvider LookupToken (MetadataToken token)
		{
			var rid = token.RID;

			if (rid == 0)
				return null;

			IMetadataTokenProvider element;
			var position = this.position;
			var context = this.context;

			switch (token.TokenType) {
			case TokenType.TypeDef:
				element = this.GetTypeDefinition (rid);
				break;
			case TokenType.TypeRef:
				element = this.GetTypeReference (rid);
				break;
			case TokenType.TypeSpec:
				element = this.GetTypeSpecification (rid);
				break;
			case TokenType.Field:
				element = this.GetFieldDefinition (rid);
				break;
			case TokenType.Method:
				element = this.GetMethodDefinition (rid);
				break;
			case TokenType.MemberRef:
				element = this.GetMemberReference (rid);
				break;
			case TokenType.MethodSpec:
				element = this.GetMethodSpecification (rid);
				break;
			default:
				return null;
			}

			this.position = position;
			this.context = context;

			return element;
		}

		public FieldDefinition GetFieldDefinition (uint rid)
		{
            this.InitializeTypeDefinitions ();

			var field = this.metadata.GetFieldDefinition (rid);
			if (field != null)
				return field;

			return this.LookupField (rid);
		}

		FieldDefinition LookupField (uint rid)
		{
			var type = this.metadata.GetFieldDeclaringType (rid);
			if (type == null)
				return null;

			InitializeCollection (type.Fields);

			return this.metadata.GetFieldDefinition (rid);
		}

		public MethodDefinition GetMethodDefinition (uint rid)
		{
            this.InitializeTypeDefinitions ();

			var method = this.metadata.GetMethodDefinition (rid);
			if (method != null)
				return method;

			return this.LookupMethod (rid);
		}

		MethodDefinition LookupMethod (uint rid)
		{
			var type = this.metadata.GetMethodDeclaringType (rid);
			if (type == null)
				return null;

			InitializeCollection (type.Methods);

			return this.metadata.GetMethodDefinition (rid);
		}

		MethodSpecification GetMethodSpecification (uint rid)
		{
			if (!this.MoveTo (Table.MethodSpec, rid))
				return null;

			var element_method = (MethodReference)this.LookupToken (this.ReadMetadataToken (CodedIndex.MethodDefOrRef));
			var signature = this.ReadBlobIndex ();

			var method_spec = this.ReadMethodSpecSignature (signature, element_method);
			method_spec.token = new MetadataToken (TokenType.MethodSpec, rid);
			return method_spec;
		}

		MethodSpecification ReadMethodSpecSignature (uint signature, MethodReference method)
		{
			var reader = this.ReadSignature (signature);
			const byte methodspec_sig = 0x0a;

			var call_conv = reader.ReadByte ();

			if (call_conv != methodspec_sig)
				throw new NotSupportedException ();

			var instance = new GenericInstanceMethod (method);

			reader.ReadGenericInstanceSignature (method, instance);

			return instance;
		}

		MemberReference GetMemberReference (uint rid)
		{
            this.InitializeMemberReferences ();

			var member = this.metadata.GetMemberReference (rid);
			if (member != null)
				return member;

			member = this.ReadMemberReference (rid);
			if (member != null && !member.ContainsGenericParameter) this.metadata.AddMemberReference (member);
			return member;
		}

		MemberReference ReadMemberReference (uint rid)
		{
			if (!this.MoveTo (Table.MemberRef, rid))
				return null;

			var token = this.ReadMetadataToken (CodedIndex.MemberRefParent);
			var name = this.ReadString ();
			var signature = this.ReadBlobIndex ();

			MemberReference member;

			switch (token.TokenType) {
			case TokenType.TypeDef:
			case TokenType.TypeRef:
			case TokenType.TypeSpec:
				member = this.ReadTypeMemberReference (token, name, signature);
				break;
			case TokenType.Method:
				member = this.ReadMethodMemberReference (token, name, signature);
				break;
			default:
				throw new NotSupportedException ();
			}

			member.token = new MetadataToken (TokenType.MemberRef, rid);

			return member;
		}

		MemberReference ReadTypeMemberReference (MetadataToken type, string name, uint signature)
		{
			var declaring_type = this.GetTypeDefOrRef (type);

			if (!declaring_type.IsArray)
				this.context = declaring_type;

			var member = this.ReadMemberReferenceSignature (signature, declaring_type);
			member.Name = name;

			return member;
		}

		MemberReference ReadMemberReferenceSignature (uint signature, TypeReference declaring_type)
		{
			var reader = this.ReadSignature (signature);
			const byte field_sig = 0x6;

			if (reader.buffer [reader.position] == field_sig) {
				reader.position++;
				var field = new FieldReference ();
				field.DeclaringType = declaring_type;
				field.FieldType = reader.ReadTypeSignature ();
				return field;
			} else {
				var method = new MethodReference ();
				method.DeclaringType = declaring_type;
				reader.ReadMethodSignature (method);
				return method;
			}
		}

		MemberReference ReadMethodMemberReference (MetadataToken token, string name, uint signature)
		{
			var method = this.GetMethodDefinition (token.RID);

			this.context = method;

			var member = this.ReadMemberReferenceSignature (signature, method.DeclaringType);
			member.Name = name;

			return member;
		}

		void InitializeMemberReferences ()
		{
			if (this.metadata.MemberReferences != null)
				return;

            this.metadata.MemberReferences = new MemberReference [this.image.GetTableLength (Table.MemberRef)];
		}

		public IEnumerable<MemberReference> GetMemberReferences ()
		{
            this.InitializeMemberReferences ();

			var length = this.image.GetTableLength (Table.MemberRef);

			var type_system = this.module.TypeSystem;

			var context = new MethodReference (string.Empty, type_system.Void);
			context.DeclaringType = new TypeReference (string.Empty, string.Empty, this.module, type_system.Corlib);

			var member_references = new MemberReference [length];

			for (uint i = 1; i <= length; i++) {
				this.context = context;
				member_references [i - 1] = this.GetMemberReference (i);
			}

			return member_references;
		}

		void InitializeConstants ()
		{
			if (this.metadata.Constants != null)
				return;

			var length = this.MoveTo (Table.Constant);

			var constants = this.metadata.Constants = new Dictionary<MetadataToken, Row<ElementType, uint>> (length);

			for (uint i = 1; i <= length; i++) {
				var type = (ElementType)this.ReadUInt16 ();
				var owner = this.ReadMetadataToken (CodedIndex.HasConstant);
				var signature = this.ReadBlobIndex ();

				constants.Add (owner, new Row<ElementType, uint> (type, signature));
			}
		}

		/*Telerik Authorship*/
		public ConstantValue ReadConstant (IConstantProvider owner)
		{
            this.InitializeConstants ();

			Row<ElementType, uint> row;
			if (!this.metadata.Constants.TryGetValue (owner.MetadataToken, out row))
				return Mixin.NoValue;

            this.metadata.Constants.Remove (owner.MetadataToken);

			switch (row.Col1) {
			case ElementType.Class:
			case ElementType.Object:
				/*Telerik Authorship*/
				return new ConstantValue(null, row.Col1);
			case ElementType.String:
				/*Telerik Authorship*/
				return new ConstantValue(ReadConstantString (this.ReadBlob (row.Col2)), row.Col1);
			default:
				/*Telerik Authorship*/
				return new ConstantValue(this.ReadConstantPrimitive (row.Col1, row.Col2), row.Col1);
			}
		}

		static string ReadConstantString (byte [] blob)
		{
			var length = blob.Length;
			if ((length & 1) == 1)
				length--;

			return Encoding.Unicode.GetString (blob, 0, length);
		}

		object ReadConstantPrimitive (ElementType type, uint signature)
		{
			var reader = this.ReadSignature (signature);
			return reader.ReadConstantSignature (type);
		}

		void InitializeCustomAttributes ()
		{
			if (this.metadata.CustomAttributes != null)
				return;

            this.metadata.CustomAttributes = this.InitializeRanges (
				Table.CustomAttribute, () => {
					var next = this.ReadMetadataToken (CodedIndex.HasCustomAttribute);
                    this.ReadMetadataToken (CodedIndex.CustomAttributeType);
                    this.ReadBlobIndex ();
					return next;
			});
		}

		public bool HasCustomAttributes (ICustomAttributeProvider owner)
		{
            this.InitializeCustomAttributes ();

			Range [] ranges;
			if (!this.metadata.TryGetCustomAttributeRanges (owner, out ranges))
				return false;

			return RangesSize (ranges) > 0;
		}

		public Collection<CustomAttribute> ReadCustomAttributes (ICustomAttributeProvider owner)
		{
            this.InitializeCustomAttributes ();

			Range [] ranges;
			if (!this.metadata.TryGetCustomAttributeRanges (owner, out ranges))
				return new Collection<CustomAttribute> ();

			var custom_attributes = new Collection<CustomAttribute> (RangesSize (ranges));

			for (int i = 0; i < ranges.Length; i++) this.ReadCustomAttributeRange (ranges [i], custom_attributes);

            this.metadata.RemoveCustomAttributeRange (owner);

			return custom_attributes;
		}

		void ReadCustomAttributeRange (Range range, Collection<CustomAttribute> custom_attributes)
		{
			if (!this.MoveTo (Table.CustomAttribute, range.Start))
				return;

			for (int i = 0; i < range.Length; i++) {
                this.ReadMetadataToken (CodedIndex.HasCustomAttribute);

				var constructor = (MethodReference)this.LookupToken (this.ReadMetadataToken (CodedIndex.CustomAttributeType));

				var signature = this.ReadBlobIndex ();

				custom_attributes.Add (new CustomAttribute (signature, constructor));
			}
		}

		static int RangesSize (Range [] ranges)
		{
			uint size = 0;
			for (int i = 0; i < ranges.Length; i++)
				size += ranges [i].Length;

			return (int) size;
		}

		public byte [] ReadCustomAttributeBlob (uint signature)
		{
			return this.ReadBlob (signature);
		}

		public void ReadCustomAttributeSignature (CustomAttribute attribute)
		{
			var reader = this.ReadSignature (attribute.signature);

			if (!reader.CanReadMore ())
				return;

			if (reader.ReadUInt16 () != 0x0001)
				throw new InvalidOperationException ();

			var constructor = attribute.Constructor;
			if (constructor.HasParameters)
				reader.ReadCustomAttributeConstructorArguments (attribute, constructor.Parameters);

			if (!reader.CanReadMore ())
				return;

			var named = reader.ReadUInt16 ();

			if (named == 0)
				return;

			reader.ReadCustomAttributeNamedArguments (named, ref attribute.fields, ref attribute.properties);
		}

		void InitializeMarshalInfos ()
		{
			if (this.metadata.FieldMarshals != null)
				return;

			var length = this.MoveTo (Table.FieldMarshal);

			var marshals = this.metadata.FieldMarshals = new Dictionary<MetadataToken, uint> (length);

			for (int i = 0; i < length; i++) {
				var token = this.ReadMetadataToken (CodedIndex.HasFieldMarshal);
				var signature = this.ReadBlobIndex ();
				if (token.RID == 0)
					continue;

				marshals.Add (token, signature);
			}
		}

		public bool HasMarshalInfo (IMarshalInfoProvider owner)
		{
            this.InitializeMarshalInfos ();

			return this.metadata.FieldMarshals.ContainsKey (owner.MetadataToken);
		}

		public MarshalInfo ReadMarshalInfo (IMarshalInfoProvider owner)
		{
            this.InitializeMarshalInfos ();

			uint signature;
			if (!this.metadata.FieldMarshals.TryGetValue (owner.MetadataToken, out signature))
				return null;

			var reader = this.ReadSignature (signature);

            this.metadata.FieldMarshals.Remove (owner.MetadataToken);

			return reader.ReadMarshalInfo ();
		}

		void InitializeSecurityDeclarations ()
		{
			if (this.metadata.SecurityDeclarations != null)
				return;

            this.metadata.SecurityDeclarations = this.InitializeRanges (
				Table.DeclSecurity, () => {
                    this.ReadUInt16 ();
					var next = this.ReadMetadataToken (CodedIndex.HasDeclSecurity);
                    this.ReadBlobIndex ();
					return next;
			});
		}

		public bool HasSecurityDeclarations (ISecurityDeclarationProvider owner)
		{
            this.InitializeSecurityDeclarations ();

			Range [] ranges;
			if (!this.metadata.TryGetSecurityDeclarationRanges (owner, out ranges))
				return false;

			return RangesSize (ranges) > 0;
		}

		public Collection<SecurityDeclaration> ReadSecurityDeclarations (ISecurityDeclarationProvider owner)
		{
            this.InitializeSecurityDeclarations ();

			Range [] ranges;
			if (!this.metadata.TryGetSecurityDeclarationRanges (owner, out ranges))
				return new Collection<SecurityDeclaration> ();

			var security_declarations = new Collection<SecurityDeclaration> (RangesSize (ranges));

			for (int i = 0; i < ranges.Length; i++) this.ReadSecurityDeclarationRange (ranges [i], security_declarations);

            this.metadata.RemoveSecurityDeclarationRange (owner);

			return security_declarations;
		}

		void ReadSecurityDeclarationRange (Range range, Collection<SecurityDeclaration> security_declarations)
		{
			if (!this.MoveTo (Table.DeclSecurity, range.Start))
				return;

			for (int i = 0; i < range.Length; i++) {
				var action = (SecurityAction)this.ReadUInt16 ();
                this.ReadMetadataToken (CodedIndex.HasDeclSecurity);
				var signature = this.ReadBlobIndex ();

				security_declarations.Add (new SecurityDeclaration (action, signature, this.module));
			}
		}

		public byte [] ReadSecurityDeclarationBlob (uint signature)
		{
			return this.ReadBlob (signature);
		}

		public void ReadSecurityDeclarationSignature (SecurityDeclaration declaration)
		{
			var signature = declaration.signature;
			var reader = this.ReadSignature (signature);

			if (reader.buffer [reader.position] != '.') {
                this.ReadXmlSecurityDeclaration (signature, declaration);
				return;
			}

			reader.position++;
			var count = reader.ReadCompressedUInt32 ();
			var attributes = new Collection<SecurityAttribute> ((int) count);

			for (int i = 0; i < count; i++)
				attributes.Add (reader.ReadSecurityAttribute ());

			declaration.security_attributes = attributes;
		}

		void ReadXmlSecurityDeclaration (uint signature, SecurityDeclaration declaration)
		{
			var blob = this.ReadBlob (signature);
			var attributes = new Collection<SecurityAttribute> (1);

			var attribute = new SecurityAttribute (this.module.TypeSystem.LookupType ("System.Security.Permissions", "PermissionSetAttribute"));

			attribute.properties = new Collection<CustomAttributeNamedArgument> (1);
			attribute.properties.Add (
				new CustomAttributeNamedArgument (
					"XML",
					new CustomAttributeArgument (this.module.TypeSystem.String,
						Encoding.Unicode.GetString (blob, 0, blob.Length))));

			attributes.Add (attribute);

			declaration.security_attributes = attributes;
		}

		public Collection<ExportedType> ReadExportedTypes ()
		{
			var length = this.MoveTo (Table.ExportedType);
			if (length == 0)
				return new Collection<ExportedType> ();

			var exported_types = new Collection<ExportedType> (length);

			for (int i = 1; i <= length; i++) {
				var attributes = (TypeAttributes)this.ReadUInt32 ();
				var identifier = this.ReadUInt32 ();
				var name = this.ReadString ();
				var @namespace = this.ReadString ();
				var implementation = this.ReadMetadataToken (CodedIndex.Implementation);

				ExportedType declaring_type = null;
				IMetadataScope scope = null;

				switch (implementation.TokenType) {
				case TokenType.AssemblyRef:
				case TokenType.File:
					scope = this.GetExportedTypeScope (implementation);
					break;
				case TokenType.ExportedType:
					// FIXME: if the table is not properly sorted
					declaring_type = exported_types [(int) implementation.RID - 1];
					break;
				}

				var exported_type = new ExportedType (@namespace, name, this.module, scope) {
					Attributes = attributes,
					Identifier = (int) identifier,
					DeclaringType = declaring_type,
				};
				exported_type.token = new MetadataToken (TokenType.ExportedType, i);

				exported_types.Add (exported_type);
			}

			return exported_types;
		}

		IMetadataScope GetExportedTypeScope (MetadataToken token)
		{
			var position = this.position;
			IMetadataScope scope;

			switch (token.TokenType) {
			case TokenType.AssemblyRef:
                this.InitializeAssemblyReferences ();
				scope = this.metadata.AssemblyReferences [(int) token.RID - 1];
				break;
			case TokenType.File:
                this.InitializeModuleReferences ();
				scope = this.GetModuleReferenceFromFile (token);
				break;
			default:
				throw new NotSupportedException ();
			}

			this.position = position;
			return scope;
		}

		ModuleReference GetModuleReferenceFromFile (MetadataToken token)
		{
			if (!this.MoveTo (Table.File, token.RID))
				return null;

            this.ReadUInt32 ();
			var file_name = this.ReadString ();
			var modules = this.module.ModuleReferences;

			ModuleReference reference;
			for (int i = 0; i < modules.Count; i++) {
				reference = modules [i];
				if (reference.Name == file_name)
					return reference;
			}

			reference = new ModuleReference (file_name);
			modules.Add (reference);
			return reference;
		}

		static void InitializeCollection (object o)
		{
		}
	}

	sealed class SignatureReader : ByteBuffer {

		readonly MetadataReader reader;
		readonly uint start, sig_length;

		TypeSystem TypeSystem {
			get { return this.reader.module.TypeSystem; }
		}

		public SignatureReader (uint blob, MetadataReader reader)
			: base (reader.buffer)
		{
			this.reader = reader;

            this.MoveToBlob (blob);

			this.sig_length = this.ReadCompressedUInt32 ();
			this.start = (uint)this.position;
		}

		void MoveToBlob (uint blob)
		{
            this.position = (int) (this.reader.image.BlobHeap.Offset + blob);
		}

		MetadataToken ReadTypeTokenSignature ()
		{
			return CodedIndex.TypeDefOrRef.GetMetadataToken (this.ReadCompressedUInt32 ());
		}

		GenericParameter GetGenericParameter (GenericParameterType type, uint var)
		{
			var context = this.reader.context;
			int index = (int) var;

			if (context == null)
				return this.GetUnboundGenericParameter (type, index);

			IGenericParameterProvider provider;

			switch (type) {
			case GenericParameterType.Type:
				provider = context.Type;
				break;
			case GenericParameterType.Method:
				provider = context.Method;
				break;
			default:
				throw new NotSupportedException ();
			}

			if (!context.IsDefinition)
				CheckGenericContext (provider, index);

			if (index >= provider.GenericParameters.Count)
				return this.GetUnboundGenericParameter (type, index);

			return provider.GenericParameters [index];
		}

		GenericParameter GetUnboundGenericParameter (GenericParameterType type, int index)
		{
			return new GenericParameter (index, type, this.reader.module);
		}

		static void CheckGenericContext (IGenericParameterProvider owner, int index)
		{
			var owner_parameters = owner.GenericParameters;

			for (int i = owner_parameters.Count; i <= index; i++)
				owner_parameters.Add (new GenericParameter (owner));
		}

		public void ReadGenericInstanceSignature (IGenericParameterProvider provider, IGenericInstance instance)
		{
			var arity = this.ReadCompressedUInt32 ();

			if (!provider.IsDefinition)
				CheckGenericContext (provider, (int) arity - 1);

			var instance_arguments = instance.GenericArguments;

			for (int i = 0; i < arity; i++)
			{
				/*Telerik Authorship*/
				var toAdd = this.ReadTypeSignature();
				instance_arguments.Add(toAdd);
				instance.AddGenericArgument(toAdd);
			}
		}

		ArrayType ReadArrayTypeSignature ()
		{
			var array = new ArrayType (this.ReadTypeSignature ());

			var rank = this.ReadCompressedUInt32 ();

			var sizes = new uint [this.ReadCompressedUInt32 ()];
			for (int i = 0; i < sizes.Length; i++)
				sizes [i] = this.ReadCompressedUInt32 ();

			var low_bounds = new int [this.ReadCompressedUInt32 ()];
			for (int i = 0; i < low_bounds.Length; i++)
				low_bounds [i] = this.ReadCompressedInt32 ();

			array.Dimensions.Clear ();

			for (int i = 0; i < rank; i++) {
				int? lower = null, upper = null;

				if (i < low_bounds.Length)
					lower = low_bounds [i];

				if (i < sizes.Length)
					upper = lower + (int) sizes [i] - 1;

				array.Dimensions.Add (new ArrayDimension (lower, upper));
			}

			return array;
		}

		TypeReference GetTypeDefOrRef (MetadataToken token)
		{
			return this.reader.GetTypeDefOrRef (token);
		}

		public TypeReference ReadTypeSignature ()
		{
			return this.ReadTypeSignature ((ElementType)this.ReadByte ());
		}

		TypeReference ReadTypeSignature (ElementType etype)
		{
			switch (etype) {
			case ElementType.ValueType: {
				var value_type = this.GetTypeDefOrRef (this.ReadTypeTokenSignature ());
				value_type.IsValueType = true;
				return value_type;
			}
			case ElementType.Class:
				return this.GetTypeDefOrRef (this.ReadTypeTokenSignature ());
			case ElementType.Ptr:
				return new PointerType (this.ReadTypeSignature ());
			case ElementType.FnPtr: {
				var fptr = new FunctionPointerType ();
                this.ReadMethodSignature (fptr);
				return fptr;
			}
			case ElementType.ByRef:
				return new ByReferenceType (this.ReadTypeSignature ());
			case ElementType.Pinned:
				return new PinnedType (this.ReadTypeSignature ());
			case ElementType.SzArray:
				return new ArrayType (this.ReadTypeSignature ());
			case ElementType.Array:
				return this.ReadArrayTypeSignature ();
			case ElementType.CModOpt:
				return new OptionalModifierType (this.GetTypeDefOrRef (this.ReadTypeTokenSignature ()), this.ReadTypeSignature ());
			case ElementType.CModReqD:
				return new RequiredModifierType (this.GetTypeDefOrRef (this.ReadTypeTokenSignature ()), this.ReadTypeSignature ());
			case ElementType.Sentinel:
				return new SentinelType (this.ReadTypeSignature ());
			case ElementType.Var:
				return this.GetGenericParameter (GenericParameterType.Type, this.ReadCompressedUInt32 ());
			case ElementType.MVar:
				return this.GetGenericParameter (GenericParameterType.Method, this.ReadCompressedUInt32 ());
			case ElementType.GenericInst: {
				var is_value_type = this.ReadByte () == (byte) ElementType.ValueType;
				var element_type = this.GetTypeDefOrRef (this.ReadTypeTokenSignature ());
				var generic_instance = new GenericInstanceType (element_type);

                this.ReadGenericInstanceSignature (element_type, generic_instance);

				if (is_value_type) {
					generic_instance.IsValueType = true;
					element_type.GetElementType ().IsValueType = true;
				}

				return generic_instance;
			}
			case ElementType.Object: return this.TypeSystem.Object;
			case ElementType.Void: return this.TypeSystem.Void;
			case ElementType.TypedByRef: return this.TypeSystem.TypedReference;
			case ElementType.I: return this.TypeSystem.IntPtr;
			case ElementType.U: return this.TypeSystem.UIntPtr;
			default: return this.GetPrimitiveType (etype);
			}
		}

		public void ReadMethodSignature (IMethodSignature method)
		{
			var calling_convention = this.ReadByte ();

			const byte has_this = 0x20;
			const byte explicit_this = 0x40;

			if ((calling_convention & has_this) != 0) {
				method.HasThis = true;
				calling_convention = (byte) (calling_convention & ~has_this);
			}

			if ((calling_convention & explicit_this) != 0) {
				method.ExplicitThis = true;
				calling_convention = (byte) (calling_convention & ~explicit_this);
			}

			method.CallingConvention = (MethodCallingConvention) calling_convention;

			var generic_context = method as MethodReference;
			if (generic_context != null && !generic_context.DeclaringType.IsArray) this.reader.context = generic_context;

			if ((calling_convention & 0x10) != 0) {
				var arity = this.ReadCompressedUInt32 ();

				if (generic_context != null && !generic_context.IsDefinition)
					CheckGenericContext (generic_context, (int) arity -1 );
			}

			var param_count = this.ReadCompressedUInt32 ();

			method.MethodReturnType.ReturnType = this.ReadTypeSignature ();

			if (param_count == 0)
				return;

			Collection<ParameterDefinition> parameters;

			var method_ref = method as MethodReference;
			if (method_ref != null)
				parameters = method_ref.parameters = new ParameterDefinitionCollection (method, (int) param_count);
			else
				parameters = method.Parameters;

			for (int i = 0; i < param_count; i++)
				parameters.Add (new ParameterDefinition (this.ReadTypeSignature ()));
		}

		public object ReadConstantSignature (ElementType type)
		{
			return this.ReadPrimitiveValue (type);
		}

		public void ReadCustomAttributeConstructorArguments (CustomAttribute attribute, Collection<ParameterDefinition> parameters)
		{
			var count = parameters.Count;
			if (count == 0)
				return;

			attribute.arguments = new Collection<CustomAttributeArgument> (count);

			for (int i = 0; i < count; i++)
				attribute.arguments.Add (this.ReadCustomAttributeFixedArgument (parameters [i].ParameterType));
		}

		CustomAttributeArgument ReadCustomAttributeFixedArgument (TypeReference type)
		{
			if (type.IsArray)
				return this.ReadCustomAttributeFixedArrayArgument ((ArrayType) type);

			return this.ReadCustomAttributeElement (type);
		}

		public void ReadCustomAttributeNamedArguments (ushort count, ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			for (int i = 0; i < count; i++) this.ReadCustomAttributeNamedArgument (ref fields, ref properties);
		}

		void ReadCustomAttributeNamedArgument (ref Collection<CustomAttributeNamedArgument> fields, ref Collection<CustomAttributeNamedArgument> properties)
		{
			var kind = this.ReadByte ();
			var type = this.ReadCustomAttributeFieldOrPropType ();
			var name = this.ReadUTF8String ();

			Collection<CustomAttributeNamedArgument> container;
			switch (kind) {
			case 0x53:
				container = GetCustomAttributeNamedArgumentCollection (ref fields);
				break;
			case 0x54:
				container = GetCustomAttributeNamedArgumentCollection (ref properties);
				break;
			default:
				throw new NotSupportedException ();
			}

			container.Add (new CustomAttributeNamedArgument (name, this.ReadCustomAttributeFixedArgument (type)));
		}

		static Collection<CustomAttributeNamedArgument> GetCustomAttributeNamedArgumentCollection (ref Collection<CustomAttributeNamedArgument> collection)
		{
			if (collection != null)
				return collection;

			return collection = new Collection<CustomAttributeNamedArgument> ();
		}

		CustomAttributeArgument ReadCustomAttributeFixedArrayArgument (ArrayType type)
		{
			var length = this.ReadUInt32 ();

			if (length == 0xffffffff)
				return new CustomAttributeArgument (type, null);

			if (length == 0)
				return new CustomAttributeArgument (type, Empty<CustomAttributeArgument>.Array);

			var arguments = new CustomAttributeArgument [length];
			var element_type = type.ElementType;

			for (int i = 0; i < length; i++)
				arguments [i] = this.ReadCustomAttributeElement (element_type);

			return new CustomAttributeArgument (type, arguments);
		}

		CustomAttributeArgument ReadCustomAttributeElement (TypeReference type)
		{
			if (type.IsArray)
				return this.ReadCustomAttributeFixedArrayArgument ((ArrayType) type);

			return new CustomAttributeArgument (
				type,
				type.etype == ElementType.Object
					? this.ReadCustomAttributeElement (this.ReadCustomAttributeFieldOrPropType ())
					: this.ReadCustomAttributeElementValue (type));
		}

		object ReadCustomAttributeElementValue (TypeReference type)
		{
			var etype = type.etype;

			switch (etype) {
			case ElementType.String:
				return this.ReadUTF8String ();
			case ElementType.None:
				if (type.IsTypeOf ("System", "Type"))
					return this.ReadTypeReference ();

				return this.ReadCustomAttributeEnum (type);
			default:
				return this.ReadPrimitiveValue (etype);
			}
		}

		object ReadPrimitiveValue (ElementType type)
		{
			switch (type) {
			case ElementType.Boolean:
				return this.ReadByte () == 1;
			case ElementType.I1:
				return (sbyte)this.ReadByte ();
			case ElementType.U1:
				return this.ReadByte ();
			case ElementType.Char:
				return (char)this.ReadUInt16 ();
			case ElementType.I2:
				return this.ReadInt16 ();
			case ElementType.U2:
				return this.ReadUInt16 ();
			case ElementType.I4:
				return this.ReadInt32 ();
			case ElementType.U4:
				return this.ReadUInt32 ();
			case ElementType.I8:
				return this.ReadInt64 ();
			case ElementType.U8:
				return this.ReadUInt64 ();
			case ElementType.R4:
				return this.ReadSingle ();
			case ElementType.R8:
				return this.ReadDouble ();
			default:
				throw new NotImplementedException (type.ToString ());
			}
		}

		TypeReference GetPrimitiveType (ElementType etype)
		{
			switch (etype) {
			case ElementType.Boolean:
				return this.TypeSystem.Boolean;
			case ElementType.Char:
				return this.TypeSystem.Char;
			case ElementType.I1:
				return this.TypeSystem.SByte;
			case ElementType.U1:
				return this.TypeSystem.Byte;
			case ElementType.I2:
				return this.TypeSystem.Int16;
			case ElementType.U2:
				return this.TypeSystem.UInt16;
			case ElementType.I4:
				return this.TypeSystem.Int32;
			case ElementType.U4:
				return this.TypeSystem.UInt32;
			case ElementType.I8:
				return this.TypeSystem.Int64;
			case ElementType.U8:
				return this.TypeSystem.UInt64;
			case ElementType.R4:
				return this.TypeSystem.Single;
			case ElementType.R8:
				return this.TypeSystem.Double;
			case ElementType.String:
				return this.TypeSystem.String;
			default:
				throw new NotImplementedException (etype.ToString ());
			}
		}

		TypeReference ReadCustomAttributeFieldOrPropType ()
		{
			var etype = (ElementType)this.ReadByte ();

			switch (etype) {
			case ElementType.Boxed:
				return this.TypeSystem.Object;
			case ElementType.SzArray:
				return new ArrayType (this.ReadCustomAttributeFieldOrPropType ());
			case ElementType.Enum:
				return this.ReadTypeReference ();
			case ElementType.Type:
				return this.TypeSystem.LookupType ("System", "Type");
			default:
				return this.GetPrimitiveType (etype);
			}
		}

		public TypeReference ReadTypeReference ()
		{
			return TypeParser.ParseType (this.reader.module, this.ReadUTF8String ());
		}

		object ReadCustomAttributeEnum (TypeReference enum_type)
		{
			var type = enum_type.CheckedResolve ();
			if (!type.IsEnum)
				throw new ArgumentException ();

			return this.ReadCustomAttributeElementValue (type.GetEnumUnderlyingType ());
		}

		public SecurityAttribute ReadSecurityAttribute ()
		{
			var attribute = new SecurityAttribute (this.ReadTypeReference ());

            this.ReadCompressedUInt32 ();

            this.ReadCustomAttributeNamedArguments (
				(ushort)this.ReadCompressedUInt32 (),
				ref attribute.fields,
				ref attribute.properties);

			return attribute;
		}

		public MarshalInfo ReadMarshalInfo ()
		{
			var native = this.ReadNativeType ();
			switch (native) {
			case NativeType.Array: {
				var array = new ArrayMarshalInfo ();
				if (this.CanReadMore ())
					array.element_type = this.ReadNativeType ();
				if (this.CanReadMore ())
					array.size_parameter_index = (int)this.ReadCompressedUInt32 ();
				if (this.CanReadMore ())
					array.size = (int)this.ReadCompressedUInt32 ();
				if (this.CanReadMore ())
					array.size_parameter_multiplier = (int)this.ReadCompressedUInt32 ();
				return array;
			}
			case NativeType.SafeArray: {
				var array = new SafeArrayMarshalInfo ();
				if (this.CanReadMore ())
					array.element_type = this.ReadVariantType ();
				return array;
			}
			case NativeType.FixedArray: {
				var array = new FixedArrayMarshalInfo ();
				if (this.CanReadMore ())
					array.size = (int)this.ReadCompressedUInt32 ();
				if (this.CanReadMore ())
					array.element_type = this.ReadNativeType ();
				return array;
			}
			case NativeType.FixedSysString: {
				var sys_string = new FixedSysStringMarshalInfo ();
				if (this.CanReadMore ())
					sys_string.size = (int)this.ReadCompressedUInt32 ();
				return sys_string;
			}
			case NativeType.CustomMarshaler: {
				var marshaler = new CustomMarshalInfo ();
				var guid_value = this.ReadUTF8String ();
				marshaler.guid = !string.IsNullOrEmpty (guid_value) ? new Guid (guid_value) : Guid.Empty;
				marshaler.unmanaged_type = this.ReadUTF8String ();
				marshaler.managed_type = this.ReadTypeReference ();
				marshaler.cookie = this.ReadUTF8String ();
				return marshaler;
			}
			default:
				return new MarshalInfo (native);
			}
		}

		NativeType ReadNativeType ()
		{
			return (NativeType)this.ReadByte ();
		}

		VariantType ReadVariantType ()
		{
			return (VariantType)this.ReadByte ();
		}

		string ReadUTF8String ()
		{
			if (this.buffer [this.position] == 0xff) {
                this.position++;
				return null;
			}

			var length = (int)this.ReadCompressedUInt32 ();
			if (length == 0)
				return string.Empty;

			var @string = Encoding.UTF8.GetString (this.buffer, this.position, this.buffer [this.position + length - 1] == 0 ? length - 1 : length);

            this.position += length;
			return @string;
		}

		public bool CanReadMore ()
		{
			return this.position - this.start < this.sig_length;
		}
	}
}
