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
using Mono.Collections.Generic;

namespace Mono.Cecil {

	public enum MetadataType : byte {
		Void = ElementType.Void,
		Boolean = ElementType.Boolean,
		Char = ElementType.Char,
		SByte = ElementType.I1,
		Byte = ElementType.U1,
		Int16 = ElementType.I2,
		UInt16 = ElementType.U2,
		Int32 = ElementType.I4,
		UInt32 = ElementType.U4,
		Int64 = ElementType.I8,
		UInt64 = ElementType.U8,
		Single = ElementType.R4,
		Double = ElementType.R8,
		String = ElementType.String,
		Pointer = ElementType.Ptr,
		ByReference = ElementType.ByRef,
		ValueType = ElementType.ValueType,
		Class = ElementType.Class,
		Var = ElementType.Var,
		Array = ElementType.Array,
		GenericInstance = ElementType.GenericInst,
		TypedByReference = ElementType.TypedByRef,
		IntPtr = ElementType.I,
		UIntPtr = ElementType.U,
		FunctionPointer = ElementType.FnPtr,
		Object = ElementType.Object,
		MVar = ElementType.MVar,
		RequiredModifier = ElementType.CModReqD,
		OptionalModifier = ElementType.CModOpt,
		Sentinel = ElementType.Sentinel,
		Pinned = ElementType.Pinned,
	}

	public class TypeReference : MemberReference, IGenericParameterProvider, IGenericContext {

		string @namespace;
		bool value_type;
		internal IMetadataScope scope;
		internal ModuleDefinition module;

		internal ElementType etype = ElementType.None;

		string fullname;

		protected Collection<GenericParameter> generic_parameters;

		public override string Name {
			get { return base.Name; }
			set {
				base.Name = value;
                this.fullname = null;
			}
		}

		/*Telerik Authorship*/
		public ElementType EType
		{
			get
			{
				return this.etype;
			}
		}

		public virtual string Namespace {
			get { return this.@namespace; }
			set {
                this.@namespace = value;
                this.fullname = null;
			}
		}

		public virtual bool IsValueType {
			get { return this.value_type; }
			set { this.value_type = value; }
		}

		public override ModuleDefinition Module {
			get {
				if (this.module != null)
					return this.module;

				var declaring_type = this.DeclaringType;
				if (declaring_type != null)
					return declaring_type.Module;

				return null;
			}
		}

		IGenericParameterProvider IGenericContext.Type {
			get { return this; }
		}

		IGenericParameterProvider IGenericContext.Method {
			get { return null; }
		}

		GenericParameterType IGenericParameterProvider.GenericParameterType {
			get { return GenericParameterType.Type; }
		}

		public virtual bool HasGenericParameters {
			get { return !this.generic_parameters.IsNullOrEmpty (); }
		}

		public virtual Collection<GenericParameter> GenericParameters {
			get {
				if (this.generic_parameters != null)
					return this.generic_parameters;

				return this.generic_parameters = new GenericParameterCollection (this);
			}
		}

		public virtual IMetadataScope Scope {
			get {
				var declaring_type = this.DeclaringType;
				if (declaring_type != null)
					return declaring_type.Scope;

				return this.scope;
			}
			set {
				/*Telerik Authorship*/
				lock (this.Module.SyncRoot)
				{
					var declaring_type = this.DeclaringType;
					if (declaring_type != null)
					{
						declaring_type.Scope = value;
						return;
					}

                    this.scope = value;
				}
			}
		}

		public bool IsNested {
			get { return this.DeclaringType != null; }
		}

		public override TypeReference DeclaringType {
			get { return base.DeclaringType; }
			set {
				base.DeclaringType = value;
                this.fullname = null;
			}
		}

		public override string FullName {
			get {
				if (this.fullname != null)
					return this.fullname;

                this.fullname = this.TypeFullName ();

				if (this.IsNested) this.fullname = this.DeclaringType.FullName + "/" + this.fullname;

				return this.fullname;
			}
		}

		public virtual bool IsByReference {
			get { return false; }
		}

		public virtual bool IsPointer {
			get { return false; }
		}

		public virtual bool IsSentinel {
			get { return false; }
		}

		public virtual bool IsArray {
			get { return false; }
		}

		public virtual bool IsGenericParameter {
			get { return false; }
		}

		public virtual bool IsGenericInstance {
			get { return false; }
		}

		public virtual bool IsRequiredModifier {
			get { return false; }
		}

