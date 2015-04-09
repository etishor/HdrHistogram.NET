namespace HdrHistogram
{
    public abstract class AbstractHistogramBase //: EncodableHistogram 
    {
        protected static AtomicLong constructionIdentityCount = new AtomicLong(0);

        // "Cold" accessed fields. Not used in the recording code path:
        protected long identity;
        protected volatile bool autoResize = false;

        protected long highestTrackableValue;
        protected long lowestDiscernibleValue;
        protected int numberOfSignificantValueDigits;

        protected int bucketCount;
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