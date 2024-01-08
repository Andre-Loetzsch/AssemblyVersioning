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

using Oleander.Assembly.Comparers.Cecil.Cil;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;
using RVA = System.UInt32;

namespace Oleander.Assembly.Comparers.Cecil {

	public sealed class MethodDefinition : MethodReference, IMemberDefinition, ISecurityDeclarationProvider, IVisibilityDefinition/*Telerik Authorship*/, ISecurityDeclarationMemberDefinition/*Telerik Authorship*/, IGenericDefinition/*Telerik Authorship*/ {

		ushort attributes;
		ushort impl_attributes;
		internal volatile bool sem_attrs_ready;
		internal MethodSemanticsAttributes sem_attrs;
		Collection<CustomAttribute> custom_attributes;
		Collection<SecurityDeclaration> security_declarations;

		internal RVA rva;
		internal PInvokeInfo pinvoke;
		Collection<MethodReference> overrides;

		internal MethodBody body;

		public MethodAttributes Attributes {
			get { return (MethodAttributes)this.attributes; }
			set { this.attributes = (ushort) value; }
		}

		/*Telerik Authorship*/
		public bool HasImplAttributes
		{
			get
			{
				return (ushort)this.ImplAttributes != 0;
			}
		}
		
		public MethodImplAttributes ImplAttributes {
			get { return (MethodImplAttributes)this.impl_attributes; }
			set { this.impl_attributes = (ushort) value; }
		}

		public MethodSemanticsAttributes SemanticsAttributes {
			get {
				if (this.sem_attrs_ready)
					return this.sem_attrs;

				if (this.HasImage) {
                    this.ReadSemantics ();
					return this.sem_attrs;
				}

                this.sem_attrs = MethodSemanticsAttributes.None;
                this.sem_attrs_ready = true;
				return this.sem_attrs;
			}
			set {
				/*Telerik Authorship*/
                this.sem_attrs_ready = true;
                this.sem_attrs = value;
			}
		}

		/*Telerik Authorship*/
		private bool? isOperator;
		/*Telerik Authorship*/
		private string operatorName;
		/*Telerik Authorship*/
		private bool? isUnsafe;

