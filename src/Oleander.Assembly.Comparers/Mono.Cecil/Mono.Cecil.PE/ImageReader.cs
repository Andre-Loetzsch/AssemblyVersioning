//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Cecil.Metadata;

using RVA = System.UInt32;

namespace Mono.Cecil.PE {

	sealed class ImageReader : BinaryStreamReader {

		readonly Image image;

		DataDirectory cli;
		DataDirectory metadata;

		public ImageReader (Stream stream)
			: base (stream)
		{
            this.image = new Image ();

            this.image.FileName = stream.GetFullyQualifiedName ();
		}

		void MoveTo (DataDirectory directory)
		{
            this.BaseStream.Position = this.image.ResolveVirtualAddress (directory.VirtualAddress);
		}

		void MoveTo (uint position)
		{
            this.BaseStream.Position = position;
		}

		void ReadImage ()
		{
			if (this.BaseStream.Length < 128)
				throw new BadImageFormatException ();

			// - DOSHeader

			// PE					2
			// Start				58
			// Lfanew				4
			// End					64

			if (this.ReadUInt16 () != 0x5a4d)
				throw new BadImageFormatException ();

            this.Advance (58);

            this.MoveTo (this.ReadUInt32 ());

			if (this.ReadUInt32 () != 0x00004550)
				throw new BadImageFormatException ();

			// - PEFileHeader

			// Machine				2
            this.image.Architecture = this.ReadArchitecture ();

			// NumberOfSections		2
			ushort sections = this.ReadUInt16 ();

			/*Telerik Authorship*/
            this.image.TimeDateStampPosition = (int)this.BaseStream.Position; 
			// TimeDateStamp		4
			// PointerToSymbolTable	4
			// NumberOfSymbols		4
			// OptionalHeaderSize	2
            this.Advance (14);

			// Characteristics		2
			ushort characteristics = this.ReadUInt16 ();

			ushort subsystem, dll_characteristics;
            this.ReadOptionalHeaders (out subsystem, out dll_characteristics);
            this.ReadSections (sections);
            this.ReadCLIHeader ();
            this.ReadMetadata ();

            this.image.Kind = GetModuleKind (characteristics, subsystem);
            this.image.Characteristics = (ModuleCharacteristics) dll_characteristics;
		}

		TargetArchitecture ReadArchitecture ()
		{
			var machine = this.ReadUInt16 ();
			switch (machine) {
			case 0x014c:
				return TargetArchitecture.I386;
			case 0x8664:
				return TargetArchitecture.AMD64;
			case 0x0200:
				return TargetArchitecture.IA64;
			case 0x01c4:
				return TargetArchitecture.ARMv7;
			}

			throw new NotSupportedException ();
		}

		static ModuleKind GetModuleKind (ushort characteristics, ushort subsystem)
		{
			if ((characteristics & 0x2000) != 0) // ImageCharacteristics.Dll
				return ModuleKind.Dll;

			if (subsystem == 0x2 || subsystem == 0x9) // SubSystem.WindowsGui || SubSystem.WindowsCeGui
				return ModuleKind.Windows;

			return ModuleKind.Console;
		}

