//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Oleander.Assembly.Comparers.Cecil.PE;

namespace Oleander.Assembly.Comparers.Cecil.Metadata {

	abstract class Heap {

		public int IndexSize;

		public readonly Section Section;
		public readonly uint Offset;
		public readonly uint Size;

		protected Heap (Section section, uint offset, uint size)
		{
			this.Section = section;
			this.Offset = offset;
			this.Size = size;
		}
	}
}
