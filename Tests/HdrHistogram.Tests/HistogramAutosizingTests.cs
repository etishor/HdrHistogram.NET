using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{

    public class HistogramAutosizingTests
    {
        private static readonly long highestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units

        [Fact]
        public void testHistogramAutoSizingEdges()
        {
            Histogram histogram = new Histogram(3);
            histogram.recordValue((1L << 62) - 1);
            Assert.Equal(52, histogram.bucketCount);
            Assert.Equal(54272, histogram.countsArrayLength);
            histogram.recordValue(long.MaxValue);
            Assert.Equal(53, histogram.bucketCount);
            Assert.Equal(55296, histogram.countsArrayLength);
        }

        // TODO: uncomment when ported

        //[Fact]
        //public void testDoubleHistogramAutoSizingEdges()
        //{
        //    DoubleHistogram histogram = new DoubleHistogram(3);
        //    histogram.recordValue(1);
        //    histogram.recordValue(1L << 48);
        //    histogram.recordValue((1L << 52) - 1);
        //    Assert.Equal(52, histogram.integerValuesHistogram.bucketCount);
        //    Assert.Equal(54272, histogram.integerValuesHistogram.countsArrayLength);
        //    histogram.recordValue((1L << 53) - 1);
        //    Assert.Equal(53, histogram.integerValuesHistogram.bucketCount);
        //    Assert.Equal(55296, histogram.integerValuesHistogram.countsArrayLength);

        //    DoubleHistogram histogram2 = new DoubleHistogram(2);
        //    histogram2.recordValue(1);
        //    histogram2.recordValue(1L << 48);
        //    histogram2.recordValue((1L << 54) - 1);
        //    Assert.Equal(55, histogram2.integerValuesHistogram.bucketCount);
        //    Assert.Equal(7168, histogram2.integerValuesHistogram.countsArrayLength);
        //    histogram2.recordValue((1L << 55) - 1);
        //    Assert.Equal(56, histogram2.integerValuesHistogram.bucketCount);
        //    Assert.Equal(7296, histogram2.integerValuesHistogram.countsArrayLength);
        //}

        [Fact]
        public void testHistogramAutoSizing()
        {
            Histogram histogram = new Histogram(3);
            for (int i = 0; i < 63; i++)
            {
                long value = 1L << i;
                histogram.recordValue(value);
            }
            Assert.Equal(53, histogram.bucketCount);
            Assert.Equal(55296, histogram.countsArrayLength);
        }

        [Fact]
        public void testConcurrentHistogramAutoSizing()
        {
            ConcurrentHistogram histogram = new ConcurrentHistogram(3);
            for (int i = 9; i < 63; i++)
            {
                long value = 1L << i;
                histogram.recordValue(value);
            }
        }

        //[Fact]
        //public void testSynchronizedHistogramAutoSizing()
        //{
        //    SynchronizedHistogram histogram = new SynchronizedHistogram(3);
        //    for (int i = 0; i < 63; i++)
        //    {
        //        long value = 1L << i;
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testIntCountsHistogramAutoSizing()
        //{
        //    IntCountsHistogram histogram = new IntCountsHistogram(3);
        //    for (int i = 0; i < 63; i++)
        //    {
        //        long value = 1L << i;
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testShortCountsHistogramAutoSizing()
        //{
        //    ShortCountsHistogram histogram = new ShortCountsHistogram(3);
        //    for (int i = 0; i < 63; i++)
        //    {
        //        long value = 1L << i;
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testDoubleHistogramAutoSizingUp()
        //{
        //    DoubleHistogram histogram = new DoubleHistogram(2);
        //    for (int i = 0; i < 55; i++)
        //    {
        //        double value = 1L << i;
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testDoubleHistogramAutoSizingDown()
        //{
        //    DoubleHistogram histogram = new DoubleHistogram(2);
        //    for (int i = 0; i < 56; i++)
        //    {
        //        double value = (1L << 45) * 1.0 / (1L << i);
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testConcurrentDoubleHistogramAutoSizingDown()
        //{
        //    ConcurrentDoubleHistogram histogram = new ConcurrentDoubleHistogram(2);
        //    for (int i = 0; i < 56; i++)
        //    {
        //        double value = (1L << 45) * 1.0 / (1L << i);
        //        histogram.recordValue(value);
        //    }
        //}

        //[Fact]
        //public void testSynchronizedDoubleHistogramAutoSizingDown()
        //{
        //    SynchronizedDoubleHistogram histogram = new SynchronizedDoubleHistogram(2);
        //    for (int i = 0; i < 56; i++)
        //    {
        //        double value = (1L << 45) * 1.0 / (1L << i);
        //        histogram.recordValue(value);
        //    }
        //}

        [Fact]
        public void testAutoSizingAdd()
        {
            Histogram histogram1 = new Histogram(2);
            Histogram histogram2 = new Histogram(2);

            histogram1.recordValue(1000L);
            histogram1.recordValue(1000000000L);

            histogram2.add(histogram1);

            histogram2.valuesAreEquivalent(histogram2.getMaxValue(), 1000000000L).Should().BeTrue("Max should be equivalent to 1000000000L");
        }

        [Fact]
        public void testAutoSizingAcrossContinuousRange()
        {
            Histogram histogram = new Histogram(2);

            for (long i = 0; i < 10000000L; i++)
            {
                histogram.recordValue(i);
            }
        }

        [Fact]
        public void testAutoSizingAcrossContinuousRangeConcurrent()
        {
            Histogram histogram = new ConcurrentHistogram(2);

            for (long i = 0; i < 1000000L; i++)
            {
                histogram.recordValue(i);
            }
        }

        //[Fact]
        //public void testAutoSizingAddDouble()
        //{
        //    DoubleHistogram histogram1 = new DoubleHistogram(2);
        //    DoubleHistogram histogram2 = new DoubleHistogram(2);

        //    histogram1.recordValue(1000L);
        //    histogram1.recordValue(1000000000L);

        //    histogram2.add(histogram1);

        //    Assert.assertTrue("Max should be equivalent to 1000000000L",
        //        histogram2.valuesAreEquivalent(histogram2.getMaxValue(), 1000000000L)
        //        );
        //}
    }
}