		void ReadOptionalHeaders (out ushort subsystem, out ushort dll_characteristics)
		{
			// - PEOptionalHeader
			//   - StandardFieldsHeader

			// Magic				2
			bool pe64 = this.ReadUInt16 () == 0x20b;

			//						pe32 || pe64

			// LMajor				1
			// LMinor				1
			// CodeSize				4
			// InitializedDataSize	4
			// UninitializedDataSize4
			// EntryPointRVA		4
			// BaseOfCode			4
			// BaseOfData			4 || 0

			//   - NTSpecificFieldsHeader

			// ImageBase			4 || 8
			// SectionAlignment		4
			// FileAlignement		4
			// OSMajor				2
			// OSMinor				2
			// UserMajor			2
			// UserMinor			2
			// SubSysMajor			2
			// SubSysMinor			2
			// Reserved				4
			// ImageSize			4
			// HeaderSize			4
			// FileChecksum			4
            this.Advance (66);

			/*Telerik Authorship*/
            this.image.FileChecksumPosition = (int)(this.BaseStream.Position - 4); 

			// SubSystem			2
			subsystem = this.ReadUInt16 ();

			// DLLFlags				2
			dll_characteristics = this.ReadUInt16 ();
			// StackReserveSize		4 || 8
			// StackCommitSize		4 || 8
			// HeapReserveSize		4 || 8
			// HeapCommitSize		4 || 8
			// LoaderFlags			4
			// NumberOfDataDir		4

			//   - DataDirectoriesHeader

			// ExportTable			8
			// ImportTable			8
			// ResourceTable		8
			// ExceptionTable		8
			// CertificateTable		8
			// BaseRelocationTable	8

            this.Advance (pe64 ? 88 : 72);

			// Debug				8
            this.image.Debug = this.ReadDataDirectory ();

			// Copyright			8
			// GlobalPtr			8
			// TLSTable				8
			// LoadConfigTable		8
			// BoundImport			8
			// IAT					8
			// DelayImportDescriptor8
            this.Advance (56);

			// CLIHeader			8
            this.cli = this.ReadDataDirectory ();

			if (this.cli.IsZero)
				throw new BadImageFormatException ();

			// Reserved				8
            this.Advance (8);
		}

		string ReadAlignedString (int length)
		{
			int read = 0;
			var buffer = new char [length];
			while (read < length) {
				var current = this.ReadByte ();
				if (current == 0)
					break;

				buffer [read++] = (char) current;
			}

            this.Advance (-1 + ((read + 4) & ~3) - read);

			return new string (buffer, 0, read);
		}

		string ReadZeroTerminatedString (int length)
		{
			int read = 0;
			var buffer = new char [length];
			var bytes = this.ReadBytes (length);
			while (read < length) {
				var current = bytes [read];
				if (current == 0)
					break;

				buffer [read++] = (char) current;
			}

			return new string (buffer, 0, read);
		}

		void ReadSections (ushort count)
		{
			var sections = new Section [count];

			for (int i = 0; i < count; i++) {
				var section = new Section ();

				// Name
				section.Name = this.ReadZeroTerminatedString (8);

				// VirtualSize		4
                this.Advance (4);

				// VirtualAddress	4
				section.VirtualAddress = this.ReadUInt32 ();
				// SizeOfRawData	4
				section.SizeOfRawData = this.ReadUInt32 ();
				// PointerToRawData	4
				section.PointerToRawData = this.ReadUInt32 ();

				// PointerToRelocations		4
				// PointerToLineNumbers		4
				// NumberOfRelocations		2
				// NumberOfLineNumbers		2
				// Characteristics			4
                this.Advance (16);

				sections [i] = section;

                this.ReadSectionData (section);
			}

            this.image.Sections = sections;
		}

		void ReadSectionData (Section section)
		{
			var position = this.BaseStream.Position;

            this.MoveTo (section.PointerToRawData);

			var length = (int) section.SizeOfRawData;
			var data = new byte [length];
			int offset = 0, read;

			while ((read = this.Read (data, offset, length - offset)) > 0)
				offset += read;

			section.Data = data;

            this.BaseStream.Position = position;
		}

		void ReadCLIHeader ()
		{
            this.MoveTo (this.cli);

			// - CLIHeader

			// Cb						4
			// MajorRuntimeVersion		2
			// MinorRuntimeVersion		2
            this.Advance (8);

			// Metadata					8
            this.metadata = this.ReadDataDirectory ();
			// Flags					4
            this.image.Attributes = (ModuleAttributes)this.ReadUInt32 ();
			// EntryPointToken			4
            this.image.EntryPointToken = this.ReadUInt32 ();
			// Resources				8
            this.image.Resources = this.ReadDataDirectory ();
			// StrongNameSignature		8
            this.image.StrongName = this.ReadDataDirectory ();
			// CodeManagerTable			8
			// VTableFixups				8
			// ExportAddressTableJumps	8
			// ManagedNativeHeader		8
		}

