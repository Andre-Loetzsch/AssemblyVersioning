//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Mono.Cecil.Cil {

	public enum ExceptionHandlerType {
		Catch = 0,
		Filter = 1,
		Finally = 2,
		Fault = 4,
	}

	public sealed class ExceptionHandler {

		Instruction try_start;
		Instruction try_end;
		Instruction filter_start;
		Instruction handler_start;
		Instruction handler_end;

		TypeReference catch_type;
		ExceptionHandlerType handler_type;

		public Instruction TryStart {
			get { return this.try_start; }
			set { this.try_start = value; }
		}

		public Instruction TryEnd {
			get { return this.try_end; }
			set { this.try_end = value; }
		}

		public Instruction FilterStart {
			get { return this.filter_start; }
			set { this.filter_start = value; }
		}

		/*Telerik Authorship*/
		public Instruction FilterEnd {
			get { return this.handler_start; }
			set { this.handler_start = value; }
		}

		public Instruction HandlerStart {
			get { return this.handler_start; }
			set { this.handler_start = value; }
		}

		public Instruction HandlerEnd {
			get { return this.handler_end; }
			set { this.handler_end = value; }
		}

		public TypeReference CatchType {
			get { return this.catch_type; }
			set { this.catch_type = value; }
		}

		public ExceptionHandlerType HandlerType {
			get { return this.handler_type; }
			set { this.handler_type = value; }
		}

		public ExceptionHandler (ExceptionHandlerType handlerType)
		{
			this.handler_type = handlerType;
		}
	}
}
