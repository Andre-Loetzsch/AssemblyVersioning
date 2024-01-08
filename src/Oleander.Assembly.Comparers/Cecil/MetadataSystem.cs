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

	struct Range {
		public uint Start;
		public uint Length;

		public Range (uint index, uint length)
		{
			this.Start = index;
			this.Length = length;
		}
	}

	sealed class MetadataSystem {

		internal AssemblyNameReference [] AssemblyReferences;
		internal ModuleReference [] ModuleReferences;

		internal TypeDefinition [] Types;
		internal TypeReference [] TypeReferences;

		internal FieldDefinition [] Fields;
		internal MethodDefinition [] Methods;
		internal MemberReference [] MemberReferences;

		internal Dictionary<uint, uint []> NestedTypes;
		internal Dictionary<uint, uint> ReverseNestedTypes;
		internal Dictionary<uint, MetadataToken []> Interfaces;
		internal Dictionary<uint, Row<ushort, uint>> ClassLayouts;
		internal Dictionary<uint, uint> FieldLayouts;
		internal Dictionary<uint, uint> FieldRVAs;
		internal Dictionary<MetadataToken, uint> FieldMarshals;
		internal Dictionary<MetadataToken, Row<ElementType, uint>> Constants;
		internal Dictionary<uint, MetadataToken []> Overrides;
		internal Dictionary<MetadataToken, Range []> CustomAttributes;
		internal Dictionary<MetadataToken, Range []> SecurityDeclarations;
		internal Dictionary<uint, Range> Events;
		internal Dictionary<uint, Range> Properties;
		internal Dictionary<uint, Row<MethodSemanticsAttributes, MetadataToken>> Semantics;
		internal Dictionary<uint, Row<PInvokeAttributes, uint, uint>> PInvokes;
		internal Dictionary<MetadataToken, Range []> GenericParameters;
		internal Dictionary<uint, MetadataToken []> GenericConstraints;

		static Dictionary<string, Row<ElementType, bool>> primitive_value_types;

		static void InitializePrimitives ()
		{
			primitive_value_types = new Dictionary<string, Row<ElementType, bool>> (18, StringComparer.Ordinal) {
				{ "Void", new Row<ElementType, bool> (ElementType.Void, false) },
				{ "Boolean", new Row<ElementType, bool> (ElementType.Boolean, true) },
				{ "Char", new Row<ElementType, bool> (ElementType.Char, true) },
				{ "SByte", new Row<ElementType, bool> (ElementType.I1, true) },
				{ "Byte", new Row<ElementType, bool> (ElementType.U1, true) },
				{ "Int16", new Row<ElementType, bool> (ElementType.I2, true) },
				{ "UInt16", new Row<ElementType, bool> (ElementType.U2, true) },
				{ "Int32", new Row<ElementType, bool> (ElementType.I4, true) },
				{ "UInt32", new Row<ElementType, bool> (ElementType.U4, true) },
				{ "Int64", new Row<ElementType, bool> (ElementType.I8, true) },
				{ "UInt64", new Row<ElementType, bool> (ElementType.U8, true) },
				{ "Single", new Row<ElementType, bool> (ElementType.R4, true) },
				{ "Double", new Row<ElementType, bool> (ElementType.R8, true) },
				{ "String", new Row<ElementType, bool> (ElementType.String, false) },
				{ "TypedReference", new Row<ElementType, bool> (ElementType.TypedByRef, false) },
				{ "IntPtr", new Row<ElementType, bool> (ElementType.I, true) },
				{ "UIntPtr", new Row<ElementType, bool> (ElementType.U, true) },
				{ "Object", new Row<ElementType, bool> (ElementType.Object, false) },
			};
		}

		public static void TryProcessPrimitiveTypeReference (TypeReference type)
		{
			if (type.Namespace != "System")
				return;

			var scope = type.scope;
			if (scope == null || scope.MetadataScopeType != MetadataScopeType.AssemblyNameReference)
				return;

			Row<ElementType, bool> primitive_data;
			if (!TryGetPrimitiveData (type, out primitive_data))
				return;

			type.etype = primitive_data.Col1;
			type.IsValueType = primitive_data.Col2;
		}

		public static bool TryGetPrimitiveElementType (TypeDefinition type, out ElementType etype)
		{
			etype = ElementType.None;

			if (type.Namespace != "System")
				return false;

			Row<ElementType, bool> primitive_data;
			if (TryGetPrimitiveData (type, out primitive_data) && primitive_data.Col1.IsPrimitive ()) {
				etype = primitive_data.Col1;
				return true;
			}

			return false;
		}

		static bool TryGetPrimitiveData (TypeReference type, out Row<ElementType, bool> primitive_data)
		{
			if (primitive_value_types == null)
				InitializePrimitives ();

			return primitive_value_types.TryGetValue (type.Name, out primitive_data);
		}

		public void Clear ()
		{
			if (this.NestedTypes != null) this.NestedTypes.Clear ();
			if (this.ReverseNestedTypes != null) this.ReverseNestedTypes.Clear ();
			if (this.Interfaces != null) this.Interfaces.Clear ();
			if (this.ClassLayouts != null) this.ClassLayouts.Clear ();
			if (this.FieldLayouts != null) this.FieldLayouts.Clear ();
			if (this.FieldRVAs != null) this.FieldRVAs.Clear ();
			if (this.FieldMarshals != null) this.FieldMarshals.Clear ();
			if (this.Constants != null) this.Constants.Clear ();
			if (this.Overrides != null) this.Overrides.Clear ();
			if (this.CustomAttributes != null) this.CustomAttributes.Clear ();
			if (this.SecurityDeclarations != null) this.SecurityDeclarations.Clear ();
			if (this.Events != null) this.Events.Clear ();
			if (this.Properties != null) this.Properties.Clear ();
			if (this.Semantics != null) this.Semantics.Clear ();
			if (this.PInvokes != null) this.PInvokes.Clear ();
			if (this.GenericParameters != null) this.GenericParameters.Clear ();
			if (this.GenericConstraints != null) this.GenericConstraints.Clear ();
		}

		public TypeDefinition GetTypeDefinition (uint rid)
		{
			if (rid < 1 || rid > this.Types.Length)
				return null;

			return this.Types [rid - 1];
		}

		public void AddTypeDefinition (TypeDefinition type)
		{
            this.Types [type.token.RID - 1] = type;
		}

		public TypeReference GetTypeReference (uint rid)
		{
			if (rid < 1 || rid > this.TypeReferences.Length)
				return null;

			return this.TypeReferences [rid - 1];
		}

		public void AddTypeReference (TypeReference type)
		{
            this.TypeReferences [type.token.RID - 1] = type;
		}

		public FieldDefinition GetFieldDefinition (uint rid)
		{
			if (rid < 1 || rid > this.Fields.Length)
				return null;

			return this.Fields [rid - 1];
		}

		public void AddFieldDefinition (FieldDefinition field)
		{
            this.Fields [field.token.RID - 1] = field;
		}

		public MethodDefinition GetMethodDefinition (uint rid)
		{
			if (rid < 1 || rid > this.Methods.Length)
				return null;

			return this.Methods [rid - 1];
		}

		public void AddMethodDefinition (MethodDefinition method)
		{
            this.Methods [method.token.RID - 1] = method;
		}

		public MemberReference GetMemberReference (uint rid)
		{
			if (rid < 1 || rid > this.MemberReferences.Length)
				return null;

			return this.MemberReferences [rid - 1];
		}

		public void AddMemberReference (MemberReference member)
		{
            this.MemberReferences [member.token.RID - 1] = member;
		}

		public bool TryGetNestedTypeMapping (TypeDefinition type, out uint [] mapping)
		{
			return this.NestedTypes.TryGetValue (type.token.RID, out mapping);
		}

		public void SetNestedTypeMapping (uint type_rid, uint [] mapping)
		{
            this.NestedTypes [type_rid] = mapping;
		}

		public void RemoveNestedTypeMapping (TypeDefinition type)
		{
            this.NestedTypes.Remove (type.token.RID);
		}

		public bool TryGetReverseNestedTypeMapping (TypeDefinition type, out uint declaring)
		{
			return this.ReverseNestedTypes.TryGetValue (type.token.RID, out declaring);
		}

		public void SetReverseNestedTypeMapping (uint nested, uint declaring)
		{
            this.ReverseNestedTypes.Add (nested, declaring);
		}

		public void RemoveReverseNestedTypeMapping (TypeDefinition type)
		{
            this.ReverseNestedTypes.Remove (type.token.RID);
		}

		public bool TryGetInterfaceMapping (TypeDefinition type, out MetadataToken [] mapping)
		{
			return this.Interfaces.TryGetValue (type.token.RID, out mapping);
		}

		public void SetInterfaceMapping (uint type_rid, MetadataToken [] mapping)
		{
            this.Interfaces [type_rid] = mapping;
		}

		public void RemoveInterfaceMapping (TypeDefinition type)
		{
            this.Interfaces.Remove (type.token.RID);
		}

		public void AddPropertiesRange (uint type_rid, Range range)
		{
            this.Properties.Add (type_rid, range);
		}

		public bool TryGetPropertiesRange (TypeDefinition type, out Range range)
		{
			return this.Properties.TryGetValue (type.token.RID, out range);
		}

		public void RemovePropertiesRange (TypeDefinition type)
		{
            this.Properties.Remove (type.token.RID);
		}

		public void AddEventsRange (uint type_rid, Range range)
		{
            this.Events.Add (type_rid, range);
		}

		public bool TryGetEventsRange (TypeDefinition type, out Range range)
		{
			return this.Events.TryGetValue (type.token.RID, out range);
		}

		public void RemoveEventsRange (TypeDefinition type)
		{
            this.Events.Remove (type.token.RID);
		}

		public bool TryGetGenericParameterRanges (IGenericParameterProvider owner, out Range [] ranges)
		{
			return this.GenericParameters.TryGetValue (owner.MetadataToken, out ranges);
		}

		public void RemoveGenericParameterRange (IGenericParameterProvider owner)
		{
            this.GenericParameters.Remove (owner.MetadataToken);
		}

		public bool TryGetCustomAttributeRanges (ICustomAttributeProvider owner, out Range [] ranges)
		{
			return this.CustomAttributes.TryGetValue (owner.MetadataToken, out ranges);
		}

		public void RemoveCustomAttributeRange (ICustomAttributeProvider owner)
		{
            this.CustomAttributes.Remove (owner.MetadataToken);
		}

		public bool TryGetSecurityDeclarationRanges (ISecurityDeclarationProvider owner, out Range [] ranges)
		{
			return this.SecurityDeclarations.TryGetValue (owner.MetadataToken, out ranges);
		}

		public void RemoveSecurityDeclarationRange (ISecurityDeclarationProvider owner)
		{
            this.SecurityDeclarations.Remove (owner.MetadataToken);
		}

		public bool TryGetGenericConstraintMapping (GenericParameter generic_parameter, out MetadataToken [] mapping)
		{
			return this.GenericConstraints.TryGetValue (generic_parameter.token.RID, out mapping);
		}

		public void SetGenericConstraintMapping (uint gp_rid, MetadataToken [] mapping)
		{
            this.GenericConstraints [gp_rid] = mapping;
		}

		public void RemoveGenericConstraintMapping (GenericParameter generic_parameter)
		{
            this.GenericConstraints.Remove (generic_parameter.token.RID);
		}

		public bool TryGetOverrideMapping (MethodDefinition method, out MetadataToken [] mapping)
		{
			return this.Overrides.TryGetValue (method.token.RID, out mapping);
		}

		public void SetOverrideMapping (uint rid, MetadataToken [] mapping)
		{
            this.Overrides [rid] = mapping;
		}

		public void RemoveOverrideMapping (MethodDefinition method)
		{
            this.Overrides.Remove (method.token.RID);
		}

		public TypeDefinition GetFieldDeclaringType (uint field_rid)
		{
			return BinaryRangeSearch (this.Types, field_rid, true);
		}

		public TypeDefinition GetMethodDeclaringType (uint method_rid)
		{
			return BinaryRangeSearch (this.Types, method_rid, false);
		}

		static TypeDefinition BinaryRangeSearch (TypeDefinition [] types, uint rid, bool field)
		{
			int min = 0;
			int max = types.Length - 1;
			while (min <= max) {
				int mid = min + ((max - min) / 2);
				var type = types [mid];
				var range = field ? type.fields_range : type.methods_range;

				if (rid < range.Start)
					max = mid - 1;
				else if (rid >= range.Start + range.Length)
					min = mid + 1;
				else
					return type;
			}

			return null;
		}
	}
}
