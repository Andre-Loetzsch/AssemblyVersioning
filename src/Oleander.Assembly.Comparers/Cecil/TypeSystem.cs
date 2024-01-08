//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public abstract class TypeSystem {

		sealed class CoreTypeSystem : TypeSystem {

			public CoreTypeSystem (ModuleDefinition module)
				: base (module)
			{
			}

			internal override TypeReference LookupType (string @namespace, string name)
			{
				var type = this.LookupTypeDefinition (@namespace, name) ?? this.LookupTypeForwarded (@namespace, name);
				if (type != null)
					return type;

				throw new NotSupportedException ();
			}

			TypeReference LookupTypeDefinition (string @namespace, string name)
			{
				var metadata = this.module.MetadataSystem;
				if (metadata.Types == null)
					Initialize (this.module.Types);

				return this.module.Read (new Row<string, string> (@namespace, name), (row, reader) => {
					var types = reader.metadata.Types;

					for (int i = 0; i < types.Length; i++) {
						if (types [i] == null)
							types [i] = reader.GetTypeDefinition ((uint) i + 1);

						var type = types [i];

						if (type.Name == row.Col2 && type.Namespace == row.Col1)
							return type;
					}

					return null;
				});
			}

			TypeReference LookupTypeForwarded (string @namespace, string name)
			{
				if (!this.module.HasExportedTypes)
					return null;

				var exported_types = this.module.ExportedTypes;
				for (int i = 0; i < exported_types.Count; i++) {
					var exported_type = exported_types [i];

					if (exported_type.Name == name && exported_type.Namespace == @namespace)
						return exported_type.CreateReference ();
				}

				return null;
			}

			static void Initialize (object obj)
			{
			}
		}

		sealed class CommonTypeSystem : TypeSystem {

			AssemblyNameReference corlib;

			public CommonTypeSystem (ModuleDefinition module)
				: base (module)
			{
			}

			internal override TypeReference LookupType (string @namespace, string name)
			{
				return this.CreateTypeReference (@namespace, name);
			}

			public AssemblyNameReference GetCorlibReference ()
			{
				if (this.corlib != null)
					return this.corlib;

				const string mscorlib = "mscorlib";

				var references = this.module.AssemblyReferences;

				for (int i = 0; i < references.Count; i++) {
					var reference = references [i];
					if (reference.Name == mscorlib)
						return this.corlib = reference;
				}


				/*Telerik Authorship*/
				/// Support for WinMD
				/// NOTE: At the time of this fix, there is still an open issue in the official version of mono, that it doesn't support 
				/// winMD completely. Link for more details on the bug follows:
				/// https://github.com/jbevain/cecil/issues/104
				/// It is very possible, that at a future update of mono, the issue is resolved and the following if is no longer required.
				for (int i = 0; i < references.Count; i++)
				{
					AssemblyNameReference reference = references[i];
					if (reference.Name == "System.Runtime")
					{
						return this.corlib = reference;
					}
				}

				/*Telerik Authorship*/
				/// This case happens when dealing with assemblies, that have no reference to mscorlib
                this.corlib = AssemblyNameReference.FakeCorlibReference;

				/*Telerik Authorship*/
				/// We don't want to polute the references list.
				//references.Add (corlib);

				return this.corlib;
			}

			Version GetCorlibVersion ()
			{
				switch (this.module.Runtime) {
				case TargetRuntime.Net_1_0:
				case TargetRuntime.Net_1_1:
					return new Version (1, 0, 0, 0);
				case TargetRuntime.Net_2_0:
					return new Version (2, 0, 0, 0);
				case TargetRuntime.Net_4_0:
					return new Version (4, 0, 0, 0);
				default:
					throw new NotSupportedException ();
				}
			}

			TypeReference CreateTypeReference (string @namespace, string name)
			{
				return new TypeReference (@namespace, name, this.module, this.GetCorlibReference ());
			}
		}

		readonly ModuleDefinition module;

		TypeReference type_object;
		TypeReference type_void;
		TypeReference type_bool;
		TypeReference type_char;
		TypeReference type_sbyte;
		TypeReference type_byte;
		TypeReference type_int16;
		TypeReference type_uint16;
		TypeReference type_int32;
		TypeReference type_uint32;
		TypeReference type_int64;
		TypeReference type_uint64;
		TypeReference type_single;
		TypeReference type_double;
		TypeReference type_intptr;
		TypeReference type_uintptr;
		TypeReference type_string;
		TypeReference type_typedref;

		TypeSystem (ModuleDefinition module)
		{
			this.module = module;
		}

		internal static TypeSystem CreateTypeSystem (ModuleDefinition module)
		{
			if (module.IsCorlib ())
				return new CoreTypeSystem (module);

			return new CommonTypeSystem (module);
		}

		internal abstract TypeReference LookupType (string @namespace, string name);

		TypeReference LookupSystemType (ref TypeReference reference, string name, ElementType element_type)
		{
			lock (this.module.SyncRoot) {
				if (reference != null)
					return reference;
				var type = this.LookupType ("System", name);
				type.etype = element_type;
				return reference = type;
			}
		}

		TypeReference LookupSystemValueType (ref TypeReference typeRef, string name, ElementType element_type)
		{
			lock (this.module.SyncRoot) {
				if (typeRef != null)
					return typeRef;
				var type = this.LookupType ("System", name);
				type.etype = element_type;
				type.IsValueType = true;
				return typeRef = type;
			}
		}

		public IMetadataScope Corlib {
			get {
				var common = this as CommonTypeSystem;
				if (common == null)
					return this.module;

				return common.GetCorlibReference ();
			}
		}

		public TypeReference Object {
			get { return this.type_object ?? (this.LookupSystemType (ref this.type_object, "Object", ElementType.Object)); }
		}

		public TypeReference Void {
			get { return this.type_void ?? (this.LookupSystemType (ref this.type_void, "Void", ElementType.Void)); }
		}

		public TypeReference Boolean {
			get { return this.type_bool ?? (this.LookupSystemValueType (ref this.type_bool, "Boolean", ElementType.Boolean)); }
		}

		public TypeReference Char {
			get { return this.type_char ?? (this.LookupSystemValueType (ref this.type_char, "Char", ElementType.Char)); }
		}

		public TypeReference SByte {
			get { return this.type_sbyte ?? (this.LookupSystemValueType (ref this.type_sbyte, "SByte", ElementType.I1)); }
		}

		public TypeReference Byte {
			get { return this.type_byte ?? (this.LookupSystemValueType (ref this.type_byte, "Byte", ElementType.U1)); }
		}

		public TypeReference Int16 {
			get { return this.type_int16 ?? (this.LookupSystemValueType (ref this.type_int16, "Int16", ElementType.I2)); }
		}

		public TypeReference UInt16 {
			get { return this.type_uint16 ?? (this.LookupSystemValueType (ref this.type_uint16, "UInt16", ElementType.U2)); }
		}

		public TypeReference Int32 {
			get { return this.type_int32 ?? (this.LookupSystemValueType (ref this.type_int32, "Int32", ElementType.I4)); }
		}

		public TypeReference UInt32 {
			get { return this.type_uint32 ?? (this.LookupSystemValueType (ref this.type_uint32, "UInt32", ElementType.U4)); }
		}

		public TypeReference Int64 {
			get { return this.type_int64 ?? (this.LookupSystemValueType (ref this.type_int64, "Int64", ElementType.I8)); }
		}

		public TypeReference UInt64 {
			get { return this.type_uint64 ?? (this.LookupSystemValueType (ref this.type_uint64, "UInt64", ElementType.U8)); }
		}

		public TypeReference Single {
			get { return this.type_single ?? (this.LookupSystemValueType (ref this.type_single, "Single", ElementType.R4)); }
		}

		public TypeReference Double {
			get { return this.type_double ?? (this.LookupSystemValueType (ref this.type_double, "Double", ElementType.R8)); }
		}

		public TypeReference IntPtr {
			get { return this.type_intptr ?? (this.LookupSystemValueType (ref this.type_intptr, "IntPtr", ElementType.I)); }
		}

		public TypeReference UIntPtr {
			get { return this.type_uintptr ?? (this.LookupSystemValueType (ref this.type_uintptr, "UIntPtr", ElementType.U)); }
		}

		public TypeReference String {
			get { return this.type_string ?? (this.LookupSystemType (ref this.type_string, "String", ElementType.String)); }
		}

		public TypeReference TypedReference {
			get { return this.type_typedref ?? (this.LookupSystemValueType (ref this.type_typedref, "TypedReference", ElementType.TypedByRef)); }
		}
	}
}
