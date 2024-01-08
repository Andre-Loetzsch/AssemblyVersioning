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
using Oleander.Assembly.Comparers.Cecil.Collections.Generic;
using MD = Mono.Cecil.Metadata;

namespace Mono.Cecil
{

    public struct ArrayDimension
    {
        private int? _lowerBound;
        private int? _upperBound;

        public int? LowerBound
        {
            get => this._lowerBound;
            set => this._lowerBound = value;
        }

        public int? UpperBound
        {
            get => this._upperBound;
            set => this._upperBound = value;
        }

        public readonly bool IsSized => this._lowerBound.HasValue || this._upperBound.HasValue;

        public ArrayDimension(int? lowerBound, int? upperBound)
        {
            this._lowerBound = lowerBound;
            this._upperBound = upperBound;
        }

        public override string ToString()
        {
            return !this.IsSized
                ? string.Empty
                : this._lowerBound + "..." + this._upperBound;
        }
    }

    public sealed class ArrayType : TypeSpecification
    {
        private Collection<ArrayDimension> _dimensions;

        public Collection<ArrayDimension> Dimensions
        {
            get
            {
                if (this._dimensions != null)
                    return this._dimensions;

                this._dimensions = new Collection<ArrayDimension> { new ArrayDimension() };
                return this._dimensions;
            }
        }

        public int Rank => this._dimensions?.Count ?? 1;

        public bool IsVector
        {
            get
            {
                if (this._dimensions == null)
                    return true;

                if (this._dimensions.Count > 1)
                    return false;

                var dimension = this._dimensions[0];

                return !dimension.IsSized;
            }
        }

        public override bool IsValueType
        {
            get => false;
            set => throw new InvalidOperationException();
        }

        public override string Name => base.Name + this.Suffix;

        public override string FullName => base.FullName + this.Suffix;

        /*Telerik Authorship*/
        public string Suffix
        {
            get
            {
                if (this.IsVector)
                    return "[]";

                var suffix = new StringBuilder();
                suffix.Append('[');
                for (var i = 0; i < this._dimensions.Count; i++)
                {
                    if (i > 0)
                        suffix.Append(',');

                    suffix.Append(this._dimensions[i].ToString());
                }
                suffix.Append(']');

                return suffix.ToString();
            }
        }

        public override bool IsArray => true;

        public ArrayType(TypeReference type)
            : base(type)
        {
            Mixin.CheckType(type);
            this.etype = MD.ElementType.Array;
        }

        public ArrayType(TypeReference type, int rank)
            : this(type)
        {
            Mixin.CheckType(type);

            if (rank == 1)
                return;

            this._dimensions = new Collection<ArrayDimension>(rank);
            for (var i = 0; i < rank; i++) this._dimensions.Add(new ArrayDimension());
            this.etype = MD.ElementType.Array;
        }
    }
}
