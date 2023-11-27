//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Cecil.PE;
using Mono.Collections.Generic;

using RVA = System.UInt32;

namespace Mono.Cecil.Cil {

	sealed class CodeReader : ByteBuffer {

		readonly internal MetadataReader reader;

		int start;
		Section code_section;

		MethodDefinition method;
		MethodBody body;

		int Offset {
			get { return this.position - this.start; }
		}

		public CodeReader (Section section, MetadataReader reader)
			: base (section.Data)
		{
			this.code_section = section;
			this.reader = reader;
		}

		public MethodBody ReadMethodBody (MethodDefinition method)
		{
			this.method = method;
			this.body = new MethodBody (method);

            this.reader.context = method;

            this.ReadMethodBody ();

			return this.body;
		}

		public void MoveTo (int rva)
		{
			if (!this.IsInSection (rva)) {
                this.code_section = this.reader.image.GetSectionAtVirtualAddress ((uint) rva);
                this.Reset (this.code_section.Data);
			}

			this.position = rva - (int)this.code_section.VirtualAddress;
		}

		bool IsInSection (int rva)
		{
			return this.code_section.VirtualAddress <= rva && rva < this.code_section.VirtualAddress + this.code_section.SizeOfRawData;
		}

		void ReadMethodBody ()
		{
            this.MoveTo (this.method.RVA);

			var flags = this.ReadByte ();
			switch (flags & 0x3) {
			case 0x2: // tiny
                this.body.code_size = flags >> 2;
                this.body.MaxStackSize = 8;
                this.ReadCode ();
				break;
			case 0x3: // fat
				this.position--;
                this.ReadFatMethod ();
				break;
			default:
				throw new InvalidOperationException ();
			}

			var symbol_reader = this.reader.module.symbol_reader;

			if (symbol_reader != null) {
				var instructions = this.body.Instructions;
				symbol_reader.Read (this.body, offset => GetInstruction (instructions, offset));
			}
		}

		void ReadFatMethod ()
		{
			var flags = this.ReadUInt16 ();
            this.body.max_stack_size = this.ReadUInt16 ();
            this.body.code_size = (int)this.ReadUInt32 ();
            this.body.local_var_token = new MetadataToken (this.ReadUInt32 ());
            this.body.init_locals = (flags & 0x10) != 0;

			if (this.body.local_var_token.RID != 0) this.body.variables = this.ReadVariables (this.body.local_var_token);

            this.ReadCode ();

			if ((flags & 0x8) != 0) this.ReadSection ();
		}

		public VariableDefinitionCollection ReadVariables (MetadataToken local_var_token)
		{
			var position = this.reader.position;
			var variables = this.reader.ReadVariables (local_var_token);
            this.reader.position = position;

			return variables;
		}

		void ReadCode ()
		{
            this.start = this.position;
			var code_size = this.body.code_size;

			if (code_size < 0 || this.buffer.Length <= (uint) (code_size + this.position))
				code_size = 0;

			var end = this.start + code_size;
			var instructions = this.body.instructions = new InstructionCollection ((code_size + 1) / 2);

			while (this.position < end) {
				var offset = this.position - this.start;
				var opcode = this.ReadOpCode ();
				var current = new Instruction (offset, opcode);

				if (opcode.OperandType != OperandType.InlineNone)
					current.operand = this.ReadOperand (current);

				instructions.Add (current);
			}

            this.ResolveBranches (instructions);
		}

		OpCode ReadOpCode ()
		{
			var il_opcode = this.ReadByte ();
			return il_opcode != 0xfe
				? OpCodes.OneByteOpCode [il_opcode]
				: OpCodes.TwoBytesOpCode [this.ReadByte ()];
		}

