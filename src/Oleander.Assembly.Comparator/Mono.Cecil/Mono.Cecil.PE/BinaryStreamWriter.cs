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

namespace Mono.Cecil.PE {

	class BinaryStreamWriter : BinaryWriter {

		public BinaryStreamWriter (Stream stream)
			: base (stream)
		{
		}

		public void WriteByte (byte value)
		{
            this.Write (value);
		}

		public void WriteUInt16 (ushort value)
		{
            this.Write (value);
		}

		public void WriteInt16 (short value)
		{
            this.Write (value);
		}

		public void WriteUInt32 (uint value)
		{
            this.Write (value);
		}

		public void WriteInt32 (int value)
		{
            this.Write (value);
		}

		public void WriteUInt64 (ulong value)
		{
            this.Write (value);
		}

		public void WriteBytes (byte [] bytes)
		{
            this.Write (bytes);
		}

		public void WriteDataDirectory (DataDirectory directory)
		{
            this.Write (directory.VirtualAddress);
            this.Write (directory.Size);
		}

		public void WriteBuffer (ByteBuffer buffer)
		{
            this.Write (buffer.buffer, 0, buffer.length);
		}

		protected void Advance (int bytes)
		{
            this.BaseStream.Seek (bytes, SeekOrigin.Current);
		}
	}
}

#endif
