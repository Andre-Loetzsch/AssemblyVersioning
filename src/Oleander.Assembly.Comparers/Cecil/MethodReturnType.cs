//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

/*Telerik Authorship*/

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using Oleander.Assembly.Comparers.Cecil.Metadata;

namespace Oleander.Assembly.Comparers.Cecil {

	public sealed class MethodReturnType : IConstantProvider, ICustomAttributeProvider, IMarshalInfoProvider {

		internal IMethodSignature method;
		internal ParameterDefinition parameter;
		TypeReference return_type;

		public IMethodSignature Method {
			get { return this.method; }
		}

		public TypeReference ReturnType {
			get { return this.return_type; }
			set { this.return_type = value; }
		}

		internal ParameterDefinition Parameter {
			get {
				if (this.parameter == null)
					Interlocked.CompareExchange (ref this.parameter, new ParameterDefinition (this.return_type, this.method), null);

				return this.parameter;
			}
		}

		public MetadataToken MetadataToken {
			get { return this.Parameter.MetadataToken; }
			set { this.Parameter.MetadataToken = value; }
		}

		public ParameterAttributes Attributes {
			get { return this.Parameter.Attributes; }
			set { this.Parameter.Attributes = value; }
		}

		public bool HasCustomAttributes {
			get { return this.parameter != null && this.parameter.HasCustomAttributes; }
		}

		public Collection<CustomAttribute> CustomAttributes {
			get { return this.Parameter.CustomAttributes; }
		}

		public bool HasDefault {
			get { return this.parameter != null && this.parameter.HasDefault; }
			set { this.Parameter.HasDefault = value; }
		}

		public bool HasConstant {
			get { return this.parameter != null && this.parameter.HasConstant; }
			set { this.Parameter.HasConstant = value; }
		}

		/*Telerik Authorship*/
		public ConstantValue Constant {
			get { return this.Parameter.Constant; }
			set { this.Parameter.Constant = value; }
		}

		public bool HasFieldMarshal {
			get { return this.parameter != null && this.parameter.HasFieldMarshal; }
			set { this.Parameter.HasFieldMarshal = value; }
		}

		public bool HasMarshalInfo {
			get { return this.parameter != null && this.parameter.HasMarshalInfo; }
		}

		public MarshalInfo MarshalInfo {
			get { return this.Parameter.MarshalInfo; }
			set { this.Parameter.MarshalInfo = value; }
		}

		public MethodReturnType (IMethodSignature method)
		{
			this.method = method;
		}
	}
}
