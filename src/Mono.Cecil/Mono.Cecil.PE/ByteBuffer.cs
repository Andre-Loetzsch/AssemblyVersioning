//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Mono.Cecil.PE {

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

#if !READ_ONLY

		public void WriteByte (byte value)
		{
			if (this.position == this.buffer.Length) this.Grow (1);

            this.buffer [this.position++] = value;

			if (this.position > this.length) this.length = this.position;
		}

		public void WriteSByte (sbyte value)
		{
            this.WriteByte ((byte) value);
		}

		public void WriteUInt16 (ushort value)
		{
			if (this.position + 2 > this.buffer.Length) this.Grow (2);

            this.buffer [this.position++] = (byte) value;
            this.buffer [this.position++] = (byte) (value >> 8);

			if (this.position > this.length) this.length = this.position;
		}

		public void WriteInt16 (short value)
		{
            this.WriteUInt16 ((ushort) value);
		}

		public void WriteUInt32 (uint value)
		{
			if (this.position + 4 > this.buffer.Length) this.Grow (4);

            this.buffer [this.position++] = (byte) value;
            this.buffer [this.position++] = (byte) (value >> 8);
            this.buffer [this.position++] = (byte) (value >> 16);
            this.buffer [this.position++] = (byte) (value >> 24);

			if (this.position > this.length) this.length = this.position;
		}

		public void WriteInt32 (int value)
		{
            this.WriteUInt32 ((uint) value);
		}

		public void WriteUInt64 (ulong value)
		{
			if (this.position + 8 > this.buffer.Length) this.Grow (8);

            this.buffer [this.position++] = (byte) value;
            this.buffer [this.position++] = (byte) (value >> 8);
            this.buffer [this.position++] = (byte) (value >> 16);
            this.buffer [this.position++] = (byte) (value >> 24);
            this.buffer [this.position++] = (byte) (value >> 32);
            this.buffer [this.position++] = (byte) (value >> 40);
            this.buffer [this.position++] = (byte) (value >> 48);
            this.buffer [this.position++] = (byte) (value >> 56);

			if (this.position > this.length) this.length = this.position;
		}

		public void WriteInt64 (long value)
		{
            this.WriteUInt64 ((ulong) value);
		}

		public void WriteCompressedUInt32 (uint value)
		{
			if (value < 0x80)
                this.WriteByte ((byte) value);
			else if (value < 0x4000) {
                this.WriteByte ((byte) (0x80 | (value >> 8)));
                this.WriteByte ((byte) (value & 0xff));
			} else {
                this.WriteByte ((byte) ((value >> 24) | 0xc0));
                this.WriteByte ((byte) ((value >> 16) & 0xff));
                this.WriteByte ((byte) ((value >> 8) & 0xff));
                this.WriteByte ((byte) (value & 0xff));
			}
		}

		public void WriteCompressedInt32 (int value)
		{
			if (value >= 0) {
                this.WriteCompressedUInt32 ((uint) (value << 1));
				return;
			}

			if (value > -0x40)
				value = 0x40 + value;
			else if (value >= -0x2000)
				value = 0x2000 + value;
			else if (value >= -0x20000000)
				value = 0x20000000 + value;

            this.WriteCompressedUInt32 ((uint) ((value << 1) | 1));
		}

		public void WriteBytes (byte [] bytes)
		{
			var length = bytes.Length;
			if (this.position + length > this.buffer.Length) this.Grow (length);

			Buffer.BlockCopy (bytes, 0, this.buffer, this.position, length);
            this.position += length;

			if (this.position > this.length)
				this.length = this.position;
		}

		public void WriteBytes (int length)
		{
			if (this.position + length > this.buffer.Length) this.Grow (length);

            this.position += length;

			if (this.position > this.length)
				this.length = this.position;
		}

		public void WriteBytes (ByteBuffer buffer)
		{
			if (this.position + buffer.length > this.buffer.Length) this.Grow (buffer.length);

			Buffer.BlockCopy (buffer.buffer, 0, this.buffer, this.position, buffer.length);
            this.position += buffer.length;

			if (this.position > this.length)
				this.length = this.position;
		}

		public void WriteSingle (float value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (!BitConverter.IsLittleEndian)
				Array.Reverse (bytes);

            this.WriteBytes (bytes);
		}

		public void WriteDouble (double value)
		{
			var bytes = BitConverter.GetBytes (value);

			if (!BitConverter.IsLittleEndian)
				Array.Reverse (bytes);

            this.WriteBytes (bytes);
		}

		void Grow (int desired)
		{
			var current = this.buffer;
			var current_length = current.Length;

			var buffer = new byte [Math.Max (current_length + desired, current_length * 2)];
			Buffer.BlockCopy (current, 0, buffer, 0, current_length);
			this.buffer = buffer;
		}

#endif

	}
}
