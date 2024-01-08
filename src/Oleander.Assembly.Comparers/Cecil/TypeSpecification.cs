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

	public abstract class TypeSpecification : TypeReference {

		readonly TypeReference element_type;

		public TypeReference ElementType {
			get { return this.element_type; }
		}

		public override string Name {
			get { return this.element_type.Name; }
			set { throw new InvalidOperationException (); }
		}

		public override string Namespace {
			get { return this.element_type.Namespace; }
			set { throw new InvalidOperationException (); }
		}

		public override IMetadataScope Scope {
			get { return this.element_type.Scope; }
			set { throw new InvalidOperationException (); }
		}

		public override ModuleDefinition Module {
			get { return this.element_type.Module; }
		}

		public override string FullName {
			get { return this.element_type.FullName; }
		}

		public override bool ContainsGenericParameter {
			get { return this.element_type.ContainsGenericParameter; }
		}

		public override MetadataType MetadataType {
			get { return (MetadataType)this.etype; }
		}

		internal TypeSpecification (TypeReference type)
			: base (null, null)
		{
			this.element_type = type;
			this.token = new MetadataToken (TokenType.TypeSpec);
		}

		public override TypeReference GetElementType ()
		{
			return this.element_type.GetElementType ();
		}
	}

	static partial class Mixin {

		public static void CheckType (TypeReference type)
		{
			if (type == null)
				throw new ArgumentNullException ("type");
		}
	}
}
