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

	public sealed class SequencePoint {

		Document document;

		int start_line;
		int start_column;
		int end_line;
		int end_column;

		public int StartLine {
			get { return this.start_line; }
			set { this.start_line = value; }
		}

		public int StartColumn {
			get { return this.start_column; }
			set { this.start_column = value; }
		}

		public int EndLine {
			get { return this.end_line; }
			set { this.end_line = value; }
		}

		public int EndColumn {
			get { return this.end_column; }
			set { this.end_column = value; }
		}

		public Document Document {
			get { return this.document; }
			set { this.document = value; }
		}

		public SequencePoint (Document document)
		{
			this.document = document;
		}
	}
}
