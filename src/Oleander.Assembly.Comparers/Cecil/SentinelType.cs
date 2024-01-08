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

	public sealed class SentinelType : TypeSpecification {

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override bool IsSentinel {
			get { return true; }
		}

		public SentinelType (TypeReference type)
			: base (type)
		{
			Mixin.CheckType (type);
			this.etype = Oleander.Assembly.Comparers.Cecil.Metadata.ElementType.Sentinel;
		}
	}
}
