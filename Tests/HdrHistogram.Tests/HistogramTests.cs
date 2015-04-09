// /**
// *
// * Written by Gil Tene of Azul Systems, and released to the public domain,
// * as explained at http://creativecommons.org/publicdomain/zero/1.0/
// *
// * Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// * Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// * Latest ported version is available in the Java submodule in the root of the repo
// *
// **/
// 
using System;
using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{
    /**
* HistogramTest.java
* Written by Gil Tene of Azul Systems, and released to the public domain,
* as explained at http://creativecommons.org/publicdomain/zero/1.0/
*
* @author Gil Tene
*/

    public class HistogramTests
    {
        private static readonly long highestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        private static readonly int numberOfSignificantValueDigits = 3;
        private static readonly long testValueLevel = 4;

        private static void verifyMaxValue(AbstractHistogram histogram)
        {
            long computedMaxValue = 0;
            for (int i = 0; i < histogram.countsArrayLength; i++)
            {
                if (histogram.getCountAtIndex(i) > 0)
                {
                    computedMaxValue = histogram.valueFromIndex(i);
                }
            }
            computedMaxValue = (computedMaxValue == 0) ? 0 : histogram.highestEquivalentValue(computedMaxValue);
            Assert.Equal(computedMaxValue, histogram.getMaxValue());
        }

        [Fact]
        public void testConstructionArgumentRanges()
        {
            bool thrown = false;
            Histogram histogram = null;

            try
            {
                // This should throw:
                histogram = new Histogram(1, numberOfSignificantValueDigits);
            }
            catch (ArgumentException e)
            {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.Equal(histogram, null);

            thrown = false;
            try
            {
                // This should throw:
                histogram = new Histogram(highestTrackableValue, 6);
            }
            catch (ArgumentException e)
            {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.Equal(histogram, null);

            thrown = false;
            try
            {
                // This should throw:
                histogram = new Histogram(highestTrackableValue, -1);
            }
            catch (ArgumentException e)
            {
                thrown = true;
            }
            Assert.True(thrown);
            Assert.Equal(histogram, null);
        }

        [Fact]
        public void testEmptyHistogram()
        {
            Histogram histogram = new Histogram(3);
            long min = histogram.getMinValue();
            Assert.Equal(0, min);
            long max = histogram.getMaxValue();
            Assert.Equal(0, max);
            histogram.getMean().Should().BeApproximately(0, 0.0000000000001D);
            histogram.getStdDeviation().Should().BeApproximately(0, 0.0000000000001D);
            histogram.getPercentileAtOrBelowValue(0).Should().BeApproximately(100, 0.0000000000001D);
        }

        [Fact]
        public void testConstructionArgumentGets()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(1, histogram.getLowestDiscernibleValue());
            Assert.Equal(highestTrackableValue, histogram.getHighestTrackableValue());
            Assert.Equal(numberOfSignificantValueDigits, histogram.getNumberOfSignificantValueDigits());
            Histogram histogram2 = new Histogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
            Assert.Equal(1000, histogram2.getLowestDiscernibleValue());
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testGetEstimatedFootprintInBytes()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            /*
            *     largestValueWithSingleUnitResolution = 2 * (10 ^ numberOfSignificantValueDigits);
            *     subBucketSize = roundedUpToNearestPowerOf2(largestValueWithSingleUnitResolution);

            *     expectedHistogramFootprintInBytes = 512 +
            *          ({primitive type size} / 2) *
            *          (log2RoundedUp((trackableValueRangeSize) / subBucketSize) + 2) *
            *          subBucketSize
            */
            long largestValueWithSingleUnitResolution = 2 * (long)Math.Pow(10, numberOfSignificantValueDigits);
            int subBucketCountMagnitude = (int)Math.Ceiling(Math.Log(largestValueWithSingleUnitResolution) / Math.Log(2));
            int subBucketSize = (int)Math.Pow(2, (subBucketCountMagnitude));

            long expectedSize = 512 +
                                ((8 *
                                  ((long)(
                                      Math.Ceiling(
                                          Math.Log(highestTrackableValue / subBucketSize)
                                          / Math.Log(2)
                                          )
                                      + 2)) *
                                  (1 << (64 - MathUtils.NumberOfLeadingZeros(2 * (long)Math.Pow(10, numberOfSignificantValueDigits))))
                                    ) / 2);
            Assert.Equal(expectedSize, histogram.getEstimatedFootprintInBytes());
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testRecordValue()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            Assert.Equal(1L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(1L, histogram.getTotalCount());
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testRecordValue_Overflow_ShouldThrowException()
        {
            Action action = () =>
            {
                Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
                histogram.recordValue(highestTrackableValue * 3);
            };

            action.ShouldThrow<IndexOutOfRangeException>();
        }

        [Fact]
        public void testConstructionWithLargeNumbers()
        {
            Histogram histogram = new Histogram(20000000, 100000000, 5);
            histogram.recordValue(100000000);
            histogram.recordValue(20000000);
            histogram.recordValue(30000000);
            Assert.True(histogram.valuesAreEquivalent(20000000, histogram.getValueAtPercentile(50.0)));
            Assert.True(histogram.valuesAreEquivalent(30000000, histogram.getValueAtPercentile(83.33)));
            Assert.True(histogram.valuesAreEquivalent(100000000, histogram.getValueAtPercentile(83.34)));
            Assert.True(histogram.valuesAreEquivalent(100000000, histogram.getValueAtPercentile(99.0)));
        }




        [Fact]
        public void testRecordValueWithExpectedInterval()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.recordValueWithExpectedInterval(testValueLevel, testValueLevel / 4);
            Histogram rawHistogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            rawHistogram.recordValue(testValueLevel);
            // The data will include corrected samples:
            Assert.Equal(1L, histogram.getCountAtValue((testValueLevel * 1) / 4));
            Assert.Equal(1L, histogram.getCountAtValue((testValueLevel * 2) / 4));
            Assert.Equal(1L, histogram.getCountAtValue((testValueLevel * 3) / 4));
            Assert.Equal(1L, histogram.getCountAtValue((testValueLevel * 4) / 4));
            Assert.Equal(4L, histogram.getTotalCount());
            // But the raw data will not:
            Assert.Equal(0L, rawHistogram.getCountAtValue((testValueLevel * 1) / 4));
            Assert.Equal(0L, rawHistogram.getCountAtValue((testValueLevel * 2) / 4));
            Assert.Equal(0L, rawHistogram.getCountAtValue((testValueLevel * 3) / 4));
            Assert.Equal(1L, rawHistogram.getCountAtValue((testValueLevel * 4) / 4));
            Assert.Equal(1L, rawHistogram.getTotalCount());

            verifyMaxValue(histogram);
        }

        [Fact]
        public void testReset()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.reset();
            Assert.Equal(0L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(0L, histogram.getTotalCount());
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testAdd()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            Histogram other = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.recordValue(testValueLevel * 1000);
            other.recordValue(testValueLevel);
            other.recordValue(testValueLevel * 1000);
            histogram.add(other);
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(4L, histogram.getTotalCount());

            Histogram biggerOther = new Histogram(highestTrackableValue * 2, numberOfSignificantValueDigits);
            biggerOther.recordValue(testValueLevel);
            biggerOther.recordValue(testValueLevel * 1000);
            biggerOther.recordValue(highestTrackableValue * 2);

            // Adding the smaller histogram to the bigger one should work:
            biggerOther.add(histogram);
            Assert.Equal(3L, biggerOther.getCountAtValue(testValueLevel));
            Assert.Equal(3L, biggerOther.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(1L, biggerOther.getCountAtValue(highestTrackableValue * 2)); // overflow smaller hist...
            Assert.Equal(7L, biggerOther.getTotalCount());

            // But trying to add a larger histogram into a smaller one should throw an AIOOB:
            bool thrown = false;
            try
            {
                // This should throw:
                histogram.add(biggerOther);
            }
            catch (IndexOutOfRangeException e)
            {
                thrown = true;
            }
            Assert.True(thrown);

            verifyMaxValue(histogram);
            verifyMaxValue(other);
            verifyMaxValue(biggerOther);
        }

        [Fact]
        public void testSubtract()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            Histogram other = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.recordValue(testValueLevel * 1000);
            other.recordValue(testValueLevel);
            other.recordValue(testValueLevel * 1000);
            histogram.add(other);
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(4L, histogram.getTotalCount());
            histogram.add(other);
            Assert.Equal(3L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(3L, histogram.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(6L, histogram.getTotalCount());
            histogram.subtract(other);
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(2L, histogram.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(4L, histogram.getTotalCount());
            // Subtracting down to zero counts should work:
            histogram.subtract(histogram);
            Assert.Equal(0L, histogram.getCountAtValue(testValueLevel));
            Assert.Equal(0L, histogram.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(0L, histogram.getTotalCount());
            // But subtracting down to negative counts should not:
            bool thrown = false;
            try
            {
                // This should throw:
                histogram.subtract(other);
            }
            catch (ArgumentException e)
            {
                thrown = true;
            }
            Assert.True(thrown);


            Histogram biggerOther = new Histogram(highestTrackableValue * 2, numberOfSignificantValueDigits);
            biggerOther.recordValue(testValueLevel);
            biggerOther.recordValue(testValueLevel * 1000);
            biggerOther.recordValue(highestTrackableValue * 2);
            biggerOther.add(biggerOther);
            biggerOther.add(biggerOther);
            Assert.Equal(4L, biggerOther.getCountAtValue(testValueLevel));
            Assert.Equal(4L, biggerOther.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(4L, biggerOther.getCountAtValue(highestTrackableValue * 2)); // overflow smaller hist...
            Assert.Equal(12L, biggerOther.getTotalCount());

            // Subtracting the smaller histogram from the bigger one should work:
            biggerOther.subtract(other);
            Assert.Equal(3L, biggerOther.getCountAtValue(testValueLevel));
            Assert.Equal(3L, biggerOther.getCountAtValue(testValueLevel * 1000));
            Assert.Equal(4L, biggerOther.getCountAtValue(highestTrackableValue * 2)); // overflow smaller hist...
            Assert.Equal(10L, biggerOther.getTotalCount());

            // But trying to subtract a larger histogram into a smaller one should throw an AIOOB:
            thrown = false;
            try
            {
                // This should throw:
                histogram.subtract(biggerOther);
            }
            catch (IndexOutOfRangeException e)
            {
                thrown = true;
            }
            Assert.True(thrown);

            verifyMaxValue(histogram);
            verifyMaxValue(other);
            verifyMaxValue(biggerOther);
        }


        [Fact]
        public void testSizeOfEquivalentValueRange()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.sizeOfEquivalentValueRange(1).Should().Be(1, "Size of equivalent range for value 1 is 1");
            histogram.sizeOfEquivalentValueRange(2500).Should().Be(2, "Size of equivalent range for value 2500 is 2");
            histogram.sizeOfEquivalentValueRange(8191).Should().Be(4, "Size of equivalent range for value 8191 is 4");
            histogram.sizeOfEquivalentValueRange(8192).Should().Be(8, "Size of equivalent range for value 8192 is 8");
            histogram.sizeOfEquivalentValueRange(10000).Should().Be(8, "Size of equivalent range for value 10000 is 8");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testScaledSizeOfEquivalentValueRange()
        {
            Histogram histogram = new Histogram(1024, highestTrackableValue, numberOfSignificantValueDigits);
            histogram.sizeOfEquivalentValueRange(1 * 1024).Should().Be(1 * 1024, "Size of equivalent range for value 1 * 1024 is 1 * 1024");
            histogram.sizeOfEquivalentValueRange(2500 * 1024).Should().Be(2 * 1024, "Size of equivalent range for value 2500 * 1024 is 2 * 1024");
            histogram.sizeOfEquivalentValueRange(8191 * 1024).Should().Be(4 * 1024, "Size of equivalent range for value 8191 * 1024 is 4 * 1024");
            histogram.sizeOfEquivalentValueRange(8192 * 1024).Should().Be(8 * 1024, "Size of equivalent range for value 8192 * 1024 is 8 * 1024");
            histogram.sizeOfEquivalentValueRange(10000 * 1024).Should().Be(8 * 1024, "Size of equivalent range for value 10000 * 1024 is 8 * 1024");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testLowestEquivalentValue()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.lowestEquivalentValue(10007).Should().Be(10000, "The lowest equivalent value to 10007 is 10000");
            histogram.lowestEquivalentValue(10009).Should().Be(10008, "The lowest equivalent value to 10009 is 10008");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testScaledLowestEquivalentValue()
        {
            Histogram histogram = new Histogram(1024, highestTrackableValue, numberOfSignificantValueDigits);
            histogram.lowestEquivalentValue(10007 * 1024).Should().Be(10000 * 1024, "The lowest equivalent value to 10007 * 1024 is 10000 * 1024");
            histogram.lowestEquivalentValue(10009 * 1024).Should().Be(10008 * 1024, "The lowest equivalent value to 10009 * 1024 is 10008 * 1024");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testHighestEquivalentValue()
        {
            Histogram histogram = new Histogram(1024, highestTrackableValue, numberOfSignificantValueDigits);
            histogram.highestEquivalentValue(8180 * 1024).Should().Be(8183 * 1024 + 1023, "The highest equivalent value to 8180 * 1024 is 8183 * 1024 + 1023");

            histogram.highestEquivalentValue(8191 * 1024).Should().Be(8191 * 1024 + 1023, "The highest equivalent value to 8187 * 1024 is 8191 * 1024 + 1023");
            histogram.highestEquivalentValue(8193 * 1024).Should().Be(8199 * 1024 + 1023, "The highest equivalent value to 8193 * 1024 is 8199 * 1024 + 1023");
            histogram.highestEquivalentValue(9995 * 1024).Should().Be(9999 * 1024 + 1023, "The highest equivalent value to 9995 * 1024 is 9999 * 1024 + 1023");
            histogram.highestEquivalentValue(10007 * 1024).Should().Be(10007 * 1024 + 1023, "The highest equivalent value to 10007 * 1024 is 10007 * 1024 + 1023");
            histogram.highestEquivalentValue(10008 * 1024).Should().Be(10015 * 1024 + 1023, "The highest equivalent value to 10008 * 1024 is 10015 * 1024 + 1023");

            verifyMaxValue(histogram);
        }


        [Fact]
        public void testScaledHighestEquivalentValue()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.highestEquivalentValue(8180).Should().Be(8183, "The highest equivalent value to 8180 is 8183");
            histogram.highestEquivalentValue(8191).Should().Be(8191, "The highest equivalent value to 8187 is 8191");
            histogram.highestEquivalentValue(8193).Should().Be(8199, "The highest equivalent value to 8193 is 8199");
            histogram.highestEquivalentValue(9995).Should().Be(9999, "The highest equivalent value to 9995 is 9999");
            histogram.highestEquivalentValue(10007).Should().Be(10007, "The highest equivalent value to 10007 is 10007");
            histogram.highestEquivalentValue(10008).Should().Be(10015, "The highest equivalent value to 10008 is 10015");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testMedianEquivalentValue()
        {
            Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            histogram.medianEquivalentValue(4).Should().Be(4, "The median equivalent value to 4 is 4");
            histogram.medianEquivalentValue(5).Should().Be(5, "The median equivalent value to 5 is 5");
            histogram.medianEquivalentValue(4000).Should().Be(4001, "The median equivalent value to 4000 is 4001");
            histogram.medianEquivalentValue(8000).Should().Be(8002, "The median equivalent value to 8000 is 8002");
            histogram.medianEquivalentValue(10007).Should().Be(10004, "The median equivalent value to 10007 is 10004");
            verifyMaxValue(histogram);
        }

        [Fact]
        public void testScaledMedianEquivalentValue()
        {
            Histogram histogram = new Histogram(1024, highestTrackableValue, numberOfSignificantValueDigits);
            histogram.medianEquivalentValue(4 * 1024).Should().Be(4 * 1024 + 512, "The median equivalent value to 4 * 1024 is 4 * 1024 + 512");
            histogram.medianEquivalentValue(5 * 1024).Should().Be(5 * 1024 + 512, "The median equivalent value to 5 * 1024 is 5 * 1024 + 512");
            histogram.medianEquivalentValue(4000 * 1024).Should().Be(4001 * 1024, "The median equivalent value to 4000 * 1024 is 4001 * 1024");
            histogram.medianEquivalentValue(8000 * 1024).Should().Be(8002 * 1024, "The median equivalent value to 8000 * 1024 is 8002 * 1024");
            histogram.medianEquivalentValue(10007 * 1024).Should().Be(10004 * 1024, "The median equivalent value to 10007 * 1024 is 10004 * 1024");
            verifyMaxValue(histogram);
        }

        //    [Fact]
        //    public void testNextNonEquivalentValue() {
        //        Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        Assert.assertNotSame(null, histogram);
        //    }

        //    void testAbstractSerialization(AbstractHistogram histogram){
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 255);
        //        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        //        ObjectOutput out = null;
        //        ByteArrayInputStream bis = null;
        //        ObjectInput in = null;
        //        AbstractHistogram newHistogram = null;
        //        try {
        //            out = new ObjectOutputStream(bos);
        //            out.writeObject(histogram);
        //            Deflater compresser = new Deflater();
        //            compresser.setInput(bos.toByteArray());
        //            compresser.finish();
        //            byte [] compressedOutput = new byte[1024*1024];
        //            int compressedDataLength = compresser.deflate(compressedOutput);
        //            System.out.println("Serialized form of " + histogram.getClass() + " with trackableValueRangeSize = " +
        //                    histogram.getHighestTrackableValue() + "\n and a numberOfSignificantValueDigits = " +
        //                    histogram.getNumberOfSignificantValueDigits() + " is " + bos.toByteArray().length +
        //                    " bytes long. Compressed form is " + compressedDataLength + " bytes long.");
        //            System.out.println("   (estimated footprint was " + histogram.getEstimatedFootprintInBytes() + " bytes)");
        //            bis = new ByteArrayInputStream(bos.toByteArray());
        //            in = new ObjectInputStream(bis);
        //            newHistogram = (AbstractHistogram) in.readObject();
        //        } finally {
        //            if (out != null) out.close();
        //            bos.close();
        //            if (in !=null) in.close();
        //            if (bis != null) bis.close();
        //        }
        //        Assert.assertNotNull(newHistogram);
        //        assertEqual(histogram, newHistogram);
        //    }

        //    private void assertEqual(AbstractHistogram expectedHistogram, AbstractHistogram actualHistogram) {
        //        Assert.Equal(expectedHistogram, actualHistogram);
        //        Assert.Equal(
        //                expectedHistogram.getCountAtValue(testValueLevel),
        //                actualHistogram.getCountAtValue(testValueLevel));
        //        Assert.Equal(
        //                expectedHistogram.getCountAtValue(testValueLevel * 10),
        //                actualHistogram.getCountAtValue(testValueLevel * 10));
        //        Assert.Equal(
        //                expectedHistogram.getTotalCount(),
        //                actualHistogram.getTotalCount());
        //        verifyMaxValue(expectedHistogram);
        //        verifyMaxValue(actualHistogram);
        //    }

        //    [Fact]
        //    public void testSerialization(){
        //        Histogram histogram = new Histogram(highestTrackableValue, 3);
        //        testAbstractSerialization(histogram);
        //        IntCountsHistogram intCountsHistogram = new IntCountsHistogram(highestTrackableValue, 3);
        //        testAbstractSerialization(intCountsHistogram);
        //        ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, 3);
        //        testAbstractSerialization(shortCountsHistogram);
        //        histogram = new Histogram(highestTrackableValue, 2);
        //        testAbstractSerialization(histogram);
        //        intCountsHistogram = new IntCountsHistogram(highestTrackableValue, 2);
        //        testAbstractSerialization(intCountsHistogram);
        //        shortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, 4); // With 2 decimal points, shorts overflow here
        //        testAbstractSerialization(shortCountsHistogram);
        //    }

        //    [Fact](expected = IllegalStateException.class)
        //    public void testOverflow(){
        //        ShortCountsHistogram histogram = new ShortCountsHistogram(highestTrackableValue, 2);
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        // This should overflow a ShortHistogram:
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 500);
        //    }

        //    [Fact]
        //    public void testCopy(){
        //        Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of Histogram:");
        //        assertEqual(histogram, histogram.copy());

        //        IntCountsHistogram intCountsHistogram = new IntCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        intCountsHistogram.recordValue(testValueLevel);
        //        intCountsHistogram.recordValue(testValueLevel * 10);
        //        intCountsHistogram.recordValueWithExpectedInterval(intCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of IntHistogram:");
        //        assertEqual(intCountsHistogram, intCountsHistogram.copy());

        //        ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        shortCountsHistogram.recordValue(testValueLevel);
        //        shortCountsHistogram.recordValue(testValueLevel * 10);
        //        shortCountsHistogram.recordValueWithExpectedInterval(shortCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of ShortHistogram:");
        //        assertEqual(shortCountsHistogram, shortCountsHistogram.copy());

        //        AtomicHistogram atomicHistogram = new AtomicHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        atomicHistogram.recordValue(testValueLevel);
        //        atomicHistogram.recordValue(testValueLevel * 10);
        //        atomicHistogram.recordValueWithExpectedInterval(atomicHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of AtomicHistogram:");
        //        assertEqual(atomicHistogram, atomicHistogram.copy());

        //        ConcurrentHistogram concurrentHistogram = new ConcurrentHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        concurrentHistogram.recordValue(testValueLevel);
        //        concurrentHistogram.recordValue(testValueLevel * 10);
        //        concurrentHistogram.recordValueWithExpectedInterval(concurrentHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of ConcurrentHistogram:");
        //        assertEqual(concurrentHistogram, concurrentHistogram.copy());

        //        SynchronizedHistogram syncHistogram = new SynchronizedHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        syncHistogram.recordValue(testValueLevel);
        //        syncHistogram.recordValue(testValueLevel * 10);
        //        syncHistogram.recordValueWithExpectedInterval(syncHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of SynchronizedHistogram:");
        //        assertEqual(syncHistogram, syncHistogram.copy());
        //    }

        //    [Fact]
        //    public void testScaledCopy(){
        //        Histogram histogram = new Histogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled Histogram:");
        //        assertEqual(histogram, histogram.copy());

        //        IntCountsHistogram intCountsHistogram = new IntCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        intCountsHistogram.recordValue(testValueLevel);
        //        intCountsHistogram.recordValue(testValueLevel * 10);
        //        intCountsHistogram.recordValueWithExpectedInterval(intCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled IntHistogram:");
        //        assertEqual(intCountsHistogram, intCountsHistogram.copy());

        //        ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        shortCountsHistogram.recordValue(testValueLevel);
        //        shortCountsHistogram.recordValue(testValueLevel * 10);
        //        shortCountsHistogram.recordValueWithExpectedInterval(shortCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled ShortHistogram:");
        //        assertEqual(shortCountsHistogram, shortCountsHistogram.copy());

        //        AtomicHistogram atomicHistogram = new AtomicHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        atomicHistogram.recordValue(testValueLevel);
        //        atomicHistogram.recordValue(testValueLevel * 10);
        //        atomicHistogram.recordValueWithExpectedInterval(atomicHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled AtomicHistogram:");
        //        assertEqual(atomicHistogram, atomicHistogram.copy());

        //        ConcurrentHistogram concurrentHistogram = new ConcurrentHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        concurrentHistogram.recordValue(testValueLevel);
        //        concurrentHistogram.recordValue(testValueLevel * 10);
        //        concurrentHistogram.recordValueWithExpectedInterval(concurrentHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled ConcurrentHistogram:");
        //        assertEqual(concurrentHistogram, concurrentHistogram.copy());

        //        SynchronizedHistogram syncHistogram = new SynchronizedHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        syncHistogram.recordValue(testValueLevel);
        //        syncHistogram.recordValue(testValueLevel * 10);
        //        syncHistogram.recordValueWithExpectedInterval(syncHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copy of scaled SynchronizedHistogram:");
        //        assertEqual(syncHistogram, syncHistogram.copy());
        //    }

        //    [Fact]
        //    public void testCopyInto(){
        //        Histogram histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        Histogram targetHistogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for Histogram:");
        //        histogram.copyInto(targetHistogram);
        //        assertEqual(histogram, targetHistogram);

        //        histogram.recordValue(testValueLevel * 20);

        //        histogram.copyInto(targetHistogram);
        //        assertEqual(histogram, targetHistogram);


        //        IntCountsHistogram intCountsHistogram = new IntCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        IntCountsHistogram targetIntCountsHistogram = new IntCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        intCountsHistogram.recordValue(testValueLevel);
        //        intCountsHistogram.recordValue(testValueLevel * 10);
        //        intCountsHistogram.recordValueWithExpectedInterval(intCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for IntHistogram:");
        //        intCountsHistogram.copyInto(targetIntCountsHistogram);
        //        assertEqual(intCountsHistogram, targetIntCountsHistogram);

        //        intCountsHistogram.recordValue(testValueLevel * 20);

        //        intCountsHistogram.copyInto(targetIntCountsHistogram);
        //        assertEqual(intCountsHistogram, targetIntCountsHistogram);


        //        ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        ShortCountsHistogram targetShortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        shortCountsHistogram.recordValue(testValueLevel);
        //        shortCountsHistogram.recordValue(testValueLevel * 10);
        //        shortCountsHistogram.recordValueWithExpectedInterval(shortCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for ShortHistogram:");
        //        shortCountsHistogram.copyInto(targetShortCountsHistogram);
        //        assertEqual(shortCountsHistogram, targetShortCountsHistogram);

        //        shortCountsHistogram.recordValue(testValueLevel * 20);

        //        shortCountsHistogram.copyInto(targetShortCountsHistogram);
        //        assertEqual(shortCountsHistogram, targetShortCountsHistogram);


        //        AtomicHistogram atomicHistogram = new AtomicHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        AtomicHistogram targetAtomicHistogram = new AtomicHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        atomicHistogram.recordValue(testValueLevel);
        //        atomicHistogram.recordValue(testValueLevel * 10);
        //        atomicHistogram.recordValueWithExpectedInterval(atomicHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for AtomicHistogram:");
        //        atomicHistogram.copyInto(targetAtomicHistogram);
        //        assertEqual(atomicHistogram, targetAtomicHistogram);

        //        atomicHistogram.recordValue(testValueLevel * 20);

        //        atomicHistogram.copyInto(targetAtomicHistogram);
        //        assertEqual(atomicHistogram, targetAtomicHistogram);


        //        ConcurrentHistogram concurrentHistogram = new ConcurrentHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        ConcurrentHistogram targetConcurrentHistogram = new ConcurrentHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        concurrentHistogram.recordValue(testValueLevel);
        //        concurrentHistogram.recordValue(testValueLevel * 10);
        //        concurrentHistogram.recordValueWithExpectedInterval(concurrentHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for ConcurrentHistogram:");
        //        concurrentHistogram.copyInto(targetConcurrentHistogram);
        //        assertEqual(concurrentHistogram, targetConcurrentHistogram);

        //        concurrentHistogram.recordValue(testValueLevel * 20);

        //        concurrentHistogram.copyInto(targetConcurrentHistogram);
        //        assertEqual(concurrentHistogram, targetConcurrentHistogram);


        //        SynchronizedHistogram syncHistogram = new SynchronizedHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        SynchronizedHistogram targetSyncHistogram = new SynchronizedHistogram(highestTrackableValue, numberOfSignificantValueDigits);
        //        syncHistogram.recordValue(testValueLevel);
        //        syncHistogram.recordValue(testValueLevel * 10);
        //        syncHistogram.recordValueWithExpectedInterval(syncHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for SynchronizedHistogram:");
        //        syncHistogram.copyInto(targetSyncHistogram);
        //        assertEqual(syncHistogram, targetSyncHistogram);

        //        syncHistogram.recordValue(testValueLevel * 20);

        //        syncHistogram.copyInto(targetSyncHistogram);
        //        assertEqual(syncHistogram, targetSyncHistogram);
        //    }

        //    [Fact]
        //    public void testScaledCopyInto(){
        //        Histogram histogram = new Histogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        Histogram targetHistogram = new Histogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        histogram.recordValue(testValueLevel);
        //        histogram.recordValue(testValueLevel * 10);
        //        histogram.recordValueWithExpectedInterval(histogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for scaled Histogram:");
        //        histogram.copyInto(targetHistogram);
        //        assertEqual(histogram, targetHistogram);

        //        histogram.recordValue(testValueLevel * 20);

        //        histogram.copyInto(targetHistogram);
        //        assertEqual(histogram, targetHistogram);


        //        IntCountsHistogram intCountsHistogram = new IntCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        IntCountsHistogram targetIntCountsHistogram = new IntCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        intCountsHistogram.recordValue(testValueLevel);
        //        intCountsHistogram.recordValue(testValueLevel * 10);
        //        intCountsHistogram.recordValueWithExpectedInterval(intCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for scaled IntHistogram:");
        //        intCountsHistogram.copyInto(targetIntCountsHistogram);
        //        assertEqual(intCountsHistogram, targetIntCountsHistogram);

        //        intCountsHistogram.recordValue(testValueLevel * 20);

        //        intCountsHistogram.copyInto(targetIntCountsHistogram);
        //        assertEqual(intCountsHistogram, targetIntCountsHistogram);


        //        ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        ShortCountsHistogram targetShortCountsHistogram = new ShortCountsHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        shortCountsHistogram.recordValue(testValueLevel);
        //        shortCountsHistogram.recordValue(testValueLevel * 10);
        //        shortCountsHistogram.recordValueWithExpectedInterval(shortCountsHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for scaled ShortHistogram:");
        //        shortCountsHistogram.copyInto(targetShortCountsHistogram);
        //        assertEqual(shortCountsHistogram, targetShortCountsHistogram);

        //        shortCountsHistogram.recordValue(testValueLevel * 20);

        //        shortCountsHistogram.copyInto(targetShortCountsHistogram);
        //        assertEqual(shortCountsHistogram, targetShortCountsHistogram);


        //        AtomicHistogram atomicHistogram = new AtomicHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        AtomicHistogram targetAtomicHistogram = new AtomicHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        atomicHistogram.recordValue(testValueLevel);
        //        atomicHistogram.recordValue(testValueLevel * 10);
        //        atomicHistogram.recordValueWithExpectedInterval(atomicHistogram.getHighestTrackableValue() - 1, 31000);

        //        atomicHistogram.copyInto(targetAtomicHistogram);
        //        assertEqual(atomicHistogram, targetAtomicHistogram);

        //        atomicHistogram.recordValue(testValueLevel * 20);

        //        System.out.println("Testing copyInto for scaled AtomicHistogram:");
        //        atomicHistogram.copyInto(targetAtomicHistogram);
        //        assertEqual(atomicHistogram, targetAtomicHistogram);


        //        ConcurrentHistogram concurrentHistogram = new ConcurrentHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        ConcurrentHistogram targetConcurrentHistogram = new ConcurrentHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        concurrentHistogram.recordValue(testValueLevel);
        //        concurrentHistogram.recordValue(testValueLevel * 10);
        //        concurrentHistogram.recordValueWithExpectedInterval(concurrentHistogram.getHighestTrackableValue() - 1, 31000);

        //        concurrentHistogram.copyInto(targetConcurrentHistogram);
        //        assertEqual(concurrentHistogram, targetConcurrentHistogram);

        //        concurrentHistogram.recordValue(testValueLevel * 20);

        //        System.out.println("Testing copyInto for scaled ConcurrentHistogram:");
        //        concurrentHistogram.copyInto(targetConcurrentHistogram);
        //        assertEqual(concurrentHistogram, targetConcurrentHistogram);


        //        SynchronizedHistogram syncHistogram = new SynchronizedHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        SynchronizedHistogram targetSyncHistogram = new SynchronizedHistogram(1000, highestTrackableValue, numberOfSignificantValueDigits);
        //        syncHistogram.recordValue(testValueLevel);
        //        syncHistogram.recordValue(testValueLevel * 10);
        //        syncHistogram.recordValueWithExpectedInterval(syncHistogram.getHighestTrackableValue() - 1, 31000);

        //        System.out.println("Testing copyInto for scaled SynchronizedHistogram:");
        //        syncHistogram.copyInto(targetSyncHistogram);
        //        assertEqual(syncHistogram, targetSyncHistogram);

        //        syncHistogram.recordValue(testValueLevel * 20);

        //        syncHistogram.copyInto(targetSyncHistogram);
        //        assertEqual(syncHistogram, targetSyncHistogram);
        //    }

        //    public void verifyMaxValue(AbstractHistogram histogram) {
        //        long computedMaxValue = 0;
        //        for (int i = 0; i < histogram.countsArrayLength; i++) {
        //            if (histogram.getCountAtIndex(i) > 0) {
        //                computedMaxValue = histogram.valueFromIndex(i);
        //            }
        //        }
        //        computedMaxValue = (computedMaxValue == 0) ? 0 : histogram.highestEquivalentValue(computedMaxValue);
        //        Assert.Equal(computedMaxValue, histogram.getMaxValue());
        //    }
        //}
    }
}
