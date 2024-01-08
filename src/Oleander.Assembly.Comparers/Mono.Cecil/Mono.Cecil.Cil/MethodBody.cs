//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Mono.Collections.Generic;

namespace Mono.Cecil.Cil {

	public sealed class MethodBody : IVariableDefinitionProvider {

		readonly internal MethodDefinition method;

		internal ParameterDefinition this_parameter;
		internal int max_stack_size;
		internal int code_size;
		internal bool init_locals;
		internal MetadataToken local_var_token;

		internal Collection<Instruction> instructions;
		internal Collection<ExceptionHandler> exceptions;
		internal Collection<VariableDefinition> variables;
		Scope scope;

		public MethodDefinition Method {
			get { return this.method; }
		}

		public int MaxStackSize {
			get { return this.max_stack_size; }
			set { this.max_stack_size = value; }
		}

		public int CodeSize {
			get { return this.code_size; }
		}

		public bool InitLocals {
			get { return this.init_locals; }
			set { this.init_locals = value; }
		}

		public MetadataToken LocalVarToken {
			get { return this.local_var_token; }
			set { this.local_var_token = value; }
		}

		public Collection<Instruction> Instructions {
			get { return this.instructions ?? (this.instructions = new InstructionCollection ()); }
		}

		public bool HasExceptionHandlers {
			get { return !this.exceptions.IsNullOrEmpty (); }
		}

		public Collection<ExceptionHandler> ExceptionHandlers {
			get { return this.exceptions ?? (this.exceptions = new Collection<ExceptionHandler> ()); }
		}

		public bool HasVariables {
			get { return !this.variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get { return this.variables ?? (this.variables = new VariableDefinitionCollection ()); }
		}

		public Scope Scope {
			get { return this.scope; }
			set { this.scope = value; }
		}

		public ParameterDefinition ThisParameter {
			get {
				if (this.method == null || this.method.DeclaringType == null)
					throw new NotSupportedException ();

				if (!this.method.HasThis)
					return null;

				if (this.this_parameter == null)
					Interlocked.CompareExchange (ref this.this_parameter, CreateThisParameter (this.method), null);

				return this.this_parameter;
			}
		}

		static ParameterDefinition CreateThisParameter (MethodDefinition method)
		{
			var declaring_type = method.DeclaringType;
			var type = declaring_type.IsValueType || declaring_type.IsPrimitive
				? new PointerType (declaring_type)
				: declaring_type as TypeReference;

			return new ParameterDefinition (type, method);
		}

		public MethodBody (MethodDefinition method)
		{
			this.method = method;
		}

		public ILProcessor GetILProcessor ()
		{
			return new ILProcessor (this);
		}
	}

	public interface IVariableDefinitionProvider {
		bool HasVariables { get; }
		Collection<VariableDefinition> Variables { get; }
	}

	class VariableDefinitionCollection : Collection<VariableDefinition> {

		internal VariableDefinitionCollection ()
		{
		}

		internal VariableDefinitionCollection (int capacity)
			: base (capacity)
		{
		}

		protected override void OnAdd (VariableDefinition item, int index)
		{
			item.index = index;
		}

		protected override void OnInsert (VariableDefinition item, int index)
		{
			item.index = index;

			for (int i = index; i < this.size; i++) this.items [i].index = i + 1;
		}

		protected override void OnSet (VariableDefinition item, int index)
		{
			item.index = index;
		}

		protected override void OnRemove (VariableDefinition item, int index)
		{
			item.index = -1;

			for (int i = index + 1; i < this.size; i++) this.items [i].index = i - 1;
		}
	}

	class InstructionCollection : Collection<Instruction> {

		internal InstructionCollection ()
		{
		}

		internal InstructionCollection (int capacity)
			: base (capacity)
		{
		}

		protected override void OnAdd (Instruction item, int index)
		{
			if (index == 0)
				return;

			var previous = this.items [index - 1];
			previous.next = item;
			item.previous = previous;
		}

		protected override void OnInsert (Instruction item, int index)
		{
			if (this.size == 0)
				return;

			var current = this.items [index];
			if (current == null) {
				var last = this.items [index - 1];
				last.next = item;
				item.previous = last;
				return;
			}

			var previous = current.previous;
			if (previous != null) {
				previous.next = item;
				item.previous = previous;
			}

			current.previous = item;
			item.next = current;
		}

		protected override void OnSet (Instruction item, int index)
		{
			var current = this.items [index];

			item.previous = current.previous;
			item.next = current.next;

			current.previous = null;
			current.next = null;
		}

		protected override void OnRemove (Instruction item, int index)
		{
			var previous = item.previous;
			if (previous != null)
				previous.next = item.next;

			var next = item.next;
			if (next != null)
				next.previous = item.previous;

			item.previous = null;
			item.next = null;
		}
	}
}
