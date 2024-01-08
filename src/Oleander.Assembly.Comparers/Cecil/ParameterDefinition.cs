//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

/*Telerik Authorship*/

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Oleander.Assembly.Comparers.Cecil {

	public sealed class ParameterDefinition : ParameterReference, ICustomAttributeProvider, IConstantProvider, IMarshalInfoProvider {

		ushort attributes;

		internal IMethodSignature method;

		/*Telerik Authorship*/
		ConstantValue constant = Mixin.NotResolved;
		Collection<CustomAttribute> custom_attributes;
		MarshalInfo marshal_info;

		public ParameterAttributes Attributes {
			get { return (ParameterAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public IMethodSignature Method {
			get { return this.method; }
		}

		public int Sequence {
			get {
				if (this.method == null)
					return -1;

				return this.method.HasImplicitThis () ? this.index + 1 : this.index;
			}
		}

		public bool HasConstant {
			get {
				this.ResolveConstant (ref this.constant, this.parameter_type.Module);

				return this.constant != Mixin.NoValue;
			}
			set { if (!value) this.constant = Mixin.NoValue; }
		}

		/*Telerik Authorship*/
		public ConstantValue Constant
		{
			get { return this.HasConstant ? this.constant : null;	}
			set { this.constant = value; }
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
				return this.GetHasCustomAttributes(ref this.hasCustomAttributes, this.parameter_type.Module);
			}
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.custom_attributes ?? (this.GetCustomAttributes (ref this.custom_attributes, this.parameter_type.Module)); }
		}

		public bool HasMarshalInfo {
			get {
				if (this.marshal_info != null)
					return true;

				return this.GetHasMarshalInfo (this.parameter_type.Module);
			}
		}

		public MarshalInfo MarshalInfo {
			get { return this.marshal_info ?? (this.GetMarshalInfo (ref this.marshal_info, this.parameter_type.Module)); }
			set { this.marshal_info = value; }
		}

		#region ParameterAttributes

		public bool IsIn {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.In); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.In, value); }
		}

		public bool IsOut {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.Out); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.Out, value); }
		}

		public bool IsLcid {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.Lcid); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.Lcid, value); }
		}

		public bool IsReturnValue {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.Retval); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.Retval, value); }
		}

		public bool IsOptional {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.Optional); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.Optional, value); }
		}

		public bool HasDefault {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.HasDefault); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.HasDefault, value); }
		}

		public bool HasFieldMarshal {
			get { return this.attributes.GetAttributes ((ushort) ParameterAttributes.HasFieldMarshal); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) ParameterAttributes.HasFieldMarshal, value); }
		}

		#endregion

		internal ParameterDefinition (TypeReference parameterType, IMethodSignature method)
			: this (string.Empty, ParameterAttributes.None, parameterType)
		{
			this.method = method;
		}

		public ParameterDefinition (TypeReference parameterType)
			: this (string.Empty, ParameterAttributes.None, parameterType)
		{
		}

		public ParameterDefinition (string name, ParameterAttributes attributes, TypeReference parameterType)
			: base (name, parameterType)
		{
			this.attributes = (ushort) attributes;
			this.token = new MetadataToken (TokenType.Param);
		}

		public override ParameterDefinition Resolve ()
		{
			return this;
		}
	}
}
