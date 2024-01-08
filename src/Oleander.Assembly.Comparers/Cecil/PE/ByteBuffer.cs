//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Oleander.Assembly.Comparers.Cecil.PE {

	class ByteBuffer {

		internal byte [] buffer;
		internal int length;
		internal int position;

		public ByteBuffer ()
		{
			this.buffer = Empty<byte>.Array;
		}

		public ByteBuffer (int length)
		{
			this.buffer = new byte [length];
		}

		public ByteBuffer (byte [] buffer)
		{
			this.buffer = buffer ?? Empty<byte>.Array;
			this.length = this.buffer.Length;
		}

		public void Reset (byte [] buffer)
		{
			this.buffer = buffer ?? Empty<byte>.Array;
			this.length = this.buffer.Length;
		}

		public void Advance (int length)
		{
            this.position += length;
		}

		public byte ReadByte ()
		{
			return this.buffer [this.position++];
		}

		public sbyte ReadSByte ()
		{
			return (sbyte)this.ReadByte ();
		}

		public byte [] ReadBytes (int length)
		{
			var bytes = new byte [length];
			Buffer.BlockCopy (this.buffer, this.position, bytes, 0, length);
            this.position += length;
			return bytes;
		}

		public ushort ReadUInt16 ()
		{
			ushort value = (ushort) (this.buffer [this.position]
				| (this.buffer [this.position + 1] << 8));
            this.position += 2;
			return value;
		}

		public short ReadInt16 ()
		{
			return (short)this.ReadUInt16 ();
		}

		public uint ReadUInt32 ()
		{
			uint value = (uint) (this.buffer [this.position]
				| (this.buffer [this.position + 1] << 8)
				| (this.buffer [this.position + 2] << 16)
				| (this.buffer [this.position + 3] << 24));
            this.position += 4;
			return value;
		}

		public int ReadInt32 ()
		{
			return (int)this.ReadUInt32 ();
		}

		public ulong ReadUInt64 ()
		{
			uint low = this.ReadUInt32 ();
			uint high = this.ReadUInt32 ();

			return (((ulong) high) << 32) | low;
		}

		public long ReadInt64 ()
		{
			return (long)this.ReadUInt64 ();
		}

		public uint ReadCompressedUInt32 ()
		{
			byte first = this.ReadByte ();
			if ((first & 0x80) == 0)
				return first;

			if ((first & 0x40) == 0)
				return ((uint) (first & ~0x80) << 8)
					| this.ReadByte ();

			return ((uint) (first & ~0xc0) << 24)
				| (uint)this.ReadByte () << 16
				| (uint)this.ReadByte () << 8
				| this.ReadByte ();
		}

		public int ReadCompressedInt32 ()
		{
			var value = (int) (this.ReadCompressedUInt32 () >> 1);
			if ((value & 1) == 0)
				return value;
			if (value < 0x40)
				return value - 0x40;
			if (value < 0x2000)
				return value - 0x2000;
			if (value < 0x10000000)
				return value - 0x10000000;
			return value - 0x20000000;
		}

		public float ReadSingle ()
		{
			if (!BitConverter.IsLittleEndian) {
				var bytes = this.ReadBytes (4);
				Array.Reverse (bytes);
				return BitConverter.ToSingle (bytes, 0);
			}

			float value = BitConverter.ToSingle (this.buffer, this.position);
            this.position += 4;
			return value;
		}

		public double ReadDouble ()
		{
			if (!BitConverter.IsLittleEndian) {
				var bytes = this.ReadBytes (8);
				Array.Reverse (bytes);
				return BitConverter.ToDouble (bytes, 0);
			}

			double value = BitConverter.ToDouble (this.buffer, this.position);
            this.position += 8;
			return value;
		}
	}
}
