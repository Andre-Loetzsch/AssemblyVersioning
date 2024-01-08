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

	public abstract class MemberReference : IMetadataTokenProvider {

		string name;
		TypeReference declaring_type;

		internal MetadataToken token;

		public virtual string Name {
			get { return this.name; }
			set { this.name = value; }
		}

		public abstract string FullName {
			get;
		}

		public virtual TypeReference DeclaringType {
			get { return this.declaring_type; }
			set { this.declaring_type = value; }
		}

		public MetadataToken MetadataToken {
			get { return this.token; }
			set { this.token = value; }
		}

		internal bool HasImage {
			get {
				var module = this.Module;
				if (module == null)
					return false;

				return module.HasImage;
			}
		}

		public virtual ModuleDefinition Module {
			get { return this.declaring_type != null ? this.declaring_type.Module : null; }
		}

		public virtual bool IsDefinition {
			get { return false; }
		}

		public virtual bool ContainsGenericParameter {
			get { return this.declaring_type != null && this.declaring_type.ContainsGenericParameter; }
		}

		internal MemberReference ()
		{
		}

		internal MemberReference (string name)
		{
			this.name = name ?? string.Empty;
		}

		internal string MemberFullName ()
		{
			if (this.declaring_type == null)
				return this.name;

			return this.declaring_type.FullName + "::" + this.name;
		}

		public override string ToString ()
		{
			return this.FullName;
		}
	}
}
