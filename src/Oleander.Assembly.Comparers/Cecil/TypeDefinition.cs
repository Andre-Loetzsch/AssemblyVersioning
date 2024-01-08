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

/*Telerik Authorship*/

using System.ComponentModel;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Oleander.Assembly.Comparers.Cecil {

	public sealed class TypeDefinition : TypeReference, IMemberDefinition, ISecurityDeclarationMemberDefinition/*Telerik Authorship*/, ISecurityDeclarationProvider, IGenericDefinition/*Telerik Authorship*/, INotifyPropertyChanged/*Telerik Authorship*/ {

		uint attributes;
		TypeReference base_type;
		internal Range fields_range;
		internal Range methods_range;

		short packing_size = Mixin.NotResolvedMarker;
		int class_size = Mixin.NotResolvedMarker;
		/*Telerik Authorship*/
		bool? isdefaultenum;
		/*Telerik Authorship*/
		bool? isStatic;

		Collection<TypeReference> interfaces;
		Collection<TypeDefinition> nested_types;
		Collection<MethodDefinition> methods;
		Collection<FieldDefinition> fields;
		Collection<EventDefinition> events;
		Collection<PropertyDefinition> properties;
		Collection<CustomAttribute> custom_attributes;
		Collection<SecurityDeclaration> security_declarations;

		/*Telerik Authorship*/
		public bool IsUnsafe
		{
			get
			{
				return false;
			}
		}

		public TypeAttributes Attributes {
			get { return (TypeAttributes)this.attributes; }
			set { this.attributes = (uint) value; }
		}

		public TypeReference BaseType {
			get { return this.base_type; }
			set { this.base_type = value; }
		}

		void ResolveLayout ()
		{
			if (this.packing_size != Mixin.NotResolvedMarker || this.class_size != Mixin.NotResolvedMarker)
				return;

			if (!this.HasImage) {
                this.packing_size = Mixin.NoDataMarker;
                this.class_size = Mixin.NoDataMarker;
				return;
			}

			var row = this.Module.Read (this, (type, reader) => reader.ReadTypeLayout (type));

            this.packing_size = row.Col1;
            this.class_size = row.Col2;
		}

		public bool HasLayoutInfo {
			get {
				if (this.packing_size >= 0 || this.class_size >= 0)
					return true;

                this.ResolveLayout ();

				return this.packing_size >= 0 || this.class_size >= 0;
			}
		}

		public short PackingSize {
			get {
				if (this.packing_size >= 0)
					return this.packing_size;

                this.ResolveLayout ();

				return this.packing_size >= 0 ? this.packing_size : (short) -1;
			}
			set { this.packing_size = value; }
		}

		public int ClassSize {
			get {
				if (this.class_size >= 0)
					return this.class_size;

                this.ResolveLayout ();

				return this.class_size >= 0 ? this.class_size : -1;
			}
			set { this.class_size = value; }
		}

		public bool HasInterfaces {
			get {
				if (this.interfaces != null)
					return this.interfaces.Count > 0;

				return this.HasImage && this.Module.Read (this, (type, reader) => reader.HasInterfaces (type));
			}
		}

		public Collection<TypeReference> Interfaces {
			get {
				if (this.interfaces != null)
					return this.interfaces;

				if (this.HasImage)
					return this.Module.Read (ref this.interfaces, this, (type, reader) => reader.ReadInterfaces (type));

				return this.interfaces = new Collection<TypeReference> ();
			}
		}

		public bool HasNestedTypes {
			get {
				if (this.nested_types != null)
					return this.nested_types.Count > 0;

				return this.HasImage && this.Module.Read (this, (type, reader) => reader.HasNestedTypes (type));
			}
		}

		public Collection<TypeDefinition> NestedTypes {
			get {
				if (this.nested_types != null)
					return this.nested_types;

				if (this.HasImage)
					return this.Module.Read (ref this.nested_types, this, (type, reader) => reader.ReadNestedTypes (type));

				return this.nested_types = new MemberDefinitionCollection<TypeDefinition> (this);
			}
		}

		public bool HasMethods {
			get {
				if (this.methods != null)
					return this.methods.Count > 0;

				return this.HasImage && this.methods_range.Length > 0;
			}
		}

		public Collection<MethodDefinition> Methods {
			get {
				if (this.methods != null)
					return this.methods;

				if (this.HasImage)
					return this.Module.Read (ref this.methods, this, (type, reader) => reader.ReadMethods (type));

				return this.methods = new MemberDefinitionCollection<MethodDefinition> (this);
			}
		}

		public bool HasFields {
			get {
				if (this.fields != null)
					return this.fields.Count > 0;

				return this.HasImage && this.fields_range.Length > 0;
			}
		}

		public Collection<FieldDefinition> Fields {
			get {
				if (this.fields != null)
					return this.fields;

				if (this.HasImage)
					return this.Module.Read (ref this.fields, this, (type, reader) => reader.ReadFields (type));

				return this.fields = new MemberDefinitionCollection<FieldDefinition> (this);
			}
		}

		public bool HasEvents {
			get {
				if (this.events != null)
					return this.events.Count > 0;

				return this.HasImage && this.Module.Read (this, (type, reader) => reader.HasEvents (type));
			}
		}

		public Collection<EventDefinition> Events {
			get {
				if (this.events != null)
					return this.events;

				if (this.HasImage)
					return this.Module.Read (ref this.events, this, (type, reader) => reader.ReadEvents (type));

				return this.events = new MemberDefinitionCollection<EventDefinition> (this);
			}
		}

		public bool HasProperties {
			get {
				if (this.properties != null)
					return this.properties.Count > 0;

				return this.HasImage && this.Module.Read (this, (type, reader) => reader.HasProperties (type));
			}
		}

		public Collection<PropertyDefinition> Properties {
			get {
				if (this.properties != null)
					return this.properties;

				if (this.HasImage)
					return this.Module.Read (ref this.properties, this, (type, reader) => reader.ReadProperties (type));

				return this.properties = new MemberDefinitionCollection<PropertyDefinition> (this);
			}
		}

		/*Telerik Authorship*/
		private bool? hasSecurityDeclarations;
		public bool HasSecurityDeclarations
		{
			get
			{
				if (this.security_declarations != null)
					return this.security_declarations.Count > 0;

				/*Telerik Authorship*/
				if (this.hasSecurityDeclarations != null)
					return this.hasSecurityDeclarations == true;

				/*Telerik Authorship*/
				return this.GetHasSecurityDeclarations(ref this.hasSecurityDeclarations, this.Module);
			}
		}

		public Collection<SecurityDeclaration> SecurityDeclarations {
			get { return this.security_declarations ?? (this.GetSecurityDeclarations (ref this.security_declarations, this.Module)); }
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

		/*Telerik Authorship*/
		private bool? hasGenericParameters;
		public override bool HasGenericParameters
		{
			get
			{
				if (this.generic_parameters != null)
					return this.generic_parameters.Count > 0;

				/*Telerik Authorship*/
				if (this.hasGenericParameters != null)
					return this.hasGenericParameters == true;

				/*Telerik Authorship*/
				return this.GetHasGenericParameters(ref this.hasGenericParameters, this.Module);
			}
		}

		public override Collection<GenericParameter> GenericParameters {
			get { return this.generic_parameters ?? (this.GetGenericParameters (ref this.generic_parameters, this.Module)); }
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

		public bool IsWindowsRuntime {
			get { return this.attributes.GetAttributes ((uint) TypeAttributes.WindowsRuntime); }
			set { this.attributes = this.attributes.SetAttributes ((uint) TypeAttributes.WindowsRuntime, value); }
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

		/*Telerik Authorship*/
		public bool IsStaticClass
		{
			get 
			{
				if (this.isStatic == null)
				{
					if (this.IsClass)
					{
						if (this.IsAbstract && this.IsSealed)
						{
							// Static class compiled with C# compiler
                            this.isStatic = true;
						}
						else if (this.CustomAttributes.Any(a => a.AttributeType.FullName == "Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute"))
						{
							// Static class compiled with VB.NET compiler aka Module
                            this.isStatic = true;
						}

						// Also, static classes does not have instance constructor (.ctor). This can be added if needed.
					}

					if (this.isStatic == null)
					{
                        this.isStatic = false;
					}
				}

				return this.isStatic.Value;
			}
		}
		#endregion

		public bool IsEnum {
			get { return this.base_type != null && this.base_type.IsTypeOf ("System", "Enum"); }
		}

		/*Telerik Authorship*/
		public bool IsDefaultEnum
		{
			get 
			{
				if (this.IsEnum == false)
				{
					return false;
				}
				if (this.isdefaultenum == null)
				{
                    this.isdefaultenum = this.IsDefaultEnumType && this.IsDefaultEnumConstants;
				}
				return this.isdefaultenum ?? false;
			}
		}

		/*Telerik Authorship*/
		public bool IsDefaultEnumType
		{
			get 
			{
				if (this.IsEnum == false)
				{
					return false;
				}
				return this.CheckDefaultEnumValueType();
			}
		}

		/*Telerik Authorship*/
		public bool IsDefaultEnumConstants
		{
			get 
			{
				if (this.IsEnum == false)
				{
					return false;
				}
				return this.CheckDefaultEnumConstants();
			}
		}

		/*Telerik Authorship*/
		private bool CheckDefaultEnumConstants()
		{
			for (int i = 1; i < this.Fields.Count; i++)
			{
				switch (this.Fields[0].FieldType.Name)
				{
					case "Byte":
						if ((byte)this.Fields[i].Constant.Value != (byte)(i - 1))
						{
							return false;
						}
						break;
					case "Int16":
						if ((short)this.Fields[i].Constant.Value != (short)(i - 1))
						{
							return false;
						}
						break;
					case "Int32":
						if ((int)this.Fields[i].Constant.Value != (i - 1))
						{
							return false;
						}
						break;
					case "Int64":
						if ((long)this.Fields[i].Constant.Value != (long)(i - 1))
						{
							return false;
						}
						break;
					case "SByte":
						if ((sbyte)this.Fields[i].Constant.Value != (sbyte)(i - 1))
						{
							return false;
						}
						break;
					case "UInt16":
						if ((ushort)this.Fields[i].Constant.Value != (ushort)(i - 1))
						{
							return false;
						}
						break;
					case "UInt32":
						if ((uint)this.Fields[i].Constant.Value != (uint)(i - 1))
						{
							return false;
						}
						break;
					case "UInt64":
						if ((ulong)this.Fields[i].Constant.Value != (ulong)(i - 1))
						{
							return false;
						}
						break;
					default:
						return false;
				}
			}
			return true;
		}

		/*Telerik Authorship*/
		private bool CheckDefaultEnumValueType()
		{
			if (this.Fields[0].FieldType.Name != "Int32")
			{
				return false;
			}
			return true;
		}

		public override bool IsValueType {
			get {
				if (this.base_type == null)
					return false;

				return this.base_type.IsTypeOf ("System", "Enum") || (this.base_type.IsTypeOf ("System", "ValueType") && !this.IsTypeOf ("System", "Enum"));
			}
		}

		public override bool IsPrimitive {
			get {
				ElementType primitive_etype;
				return MetadataSystem.TryGetPrimitiveElementType (this, out primitive_etype);
			}
		}

		public override MetadataType MetadataType {
			get {
				ElementType primitive_etype;
				if (MetadataSystem.TryGetPrimitiveElementType (this, out primitive_etype))
					return (MetadataType) primitive_etype;

				return base.MetadataType;
			}
		}

		public override bool IsDefinition {
			get { return true; }
		}

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public TypeDefinition (string @namespace, string name, TypeAttributes attributes)
			: base (@namespace, name)
		{
			this.attributes = (uint) attributes;
			this.token = new MetadataToken (TokenType.TypeDef);
		}

		public TypeDefinition (string @namespace, string name, TypeAttributes attributes, TypeReference baseType) :
			this (@namespace, name, attributes)
		{
			this.BaseType = baseType;
		}

		public override TypeDefinition Resolve ()
		{
			return this;
		}

		/*Telerik Authorship*/
		public event PropertyChangedEventHandler PropertyChanged = delegate { };
	}

	/*Telerik Authorship*/
	public static class TypeDefinitionMixin {

		public static TypeReference GetEnumUnderlyingType (this TypeDefinition self)
		{
			var fields = self.Fields;

			for (int i = 0; i < fields.Count; i++) {
				var field = fields [i];
				if (!field.IsStatic)
					return field.FieldType;
			}

			throw new ArgumentException ();
		}

		public static TypeDefinition GetNestedType (this TypeDefinition self, string fullname)
		{
			if (!self.HasNestedTypes)
				return null;

			var nested_types = self.NestedTypes;

			for (int i = 0; i < nested_types.Count; i++) {
				var nested_type = nested_types [i];

				if (nested_type.TypeFullName () == fullname)
					return nested_type;
			}

			return null;
		}
	}
}
