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

using Mono.Cecil.Cil;
using Mono.Cecil.Metadata;

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	sealed class ImageWriter : BinaryStreamWriter {

		readonly ModuleDefinition module;
		readonly MetadataBuilder metadata;
		readonly TextMap text_map;

		ImageDebugDirectory debug_directory;
		byte [] debug_data;

		ByteBuffer win32_resources;

		const uint pe_header_size = 0x98u;
		const uint section_header_size = 0x28u;
		const uint file_alignment = 0x200;
		const uint section_alignment = 0x2000;
		const ulong image_base = 0x00400000;

		internal const RVA text_rva = 0x2000;

		readonly bool pe64;
		readonly bool has_reloc;
		readonly uint time_stamp;

		internal Section text;
		internal Section rsrc;
		internal Section reloc;

		ushort sections;

		ImageWriter (ModuleDefinition module, MetadataBuilder metadata, Stream stream)
			: base (stream)
		{
			this.module = module;
			this.metadata = metadata;
			this.pe64 = module.Architecture == TargetArchitecture.AMD64 || module.Architecture == TargetArchitecture.IA64;
			this.has_reloc = module.Architecture == TargetArchitecture.I386;
			this.GetDebugHeader ();
			this.GetWin32Resources ();
			this.text_map = this.BuildTextMap ();
			this.sections = (ushort) (this.has_reloc ? 2 : 1); // text + reloc?
			this.time_stamp = (uint) DateTime.UtcNow.Subtract (new DateTime (1970, 1, 1)).TotalSeconds;
		}

		void GetDebugHeader ()
		{
			var symbol_writer = this.metadata.symbol_writer;
			if (symbol_writer == null)
				return;

			if (!symbol_writer.GetDebugHeader (out this.debug_directory, out this.debug_data)) this.debug_data = Empty<byte>.Array;
		}

		void GetWin32Resources ()
		{
			var rsrc = this.GetImageResourceSection ();
			if (rsrc == null)
				return;

			var raw_resources = new byte [rsrc.Data.Length];
			Buffer.BlockCopy (rsrc.Data, 0, raw_resources, 0, rsrc.Data.Length);
            this.win32_resources = new ByteBuffer (raw_resources);
		}

		Section GetImageResourceSection ()
		{
			if (!this.module.HasImage)
				return null;

			const string rsrc_section = ".rsrc";

			return this.module.Image.GetSection (rsrc_section);
		}

		public static ImageWriter CreateWriter (ModuleDefinition module, MetadataBuilder metadata, Stream stream)
		{
			var writer = new ImageWriter (module, metadata, stream);
			writer.BuildSections ();
			return writer;
		}

		void BuildSections ()
		{
			var has_win32_resources = this.win32_resources != null;
			if (has_win32_resources) this.sections++;

            this.text = this.CreateSection (".text", this.text_map.GetLength (), null);
			var previous = this.text;

			if (has_win32_resources) {
                this.rsrc = this.CreateSection (".rsrc", (uint)this.win32_resources.length, previous);

                this.PatchWin32Resources (this.win32_resources);
				previous = this.rsrc;
			}

			if (this.has_reloc) this.reloc = this.CreateSection (".reloc", 12u, previous);
		}

		Section CreateSection (string name, uint size, Section previous)
		{
			return new Section {
				Name = name,
				VirtualAddress = previous != null
					? previous.VirtualAddress + Align (previous.VirtualSize, section_alignment)
					: text_rva,
				VirtualSize = size,
				PointerToRawData = previous != null
					? previous.PointerToRawData + previous.SizeOfRawData
					: Align (this.GetHeaderSize (), file_alignment),
				SizeOfRawData = Align (size, file_alignment)
			};
		}

		static uint Align (uint value, uint align)
		{
			align--;
			return (value + align) & ~align;
		}

		void WriteDOSHeader ()
		{
            this.Write (new byte [] {
				// dos header start
				0x4d, 0x5a, 0x90, 0x00, 0x03, 0x00, 0x00,
				0x00, 0x04, 0x00, 0x00, 0x00, 0xff, 0xff,
				0x00, 0x00, 0xb8, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x40, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00, 0x00, 0x00, 0x00,
				// lfanew
				0x80, 0x00, 0x00, 0x00,
				// dos header end
				0x0e, 0x1f, 0xba, 0x0e, 0x00, 0xb4, 0x09,
				0xcd, 0x21, 0xb8, 0x01, 0x4c, 0xcd, 0x21,
				0x54, 0x68, 0x69, 0x73, 0x20, 0x70, 0x72,
				0x6f, 0x67, 0x72, 0x61, 0x6d, 0x20, 0x63,
				0x61, 0x6e, 0x6e, 0x6f, 0x74, 0x20, 0x62,
				0x65, 0x20, 0x72, 0x75, 0x6e, 0x20, 0x69,
				0x6e, 0x20, 0x44, 0x4f, 0x53, 0x20, 0x6d,
				0x6f, 0x64, 0x65, 0x2e, 0x0d, 0x0d, 0x0a,
				0x24, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
				0x00
			});
		}

		ushort SizeOfOptionalHeader ()
		{
			return (ushort) (!this.pe64 ? 0xe0 : 0xf0);
		}

		void WritePEFileHeader ()
		{
            this.WriteUInt32 (0x00004550);		// Magic
            this.WriteUInt16 (this.GetMachine ());	// Machine
            this.WriteUInt16 (this.sections);			// NumberOfSections
            this.WriteUInt32 (this.time_stamp);
            this.WriteUInt32 (0);	// PointerToSymbolTable
            this.WriteUInt32 (0);	// NumberOfSymbols
            this.WriteUInt16 (this.SizeOfOptionalHeader ());	// SizeOfOptionalHeader

			// ExecutableImage | (pe64 ? 32BitsMachine : LargeAddressAware)
			var characteristics = (ushort) (0x0002 | (!this.pe64 ? 0x0100 : 0x0020));
			if (this.module.Kind == ModuleKind.Dll || this.module.Kind == ModuleKind.NetModule)
				characteristics |= 0x2000;
            this.WriteUInt16 (characteristics);	// Characteristics
		}

		ushort GetMachine ()
		{
			switch (this.module.Architecture) {
			case TargetArchitecture.I386:
				return 0x014c;
			case TargetArchitecture.AMD64:
				return 0x8664;
			case TargetArchitecture.IA64:
				return 0x0200;
			case TargetArchitecture.ARMv7:
				return 0x01c4;
			}

			throw new NotSupportedException ();
		}

		Section LastSection ()
		{
			if (this.reloc != null)
				return this.reloc;

			if (this.rsrc != null)
				return this.rsrc;

			return this.text;
		}

		void WriteOptionalHeaders ()
		{
            this.WriteUInt16 ((ushort) (!this.pe64 ? 0x10b : 0x20b));	// Magic
            this.WriteByte (8);	// LMajor
            this.WriteByte (0);	// LMinor
            this.WriteUInt32 (this.text.SizeOfRawData);	// CodeSize
            this.WriteUInt32 ((this.reloc != null ? this.reloc.SizeOfRawData : 0)
                              + (this.rsrc != null ? this.rsrc.SizeOfRawData : 0));	// InitializedDataSize
            this.WriteUInt32 (0);	// UninitializedDataSize

			var startub_stub = this.text_map.GetRange (TextSegment.StartupStub);
            this.WriteUInt32 (startub_stub.Length > 0 ? startub_stub.Start : 0);  // EntryPointRVA
            this.WriteUInt32 (text_rva);	// BaseOfCode

			if (!this.pe64) {
                this.WriteUInt32 (0);	// BaseOfData
                this.WriteUInt32 ((uint) image_base);	// ImageBase
			} else {
                this.WriteUInt64 (image_base);	// ImageBase
			}

            this.WriteUInt32 (section_alignment);	// SectionAlignment
            this.WriteUInt32 (file_alignment);		// FileAlignment

            this.WriteUInt16 (4);	// OSMajor
            this.WriteUInt16 (0);	// OSMinor
            this.WriteUInt16 (0);	// UserMajor
            this.WriteUInt16 (0);	// UserMinor
            this.WriteUInt16 (4);	// SubSysMajor
            this.WriteUInt16 (0);	// SubSysMinor
            this.WriteUInt32 (0);	// Reserved

			var last_section = this.LastSection();
            this.WriteUInt32 (last_section.VirtualAddress + Align (last_section.VirtualSize, section_alignment));	// ImageSize
            this.WriteUInt32 (this.text.PointerToRawData);	// HeaderSize

            this.WriteUInt32 (0);	// Checksum
            this.WriteUInt16 (this.GetSubSystem ());	// SubSystem
            this.WriteUInt16 ((ushort)this.module.Characteristics);	// DLLFlags

			const ulong stack_reserve = 0x100000;
			const ulong stack_commit = 0x1000;
			const ulong heap_reserve = 0x100000;
			const ulong heap_commit = 0x1000;

			if (!this.pe64) {
                this.WriteUInt32 ((uint) stack_reserve);
                this.WriteUInt32 ((uint) stack_commit);
                this.WriteUInt32 ((uint) heap_reserve);
                this.WriteUInt32 ((uint) heap_commit);
			} else {
                this.WriteUInt64 (stack_reserve);
                this.WriteUInt64 (stack_commit);
                this.WriteUInt64 (heap_reserve);
                this.WriteUInt64 (heap_commit);
			}

            this.WriteUInt32 (0);	// LoaderFlags
            this.WriteUInt32 (16);	// NumberOfDataDir

            this.WriteZeroDataDirectory ();	// ExportTable
            this.WriteDataDirectory (this.text_map.GetDataDirectory (TextSegment.ImportDirectory));	// ImportTable
			if (this.rsrc != null) {							// ResourceTable
                this.WriteUInt32 (this.rsrc.VirtualAddress);
                this.WriteUInt32 (this.rsrc.VirtualSize);
			} else
                this.WriteZeroDataDirectory ();

            this.WriteZeroDataDirectory ();	// ExceptionTable
            this.WriteZeroDataDirectory ();	// CertificateTable
            this.WriteUInt32 (this.reloc != null ? this.reloc.VirtualAddress : 0);			// BaseRelocationTable
            this.WriteUInt32 (this.reloc != null ? this.reloc.VirtualSize : 0);

			if (this.text_map.GetLength (TextSegment.DebugDirectory) > 0) {
                this.WriteUInt32 (this.text_map.GetRVA (TextSegment.DebugDirectory));
                this.WriteUInt32 (28u);
			} else
                this.WriteZeroDataDirectory ();

            this.WriteZeroDataDirectory ();	// Copyright
            this.WriteZeroDataDirectory ();	// GlobalPtr
            this.WriteZeroDataDirectory ();	// TLSTable
            this.WriteZeroDataDirectory ();	// LoadConfigTable
            this.WriteZeroDataDirectory ();	// BoundImport
            this.WriteDataDirectory (this.text_map.GetDataDirectory (TextSegment.ImportAddressTable));	// IAT
            this.WriteZeroDataDirectory ();	// DelayImportDesc
            this.WriteDataDirectory (this.text_map.GetDataDirectory (TextSegment.CLIHeader));	// CLIHeader
            this.WriteZeroDataDirectory ();	// Reserved
		}

		void WriteZeroDataDirectory ()
		{
            this.WriteUInt32 (0);
            this.WriteUInt32 (0);
		}

		ushort GetSubSystem ()
		{
			switch (this.module.Kind) {
			case ModuleKind.Console:
			case ModuleKind.Dll:
			case ModuleKind.NetModule:
				return 0x3;
			case ModuleKind.Windows:
				return 0x2;
			default:
				throw new ArgumentOutOfRangeException ();
			}
		}

		void WriteSectionHeaders ()
		{
            this.WriteSection (this.text, 0x60000020);

			if (this.rsrc != null) this.WriteSection (this.rsrc, 0x40000040);

			if (this.reloc != null) this.WriteSection (this.reloc, 0x42000040);
		}

		void WriteSection (Section section, uint characteristics)
		{
			var name = new byte [8];
			var sect_name = section.Name;
			for (int i = 0; i < sect_name.Length; i++)
				name [i] = (byte) sect_name [i];

            this.WriteBytes (name);
            this.WriteUInt32 (section.VirtualSize);
            this.WriteUInt32 (section.VirtualAddress);
            this.WriteUInt32 (section.SizeOfRawData);
            this.WriteUInt32 (section.PointerToRawData);
            this.WriteUInt32 (0);	// PointerToRelocations
            this.WriteUInt32 (0);	// PointerToLineNumbers
            this.WriteUInt16 (0);	// NumberOfRelocations
            this.WriteUInt16 (0);	// NumberOfLineNumbers
            this.WriteUInt32 (characteristics);
		}

		void MoveTo (uint pointer)
		{
            this.BaseStream.Seek (pointer, SeekOrigin.Begin);
		}

		void MoveToRVA (Section section, RVA rva)
		{
            this.BaseStream.Seek (section.PointerToRawData + rva - section.VirtualAddress, SeekOrigin.Begin);
		}

		void MoveToRVA (TextSegment segment)
		{
            this.MoveToRVA (this.text, this.text_map.GetRVA (segment));
		}

		void WriteRVA (RVA rva)
		{
			if (!this.pe64)
                this.WriteUInt32 (rva);
			else
                this.WriteUInt64 (rva);
		}

		void PrepareSection (Section section)
		{
            this.MoveTo (section.PointerToRawData);

			const int buffer_size = 4096;

			if (section.SizeOfRawData <= buffer_size) {
                this.Write (new byte [section.SizeOfRawData]);
                this.MoveTo (section.PointerToRawData);
				return;
			}

			var written = 0;
			var buffer = new byte [buffer_size];
			while (written != section.SizeOfRawData) {
				var write_size = Math.Min((int) section.SizeOfRawData - written, buffer_size);
                this.Write (buffer, 0, write_size);
				written += write_size;
			}

            this.MoveTo (section.PointerToRawData);
		}

		void WriteText ()
		{
            this.PrepareSection (this.text);

			// ImportAddressTable

			if (this.has_reloc) {
                this.WriteRVA (this.text_map.GetRVA (TextSegment.ImportHintNameTable));
                this.WriteRVA (0);
			}

			// CLIHeader

            this.WriteUInt32 (0x48);
            this.WriteUInt16 (2);
            this.WriteUInt16 ((ushort) ((this.module.Runtime <= TargetRuntime.Net_1_1) ? 0 : 5));

            this.WriteUInt32 (this.text_map.GetRVA (TextSegment.MetadataHeader));
            this.WriteUInt32 (this.GetMetadataLength ());
            this.WriteUInt32 ((uint)this.module.Attributes);
            this.WriteUInt32 (this.metadata.entry_point.ToUInt32 ());
            this.WriteDataDirectory (this.text_map.GetDataDirectory (TextSegment.Resources));
            this.WriteDataDirectory (this.text_map.GetDataDirectory (TextSegment.StrongNameSignature));
            this.WriteZeroDataDirectory ();	// CodeManagerTable
            this.WriteZeroDataDirectory ();	// VTableFixups
            this.WriteZeroDataDirectory ();	// ExportAddressTableJumps
            this.WriteZeroDataDirectory ();	// ManagedNativeHeader

			// Code

            this.MoveToRVA (TextSegment.Code);
            this.WriteBuffer (this.metadata.code);

			// Resources

            this.MoveToRVA (TextSegment.Resources);
            this.WriteBuffer (this.metadata.resources);

			// Data

			if (this.metadata.data.length > 0) {
                this.MoveToRVA (TextSegment.Data);
                this.WriteBuffer (this.metadata.data);
			}

			// StrongNameSignature
			// stays blank

			// MetadataHeader

            this.MoveToRVA (TextSegment.MetadataHeader);
            this.WriteMetadataHeader ();

            this.WriteMetadata ();

			// DebugDirectory
			if (this.text_map.GetLength (TextSegment.DebugDirectory) > 0) {
                this.MoveToRVA (TextSegment.DebugDirectory);
                this.WriteDebugDirectory ();
			}

			if (!this.has_reloc)
				return;

			// ImportDirectory
            this.MoveToRVA (TextSegment.ImportDirectory);
            this.WriteImportDirectory ();

			// StartupStub
            this.MoveToRVA (TextSegment.StartupStub);
            this.WriteStartupStub ();
		}

		uint GetMetadataLength ()
		{
			return this.text_map.GetRVA (TextSegment.DebugDirectory) - this.text_map.GetRVA (TextSegment.MetadataHeader);
		}

		void WriteMetadataHeader ()
		{
            this.WriteUInt32 (0x424a5342);	// Signature
            this.WriteUInt16 (1);	// MajorVersion
            this.WriteUInt16 (1);	// MinorVersion
            this.WriteUInt32 (0);	// Reserved

			var version = GetZeroTerminatedString (this.module.runtime_version);
            this.WriteUInt32 ((uint) version.Length);
            this.WriteBytes (version);
            this.WriteUInt16 (0);	// Flags
            this.WriteUInt16 (this.GetStreamCount ());

			uint offset = this.text_map.GetRVA (TextSegment.TableHeap) - this.text_map.GetRVA (TextSegment.MetadataHeader);

            this.WriteStreamHeader (ref offset, TextSegment.TableHeap, "#~");
            this.WriteStreamHeader (ref offset, TextSegment.StringHeap, "#Strings");
            this.WriteStreamHeader (ref offset, TextSegment.UserStringHeap, "#US");
            this.WriteStreamHeader (ref offset, TextSegment.GuidHeap, "#GUID");
            this.WriteStreamHeader (ref offset, TextSegment.BlobHeap, "#Blob");
		}

		ushort GetStreamCount ()
		{
			return (ushort) (
				1	// #~
				+ 1	// #Strings
				+ (this.metadata.user_string_heap.IsEmpty ? 0 : 1)	// #US
				+ 1	// GUID
				+ (this.metadata.blob_heap.IsEmpty ? 0 : 1));	// #Blob
		}

		void WriteStreamHeader (ref uint offset, TextSegment heap, string name)
		{
			var length = (uint)this.text_map.GetLength (heap);
			if (length == 0)
				return;

            this.WriteUInt32 (offset);
            this.WriteUInt32 (length);
            this.WriteBytes (GetZeroTerminatedString (name));
			offset += length;
		}

		static byte [] GetZeroTerminatedString (string @string)
		{
			return GetString (@string, (@string.Length + 1 + 3) & ~3);
		}

		static byte [] GetSimpleString (string @string)
		{
			return GetString (@string, @string.Length);
		}

		static byte [] GetString (string @string, int length)
		{
			var bytes = new byte [length];
			for (int i = 0; i < @string.Length; i++)
				bytes [i] = (byte) @string [i];

			return bytes;
		}

		void WriteMetadata ()
		{
            this.WriteHeap (TextSegment.TableHeap, this.metadata.table_heap);
            this.WriteHeap (TextSegment.StringHeap, this.metadata.string_heap);
            this.WriteHeap (TextSegment.UserStringHeap, this.metadata.user_string_heap);
            this.WriteGuidHeap ();
            this.WriteHeap (TextSegment.BlobHeap, this.metadata.blob_heap);
		}

		void WriteHeap (TextSegment heap, HeapBuffer buffer)
		{
			if (buffer.IsEmpty)
				return;

            this.MoveToRVA (heap);
            this.WriteBuffer (buffer);
		}

		void WriteGuidHeap ()
		{
            this.MoveToRVA (TextSegment.GuidHeap);
            this.WriteBytes (this.module.Mvid.ToByteArray ());
		}

		void WriteDebugDirectory ()
		{
            this.WriteInt32 (this.debug_directory.Characteristics);
            this.WriteUInt32 (this.time_stamp);
            this.WriteInt16 (this.debug_directory.MajorVersion);
            this.WriteInt16 (this.debug_directory.MinorVersion);
            this.WriteInt32 (this.debug_directory.Type);
            this.WriteInt32 (this.debug_directory.SizeOfData);
            this.WriteInt32 (this.debug_directory.AddressOfRawData);
            this.WriteInt32 ((int)this.BaseStream.Position + 4);

            this.WriteBytes (this.debug_data);
		}

		void WriteImportDirectory ()
		{
            this.WriteUInt32 (this.text_map.GetRVA (TextSegment.ImportDirectory) + 40);	// ImportLookupTable
            this.WriteUInt32 (0);	// DateTimeStamp
            this.WriteUInt32 (0);	// ForwarderChain
            this.WriteUInt32 (this.text_map.GetRVA (TextSegment.ImportHintNameTable) + 14);
            this.WriteUInt32 (this.text_map.GetRVA (TextSegment.ImportAddressTable));
            this.Advance (20);

			// ImportLookupTable
            this.WriteUInt32 (this.text_map.GetRVA (TextSegment.ImportHintNameTable));

			// ImportHintNameTable
            this.MoveToRVA (TextSegment.ImportHintNameTable);

            this.WriteUInt16 (0);	// Hint
            this.WriteBytes (this.GetRuntimeMain ());
            this.WriteByte (0);
            this.WriteBytes (GetSimpleString ("mscoree.dll"));
            this.WriteUInt16 (0);
		}

		byte [] GetRuntimeMain ()
		{
			return this.module.Kind == ModuleKind.Dll || this.module.Kind == ModuleKind.NetModule
				? GetSimpleString ("_CorDllMain")
				: GetSimpleString ("_CorExeMain");
		}

		void WriteStartupStub ()
		{
			switch (this.module.Architecture) {
			case TargetArchitecture.I386:
                this.WriteUInt16 (0x25ff);
                this.WriteUInt32 ((uint) image_base + this.text_map.GetRVA (TextSegment.ImportAddressTable));
				return;
			default:
				throw new NotSupportedException ();
			}
		}

		void WriteRsrc ()
		{
            this.PrepareSection (this.rsrc);
            this.WriteBuffer (this.win32_resources);
		}

		void WriteReloc ()
		{
            this.PrepareSection (this.reloc);

			var reloc_rva = this.text_map.GetRVA (TextSegment.StartupStub);
			reloc_rva += this.module.Architecture == TargetArchitecture.IA64 ? 0x20u : 2;
			var page_rva = reloc_rva & ~0xfffu;

            this.WriteUInt32 (page_rva);	// PageRVA
            this.WriteUInt32 (0x000c);	// Block Size

			switch (this.module.Architecture) {
			case TargetArchitecture.I386:
                this.WriteUInt32 (0x3000 + reloc_rva - page_rva);
				break;
			default:
				throw new NotSupportedException();
			}
		}

		public void WriteImage ()
		{
            this.WriteDOSHeader ();
            this.WritePEFileHeader ();
            this.WriteOptionalHeaders ();
            this.WriteSectionHeaders ();
            this.WriteText ();
			if (this.rsrc != null) this.WriteRsrc ();
			if (this.reloc != null) this.WriteReloc ();
		}

		TextMap BuildTextMap ()
		{
			var map = this.metadata.text_map;

			map.AddMap (TextSegment.Code, this.metadata.code.length, !this.pe64 ? 4 : 16);
			map.AddMap (TextSegment.Resources, this.metadata.resources.length, 8);
			map.AddMap (TextSegment.Data, this.metadata.data.length, 4);
			if (this.metadata.data.length > 0) this.metadata.table_heap.FixupData (map.GetRVA (TextSegment.Data));
			map.AddMap (TextSegment.StrongNameSignature, this.GetStrongNameLength (), 4);

			map.AddMap (TextSegment.MetadataHeader, this.GetMetadataHeaderLength ());
			map.AddMap (TextSegment.TableHeap, this.metadata.table_heap.length, 4);
			map.AddMap (TextSegment.StringHeap, this.metadata.string_heap.length, 4);
			map.AddMap (TextSegment.UserStringHeap, this.metadata.user_string_heap.IsEmpty ? 0 : this.metadata.user_string_heap.length, 4);
			map.AddMap (TextSegment.GuidHeap, 16);
			map.AddMap (TextSegment.BlobHeap, this.metadata.blob_heap.IsEmpty ? 0 : this.metadata.blob_heap.length, 4);

			int debug_dir_len = 0;
			if (!this.debug_data.IsNullOrEmpty ()) {
				const int debug_dir_header_len = 28;

                this.debug_directory.AddressOfRawData = (int) map.GetNextRVA (TextSegment.BlobHeap) + debug_dir_header_len;
				debug_dir_len = this.debug_data.Length + debug_dir_header_len;
			}

			map.AddMap (TextSegment.DebugDirectory, debug_dir_len, 4);

			if (!this.has_reloc) {
				var start = map.GetNextRVA (TextSegment.DebugDirectory);
				map.AddMap (TextSegment.ImportDirectory, new Range (start, 0));
				map.AddMap (TextSegment.ImportHintNameTable, new Range (start, 0));
				map.AddMap (TextSegment.StartupStub, new Range (start, 0));
				return map;
			}

			RVA import_dir_rva = map.GetNextRVA (TextSegment.DebugDirectory);
			RVA import_hnt_rva = import_dir_rva + 48u;
			import_hnt_rva = (import_hnt_rva + 15u) & ~15u;
			uint import_dir_len = (import_hnt_rva - import_dir_rva) + 27u;

			RVA startup_stub_rva = import_dir_rva + import_dir_len;
			startup_stub_rva = this.module.Architecture == TargetArchitecture.IA64
				? (startup_stub_rva + 15u) & ~15u
				: 2 + ((startup_stub_rva + 3u) & ~3u);

			map.AddMap (TextSegment.ImportDirectory, new Range (import_dir_rva, import_dir_len));
			map.AddMap (TextSegment.ImportHintNameTable, new Range (import_hnt_rva, 0));
			map.AddMap (TextSegment.StartupStub, new Range (startup_stub_rva, this.GetStartupStubLength ()));

			return map;
		}

		uint GetStartupStubLength ()
		{
			switch (this.module.Architecture) {
			case TargetArchitecture.I386:
				return 6;
			default:
				throw new NotSupportedException ();
			}
		}

		int GetMetadataHeaderLength ()
		{
			return
				// MetadataHeader
				40
				// #~ header
				+ 12
				// #Strings header
				+ 20
				// #US header
				+ (this.metadata.user_string_heap.IsEmpty ? 0 : 12)
				// #GUID header
				+ 16
				// #Blob header
				+ (this.metadata.blob_heap.IsEmpty ? 0 : 16);
		}

		int GetStrongNameLength ()
		{
			if (this.module.Assembly == null)
				return 0;

			var public_key = this.module.Assembly.Name.PublicKey;
			if (public_key.IsNullOrEmpty ())
				return 0;

			// in fx 2.0 the key may be from 384 to 16384 bits
			// so we must calculate the signature size based on
			// the size of the public key (minus the 32 byte header)
			int size = public_key.Length;
			if (size > 32)
				return size - 32;

			// note: size == 16 for the ECMA "key" which is replaced
			// by the runtime with a 1024 bits key (128 bytes)

			return 128; // default strongname signature size
		}

		public DataDirectory GetStrongNameSignatureDirectory ()
		{
			return this.text_map.GetDataDirectory (TextSegment.StrongNameSignature);
		}

		public uint GetHeaderSize ()
		{
			return pe_header_size + this.SizeOfOptionalHeader () + (this.sections * section_header_size);
		}

		void PatchWin32Resources (ByteBuffer resources)
		{
            this.PatchResourceDirectoryTable (resources);
		}

		void PatchResourceDirectoryTable (ByteBuffer resources)
		{
			resources.Advance (12);

			var entries = resources.ReadUInt16 () + resources.ReadUInt16 ();

			for (int i = 0; i < entries; i++) this.PatchResourceDirectoryEntry (resources);
		}

		void PatchResourceDirectoryEntry (ByteBuffer resources)
		{
			resources.Advance (4);
			var child = resources.ReadUInt32 ();

			var position = resources.position;
			resources.position = (int) child & 0x7fffffff;

			if ((child & 0x80000000) != 0)
                this.PatchResourceDirectoryTable (resources);
			else
                this.PatchResourceDataEntry (resources);

			resources.position = position;
		}

		void PatchResourceDataEntry (ByteBuffer resources)
		{
			var old_rsrc = this.GetImageResourceSection ();
			var rva = resources.ReadUInt32 ();
			resources.position -= 4;
			resources.WriteUInt32 (rva - old_rsrc.VirtualAddress + this.rsrc.VirtualAddress);
		}
	}
}

#endif
