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

namespace Oleander.Assembly.Comparers.Cecil.Cil {

	public abstract class VariableReference {

		string name;
		internal int index = -1;
		protected TypeReference variable_type;
		/*Telerik Authorship*/
		private bool variableNameChanged;

		/*Telerik Authorship*/
		public string Name {
			get
			{
				if (!string.IsNullOrEmpty(this.name))
					return this.name;

				if (this.index >= 0)
					return "V_" + this.index;

				return string.Empty;
			}
			set
			{
                this.name = value;
                this.variableNameChanged = true;
			}
		}

		public TypeReference VariableType {
			get { return this.variable_type; }
			set { this.variable_type = value; }
		}

		public int Index {
			get { return this.index; }
		}

		/*Telerik Authorship*/
		public bool VariableNameChanged
		{
			get
			{
				return this.variableNameChanged;
			}
		}

		internal VariableReference (TypeReference variable_type)
			: this (string.Empty, variable_type)
		{
		}

		internal VariableReference (string name, TypeReference variable_type)
		{
			this.name = name;
			this.variable_type = variable_type;
		}

		public abstract VariableDefinition Resolve ();

		/*Telerik Authorship*/
		public override string ToString ()
		{
			return this.Name;
		}
	}
}
