//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Oleander.Assembly.Comparers.Cecil.Metadata {

	public struct MetadataToken {

		readonly uint token;

		public uint RID	{
			get { return this.token & 0x00ffffff; }
		}

		public TokenType TokenType {
			get { return (TokenType) (this.token & 0xff000000); }
		}

		public static readonly MetadataToken Zero = new MetadataToken ((uint) 0);

		public MetadataToken (uint token)
		{
			this.token = token;
		}

		public MetadataToken (TokenType type)
			: this (type, 0)
		{
		}

		public MetadataToken (TokenType type, uint rid)
		{
            this.token = (uint) type | rid;
		}

		public MetadataToken (TokenType type, int rid)
		{
            this.token = (uint) type | (uint) rid;
		}

		public int ToInt32 ()
		{
			return (int)this.token;
		}

		public uint ToUInt32 ()
		{
			return this.token;
		}

		public override int GetHashCode ()
		{
			return (int)this.token;
		}

		public override bool Equals (object obj)
		{
			if (obj is MetadataToken) {
				var other = (MetadataToken) obj;
				return other.token == this.token;
			}

			return false;
		}

		public static bool operator == (MetadataToken one, MetadataToken other)
		{
			return one.token == other.token;
		}

		public static bool operator != (MetadataToken one, MetadataToken other)
		{
			return one.token != other.token;
		}

		public override string ToString ()
		{
			return string.Format ("[{0}:0x{1}]", this.TokenType, this.RID.ToString ("x4"));
		}
	}
}