		internal void ReadSemantics ()
		{
			if (this.sem_attrs_ready)
				return;

			var module = this.Module;
			if (module == null)
				return;

			if (!module.HasImage)
				return;

			module.Read (this, (method, reader) => reader.ReadAllSemantics (method));
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

		public int RVA {
			get { return (int)this.rva; }
		}

		public bool HasBody {
			get {
				return (this.attributes & (ushort) MethodAttributes.Abstract) == 0 &&
					(this.attributes & (ushort) MethodAttributes.PInvokeImpl) == 0 &&
					(this.impl_attributes & (ushort) MethodImplAttributes.InternalCall) == 0 &&
					(this.impl_attributes & (ushort) MethodImplAttributes.Native) == 0 &&
					(this.impl_attributes & (ushort) MethodImplAttributes.Unmanaged) == 0 &&
					(this.impl_attributes & (ushort) MethodImplAttributes.Runtime) == 0;
			}
		}

		public MethodBody Body {
			get {
				MethodBody localBody = this.body;
				if (localBody != null)
					return localBody;

				if (!this.HasBody)
					return null;

				if (this.HasImage && this.rva != 0)
					return this.Module.Read (ref this.body, this, (method, reader) => reader.ReadMethodBody (method));

				return this.body = new MethodBody (this);
			}
			set {
				var module = this.Module;
				if (module == null) {
                    this.body = value;
					return;
				}

				// we reset Body to null in ILSpy to save memory; so we need that operation to be thread-safe
				lock (module.SyncRoot) {
                    this.body = value;
				}
			}
		}

		/*Telerik Authorship*/
		public void RefreshBody()
		{
			lock (this.Module.SyncRoot)
			{
				if(!this.HasBody)
				{
                    this.body = null;
				}
				else if (this.HasImage && this.rva != 0)
				{
                    this.body = this.Module.Read(this, (method, reader) => reader.ReadMethodBody(method));
				}
				else
				{
                    this.body = new MethodBody(this);
				}
			}
		}

		public bool HasPInvokeInfo {
			get {
				if (this.pinvoke != null)
					return true;

				return this.IsPInvokeImpl;
			}
		}

		public PInvokeInfo PInvokeInfo {
			get {
				if (this.pinvoke != null)
					return this.pinvoke;

				if (this.HasImage && this.IsPInvokeImpl)
					return this.Module.Read (ref this.pinvoke, this, (method, reader) => reader.ReadPInvokeInfo (method));

				return null;
			}
			set {
                this.IsPInvokeImpl = true;
                this.pinvoke = value;
			}
		}

		/*Telerik Authorship*/
		private bool? hasOverrides;
		public bool HasOverrides {
			get {
				if (this.overrides != null)
					return this.overrides.Count > 0;

				/*Telerik Authorship*/
				if (this.hasOverrides != null)
					return this.hasOverrides == true;

				/*Telerik Authorship*/
				if (this.HasImage)
					return this.Module.Read (ref this.hasOverrides, this, (method, reader) => reader.HasOverrides (method)) == true;

				/*Telerik Authorship*/
				return false;
			}
		}

		public Collection<MethodReference> Overrides {
			get {
				if (this.overrides != null)
					return this.overrides;

				if (this.HasImage)
					return this.Module.Read (ref this.overrides, this, (method, reader) => reader.ReadOverrides (method));

				return this.overrides = new Collection<MethodReference> ();
			}
		}

		/*Telerik Authorship*/
		private bool? hasGenericParameters;
		public override bool HasGenericParameters {
			get {
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

		#region MethodAttributes

		public bool IsCompilerControlled {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.CompilerControlled); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.CompilerControlled, value); }
		}

