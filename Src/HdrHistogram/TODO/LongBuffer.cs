﻿// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HdrHistogram
{
    class LongBuffer
    {
        internal long get()
        {
            throw new NotImplementedException();
        }

        internal void get(long[] counts, int p, int length)
        {
            throw new NotImplementedException();
        }

        internal void rewind()
        {
            throw new NotImplementedException();
        }

        internal void put(long[] counts, int zeroIndex, int lengthFromZeroIndexToEnd)
        {
            throw new NotImplementedException();
        }
    }
}