		object ReadOperand (Instruction instruction)
		{
			switch (instruction.opcode.OperandType) {
			case OperandType.InlineSwitch:
				var length = this.ReadInt32 ();
				var base_offset = this.Offset + (4 * length);
				var branches = new int [length];
				for (int i = 0; i < length; i++)
					branches [i] = base_offset + this.ReadInt32 ();
				return branches;
			case OperandType.ShortInlineBrTarget:
				return this.ReadSByte () + this.Offset;
			case OperandType.InlineBrTarget:
				return this.ReadInt32 () + this.Offset;
			case OperandType.ShortInlineI:
				if (instruction.opcode == OpCodes.Ldc_I4_S)
					return this.ReadSByte ();

				return this.ReadByte ();
			case OperandType.InlineI:
				return this.ReadInt32 ();
			case OperandType.ShortInlineR:
				return this.ReadSingle ();
			case OperandType.InlineR:
				return this.ReadDouble ();
			case OperandType.InlineI8:
				return this.ReadInt64 ();
			case OperandType.ShortInlineVar:
				return this.GetVariable (this.ReadByte ());
			case OperandType.InlineVar:
				return this.GetVariable (this.ReadUInt16 ());
			case OperandType.ShortInlineArg:
				return this.GetParameter (this.ReadByte ());
			case OperandType.InlineArg:
				return this.GetParameter (this.ReadUInt16 ());
			case OperandType.InlineSig:
				return this.GetCallSite (this.ReadToken ());
			case OperandType.InlineString:
				return this.GetString (this.ReadToken ());
			case OperandType.InlineTok:
			case OperandType.InlineType:
			case OperandType.InlineMethod:
			case OperandType.InlineField:
				return this.reader.LookupToken (this.ReadToken ());
			default:
				throw new NotSupportedException ();
			}
		}

		public string GetString (MetadataToken token)
		{
			return this.reader.image.UserStringHeap.Read (token.RID);
		}

		public ParameterDefinition GetParameter (int index)
		{
			return this.body.GetParameter (index);
		}

		public VariableDefinition GetVariable (int index)
		{
			return this.body.GetVariable (index);
		}

		public CallSite GetCallSite (MetadataToken token)
		{
			return this.reader.ReadCallSite (token);
		}

		void ResolveBranches (Collection<Instruction> instructions)
		{
			var items = instructions.items;
			var size = instructions.size;

			for (int i = 0; i < size; i++) {
				var instruction = items [i];
				switch (instruction.opcode.OperandType) {
				case OperandType.ShortInlineBrTarget:
				case OperandType.InlineBrTarget:
					instruction.operand = this.GetInstruction ((int) instruction.operand);
					break;
				case OperandType.InlineSwitch:
					var offsets = (int []) instruction.operand;
					var branches = new Instruction [offsets.Length];
					for (int j = 0; j < offsets.Length; j++)
						branches [j] = this.GetInstruction (offsets [j]);

					instruction.operand = branches;
					break;
				}
			}
		}

		Instruction GetInstruction (int offset)
		{
			return GetInstruction (this.body.Instructions, offset);
		}

		static Instruction GetInstruction (Collection<Instruction> instructions, int offset)
		{
			var size = instructions.size;
			var items = instructions.items;
			if (offset < 0 || offset > items [size - 1].offset)
				return null;

			int min = 0;
			int max = size - 1;
			while (min <= max) {
				int mid = min + ((max - min) / 2);
				var instruction = items [mid];
				var instruction_offset = instruction.offset;

				if (offset == instruction_offset)
					return instruction;

				if (offset < instruction_offset)
					max = mid - 1;
				else
					min = mid + 1;
			}

			return null;
		}

		void ReadSection ()
		{
            this.Align (4);

			const byte fat_format = 0x40;
			const byte more_sects = 0x80;

			var flags = this.ReadByte ();
			if ((flags & fat_format) == 0)
                this.ReadSmallSection ();
			else
                this.ReadFatSection ();

			if ((flags & more_sects) != 0) this.ReadSection ();
		}

		void ReadSmallSection ()
		{
			var count = this.ReadByte () / 12;
            this.Advance (2);

            this.ReadExceptionHandlers (
				count,
				() => (int)this.ReadUInt16 (),
				() => (int)this.ReadByte ());
		}

		void ReadFatSection ()
		{
            this.position--;
			var count = (this.ReadInt32 () >> 8) / 24;

            this.ReadExceptionHandlers (
				count, this.ReadInt32, this.ReadInt32);
		}

