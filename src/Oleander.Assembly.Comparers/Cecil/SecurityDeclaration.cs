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

namespace Oleander.Assembly.Comparers.Cecil {

	public enum SecurityAction : ushort {
		Request = 1,
		Demand = 2,
		Assert = 3,
		Deny = 4,
		PermitOnly = 5,
		LinkDemand = 6,
		InheritDemand = 7,
		RequestMinimum = 8,
		RequestOptional = 9,
		RequestRefuse = 10,
		PreJitGrant = 11,
		PreJitDeny = 12,
		NonCasDemand = 13,
		NonCasLinkDemand = 14,
		NonCasInheritance = 15
	}

	public interface ISecurityDeclarationProvider : IMetadataTokenProvider {

		bool HasSecurityDeclarations { get; }
		Collection<SecurityDeclaration> SecurityDeclarations { get; }
	}

	public sealed class SecurityAttribute : ICustomAttribute {

		TypeReference attribute_type;

		internal Collection<CustomAttributeNamedArgument> fields;
		internal Collection<CustomAttributeNamedArgument> properties;

		public TypeReference AttributeType {
			get { return this.attribute_type; }
			set { this.attribute_type = value; }
		}

		public bool HasFields {
			get { return !this.fields.IsNullOrEmpty (); }
		}

		public Collection<CustomAttributeNamedArgument> Fields {
			get { return this.fields ?? (this.fields = new Collection<CustomAttributeNamedArgument> ()); }
		}

		public bool HasProperties {
			get { return !this.properties.IsNullOrEmpty (); }
		}

		public Collection<CustomAttributeNamedArgument> Properties {
			get { return this.properties ?? (this.properties = new Collection<CustomAttributeNamedArgument> ()); }
		}

		public SecurityAttribute (TypeReference attributeType)
		{
			this.attribute_type = attributeType;
		}
	}

	public sealed class SecurityDeclaration {

		readonly internal uint signature;
		byte [] blob;
		readonly ModuleDefinition module;

		internal bool resolved;
		SecurityAction action;
		internal Collection<SecurityAttribute> security_attributes;

		public SecurityAction Action {
			get { return this.action; }
			set { this.action = value; }
		}

		public bool HasSecurityAttributes {
			get {
                this.Resolve ();

				return !this.security_attributes.IsNullOrEmpty ();
			}
		}

		public Collection<SecurityAttribute> SecurityAttributes {
			get {
                this.Resolve ();

				return this.security_attributes ?? (this.security_attributes = new Collection<SecurityAttribute> ());
			}
		}

		internal bool HasImage {
			get { return this.module != null && this.module.HasImage; }
		}

		internal SecurityDeclaration (SecurityAction action, uint signature, ModuleDefinition module)
		{
			this.action = action;
			this.signature = signature;
			this.module = module;
		}

		public SecurityDeclaration (SecurityAction action)
		{
			this.action = action;
			this.resolved = true;
		}

		public SecurityDeclaration (SecurityAction action, byte [] blob)
		{
			this.action = action;
			this.resolved = false;
			this.blob = blob;
		}

		public byte [] GetBlob ()
		{
			if (this.blob != null)
				return this.blob;

			if (!this.HasImage || this.signature == 0)
				throw new NotSupportedException ();

			return this.blob = this.module.Read (this, (declaration, reader) => reader.ReadSecurityDeclarationBlob (declaration.signature));
		}

		void Resolve ()
		{
			if (this.resolved || !this.HasImage)
				return;

            this.module.Read (this, (declaration, reader) => {
				reader.ReadSecurityDeclarationSignature (declaration);
				return this;
			});

            this.resolved = true;
		}
	}

	static partial class Mixin {

		public static bool GetHasSecurityDeclarations (
			this ISecurityDeclarationProvider self,
			/*Telerik Authorship*/ ref bool? variable,
			ModuleDefinition module)
		{
			/*Telerik Authorship*/
			return module.HasImage () && module.Read (ref variable, self, (provider, reader) => reader.HasSecurityDeclarations (provider)) == true;
		}

		public static Collection<SecurityDeclaration> GetSecurityDeclarations (
			this ISecurityDeclarationProvider self,
			ref Collection<SecurityDeclaration> variable,
			ModuleDefinition module)
		{
			return module.HasImage ()
				? module.Read (ref variable, self, (provider, reader) => reader.ReadSecurityDeclarations (provider))
				: variable = new Collection<SecurityDeclaration>();
		}
	}
}
