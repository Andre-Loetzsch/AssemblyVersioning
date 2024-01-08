//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Mono.Cecil.PE
{

    class BinaryStreamReader : BinaryReader
    {

        public BinaryStreamReader(Stream stream)
            : base(stream)
        {
        }

        protected void Advance(int bytes)
        {
            this.BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        protected DataDirectory ReadDataDirectory()
        {
            return new DataDirectory(this.ReadUInt32(), this.ReadUInt32());
        }
    }
}