		// inline ?
		void ReadExceptionHandlers (int count, Func<int> read_entry, Func<int> read_length)
		{
			for (int i = 0; i < count; i++) {
				var handler = new ExceptionHandler (
					(ExceptionHandlerType) (read_entry () & 0x7));

				handler.TryStart = this.GetInstruction (read_entry ());
				handler.TryEnd = this.GetInstruction (handler.TryStart.Offset + read_length ());

				handler.HandlerStart = this.GetInstruction (read_entry ());
				handler.HandlerEnd = this.GetInstruction (handler.HandlerStart.Offset + read_length ());

                this.ReadExceptionHandlerSpecific (handler);

				this.body.ExceptionHandlers.Add (handler);
			}
		}

		void ReadExceptionHandlerSpecific (ExceptionHandler handler)
		{
			switch (handler.HandlerType) {
			case ExceptionHandlerType.Catch:
				handler.CatchType = (TypeReference)this.reader.LookupToken (this.ReadToken ());
				break;
			case ExceptionHandlerType.Filter:
				handler.FilterStart = this.GetInstruction (this.ReadInt32 ());
				break;
			default:
                this.Advance (4);
				break;
			}
		}

		void Align (int align)
		{
			align--;
            this.Advance (((this.position + align) & ~align) - this.position);
		}

		public MetadataToken ReadToken ()
		{
			return new MetadataToken (this.ReadUInt32 ());
		}

#if !READ_ONLY

		public ByteBuffer PatchRawMethodBody (MethodDefinition method, CodeWriter writer, out MethodSymbols symbols)
		{
			var buffer = new ByteBuffer ();
			symbols = new MethodSymbols (method.Name);

			this.method = method;
            this.reader.context = method;

            this.MoveTo (method.RVA);

			var flags = this.ReadByte ();

			MetadataToken local_var_token;

			switch (flags & 0x3) {
			case 0x2: // tiny
				buffer.WriteByte (flags);
				local_var_token = MetadataToken.Zero;
				symbols.code_size = flags >> 2;
                this.PatchRawCode (buffer, symbols.code_size, writer);
				break;
			case 0x3: // fat
				this.position--;

                this.PatchRawFatMethod (buffer, symbols, writer, out local_var_token);
				break;
			default:
				throw new NotSupportedException ();
			}

			var symbol_reader = this.reader.module.symbol_reader;
			if (symbol_reader != null && writer.metadata.write_symbols) {
				symbols.method_token = GetOriginalToken (writer.metadata, method);
				symbols.local_var_token = local_var_token;
				symbol_reader.Read (symbols);
			}

			return buffer;
		}

		void PatchRawFatMethod (ByteBuffer buffer, MethodSymbols symbols, CodeWriter writer, out MetadataToken local_var_token)
		{
			var flags = this.ReadUInt16 ();
			buffer.WriteUInt16 (flags);
			buffer.WriteUInt16 (this.ReadUInt16 ());
			symbols.code_size = this.ReadInt32 ();
			buffer.WriteInt32 (symbols.code_size);
			local_var_token = this.ReadToken ();

			if (local_var_token.RID > 0) {
				var variables = symbols.variables = this.ReadVariables (local_var_token);
				buffer.WriteUInt32 (variables != null
					? writer.GetStandAloneSignature (symbols.variables).ToUInt32 ()
					: 0);
			} else
				buffer.WriteUInt32 (0);

            this.PatchRawCode (buffer, symbols.code_size, writer);

			if ((flags & 0x8) != 0) this.PatchRawSection (buffer, writer.metadata);
		}

		/*Telerik Authorship*/
		public static MetadataToken GetOriginalToken(MetadataBuilder metadata, MethodDefinition method)
		{
			MetadataToken original;
			if (metadata.TryGetOriginalMethodToken(method.token, out original))
			{
				return original;
			}

			return MetadataToken.Zero;
		}

		/*Telerik Authorship*/
		public static MetadataToken GetOriginalLocalVarToken(MetadataBuilder metadata, MethodDefinition method)
		{
			MetadataToken original;
			if (metadata.TryGetOriginalMethodBodyLocalVarToken(method.token, out original))
			{
				return original;
			}

			return MetadataToken.Zero;
		}