		void ReadMetadata ()
		{
            this.MoveTo (this.metadata);

			if (this.ReadUInt32 () != 0x424a5342)
				throw new BadImageFormatException ();

			// MajorVersion			2
			// MinorVersion			2
			// Reserved				4
            this.Advance (8);

            this.image.RuntimeVersion = this.ReadZeroTerminatedString (this.ReadInt32 ());

			// Flags		2
            this.Advance (2);

			var streams = this.ReadUInt16 ();

			var section = this.image.GetSectionAtVirtualAddress (this.metadata.VirtualAddress);
			if (section == null)
				throw new BadImageFormatException ();

            this.image.MetadataSection = section;

			for (int i = 0; i < streams; i++) this.ReadMetadataStream (section);

			if (this.image.TableHeap != null) this.ReadTableHeap ();
		}

		void ReadMetadataStream (Section section)
		{
			// Offset		4
			uint start = this.metadata.VirtualAddress - section.VirtualAddress + this.ReadUInt32 (); // relative to the section start

			// Size			4
			uint size = this.ReadUInt32 ();

			var name = this.ReadAlignedString (16);
			switch (name) {
			case "#~":
			case "#-":
                this.image.TableHeap = new TableHeap (section, start, size);
				break;
			case "#Strings":
                this.image.StringHeap = new StringHeap (section, start, size);
				break;
			case "#Blob":
                this.image.BlobHeap = new BlobHeap (section, start, size);
				break;
			case "#GUID":
                this.image.GuidHeap = new GuidHeap (section, start, size);
				break;
			case "#US":
                this.image.UserStringHeap = new UserStringHeap (section, start, size);
				break;
			}
		}

		void ReadTableHeap ()
		{
			var heap = this.image.TableHeap;

			uint start = heap.Section.PointerToRawData;

            this.MoveTo (heap.Offset + start);

			// Reserved			4
			// MajorVersion		1
			// MinorVersion		1
            this.Advance (6);

			// HeapSizes		1
			var sizes = this.ReadByte ();

			// Reserved2		1
            this.Advance (1);

			// Valid			8
			heap.Valid = this.ReadInt64 ();

			// Sorted			8
			heap.Sorted = this.ReadInt64 ();

			for (int i = 0; i < TableHeap.TableCount; i++) {
				if (!heap.HasTable ((Table) i))
					continue;

				heap.Tables [i].Length = this.ReadUInt32 ();
			}

			SetIndexSize (this.image.StringHeap, sizes, 0x1);
			SetIndexSize (this.image.GuidHeap, sizes, 0x2);
			SetIndexSize (this.image.BlobHeap, sizes, 0x4);

            this.ComputeTableInformations ();
		}

		static void SetIndexSize (Heap heap, uint sizes, byte flag)
		{
			if (heap == null)
				return;

			heap.IndexSize = (sizes & flag) > 0 ? 4 : 2;
		}

		int GetTableIndexSize (Table table)
		{
			return this.image.GetTableIndexSize (table);
		}

		int GetCodedIndexSize (CodedIndex index)
		{
			return this.image.GetCodedIndexSize (index);
		}

