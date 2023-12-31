//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
//
// Licensed under the MIT/X11 license.
//

using Oleander.Assembly.Comparers.Cecil.Collections.Generic;

namespace Oleander.Assembly.Comparers.Cecil;

internal static class Empty<T>
{

    public static readonly T[] Array = System.Array.Empty<T>();
}

internal static partial class Mixin
{

    public static bool IsNullOrEmpty<T>(this T[] self)
    {
        return self == null || self.Length == 0;
    }

    public static bool IsNullOrEmpty<T>(this Collection<T> self)
    {
        return self == null || self.size == 0;
    }

    public static T[] Resize<T>(this T[] self, int length)
    {
        Array.Resize(ref self, length);
        return self;
    }
}