		public bool IsPrivate {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Private); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Private, value); }
		}

		public bool IsFamilyAndAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.FamANDAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.FamANDAssem, value); }
		}

		public bool IsAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Assembly); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Assembly, value); }
		}

		public bool IsFamily {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Family); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Family, value); }
		}

		public bool IsFamilyOrAssembly {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.FamORAssem); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.FamORAssem, value); }
		}

		public bool IsPublic {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Public); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.MemberAccessMask, (ushort) MethodAttributes.Public, value); }
		}

		public bool IsStatic {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.Static); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.Static, value); }
		}

		public bool IsFinal {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.Final); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.Final, value); }
		}

		public bool IsVirtual {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.Virtual); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.Virtual, value); }
		}

		public bool IsHideBySig {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.HideBySig); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.HideBySig, value); }
		}

		public bool IsReuseSlot {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.VtableLayoutMask, (ushort) MethodAttributes.ReuseSlot); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.VtableLayoutMask, (ushort) MethodAttributes.ReuseSlot, value); }
		}

		public bool IsNewSlot {
			get { return this.attributes.GetMaskedAttributes ((ushort) MethodAttributes.VtableLayoutMask, (ushort) MethodAttributes.NewSlot); }
			set { this.attributes = this.attributes.SetMaskedAttributes ((ushort) MethodAttributes.VtableLayoutMask, (ushort) MethodAttributes.NewSlot, value); }
		}

		public bool IsCheckAccessOnOverride {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.CheckAccessOnOverride); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.CheckAccessOnOverride, value); }
		}

		public bool IsAbstract {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.Abstract); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.Abstract, value); }
		}

		public bool IsSpecialName {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.SpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.SpecialName, value); }
		}

		public bool IsPInvokeImpl {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.PInvokeImpl); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.PInvokeImpl, value); }
		}

		public bool IsUnmanagedExport {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.UnmanagedExport); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.UnmanagedExport, value); }
		}

		public bool IsRuntimeSpecialName {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.RTSpecialName); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.RTSpecialName, value); }
		}

		public bool HasSecurity {
			get { return this.attributes.GetAttributes ((ushort) MethodAttributes.HasSecurity); }
			set { this.attributes = this.attributes.SetAttributes ((ushort) MethodAttributes.HasSecurity, value); }
		}

		#endregion

		#region MethodImplAttributes

		public bool IsIL {
			get { return this.impl_attributes.GetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.IL); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.IL, value); }
		}

		public bool IsNative {
			get { return this.impl_attributes.GetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.Native); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.Native, value); }
		}

		/*Telerik Authorship*/
		public bool IsOPTIL
		{
			get { return this.impl_attributes.GetMaskedAttributes((ushort)MethodImplAttributes.CodeTypeMask, (ushort)MethodImplAttributes.OPTIL); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes((ushort)MethodImplAttributes.CodeTypeMask, (ushort)MethodImplAttributes.OPTIL, value); }
		}

		public bool IsRuntime {
			get { return this.impl_attributes.GetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.Runtime); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes ((ushort) MethodImplAttributes.CodeTypeMask, (ushort) MethodImplAttributes.Runtime, value); }
		}

		public bool IsUnmanaged {
			get { return this.impl_attributes.GetMaskedAttributes ((ushort) MethodImplAttributes.ManagedMask, (ushort) MethodImplAttributes.Unmanaged); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes ((ushort) MethodImplAttributes.ManagedMask, (ushort) MethodImplAttributes.Unmanaged, value); }
		}

		public bool IsManaged {
			get { return this.impl_attributes.GetMaskedAttributes ((ushort) MethodImplAttributes.ManagedMask, (ushort) MethodImplAttributes.Managed); }
			set { this.impl_attributes = this.impl_attributes.SetMaskedAttributes ((ushort) MethodImplAttributes.ManagedMask, (ushort) MethodImplAttributes.Managed, value); }
		}

		public bool IsForwardRef {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.ForwardRef); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.ForwardRef, value); }
		}

		public bool IsPreserveSig {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.PreserveSig); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.PreserveSig, value); }
		}

		public bool IsInternalCall {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.InternalCall); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.InternalCall, value); }
		}

		public bool IsSynchronized {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.Synchronized); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.Synchronized, value); }
		}

		public bool NoInlining {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.NoInlining); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.NoInlining, value); }
		}

		public bool NoOptimization {
			get { return this.impl_attributes.GetAttributes ((ushort) MethodImplAttributes.NoOptimization); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes ((ushort) MethodImplAttributes.NoOptimization, value); }
		}

		/*Telerik Authorship*/
		public bool AggressiveInlining
		{
			get { return this.impl_attributes.GetAttributes((ushort)MethodImplAttributes.AggressiveInlining); }
			set { this.impl_attributes = this.impl_attributes.SetAttributes((ushort)MethodImplAttributes.AggressiveInlining, value); }
		}

		#endregion

		#region MethodSemanticsAttributes

		public bool IsSetter {
			get { return this.GetSemantics (MethodSemanticsAttributes.Setter); }
			set { this.SetSemantics (MethodSemanticsAttributes.Setter, value); }
		}

		public bool IsGetter {
			get { return this.GetSemantics (MethodSemanticsAttributes.Getter); }
			set { this.SetSemantics (MethodSemanticsAttributes.Getter, value); }
		}

		public bool IsOther {
			get { return this.GetSemantics (MethodSemanticsAttributes.Other); }
			set { this.SetSemantics (MethodSemanticsAttributes.Other, value); }
		}

		public bool IsAddOn {
			get { return this.GetSemantics (MethodSemanticsAttributes.AddOn); }
			set { this.SetSemantics (MethodSemanticsAttributes.AddOn, value); }
		}

		public bool IsRemoveOn {
			get { return this.GetSemantics (MethodSemanticsAttributes.RemoveOn); }
			set { this.SetSemantics (MethodSemanticsAttributes.RemoveOn, value); }
		}

		public bool IsFire {
			get { return this.GetSemantics (MethodSemanticsAttributes.Fire); }
			set { this.SetSemantics (MethodSemanticsAttributes.Fire, value); }
		}

		#endregion

		public new TypeDefinition DeclaringType {
			get { return (TypeDefinition) base.DeclaringType; }
			set { base.DeclaringType = value; }
		}

		public /*Telerik Authorship*/ override bool IsConstructor {
			get {
				return this.IsRuntimeSpecialName
					&& this.IsSpecialName
					/*Telerik Authorship*/
					&& base.IsConstructor;
			}
		}

		public override bool IsDefinition {
			get { return true; }
		}

		internal MethodDefinition ()
		{
			this.token = new MetadataToken (TokenType.Method);
		}

		public MethodDefinition (string name, MethodAttributes attributes, TypeReference returnType)
			: base (name, returnType)
		{
			this.attributes = (ushort) attributes;
			this.HasThis = !this.IsStatic;
			this.token = new MetadataToken (TokenType.Method);
		}

		public override MethodDefinition Resolve ()
		{
			return this;
		}

		/*Telerik Authorship*/
		public bool IsExtensionMethod
		{
			get
			{
				if ((this.CustomAttributes.Count > 0) &&
					(this.CustomAttributes[0].AttributeType.FullName == "System.Runtime.CompilerServices.ExtensionAttribute"))
				{
					return true;
				}
				return false;
			}
		}

		/*Telerik Authorship*/
		public bool IsOperator
		{
			get
			{
				if (this.isOperator == null)
				{
                    this.isOperator = this.Name.StartsWith("op_");
				}
				return this.isOperator.Value;
			}
		}

		/*Telerik Authorship*/
		public string OperatorName
		{
			get
			{
				if (this.IsOperator)
				{
					if (string.IsNullOrEmpty(this.operatorName))
					{
                        this.operatorName = this.Name.Remove(0, 3); //chars op_
					}
				}
				return this.operatorName;
			}
		}

		/*Telerik Authorship*/
		public bool IsUnsafe
		{
			get
			{
				if (this.isUnsafe.HasValue == false)
				{
					if (this.ReturnType.IsPointer)
					{
                        this.isUnsafe = true;
						return true;
					}
					if (this.parameters != null)
					{
						foreach (ParameterDefinition parameter in this.parameters)
						{
							if (parameter.ParameterType.IsPointer)
							{
                                this.isUnsafe = true;
								return true;
							}
						}
					}
					if (this.body != null)
					{
						if (this.body.Variables != null)
						{
							foreach (VariableDefinition variable in this.body.Variables)
							{
								if (variable.VariableType.IsPointer)
								{
                                    this.isUnsafe = true;
									return true;
								}
							}
						}

                        this.isUnsafe = false;
					}
				}

				return this.isUnsafe??false;
			}
			private set
			{
				this.isUnsafe = value;
			}
		}

		/*Telerik Authorship*/
		public bool IsJustDecompileGenerated { get; set; }
	}
	static partial class Mixin {

		public static ParameterDefinition GetParameter (this MethodBody self, int index)
		{
			var method = self.method;

			if (method.HasThis) {
				if (index == 0)
					return self.ThisParameter;

				index--;
			}

			var parameters = method.Parameters;

			if (index < 0 || index >= parameters.size)
				return null;

			return parameters [index];
		}

		public static VariableDefinition GetVariable (this MethodBody self, int index)
		{
			var variables = self.Variables;

			if (index < 0 || index >= variables.size)
				return null;

			return variables [index];
		}

		public static bool GetSemantics (this MethodDefinition self, MethodSemanticsAttributes semantics)
		{
			return (self.SemanticsAttributes & semantics) != 0;
		}

		public static void SetSemantics (this MethodDefinition self, MethodSemanticsAttributes semantics, bool value)
		{
			if (value)
				self.SemanticsAttributes |= semantics;
			else
				self.SemanticsAttributes &= ~semantics;
		}
	}
}