		void ComputeTableInformations ()
		{
			uint offset = (uint)this.BaseStream.Position - this.image.MetadataSection.PointerToRawData; // header

			int stridx_size = this.image.StringHeap.IndexSize;
			int blobidx_size = this.image.BlobHeap != null ? this.image.BlobHeap.IndexSize : 2;

			var heap = this.image.TableHeap;
			var tables = heap.Tables;

			for (int i = 0; i < TableHeap.TableCount; i++) {
				var table = (Table) i;
				if (!heap.HasTable (table))
					continue;

				int size;
				switch (table) {
				case Table.Module:
					size = 2	// Generation
						+ stridx_size	// Name
						+ (this.image.GuidHeap.IndexSize * 3);	// Mvid, EncId, EncBaseId
					break;
				case Table.TypeRef:
					size = this.GetCodedIndexSize (CodedIndex.ResolutionScope)	// ResolutionScope
						+ (stridx_size * 2);	// Name, Namespace
					break;
				case Table.TypeDef:
					size = 4	// Flags
						+ (stridx_size * 2)	// Name, Namespace
						+ this.GetCodedIndexSize (CodedIndex.TypeDefOrRef)	// BaseType
						+ this.GetTableIndexSize (Table.Field)	// FieldList
						+ this.GetTableIndexSize (Table.Method);	// MethodList
					break;
				case Table.FieldPtr:
					size = this.GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.Field:
					size = 2	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// Signature
					break;
				case Table.MethodPtr:
					size = this.GetTableIndexSize (Table.Method);	// Method
					break;
				case Table.Method:
					size = 8	// Rva 4, ImplFlags 2, Flags 2
						+ stridx_size	// Name
						+ blobidx_size	// Signature
						+ this.GetTableIndexSize (Table.Param); // ParamList
					break;
				case Table.ParamPtr:
					size = this.GetTableIndexSize (Table.Param); // Param
					break;
				case Table.Param:
					size = 4	// Flags 2, Sequence 2
						+ stridx_size;	// Name
					break;
				case Table.InterfaceImpl:
					size = this.GetTableIndexSize (Table.TypeDef)	// Class
						+ this.GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// Interface
					break;
				case Table.MemberRef:
					size = this.GetCodedIndexSize (CodedIndex.MemberRefParent)	// Class
						+ stridx_size	// Name
						+ blobidx_size;	// Signature
					break;
				case Table.Constant:
					size = 2	// Type
						+ this.GetCodedIndexSize (CodedIndex.HasConstant)	// Parent
						+ blobidx_size;	// Value
					break;
				case Table.CustomAttribute:
					size = this.GetCodedIndexSize (CodedIndex.HasCustomAttribute)	// Parent
						+ this.GetCodedIndexSize (CodedIndex.CustomAttributeType)	// Type
						+ blobidx_size;	// Value
					break;
				case Table.FieldMarshal:
					size = this.GetCodedIndexSize (CodedIndex.HasFieldMarshal)	// Parent
						+ blobidx_size;	// NativeType
					break;
				case Table.DeclSecurity:
					size = 2	// Action
						+ this.GetCodedIndexSize (CodedIndex.HasDeclSecurity)	// Parent
						+ blobidx_size;	// PermissionSet
					break;
				case Table.ClassLayout:
					size = 6	// PackingSize 2, ClassSize 4
						+ this.GetTableIndexSize (Table.TypeDef);	// Parent
					break;
				case Table.FieldLayout:
					size = 4	// Offset
						+ this.GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.StandAloneSig:
					size = blobidx_size;	// Signature
					break;
				case Table.EventMap:
					size = this.GetTableIndexSize (Table.TypeDef)	// Parent
						+ this.GetTableIndexSize (Table.Event);	// EventList
					break;
				case Table.EventPtr:
					size = this.GetTableIndexSize (Table.Event);	// Event
					break;
				case Table.Event:
					size = 2	// Flags
						+ stridx_size // Name
						+ this.GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// EventType
					break;
				case Table.PropertyMap:
					size = this.GetTableIndexSize (Table.TypeDef)	// Parent
						+ this.GetTableIndexSize (Table.Property);	// PropertyList
					break;
				case Table.PropertyPtr:
					size = this.GetTableIndexSize (Table.Property);	// Property
					break;
				case Table.Property:
					size = 2	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// Type
					break;
				case Table.MethodSemantics:
					size = 2	// Semantics
						+ this.GetTableIndexSize (Table.Method)	// Method
						+ this.GetCodedIndexSize (CodedIndex.HasSemantics);	// Association
					break;
				case Table.MethodImpl:
					size = this.GetTableIndexSize (Table.TypeDef)	// Class
						+ this.GetCodedIndexSize (CodedIndex.MethodDefOrRef)	// MethodBody
						+ this.GetCodedIndexSize (CodedIndex.MethodDefOrRef);	// MethodDeclaration
					break;
				case Table.ModuleRef:
					size = stridx_size;	// Name
					break;
				case Table.TypeSpec:
					size = blobidx_size;	// Signature
					break;
				case Table.ImplMap:
					size = 2	// MappingFlags
						+ this.GetCodedIndexSize (CodedIndex.MemberForwarded)	// MemberForwarded
						+ stridx_size	// ImportName
						+ this.GetTableIndexSize (Table.ModuleRef);	// ImportScope
					break;
				case Table.FieldRVA:
					size = 4	// RVA
						+ this.GetTableIndexSize (Table.Field);	// Field
					break;
				case Table.EncLog:
					size = 8;
					break;
				case Table.EncMap:
					size = 4;
					break;
				case Table.Assembly:
					size = 16 // HashAlgId 4, Version 4 * 2, Flags 4
						+ blobidx_size	// PublicKey
						+ (stridx_size * 2);	// Name, Culture
					break;
				case Table.AssemblyProcessor:
					size = 4;	// Processor
					break;
				case Table.AssemblyOS:
					size = 12;	// Platform 4, Version 2 * 4
					break;
				case Table.AssemblyRef:
					size = 12	// Version 2 * 4 + Flags 4
						+ (blobidx_size * 2)	// PublicKeyOrToken, HashValue
						+ (stridx_size * 2);	// Name, Culture
					break;
				case Table.AssemblyRefProcessor:
					size = 4	// Processor
						+ this.GetTableIndexSize (Table.AssemblyRef);	// AssemblyRef
					break;
				case Table.AssemblyRefOS:
					size = 12	// Platform 4, Version 2 * 4
						+ this.GetTableIndexSize (Table.AssemblyRef);	// AssemblyRef
					break;
				case Table.File:
					size = 4	// Flags
						+ stridx_size	// Name
						+ blobidx_size;	// HashValue
					break;
				case Table.ExportedType:
					size = 8	// Flags 4, TypeDefId 4
						+ (stridx_size * 2)	// Name, Namespace
						+ this.GetCodedIndexSize (CodedIndex.Implementation);	// Implementation
					break;
				case Table.ManifestResource:
					size = 8	// Offset, Flags
						+ stridx_size	// Name
						+ this.GetCodedIndexSize (CodedIndex.Implementation);	// Implementation
					break;
				case Table.NestedClass:
					size = this.GetTableIndexSize (Table.TypeDef)	// NestedClass
						+ this.GetTableIndexSize (Table.TypeDef);	// EnclosingClass
					break;
				case Table.GenericParam:
					size = 4	// Number, Flags
						+ this.GetCodedIndexSize (CodedIndex.TypeOrMethodDef)	// Owner
						+ stridx_size;	// Name
					break;
				case Table.MethodSpec:
					size = this.GetCodedIndexSize (CodedIndex.MethodDefOrRef)	// Method
						+ blobidx_size;	// Instantiation
					break;
				case Table.GenericParamConstraint:
					size = this.GetTableIndexSize (Table.GenericParam)	// Owner
						+ this.GetCodedIndexSize (CodedIndex.TypeDefOrRef);	// Constraint
					break;
				default:
					throw new NotSupportedException ();
				}

				tables [i].RowSize = (uint) size;
				tables [i].Offset = offset;

				offset += (uint) size * tables [i].Length;
			}
		}

		public static Image ReadImageFrom (Stream stream)
		{
			try {
				var reader = new ImageReader (stream);
				reader.ReadImage ();
				return reader.image;
			} catch (EndOfStreamException e) {
				throw new BadImageFormatException (stream.GetFullyQualifiedName (), e);
			}
		}
	}
}
