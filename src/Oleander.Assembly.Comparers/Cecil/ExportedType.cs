//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Mono.Cecil {

	public class ExportedType : IMetadataTokenProvider {

		string @namespace;
		string name;
		uint attributes;
		IMetadataScope scope;
		ModuleDefinition module;
		int identifier;
		ExportedType declaring_type;
		internal MetadataToken token;

		public string Namespace {
			get { return this.@namespace; }
			set { this.@namespace = value; }
		}

		public string Name {
			get { return this.name; }
			set { this.name = value; }
		}

		public TypeAttributes Attributes {
			get { return (TypeAttributes)this.attributes; }
			set { this.attributes = (uint) value; }
		}

		public IMetadataScope Scope {
			get {
				if (this.declaring_type != null)
					return this.declaring_type.Scope;

				return this.scope;
			}
		}

		public ExportedType DeclaringType {
			get { return this.declaring_type; }
			set { this.declaring_type = value; }
		}

		public MetadataToken MetadataToken {
			get { return this.token; }
			set { this.token = value; }
		}

		public int Identifier {
			get { return this.identifier; }
			set { this.identifier = value; }
		}

		#region TypeAttributes

		public bool IsNotPublic {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NotPublic); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NotPublic, value); }
		}

		public bool IsPublic {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.Public); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.Public, value); }
		}

		public bool IsNestedPublic {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPublic); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPublic, value); }
		}

		public bool IsNestedPrivate {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPrivate); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedPrivate, value); }
		}

		public bool IsNestedFamily {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamily); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamily, value); }
		}

		public bool IsNestedAssembly {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedAssembly); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedAssembly, value); }
		}

		public bool IsNestedFamilyAndAssembly {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamANDAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamANDAssem, value); }
		}

		public bool IsNestedFamilyOrAssembly {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamORAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.VisibilityMask, (uint) TypeAttributes.NestedFamORAssem, value); }
		}

		public bool IsAutoLayout {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.AutoLayout); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.AutoLayout, value); }
		}

		public bool IsSequentialLayout {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.SequentialLayout); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.SequentialLayout, value); }
		}

		public bool IsExplicitLayout {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.ExplicitLayout); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.LayoutMask, (uint) TypeAttributes.ExplicitLayout, value); }
		}

		public bool IsClass {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Class); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Class, value); }
		}

		public bool IsInterface {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Interface); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.ClassSemanticMask, (uint) TypeAttributes.Interface, value); }
		}

		public bool IsAbstract {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.Abstract); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.Abstract, value); }
		}

		public bool IsSealed {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.Sealed); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.Sealed, value); }
		}

		public bool IsSpecialName {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.SpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.SpecialName, value); }
		}

		public bool IsImport {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.Import); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.Import, value); }
		}

		public bool IsSerializable {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.Serializable); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.Serializable, value); }
		}

		public bool IsAnsiClass {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AnsiClass); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AnsiClass, value); }
		}

		public bool IsUnicodeClass {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.UnicodeClass); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.UnicodeClass, value); }
		}

		public bool IsAutoClass {
			get { return this.attributes.GetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AutoClass); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((uint) TypeAttributes.StringFormatMask, (uint) TypeAttributes.AutoClass, value); }
		}

		public bool IsBeforeFieldInit {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.BeforeFieldInit); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.BeforeFieldInit, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.RTSpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.RTSpecialName, value); }
		}

		public bool HasSecurity {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.HasSecurity); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.HasSecurity, value); }
		}

		#endregion

		public bool IsForwarder {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.Forwarder); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.Forwarder, value); }
		}

		public string FullName {
			get {
				var fullname = string.IsNullOrEmpty (this.@namespace)
					? this.name
					: this.@namespace + '.' + this.name;

				if (this.declaring_type != null)
					return this.declaring_type.FullName + "/" + fullname;

				return fullname;
			}
		}

		public ExportedType (string @namespace, string name, ModuleDefinition module, IMetadataScope scope)
		{
			this.@namespace = @namespace;
			this.name = name;
			this.scope = scope;
			this.module = module;
		}

		public override string ToString ()
		{
			return this.FullName;
		}

		public TypeDefinition Resolve ()
		{
			return this.module.Resolve (this.CreateReference ());
		}

		/*Telerik Authorship*/
		internal TypeDefinition Resolve(ICollection<string> visitedDlls)
		{
			return this.module.Resolve(this.CreateReference(), visitedDlls);
		}

		/*Telerik Authorship*/
		public TypeReference CreateReference ()
		{
			return new TypeReference (this.@namespace, this.name, this.module, this.scope) {
				DeclaringType = this.declaring_type != null ? this.declaring_type.CreateReference () : null,
			};
		}
	}
}
