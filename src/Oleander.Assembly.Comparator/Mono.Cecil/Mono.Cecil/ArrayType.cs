//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using System.Text;
using Mono.Collections.Generic;
using MD = Mono.Cecil.Metadata;

namespace Mono.Cecil {

	public struct ArrayDimension {

		int? lower_bound;
		int? upper_bound;

		public int? LowerBound {
			get { return this.lower_bound; }
			set { this.lower_bound = value; }
		}

		public int? UpperBound {
			get { return this.upper_bound; }
			set { this.upper_bound = value; }
		}

		public bool IsSized {
			get { return this.lower_bound.HasValue || this.upper_bound.HasValue; }
		}

		public ArrayDimension (int? lowerBound, int? upperBound)
		{
			this.lower_bound = lowerBound;
			this.upper_bound = upperBound;
		}

		public override string ToString ()
		{
			return !this.IsSized
				? string.Empty
				: this.lower_bound + "..." + this.upper_bound;
		}
	}

	public sealed class ArrayType : TypeSpecification {

		Collection<ArrayDimension> dimensions;

		public Collection<ArrayDimension> Dimensions {
			get {
				if (this.dimensions != null)
					return this.dimensions;

                this.dimensions = new Collection<ArrayDimension> ();
                this.dimensions.Add (new ArrayDimension ());
				return this.dimensions;
			}
		}

		public int Rank {
			get { return this.dimensions == null ? 1 : this.dimensions.Count; }
		}

		public bool IsVector {
			get {
				if (this.dimensions == null)
					return true;

				if (this.dimensions.Count > 1)
					return false;

				var dimension = this.dimensions [0];

				return !dimension.IsSized;
			}
		}

		public override bool IsValueType {
			get { return false; }
			set { throw new InvalidOperationException (); }
		}

		public override string Name {
			get { return base.Name + this.Suffix; }
		}

		public override string FullName {
			get { return base.FullName + this.Suffix; }
		}

		/*Telerik Authorship*/
		public string Suffix {
			get {
				if (this.IsVector)
					return "[]";

				var suffix = new StringBuilder ();
				suffix.Append ("[");
				for (int i = 0; i < this.dimensions.Count; i++) {
					if (i > 0)
						suffix.Append (",");

					suffix.Append (this.dimensions [i].ToString ());
				}
				suffix.Append ("]");

				return suffix.ToString ();
			}
		}

		public override bool IsArray {
			get { return true; }
		}

		public ArrayType (TypeReference type)
			: base (type)
		{
			Mixin.CheckType (type);
			this.etype = MD.ElementType.Array;
		}

		public ArrayType (TypeReference type, int rank)
			: this (type)
		{
			Mixin.CheckType (type);

			if (rank == 1)
				return;

            this.dimensions = new Collection<ArrayDimension> (rank);
			for (int i = 0; i < rank; i++) this.dimensions.Add (new ArrayDimension ());
			this.etype = MD.ElementType.Array;
		}
	}
}
