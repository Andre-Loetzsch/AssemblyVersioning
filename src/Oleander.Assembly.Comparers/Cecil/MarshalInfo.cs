//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

namespace Oleander.Assembly.Comparers.Cecil {

	public class MarshalInfo {

		internal NativeType native;

		public NativeType NativeType {
			get { return this.native; }
			set { this.native = value; }
		}

		public MarshalInfo (NativeType native)
		{
			this.native = native;
		}
	}

	public sealed class ArrayMarshalInfo : MarshalInfo {

		internal NativeType element_type;
		internal int size_parameter_index;
		internal int size;
		internal int size_parameter_multiplier;

		public NativeType ElementType {
			get { return this.element_type; }
			set { this.element_type = value; }
		}

		public int SizeParameterIndex {
			get { return this.size_parameter_index; }
			set { this.size_parameter_index = value; }
		}

		public int Size {
			get { return this.size; }
			set { this.size = value; }
		}

		public int SizeParameterMultiplier {
			get { return this.size_parameter_multiplier; }
			set { this.size_parameter_multiplier = value; }
		}

		public ArrayMarshalInfo ()
			: base (NativeType.Array)
		{
            this.element_type = NativeType.None;
            this.size_parameter_index = -1;
            this.size = -1;
            this.size_parameter_multiplier = -1;
		}
	}

	public sealed class CustomMarshalInfo : MarshalInfo {

		internal Guid guid;
		internal string unmanaged_type;
		internal TypeReference managed_type;
		internal string cookie;

		public Guid Guid {
			get { return this.guid; }
			set { this.guid = value; }
		}

		public string UnmanagedType {
			get { return this.unmanaged_type; }
			set { this.unmanaged_type = value; }
		}

		public TypeReference ManagedType {
			get { return this.managed_type; }
			set { this.managed_type = value; }
		}

		public string Cookie {
			get { return this.cookie; }
			set { this.cookie = value; }
		}

		public CustomMarshalInfo ()
			: base (NativeType.CustomMarshaler)
		{
		}
	}

	public sealed class SafeArrayMarshalInfo : MarshalInfo {

		internal VariantType element_type;

		public VariantType ElementType {
			get { return this.element_type; }
			set { this.element_type = value; }
		}

		public SafeArrayMarshalInfo ()
			: base (NativeType.SafeArray)
		{
            this.element_type = VariantType.None;
		}
	}

	public sealed class FixedArrayMarshalInfo : MarshalInfo {

		internal NativeType element_type;
		internal int size;

		public NativeType ElementType {
			get { return this.element_type; }
			set { this.element_type = value; }
		}

		public int Size {
			get { return this.size; }
			set { this.size = value; }
		}

		public FixedArrayMarshalInfo ()
			: base (NativeType.FixedArray)
		{
            this.element_type = NativeType.None;
		}
	}

	public sealed class FixedSysStringMarshalInfo : MarshalInfo {

		internal int size;

		public int Size {
			get { return this.size; }
			set { this.size = value; }
		}

		public FixedSysStringMarshalInfo ()
			: base (NativeType.FixedSysString)
		{
            this.size = -1;
		}
	}
}
