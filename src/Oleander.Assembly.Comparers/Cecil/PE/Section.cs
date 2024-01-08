//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using RVA = System.UInt32;

namespace Oleander.Assembly.Comparers.Cecil.PE {

	sealed class Section {
		public string Name;
		public RVA VirtualAddress;
        #pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        public uint VirtualSize;
        #pragma warning restore CS0649 // Field is never assigned to, and will always have its default value
        public uint SizeOfRawData;
		public uint PointerToRawData;
		public byte [] Data;
	}
}
