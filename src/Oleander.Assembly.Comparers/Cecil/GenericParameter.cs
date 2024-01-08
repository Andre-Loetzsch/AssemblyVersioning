//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Oleander.Assembly.Comparers.Cecil {

	public sealed class GenericParameter : TypeReference, ICustomAttributeProvider {

		internal int position;
		internal GenericParameterType type;
		internal IGenericParameterProvider owner;

		ushort attributes;
		Collection<TypeReference> constraints;
		Collection<CustomAttribute> custom_attributes;

		public GenericParameterAttributes Attributes {
			get { return (GenericParameterAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public int Position {
			get { return this.position; }
		}

		public GenericParameterType Type {
			get { return this.type; }
		}

		public IGenericParameterProvider Owner {
			get { return this.owner; }
		}

		public bool HasConstraints {
			get {
				if (this.constraints != null)
					return this.constraints.Count > 0;

				return this.HasImage && this.Module.Read (this, (generic_parameter, reader) => reader.HasGenericConstraints (generic_parameter));
			}
		}

		public Collection<TypeReference> Constraints {
			get {
				if (this.constraints != null)
					return this.constraints;

				if (this.HasImage)
					return this.Module.Read (ref this.constraints, this, (generic_parameter, reader) => reader.ReadGenericConstraints (generic_parameter));

				return this.constraints = new Collection<TypeReference> ();
			}
		}

		/*Telerik Authorship*/
		private bool? hasCustomAttributes;
		public bool HasCustomAttributes
		{
			get
			{
				if (this.custom_attributes != null)
					return this.custom_attributes.Count > 0;

				/*Telerik Authorship*/
				if (this.hasCustomAttributes != null)
					return this.hasCustomAttributes == true;

				/*Telerik Authorship*/
				return this.GetHasCustomAttributes(ref this.hasCustomAttributes, this.Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this.Module)); }
		}

		public override IMetadataScope Scope {
			get {
				if (this.owner == null)
					return null;

				return this.owner.GenericParameterType == GenericParameterType.Method
					? ((MethodReference)this.owner).DeclaringType.Scope
					: ((TypeReference)this.owner).Scope;
			}
			set { throw new InvalidOperationException (); }
		}

		public override TypeReference DeclaringType {
			get { return this.owner as TypeReference; }
			set { throw new InvalidOperationException (); }
		}

		public MethodReference DeclaringMethod {
			get { return this.owner as MethodReference; }
		}

		public override ModuleDefinition Module {
			get { return this.module ?? this.owner.Module; }
		}

		public override string Name {
			get {
				if (!string.IsNullOrEmpty (base.Name))
					return base.Name;

				return base.Name = (this.type == GenericParameterType.Method ? "!!" : "!") + this.position;
			}
		}

		public override string Namespace {
			get { return string.Empty; }
			set { throw new InvalidOperationException (); }
		}

		public override string FullName {
			get { return this.Name; }
		}

		public override bool IsGenericParameter {
			get { return true; }
		}

		public override bool ContainsGenericParameter {
			get { return true; }
		}

		public override MetadataType MetadataType {
			get { return (MetadataType)this.etype; }
		}

		#region GenericParameterAttributes

		public bool IsNonVariant {
			get { return this.attributes.GetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.NonVariant); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.NonVariant, value); }
		}

		public bool IsCovariant {
			get { return this.attributes.GetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.Covariant); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.Covariant, value); }
		}

		public bool IsContravariant {
			get { return this.attributes.GetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.Contravariant); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) GenericParameterAttributes.VarianceMask, (ushort) GenericParameterAttributes.Contravariant, value); }
		}

		public bool HasReferenceTypeConstraint {
			get { return this.attributes.GetAttributes ((ushort) GenericParameterAttributes.ReferenceTypeConstraint); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) GenericParameterAttributes.ReferenceTypeConstraint, value); }
		}

		public bool HasNotNullableValueTypeConstraint {
			get { return this.attributes.GetAttributes ((ushort) GenericParameterAttributes.NotNullableValueTypeConstraint); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) GenericParameterAttributes.NotNullableValueTypeConstraint, value); }
		}

		public bool HasDefaultConstructorConstraint {
			get { return this.attributes.GetAttributes ((ushort) GenericParameterAttributes.DefaultConstructorConstraint); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) GenericParameterAttributes.DefaultConstructorConstraint, value); }
		}

		#endregion

		public GenericParameter (IGenericParameterProvider owner)
			: this (string.Empty, owner)
		{
		}

		public GenericParameter (string name, IGenericParameterProvider owner)
			: base (string.Empty, name)
		{
			if (owner == null)
				throw new ArgumentNullException ();

			this.position = -1;
			this.owner = owner;
			this.type = owner.GenericParameterType;
			this.etype = ConvertGenericParameterType (this.type);
			this.token = new MetadataToken (TokenType.GenericParam);

		}

		internal GenericParameter (int position, GenericParameterType type, ModuleDefinition module)
			: base (string.Empty, string.Empty)
		{
			if (module == null)
				throw new ArgumentNullException ();

			this.position = position;
			this.type = type;
			this.etype = ConvertGenericParameterType (type);
			this.module = module;
			this.token = new MetadataToken (TokenType.GenericParam);
		}

		static ElementType ConvertGenericParameterType (GenericParameterType type)
		{
			switch (type) {
			case GenericParameterType.Type:
				return ElementType.Var;
			case GenericParameterType.Method:
				return ElementType.MVar;
			}

			throw new ArgumentOutOfRangeException ();
		}

		public override TypeDefinition Resolve ()
		{
			return null;
		}
	}

	sealed class GenericParameterCollection : Collection<GenericParameter> {

		readonly IGenericParameterProvider owner;

		internal GenericParameterCollection (IGenericParameterProvider owner)
		{
			this.owner = owner;
		}

		internal GenericParameterCollection (IGenericParameterProvider owner, int capacity)
			: base (capacity)
		{
			this.owner = owner;
		}

		protected override void OnAdd (GenericParameter item, int index)
		{
            this.UpdateGenericParameter (item, index);
		}

		protected override void OnInsert (GenericParameter item, int index)
		{
            this.UpdateGenericParameter (item, index);

			for (int i = index; i < this.size; i++) this.items[i].position = i + 1;
		}

		protected override void OnSet (GenericParameter item, int index)
		{
            this.UpdateGenericParameter (item, index);
		}

		void UpdateGenericParameter (GenericParameter item, int index)
		{
			item.owner = this.owner;
			item.position = index;
			item.type = this.owner.GenericParameterType;
		}

		protected override void OnRemove (GenericParameter item, int index)
		{
			item.owner = null;
			item.position = -1;
			item.type = GenericParameterType.Type;

			for (int i = index + 1; i < this.size; i++) this.items[i].position = i - 1;
		}
	}
}
