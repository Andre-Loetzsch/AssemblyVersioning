//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Cecil;
using Mono.Cecil.PE;
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Oleander.Assembly.Comparers.Cecil.Cil {

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

	}
}
