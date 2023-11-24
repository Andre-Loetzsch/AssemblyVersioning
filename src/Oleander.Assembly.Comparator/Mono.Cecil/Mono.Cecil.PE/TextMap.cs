//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

#if !READ_ONLY

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	enum TextSegment {
		ImportAddressTable,
		CLIHeader,
		Code,
		Resources,
		Data,
		StrongNameSignature,

		// Metadata
		MetadataHeader,
		TableHeap,
		StringHeap,
		UserStringHeap,
		GuidHeap,
		BlobHeap,
		// End Metadata

		DebugDirectory,
		ImportDirectory,
		ImportHintNameTable,
		StartupStub,
	}

	sealed class TextMap {

		readonly Range [] map = new Range [16 /*Enum.GetValues (typeof (TextSegment)).Length*/];

		public void AddMap (TextSegment segment, int length)
		{
            this.map [(int) segment] = new Range (this.GetStart (segment), (uint) length);
		}

		public void AddMap (TextSegment segment, int length, int align)
		{
			align--;

            this.AddMap (segment, (length + align) & ~align);
		}

		public void AddMap (TextSegment segment, Range range)
		{
            this.map [(int) segment] = range;
		}

		public Range GetRange (TextSegment segment)
		{
			return this.map [(int) segment];
		}

		public DataDirectory GetDataDirectory (TextSegment segment)
		{
			var range = this.map [(int) segment];

			return new DataDirectory (range.Length == 0 ? 0 : range.Start, range.Length);
		}

		public RVA GetRVA (TextSegment segment)
		{
			return this.map [(int) segment].Start;
		}

		public RVA GetNextRVA (TextSegment segment)
		{
			var i = (int) segment;
			return this.map [i].Start + this.map [i].Length;
		}

		public int GetLength (TextSegment segment)
		{
			return (int)this.map [(int) segment].Length;
		}

		RVA GetStart (TextSegment segment)
		{
			var index = (int) segment;
			return index == 0 ? ImageWriter.text_rva : this.ComputeStart (index);
		}

		RVA ComputeStart (int index)
		{
			index--;
			return this.map [index].Start + this.map [index].Length;
		}

		public uint GetLength ()
		{
			var range = this.map [(int) TextSegment.StartupStub];
			return range.Start - ImageWriter.text_rva + range.Length;
		}
	}
}

#endif
