//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Collections.Generic;
/*Telerik Authorship*/
using Mono.Cecil.Mono.Cecil;

namespace Mono.Cecil {

	public sealed class FieldDefinition : FieldReference, IMemberDefinition, IConstantProvider, IMarshalInfoProvider, IVisibilityDefinition/*Telerik Authorship*/ {

		ushort attributes;
		Collection<CustomAttribute> custom_attributes;

		int offset = Mixin.NotResolvedMarker;

		internal int rva = Mixin.NotResolvedMarker;
		byte [] initial_value;

		/*Telerik Authorship*/
		ConstantValue constant = Mixin.NotResolved;

		MarshalInfo marshal_info;

		void ResolveLayout ()
		{
			if (this.offset != Mixin.NotResolvedMarker)
				return;

			if (!this.HasImage) {
                this.offset = Mixin.NoDataMarker;
				return;
			}

            this.offset = this.Module.Read (this, (field, reader) => reader.ReadFieldLayout (field));
		}

		public bool HasLayoutInfo {
			get {
				if (this.offset >= 0)
					return true;

                this.ResolveLayout ();

				return this.offset >= 0;
			}
		}

		public int Offset {
			get {
				if (this.offset >= 0)
					return this.offset;

                this.ResolveLayout ();

				return this.offset >= 0 ? this.offset : -1;
			}
			set { this.offset = value; }
		}

		void ResolveRVA ()
		{
			if (this.rva != Mixin.NotResolvedMarker)
				return;

			if (!this.HasImage)
				return;

            this.rva = this.Module.Read (this, (field, reader) => reader.ReadFieldRVA (field));
		}

		public int RVA {
			get {
				if (this.rva > 0)
					return this.rva;

                this.ResolveRVA ();

				return this.rva > 0 ? this.rva : 0;
			}
		}

		public byte [] InitialValue {
			get {
				if (this.initial_value != null)
					return this.initial_value;

                this.ResolveRVA ();

				if (this.initial_value == null) this.initial_value = Empty<byte>.Array;

				return this.initial_value;
			}
			set {
                this.initial_value = value;
                this.rva = 0;
			}
		}

		public FieldAttributes Attributes {
			get { return (FieldAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public bool HasConstant {
			get {
				this.ResolveConstant (ref this.constant, this.Module);

				return this.constant != Mixin.NoValue;
			}
			set { if (!value) this.constant = Mixin.NoValue; }
		}

		/*Telerik Authorship*/
		public ConstantValue Constant {
			get { return this.HasConstant ? this.constant : null;	}
			set { this.constant = value; }
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
				return this.GetHasCustomAttributes (ref this.hasCustomAttributes, this.Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this.Module)); }
		}

		public bool HasMarshalInfo {
			get {
				if (this.marshal_info != null)
					return true;

				return this.GetHasMarshalInfo (this.Module);
			}
		}

		public MarshalInfo MarshalInfo {
			get { return this.marshal_info ?? (this.GetMarshalInfo (ref this.marshal_info, this.Module)); }
			set { this.marshal_info = value; }
		}

		#region FieldAttributes

		public bool IsCompilerControlled {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.CompilerControlled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.CompilerControlled, value); }
		}

		public bool IsPrivate {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Private); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Private, value); }
		}

		public bool IsFamilyAndAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamANDAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamANDAssem, value); }
		}

		public bool IsAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Assembly); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Assembly, value); }
		}

		public bool IsFamily {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Family); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Family, value); }
		}

		public bool IsFamilyOrAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamORAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.FamORAssem, value); }
		}

		public bool IsPublic {
			get { return this.attributes.GetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Public); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) FieldAttributes.FieldAccessMask, (ushort) FieldAttributes.Public, value); }
		}

		public bool IsStatic {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.Static); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.Static, value); }
		}

		public bool IsInitOnly {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.InitOnly); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.InitOnly, value); }
		}

		public bool IsLiteral {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.Literal); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.Literal, value); }
		}

		public bool IsNotSerialized {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.NotSerialized); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.NotSerialized, value); }
		}

		public bool IsSpecialName {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.SpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.SpecialName, value); }
		}

		public bool IsPInvokeImpl {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.PInvokeImpl); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.PInvokeImpl, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.RTSpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.RTSpecialName, value); }
		}

		public bool HasDefault {
			get { return this.attributes.GetAttributes ((ushort) FieldAttributes.HasDefault); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) FieldAttributes.HasDefault, value); }
		}

		#endregion

		public override bool IsDefinition {
			get { return true; }
		}

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public FieldDefinition (string name, FieldAttributes attributes, TypeReference fieldType)
			: base (name, fieldType)
		{
			this.attributes = (ushort) attributes;
		}

		public override FieldDefinition Resolve ()
		{
			return this;
		}

		/*Telerik Authorship*/
		public bool IsUnsafe 
		{
			get
			{
				return this.FieldType.IsPointer;
			}
		}
	}

	static partial class Mixin {

		public const int NotResolvedMarker = -2;
		public const int NoDataMarker = -1;
	}
}