		void PatchRawCode (ByteBuffer buffer, int code_size, CodeWriter writer)
		{
			var metadata = writer.metadata;
			buffer.WriteBytes (this.ReadBytes (code_size));
			var end = buffer.position;
			buffer.position -= code_size;

			while (buffer.position < end) {
				OpCode opcode;
				var il_opcode = buffer.ReadByte ();
				if (il_opcode != 0xfe) {
					opcode = OpCodes.OneByteOpCode [il_opcode];
				} else {
					var il_opcode2 = buffer.ReadByte ();
					opcode = OpCodes.TwoBytesOpCode [il_opcode2];
				}

				switch (opcode.OperandType) {
				case OperandType.ShortInlineI:
				case OperandType.ShortInlineBrTarget:
				case OperandType.ShortInlineVar:
				case OperandType.ShortInlineArg:
					buffer.position += 1;
					break;
				case OperandType.InlineVar:
				case OperandType.InlineArg:
					buffer.position += 2;
					break;
				case OperandType.InlineBrTarget:
				case OperandType.ShortInlineR:
				case OperandType.InlineI:
					buffer.position += 4;
					break;
				case OperandType.InlineI8:
				case OperandType.InlineR:
					buffer.position += 8;
					break;
				case OperandType.InlineSwitch:
					var length = buffer.ReadInt32 ();
					buffer.position += length * 4;
					break;
				case OperandType.InlineString:
					var @string = this.GetString (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (
						new MetadataToken (
							TokenType.String,
							metadata.user_string_heap.GetStringIndex (@string)).ToUInt32 ());
					break;
				case OperandType.InlineSig:
					var call_site = this.GetCallSite (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (writer.GetStandAloneSignature (call_site).ToUInt32 ());
					break;
				case OperandType.InlineTok:
				case OperandType.InlineType:
				case OperandType.InlineMethod:
				case OperandType.InlineField:
					var provider = this.reader.LookupToken (new MetadataToken (buffer.ReadUInt32 ()));
					buffer.position -= 4;
					buffer.WriteUInt32 (metadata.LookupToken (provider).ToUInt32 ());
					break;
				}
			}
		}

		void PatchRawSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
			var position = this.position;
            this.Align (4);
			buffer.WriteBytes (this.position - position);

			const byte fat_format = 0x40;
			const byte more_sects = 0x80;

			var flags = this.ReadByte ();
			if ((flags & fat_format) == 0) {
				buffer.WriteByte (flags);
                this.PatchRawSmallSection (buffer, metadata);
			} else
                this.PatchRawFatSection (buffer, metadata);

			if ((flags & more_sects) != 0) this.PatchRawSection (buffer, metadata);
		}

		void PatchRawSmallSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
			var length = this.ReadByte ();
			buffer.WriteByte (length);
            this.Advance (2);

			buffer.WriteUInt16 (0);

			var count = length / 12;

            this.PatchRawExceptionHandlers (buffer, metadata, count, false);
		}

		void PatchRawFatSection (ByteBuffer buffer, MetadataBuilder metadata)
		{
            this.position--;
			var length = this.ReadInt32 ();
			buffer.WriteInt32 (length);

			var count = (length >> 8) / 24;

            this.PatchRawExceptionHandlers (buffer, metadata, count, true);
		}

		void PatchRawExceptionHandlers (ByteBuffer buffer, MetadataBuilder metadata, int count, bool fat_entry)
		{
			const int fat_entry_size = 16;
			const int small_entry_size = 6;

			for (int i = 0; i < count; i++) {
				ExceptionHandlerType handler_type;
				if (fat_entry) {
					var type = this.ReadUInt32 ();
					handler_type = (ExceptionHandlerType) (type & 0x7);
					buffer.WriteUInt32 (type);
				} else {
					var type = this.ReadUInt16 ();
					handler_type = (ExceptionHandlerType) (type & 0x7);
					buffer.WriteUInt16 (type);
				}

				buffer.WriteBytes (this.ReadBytes (fat_entry ? fat_entry_size : small_entry_size));

				switch (handler_type) {
				case ExceptionHandlerType.Catch:
					var exception = this.reader.LookupToken (this.ReadToken ());
					buffer.WriteUInt32 (metadata.LookupToken (exception).ToUInt32 ());
					break;
				default:
					buffer.WriteUInt32 (this.ReadUInt32 ());
					break;
				}
			}
		}

#endif

	}
}
