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
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using MD = Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public sealed class FunctionPointerType : TypeSpecification, IMethodSignature {

		readonly MethodReference function;

		public bool HasThis {
			get { return this.function.HasThis; }
			set { this.function.HasThis = value; }
		}

		public bool ExplicitThis {
			get { return this.function.ExplicitThis; }
			set { this.function.ExplicitThis = value; }
		}

		public MethodCallingConvention CallingConvention {
			get { return this.function.CallingConvention; }
			set { this.function.CallingConvention = value; }
		}

		public bool HasParameters {
			get { return this.function.HasParameters; }
		}

		public Collection<ParameterDefinition> Parameters {
			get { return this.function.Parameters; }
		}

		public TypeReference ReturnType {
			get { return this.function.MethodReturnType.ReturnType; }
			set { this.function.MethodReturnType.ReturnType = value; }
		}

		public MethodReturnType MethodReturnType {
			get { return this.function.MethodReturnType; }
		}

		public override string Name {
			get { return this.function.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override string Namespace {
			get { return string.Empty; }
			set { throw new InvalidOperationException (); }
		}

		public override ModuleDefinition Module {
			get { return this.ReturnType.Module; }
		}

		public override IMetadataScope Scope {
			get { return this.function.ReturnType.Scope; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsFunctionPointer {
			get { return true; }
		}

		public override bool ContainsGenericParameter {
			get { return this.function.ContainsGenericParameter; }
		}

		public override string FullName {
			get {
				var signature = new StringBuilder ();
				signature.Append (this.function.Name);
				signature.Append (" ");
				signature.Append (this.function.ReturnType.FullName);
				signature.Append (" *");
				this.MethodSignatureFullName (signature);
				return signature.ToString ();
			}
		}

		/*Telerik Authorship*/
		public MethodReference Function {
			get { return this.function; }
		}

		public FunctionPointerType ()
			: base (null)
		{
			this.function = new MethodReference ();
			this.function.Name = "method";
			this.etype = MD.ElementType.FnPtr;
		}

		public override TypeDefinition Resolve ()
		{
			return null;
		}

		public override TypeReference GetElementType ()
		{
			return this;
		}
	}
}