		public virtual bool IsOptionalModifier {
			get { return false; }
		}

		public virtual bool IsPinned {
			get { return false; }
		}

		public virtual bool IsFunctionPointer {
			get { return false; }
		}

		public virtual bool IsPrimitive {
			get { return this.etype.IsPrimitive (); }
		}

		public virtual MetadataType MetadataType {
			get {
				switch (this.etype) {
				case ElementType.None:
					return this.IsValueType ? MetadataType.ValueType : MetadataType.Class;
				default:
					return (MetadataType)this.etype;
				}
			}
		}

		protected TypeReference (string @namespace, string name)
			: base (name)
		{
			this.@namespace = @namespace ?? string.Empty;
			this.token = new MetadataToken (TokenType.TypeRef, 0);
			/*Telerik Authorship*/
            this.TrySetElementType(@namespace, name);
		}
		
		/*Telerik Authorship*/
		private bool TrySetElementType(string @namespace, string name)
		{
			if (@namespace != "System")
			{
				return false;
			}

			bool success = true;

			switch (name)
			{
				case "Void":
                    this.etype = ElementType.Void;
					break;
				case "Object":
                    this.etype = ElementType.Object;
					break;
				case "Boolean":
                    this.etype = ElementType.Boolean;
					break;
				case "Char":
                    this.etype = ElementType.Char;
					break;
				case "IntPtr":
                    this.etype = ElementType.I;
					break;
				case "UIntPtr":
                    this.etype = ElementType.U;
					break;
				case "SByte":
                    this.etype = ElementType.I1;
					break;
				case "Byte":
                    this.etype = ElementType.U1;
					break;
				case "Int16":
                    this.etype = ElementType.I2;
					break;
				case "UInt16":
                    this.etype = ElementType.U2;
					break;
				case "Int32":
                    this.etype = ElementType.I4;
					break;
				case "UInt32":
                    this.etype = ElementType.U4;
					break;
				case "Int64":
                    this.etype = ElementType.I8;
					break;
				case "UInt64":
                    this.etype = ElementType.U8;
					break;
				case "Single":
                    this.etype = ElementType.R4;
					break;
				case "Double":
                    this.etype = ElementType.R8;
					break;
				case "String":
                    this.etype = ElementType.String;
					break;
				case "TypedReference":
                    this.etype = ElementType.TypedByRef;
					break;
				default:
					success = false;
					break;
			}

			return success;
		}

		public TypeReference (string @namespace, string name, ModuleDefinition module, IMetadataScope scope)
			: this (@namespace, name)
		{
			this.module = module;
			this.scope = scope;
		}

		public TypeReference (string @namespace, string name, ModuleDefinition module, IMetadataScope scope, bool valueType) :
			this (@namespace, name, module, scope)
		{
            this.value_type = valueType;
		}

		public virtual TypeReference GetElementType ()
		{
			return this;
		}

		public virtual TypeDefinition Resolve ()
		{
			var module = this.Module;
			if (module == null)
				/*Telerik Authorship*/
				//throw new NotSupportedException ();
				return null;

			return module.Resolve (this);
		}
	}

	/*Telerik Authorship*/
	public static class TypeReferenceMixin {

		public static bool IsPrimitive (this ElementType self)
		{
			switch (self) {
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I:
			case ElementType.U:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
				return true;
			default:
				return false;
			}
		}

		public static string TypeFullName (this TypeReference self)
		{
			return string.IsNullOrEmpty (self.Namespace)
				? self.Name
				: self.Namespace + '.' + self.Name;
		}

		public static bool IsTypeOf (this TypeReference self, string @namespace, string name)
		{
			return self.Name == name
				&& self.Namespace == @namespace;
		}

		public static bool IsTypeSpecification (this TypeReference type)
		{
			switch (type.etype) {
			case ElementType.Array:
			case ElementType.ByRef:
			case ElementType.CModOpt:
			case ElementType.CModReqD:
			case ElementType.FnPtr:
			case ElementType.GenericInst:
			case ElementType.MVar:
			case ElementType.Pinned:
			case ElementType.Ptr:
			case ElementType.SzArray:
			case ElementType.Sentinel:
			case ElementType.Var:
				return true;
			}

			return false;
		}

		public static TypeDefinition CheckedResolve (this TypeReference self)
		{
			var type = self.Resolve ();
			if (type == null)
				throw new ResolutionException (self);

			return type;
		}
	}
}
