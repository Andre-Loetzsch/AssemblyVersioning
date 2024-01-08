//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//


namespace Mono.Cecil {

	public interface IModifierType {
		TypeReference ModifierType { get; }
		TypeReference ElementType { get; }
	}

	public sealed class OptionalModifierType : TypeSpecification, IModifierType {

		TypeReference modifier_type;

		public TypeReference ModifierType {
			get { return this.modifier_type; }
			set { this.modifier_type = value; }
		}

		public override string Name {
			get { return base.Name + this.Suffix; }
		}

		public override string FullName {
			get { return base.FullName + this.Suffix; }
		}

		/*Telerik Authorship*/
		public override bool IsPointer
		{
			get
			{
				return this.ElementType.IsPointer;
			}
		}

		/*Telerik Authorship*/
		public string Suffix {
			get { return " modopt(" + this.modifier_type + ")"; }
		}

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsOptionalModifier {
			get { return true; }
		}

		public override bool ContainsGenericParameter {
			get { return this.modifier_type.ContainsGenericParameter || base.ContainsGenericParameter; }
		}

		public OptionalModifierType (TypeReference modifierType, TypeReference type)
			: base (type)
		{
			Mixin.CheckModifier (modifierType, type);
			this.modifier_type = modifierType;
			this.etype = Oleander.Assembly.Comparers.Cecil.Metadata.ElementType.CModOpt;
		}
	}

	public sealed class RequiredModifierType : TypeSpecification, IModifierType {

		TypeReference modifier_type;

		public TypeReference ModifierType {
			get { return this.modifier_type; }
			set { this.modifier_type = value; }
		}

		/*Telerik Authorship*/
		public override bool IsPointer
		{
			get
			{
				return this.ElementType.IsPointer;
			}
		}

		public override string Name {
			get { return base.Name + this.Suffix; }
		}

		public override string FullName {
			get { return base.FullName + this.Suffix; }
		}

		/*Telerik Authorship*/
		public string Suffix {
			get { return " modreq(" + this.modifier_type + ")"; }
		}

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsRequiredModifier {
			get { return true; }
		}

		public override bool ContainsGenericParameter {
			get { return this.modifier_type.ContainsGenericParameter || base.ContainsGenericParameter; }
		}

		public RequiredModifierType (TypeReference modifierType, TypeReference type)
			: base (type)
		{
			Mixin.CheckModifier (modifierType, type);
			this.modifier_type = modifierType;
			this.etype = Oleander.Assembly.Comparers.Cecil.Metadata.ElementType.CModReqD;
		}

	}

	static partial class Mixin {

		public static void CheckModifier (TypeReference modifierType, TypeReference type)
		{
			if (modifierType == null)
				throw new ArgumentNullException ("modifierType");
			if (type == null)
				throw new ArgumentNullException ("type");
		}
	}
}
