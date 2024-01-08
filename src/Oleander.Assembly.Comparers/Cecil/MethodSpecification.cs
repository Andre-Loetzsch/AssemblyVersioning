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

namespace Oleander.Assembly.Comparers.Cecil {

	public abstract class MethodSpecification : MethodReference {

		readonly MethodReference method;

		public MethodReference ElementMethod {
			get { return this.method; }
		}

		public override string Name {
			get { return this.method.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodCallingConvention CallingConvention {
			get { return this.method.CallingConvention; }
			set { throw new InvalidOperationException (); }
		}

		public override bool HasThis {
			get { return this.method.HasThis; }
			set { throw new InvalidOperationException (); }
		}

		public override bool ExplicitThis {
			get { return this.method.ExplicitThis; }
			set { throw new InvalidOperationException (); }
		}

		public override MethodReturnType MethodReturnType
		{
			/*Original*/
			//get { return method.MethodReturnType; }
			//set { throw new InvalidOperationException (); }

			/* Telerik Authorship */
			get;
			set;
		}

		public override TypeReference DeclaringType {
			get { return this.method.DeclaringType; }
			set { throw new InvalidOperationException (); }
		}

		public override ModuleDefinition Module {
			get { return this.method.Module; }
		}

		public override bool HasParameters {
			get { return this.method.HasParameters; }
		}

		/*Telerik Authorship*/
		//public override Collection<ParameterDefinition> Parameters {
		//	get { return method.Parameters; }
		//}

		public override bool ContainsGenericParameter {
			get { return this.method.ContainsGenericParameter; }
		}

		internal MethodSpecification (MethodReference method)
		{
			if (method == null)
				throw new ArgumentNullException ("method");

			this.method = method;
			this.token = new MetadataToken (TokenType.MethodSpec);
		}

		public sealed override MethodReference GetElementMethod ()
		{
			return this.method.GetElementMethod ();
		}
	}
}
