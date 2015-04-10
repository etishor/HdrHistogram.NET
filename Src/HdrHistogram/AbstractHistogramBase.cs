// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
namespace HdrHistogram
{
    public abstract class AbstractHistogramBase : EncodableHistogram
    {
        protected static AtomicLong constructionIdentityCount = new AtomicLong(0);

        // "Cold" accessed fields. Not used in the recording code path:
        protected internal long identity;
        protected volatile bool autoResize = false;

        protected long highestTrackableValue;
        protected long lowestDiscernibleValue;
        protected int numberOfSignificantValueDigits;

        protected internal int bucketCount;
        protected int subBucketCount;
        internal int countsArrayLength;
        protected int wordSizeInBytes;

        protected long startTimeStampMsec = long.MaxValue;
        protected long endTimeStampMsec = 0;

        protected double integerToDoubleValueConversionRatio = 1.0;

        protected PercentileIterator percentileIterator;
        protected RecordedValuesIterator recordedValuesIterator;
        protected ByteBuffer intermediateUncompressedByteBuffer = null;

        internal double getIntegerToDoubleValueConversionRatio()
        {
            return integerToDoubleValueConversionRatio;
        }

        protected void setIntegerToDoubleValueConversionRatio(double integerToDoubleValueConversionRatio)
        {
            this.integerToDoubleValueConversionRatio = integerToDoubleValueConversionRatio;
        }
    }
}