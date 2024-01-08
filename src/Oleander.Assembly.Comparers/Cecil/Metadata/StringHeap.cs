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
using Mono.Cecil.PE;

namespace Oleander.Assembly.Comparers.Cecil.Metadata {

	class StringHeap : Heap {

		readonly Dictionary<uint, string> strings = new Dictionary<uint, string> ();

		public StringHeap (Section section, uint start, uint size)
			: base (section, start, size)
		{
		}

		public string Read (uint index)
		{
			if (index == 0)
				return string.Empty;

			string @string;
			if (this.strings.TryGetValue (index, out @string))
				return @string;

			if (index > this.Size - 1)
				return string.Empty;

			@string = this.ReadStringAt (index);
			if (@string.Length != 0) this.strings.Add (index, @string);

			return @string;
		}

		protected virtual string ReadStringAt (uint index)
		{
			int length = 0;
			byte [] data = this.Section.Data;
			int start = (int) (index + this.Offset);

			for (int i = start; ; i++) {
				if (data [i] == 0)
					break;

				length++;
			}

			return Encoding.UTF8.GetString (data, start, length);
		}
	}
}
