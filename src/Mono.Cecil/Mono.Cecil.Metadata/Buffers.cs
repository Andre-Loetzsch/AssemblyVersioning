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

using RVA = System.UInt32;

#if !READ_ONLY

namespace Mono.Cecil.Metadata {

	sealed class TableHeapBuffer : HeapBuffer {

		readonly ModuleDefinition module;
		readonly MetadataBuilder metadata;

		internal MetadataTable [] tables = new MetadataTable [45];

		bool large_string;
		bool large_blob;
		readonly int [] coded_index_sizes = new int [13];
		readonly Func<Table, int> counter;

		public override bool IsEmpty {
			get { return false; }
		}

		public TableHeapBuffer (ModuleDefinition module, MetadataBuilder metadata)
			: base (24)
		{
			this.module = module;
			this.metadata = metadata;
			this.counter = this.GetTableLength;
		}

		int GetTableLength (Table table)
		{
			var md_table = this.tables [(int) table];
			return md_table != null ? md_table.Length : 0;
		}

		public TTable GetTable<TTable> (Table table) where TTable : MetadataTable, new ()
		{
			var md_table = (TTable)this.tables [(int) table];
			if (md_table != null)
				return md_table;

			md_table = new TTable ();
            this.tables [(int) table] = md_table;
			return md_table;
		}

		public void WriteBySize (uint value, int size)
		{
			if (size == 4)
                this.WriteUInt32 (value);
			else
                this.WriteUInt16 ((ushort) value);
		}

		public void WriteBySize (uint value, bool large)
		{
			if (large)
                this.WriteUInt32 (value);
			else
                this.WriteUInt16 ((ushort) value);
		}

		public void WriteString (uint @string)
		{
            this.WriteBySize (@string, this.large_string);
		}

		public void WriteBlob (uint blob)
		{
            this.WriteBySize (blob, this.large_blob);
		}

		public void WriteRID (uint rid, Table table)
		{
			var md_table = this.tables [(int) table];
            this.WriteBySize (rid, md_table == null ? false : md_table.IsLarge);
		}

		int GetCodedIndexSize (CodedIndex coded_index)
		{
			var index = (int) coded_index;
			var size = this.coded_index_sizes [index];
			if (size != 0)
				return size;

			return this.coded_index_sizes [index] = coded_index.GetSize (this.counter);
		}

		public void WriteCodedRID (uint rid, CodedIndex coded_index)
		{
            this.WriteBySize (rid, this.GetCodedIndexSize (coded_index));
		}

		public void WriteTableHeap ()
		{
            this.WriteUInt32 (0);					// Reserved
            this.WriteByte (this.GetTableHeapVersion ());	// MajorVersion
            this.WriteByte (0);						// MinorVersion
            this.WriteByte (this.GetHeapSizes ());		// HeapSizes
            this.WriteByte (10);						// Reserved2
            this.WriteUInt64 (this.GetValid ());			// Valid
            this.WriteUInt64 (0x0016003301fa00);		// Sorted

            this.WriteRowCount ();
            this.WriteTables ();
		}

		void WriteRowCount ()
		{
			for (int i = 0; i < this.tables.Length; i++) {
				var table = this.tables [i];
				if (table == null || table.Length == 0)
					continue;

                this.WriteUInt32 ((uint) table.Length);
			}
		}

		void WriteTables ()
		{
			for (int i = 0; i < this.tables.Length; i++) {
				var table = this.tables [i];
				if (table == null || table.Length == 0)
					continue;

				table.Write (this);
			}
		}

		ulong GetValid ()
		{
			ulong valid = 0;

			for (int i = 0; i < this.tables.Length; i++) {
				var table = this.tables [i];
				if (table == null || table.Length == 0)
					continue;

				table.Sort ();
				valid |= (1UL << i);
			}

			return valid;
		}

		byte GetHeapSizes ()
		{
			byte heap_sizes = 0;

			if (this.metadata.string_heap.IsLarge) {
                this.large_string = true;
				heap_sizes |= 0x01;
			}

			if (this.metadata.blob_heap.IsLarge) {
                this.large_blob = true;
				heap_sizes |= 0x04;
			}

			return heap_sizes;
		}

