//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Runtime.InteropServices;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;
using SR = System.Reflection;

/*Telerik Authorship*/

namespace Oleander.Assembly.Comparers.Cecil.Cil {

	[StructLayout (LayoutKind.Sequential)]
	public struct ImageDebugDirectory {
		public int Characteristics;
		public int TimeDateStamp;
		public short MajorVersion;
		public short MinorVersion;
		public int Type;
		public int SizeOfData;
		public int AddressOfRawData;
		public int PointerToRawData;
	}

	public sealed class Scope : IVariableDefinitionProvider {

		Instruction start;
		Instruction end;

		Collection<Scope> scopes;
		Collection<VariableDefinition> variables;

		public Instruction Start {
			get { return this.start; }
			set { this.start = value; }
		}

		public Instruction End {
			get { return this.end; }
			set { this.end = value; }
		}

		public bool HasScopes {
			get { return !this.scopes.IsNullOrEmpty (); }
		}

		public Collection<Scope> Scopes {
			get {
				if (this.scopes == null) this.scopes = new Collection<Scope> ();

				return this.scopes;
			}
		}

		public bool HasVariables {
			get { return !this.variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get {
				if (this.variables == null) this.variables = new Collection<VariableDefinition> ();

				return this.variables;
			}
		}
	}

	public struct InstructionSymbol {

		public readonly int Offset;
		public readonly SequencePoint SequencePoint;

		public InstructionSymbol (int offset, SequencePoint sequencePoint)
		{
			this.Offset = offset;
			this.SequencePoint = sequencePoint;
		}
	}

	public sealed class MethodSymbols {

        #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        internal int code_size;
        internal string method_name;
		internal MetadataToken method_token;
		internal MetadataToken local_var_token;
		internal Collection<VariableDefinition> variables;
		internal Collection<InstructionSymbol> instructions;
        #pragma warning restore CS0649 // Field is never assigned to, and will always have its default value


        public bool HasVariables {
			get { return !this.variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get {
				if (this.variables == null) this.variables = new Collection<VariableDefinition> ();

				return this.variables;
			}
		}

		public Collection<InstructionSymbol> Instructions {
			get {
				if (this.instructions == null) this.instructions = new Collection<InstructionSymbol> ();

				return this.instructions;
			}
		}

		public int CodeSize => this.code_size;

        public string MethodName => this.method_name;

        public MetadataToken MethodToken => this.method_token;

        public MetadataToken LocalVarToken => this.local_var_token;
        internal MethodSymbols (string methodName)
		{
			this.method_name = methodName;
		}

		public MethodSymbols (MetadataToken methodToken)
		{
			this.method_token = methodToken;
		}
	}

	public delegate Instruction InstructionMapper (int offset);

	public interface ISymbolReader : IDisposable {

		bool ProcessDebugHeader (ImageDebugDirectory directory, byte [] header);
		void Read (MethodBody body, InstructionMapper mapper);
		void Read (MethodSymbols symbols);
	}

	public interface ISymbolReaderProvider {

		ISymbolReader GetSymbolReader (ModuleDefinition module, string fileName);
		ISymbolReader GetSymbolReader (ModuleDefinition module, Stream symbolStream);
	}

	static class SymbolProvider {

		static readonly string symbol_kind = Type.GetType ("Mono.Runtime") != null ? "Mdb" : "Pdb";

		static SR.AssemblyName GetPlatformSymbolAssemblyName ()
		{
			var cecil_name = typeof (SymbolProvider).Assembly.GetName ();

			var name = new SR.AssemblyName {
				/*Telerik Authorship*/
				Name = "Telerik.JustDecompile.Mono.Cecil." + symbol_kind,
				Version = cecil_name.Version,
			};

			name.SetPublicKeyToken (cecil_name.GetPublicKeyToken ());

			return name;
		}

		static Type GetPlatformType (string fullname)
		{
			var type = Type.GetType (fullname);
			if (type != null)
				return type;

			var assembly_name = GetPlatformSymbolAssemblyName ();

			type = Type.GetType (fullname + ", " + assembly_name.FullName);
			if (type != null)
				return type;

			try {
				var assembly = SR.Assembly.Load (assembly_name);
				if (assembly != null)
					return assembly.GetType (fullname);
			} catch (FileNotFoundException) {
#if !CF
			} catch (FileLoadException) {
#endif
			}

			return null;
		}

		static ISymbolReaderProvider reader_provider;

		public static ISymbolReaderProvider GetPlatformReaderProvider ()
		{
			if (reader_provider != null)
				return reader_provider;

			var type = GetPlatformType (GetProviderTypeName ("ReaderProvider"));
			if (type == null)
				return null;

			return reader_provider = (ISymbolReaderProvider) Activator.CreateInstance (type);
		}

		static string GetProviderTypeName (string name)
		{
			return "Mono.Cecil." + symbol_kind + "." + symbol_kind + name;
		}
	}
}
