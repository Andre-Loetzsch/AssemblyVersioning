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
/*Telerik Authorship*/
using Mono.Cecil.Mono.Cecil;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Mono.Cecil {

	public sealed class PropertyDefinition : PropertyReference, IMemberDefinition, IConstantProvider {

		bool? has_this;
		ushort attributes;

		Collection<CustomAttribute> custom_attributes;

		internal MethodDefinition get_method;
		internal MethodDefinition set_method;
		internal Collection<MethodDefinition> other_methods;

		/*Telerik Authorship*/
		ConstantValue constant = Mixin.NotResolved;

		/*Telerik Authorship*/
		public bool IsUnsafe
		{
			get
			{
				return this.PropertyType.IsPointer;
			}
		}

		public PropertyAttributes Attributes {
			get { return (PropertyAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		public bool HasThis {
			get {
				if (this.has_this.HasValue)
					return this.has_this.Value;

				if (this.GetMethod != null)
					return this.get_method.HasThis;

				if (this.SetMethod != null)
					return this.set_method.HasThis;

				return false;
			}
			set { this.has_this = value; }
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

		public MethodDefinition GetMethod {
			get {
				if (this.get_method != null)
					return this.get_method;

                this.InitializeMethods ();
				return this.get_method;
			}
			set { this.get_method = value; }
		}

		public MethodDefinition SetMethod {
			get {
				if (this.set_method != null)
					return this.set_method;

                this.InitializeMethods ();
				return this.set_method;
			}
			set { this.set_method = value; }
		}

		public bool HasOtherMethods {
			get {
				if (this.other_methods != null)
					return this.other_methods.Count > 0;

                this.InitializeMethods ();
				return !this.other_methods.IsNullOrEmpty ();
			}
		}

		public Collection<MethodDefinition> OtherMethods {
			get {
				if (this.other_methods != null)
					return this.other_methods;

                this.InitializeMethods ();

				if (this.other_methods != null)
					return this.other_methods;

				return this.other_methods = new Collection<MethodDefinition> ();
			}
		}

		public bool HasParameters {
			get {
                this.InitializeMethods ();

				if (this.get_method != null)
					return this.get_method.HasParameters;

				if (this.set_method != null)
					return this.set_method.HasParameters && this.set_method.Parameters.Count > 1;

				return false;
			}
		}

		public override Collection<ParameterDefinition> Parameters {
			get {
                this.InitializeMethods ();

				if (this.get_method != null)
					return MirrorParameters (this.get_method, 0);

				if (this.set_method != null)
					return MirrorParameters (this.set_method, 1);

				return new Collection<ParameterDefinition> ();
			}
		}

		static Collection<ParameterDefinition> MirrorParameters (MethodDefinition method, int bound)
		{
			var parameters = new Collection<ParameterDefinition> ();
			if (!method.HasParameters)
				return parameters;

			var original_parameters = method.Parameters;
			var end = original_parameters.Count - bound;

			for (int i = 0; i < end; i++)
				parameters.Add (original_parameters [i]);

			return parameters;
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

		#region PropertyAttributes

		public bool IsSpecialName {
			get { return this.attributes.GetAttributes ((ushort) PropertyAttributes.SpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) PropertyAttributes.SpecialName, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return this.attributes.GetAttributes ((ushort) PropertyAttributes.RTSpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) PropertyAttributes.RTSpecialName, value); }
		}

		public bool HasDefault {
			get { return this.attributes.GetAttributes ((ushort) PropertyAttributes.HasDefault); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) PropertyAttributes.HasDefault, value); }
		}

		#endregion

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public override string FullName {
			get {
				var builder = new StringBuilder ();
				builder.Append (this.PropertyType.ToString ());
				builder.Append (' ');
				builder.Append (this.MemberFullName ());
				builder.Append ('(');
				if (this.HasParameters) {
					var parameters = this.Parameters;
					for (int i = 0; i < parameters.Count; i++) {
						if (i > 0)
							builder.Append (',');
						builder.Append (parameters [i].ParameterType.FullName);
					}
				}
				builder.Append (')');
				return builder.ToString ();
			}
		}

		public PropertyDefinition (string name, PropertyAttributes attributes, TypeReference propertyType)
			: base (name, propertyType)
		{
			this.attributes = (ushort) attributes;
			this.token = new MetadataToken (TokenType.Property);
		}

		void InitializeMethods ()
		{
			var module = this.Module;
			if (module == null)
				return;

			lock (module.SyncRoot) {
				if (this.get_method != null || this.set_method != null)
					return;

				if (!module.HasImage ())
					return;

				module.Read (this, (property, reader) => reader.ReadMethods (property));
			}
		}

		public override PropertyDefinition Resolve ()
		{
			return this;
		}
	}
}