		byte GetTableHeapVersion ()
		{
			switch (this.module.Runtime) {
			case TargetRuntime.Net_1_0:
			case TargetRuntime.Net_1_1:
				return 1;
			default:
				return 2;
			}
		}

		public void FixupData (RVA data_rva)
		{
			var table = this.GetTable<FieldRVATable> (Table.FieldRVA);
			if (table.length == 0)
				return;

			var field_idx_size = this.GetTable<FieldTable> (Table.Field).IsLarge ? 4 : 2;
			var previous = this.position;

			this.position = table.position;
			for (int i = 0; i < table.length; i++) {
				var rva = this.ReadUInt32 ();
				this.position -= 4;
                this.WriteUInt32 (rva + data_rva);
				this.position += field_idx_size;
			}

			this.position = previous;
		}
	}

	sealed class ResourceBuffer : ByteBuffer {

		public ResourceBuffer ()
			: base (0)
		{
		}

		public uint AddResource (byte [] resource)
		{
			var offset = (uint) this.position;
            this.WriteInt32 (resource.Length);
            this.WriteBytes (resource);
			return offset;
		}
	}

	sealed class DataBuffer : ByteBuffer {

		public DataBuffer ()
			: base (0)
		{
		}

		public RVA AddData (byte [] data)
		{
			var rva = (RVA)this.position;
            this.WriteBytes (data);
			return rva;
		}
	}

	abstract class HeapBuffer : ByteBuffer {

		public bool IsLarge {
			get { return this.length > 65535; }
		}

		public abstract bool IsEmpty { get; }

		protected HeapBuffer (int length)
			: base (length)
		{
		}
	}

	class StringHeapBuffer : HeapBuffer {

		readonly Dictionary<string, uint> strings = new Dictionary<string, uint> (StringComparer.Ordinal);

		public sealed override bool IsEmpty {
			get { return this.length <= 1; }
		}

		public StringHeapBuffer ()
			: base (1)
		{
            this.WriteByte (0);
		}

		public uint GetStringIndex (string @string)
		{
			uint index;
			if (this.strings.TryGetValue (@string, out index))
				return index;

			index = (uint) this.position;
            this.WriteString (@string);
            this.strings.Add (@string, index);
			return index;
		}

		protected virtual void WriteString (string @string)
		{
            this.WriteBytes (Encoding.UTF8.GetBytes (@string));
            this.WriteByte (0);
		}
	}

	sealed class BlobHeapBuffer : HeapBuffer {

		readonly Dictionary<ByteBuffer, uint> blobs = new Dictionary<ByteBuffer, uint> (new ByteBufferEqualityComparer ());

		public override bool IsEmpty {
			get { return this.length <= 1; }
		}

		public BlobHeapBuffer ()
			: base (1)
		{
            this.WriteByte (0);
		}

		public uint GetBlobIndex (ByteBuffer blob)
		{
			uint index;
			if (this.blobs.TryGetValue (blob, out index))
				return index;

			index = (uint) this.position;
            this.WriteBlob (blob);
            this.blobs.Add (blob, index);
			return index;
		}

		void WriteBlob (ByteBuffer blob)
		{
            this.WriteCompressedUInt32 ((uint) blob.length);
            this.WriteBytes (blob);
		}
	}

	sealed class UserStringHeapBuffer : StringHeapBuffer {

		protected override void WriteString (string @string)
		{
            this.WriteCompressedUInt32 ((uint) @string.Length * 2 + 1);

			byte special = 0;

			for (int i = 0; i < @string.Length; i++) {
				var @char = @string [i];
                this.WriteUInt16 (@char);

				if (special == 1)
					continue;

				if (@char < 0x20 || @char > 0x7e) {
					if (@char > 0x7e
						|| (@char >= 0x01 && @char <= 0x08)
						|| (@char >= 0x0e && @char <= 0x1f)
						|| @char == 0x27
						|| @char == 0x2d) {

						special = 1;
					}
				}
			}

            this.WriteByte (special);
		}
	}
}

#endif
