// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
namespace HdrHistogram
{
    public static class MathUtils
    {
        public static int NumberOfLeadingZeros(long i)
        {
            if (i == 0)
                return 64;
            int n = 1;
            int x = (int)unchecked((long)((ulong)i >> 32));
            if (x == 0) { n += 32; x = (int)i; }
            if ((uint)x >> 16 == 0) { n += 16; x <<= 16; }
            if ((uint)x >> 24 == 0) { n += 8; x <<= 8; }
            if ((uint)x >> 28 == 0) { n += 4; x <<= 4; }
            if ((uint)x >> 30 == 0) { n += 2; x <<= 2; }
            n -= (int)((uint)x >> 31);
            return n;
        }
    }
}
