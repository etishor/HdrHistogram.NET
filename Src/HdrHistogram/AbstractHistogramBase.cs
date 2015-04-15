// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo

using System;

namespace HdrHistogram
{
    /// <summary>
    /// This non-public AbstractHistogramBase super-class separation is meant to bunch "cold" fields
    /// separately from "hot" fields, in an attempt to force the JVM to place the (hot) fields
    /// commonly used in the value recording code paths close together.
    /// Subclass boundaries tend to be strongly control memory layout decisions in most practical
    /// JVM implementations, making this an effective method for control filed grouping layout.
    /// </summary>
    public abstract class AbstractHistogramBase : EncodableHistogram
    {
        private static AtomicLong constructionIdentityCount = new AtomicLong(0);

        protected AbstractHistogramBase(int numberOfSignificantValueDigits, bool autoResize)
        {
            if ((numberOfSignificantValueDigits < 0) || (numberOfSignificantValueDigits > 5))
            {
                throw new ArgumentException("numberOfSignificantValueDigits must be between 0 and 5");
            }

            this.Identity = constructionIdentityCount.GetAndIncrement();
            this.NumberOfSignificantValueDigits = numberOfSignificantValueDigits;
            this.AutoResize = autoResize;
        }

        // "Cold" accessed fields. Not used in the recording code path:
        internal protected readonly long Identity;
        internal protected readonly int NumberOfSignificantValueDigits;
        protected readonly bool AutoResize;

        internal protected long highestTrackableValue;
        protected long lowestDiscernibleValue;

        protected internal int bucketCount;
        protected int subBucketCount;
        internal int countsArrayLength;
        protected int wordSizeInBytes;

        internal protected long startTimeStampMsec = long.MaxValue;
        internal protected long endTimeStampMsec = 0;

        internal protected double integerToDoubleValueConversionRatio = 1.0;

        protected PercentileIterator percentileIterator;
        protected RecordedValuesIterator recordedValuesIterator;
        protected ByteBuffer intermediateUncompressedByteBuffer = null;
    }
}