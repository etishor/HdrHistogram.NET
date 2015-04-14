// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
using System;
using System.Diagnostics;
using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{
    public class DoubleHistogramTest
    {
        private static long trackableValueRangeSize = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        private static int numberOfSignificantValueDigits = 3;
        // static long testValueLevel = 12340;
        private static double testValueLevel = 4.0;

        private void assertEquals(double expected, double value, double epsilon)
        {
            value.Should().BeApproximately(expected, epsilon);
        }

        private void assertEquals(long expected, long value)
        {
            value.Should().Be(expected);
        }

        private void assertEquals(string because, double expected, double value, double epsilon)
        {
            value.Should().BeApproximately(expected, epsilon, because);
        }

        [Fact]
        public void testTrackableValueRangeMustBeGreaterThanTwo()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new DoubleHistogram(1, numberOfSignificantValueDigits);
            });
        }

        [Fact]
        public void testNumberOfSignificantValueDigitsMustBeLessThanSix()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new DoubleHistogram(trackableValueRangeSize, 6);
            });
        }

        [Fact]
        public void testNumberOfSignificantValueDigitsMustBePositive()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new DoubleHistogram(trackableValueRangeSize, -1);
            });
        }

        [Fact]
        public void testConstructionArgumentGets()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            // Record 1.0, and verify that the range adjust to it:
            histogram.recordValue(Math.Pow(2.0, 20));
            histogram.recordValue(1.0);

            assertEquals(1.0, histogram.getCurrentLowestTrackableNonZeroValue(), 0.001);
            assertEquals(trackableValueRangeSize, histogram.getHighestToLowestValueRatio());
            assertEquals(numberOfSignificantValueDigits, histogram.getNumberOfSignificantValueDigits());

            DoubleHistogram histogram2 = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            // Record a larger value, and verify that the range adjust to it too:
            histogram2.recordValue(2048.0 * 1024.0 * 1024.0);
            assertEquals(2048.0 * 1024.0 * 1024.0, histogram2.getCurrentLowestTrackableNonZeroValue(), 0.001);

            DoubleHistogram histogram3 = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            // Record a value that is 1000x outside of the initially set range, which should scale us by 1/1024x:
            histogram3.recordValue(1 / 1000.0);
            assertEquals(1.0 / 1024, histogram3.getCurrentLowestTrackableNonZeroValue(), 0.001);
        }

        [Fact]
        public void testDataRange()
        {
            // A trackableValueRangeSize histigram
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(0.0); // Include a zero value to make sure things are handled right.
            assertEquals(1L, histogram.getCountAtValue(0.0));

            double topValue = 1.0;
            try
            {
                while (true)
                {
                    histogram.recordValue(topValue);
                    topValue *= 2.0;
                }
            }
            catch (IndexOutOfRangeException ex)
            {
            }
            assertEquals(1L << 33, topValue, 0.00001);
            assertEquals(1L, histogram.getCountAtValue(0.0));

            histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(0.0); // Include a zero value to make sure things are handled right.

            double bottomValue = 1L << 33;
            try
            {
                while (true)
                {
                    histogram.recordValue(bottomValue);
                    bottomValue /= 2.0;
                }
            }
            catch (IndexOutOfRangeException ex)
            {
                Trace.WriteLine("Bottom value at exception point = " + bottomValue);
            }
            assertEquals(1.0, bottomValue, 0.00001);

            long expectedRange = 1L << (findContainingBinaryOrderOfMagnitude(trackableValueRangeSize) + 1);
            assertEquals(expectedRange, (topValue / bottomValue), 0.00001);
            assertEquals(1L, histogram.getCountAtValue(0.0));
        }

        [Fact]
        public void testRecordValue()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            assertEquals(1L, histogram.getCountAtValue(testValueLevel));
            assertEquals(1L, histogram.getTotalCount());
        }

        [Fact]
        public void testRecordValue_Overflow_ShouldThrowException()
        {
            Assert.Throws<IndexOutOfRangeException>(() =>
            {
                DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
                histogram.recordValue(trackableValueRangeSize * 3);
                histogram.recordValue(1.0);
            });
        }

        [Fact]
        public void testRecordValueWithExpectedInterval()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(0);
            histogram.recordValueWithExpectedInterval(testValueLevel, testValueLevel / 4);
            DoubleHistogram rawHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            rawHistogram.recordValue(0);
            rawHistogram.recordValue(testValueLevel);
            // The raw data will not include corrected samples:
            assertEquals(1L, rawHistogram.getCountAtValue(0));
            assertEquals(0L, rawHistogram.getCountAtValue((testValueLevel * 1) / 4));
            assertEquals(0L, rawHistogram.getCountAtValue((testValueLevel * 2) / 4));
            assertEquals(0L, rawHistogram.getCountAtValue((testValueLevel * 3) / 4));
            assertEquals(1L, rawHistogram.getCountAtValue((testValueLevel * 4) / 4));
            assertEquals(2L, rawHistogram.getTotalCount());
            // The data will include corrected samples:
            assertEquals(1L, histogram.getCountAtValue(0));
            assertEquals(1L, histogram.getCountAtValue((testValueLevel * 1) / 4));
            assertEquals(1L, histogram.getCountAtValue((testValueLevel * 2) / 4));
            assertEquals(1L, histogram.getCountAtValue((testValueLevel * 3) / 4));
            assertEquals(1L, histogram.getCountAtValue((testValueLevel * 4) / 4));
            assertEquals(5L, histogram.getTotalCount());
        }

        [Fact]
        public void testReset()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.reset();
            assertEquals(0L, histogram.getCountAtValue(testValueLevel));
            assertEquals(0L, histogram.getTotalCount());
        }

        [Fact]
        public void testAdd()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            DoubleHistogram other = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);

            histogram.recordValue(testValueLevel);
            histogram.recordValue(testValueLevel * 1000);
            other.recordValue(testValueLevel);
            other.recordValue(testValueLevel * 1000);
            histogram.add(other);
            assertEquals(2L, histogram.getCountAtValue(testValueLevel));
            assertEquals(2L, histogram.getCountAtValue(testValueLevel * 1000));
            assertEquals(4L, histogram.getTotalCount());

            DoubleHistogram biggerOther = new DoubleHistogram(trackableValueRangeSize * 2, numberOfSignificantValueDigits);
            biggerOther.recordValue(testValueLevel);
            biggerOther.recordValue(testValueLevel * 1000);

            // Adding the smaller histogram to the bigger one should work:
            biggerOther.add(histogram);
            assertEquals(3L, biggerOther.getCountAtValue(testValueLevel));
            assertEquals(3L, biggerOther.getCountAtValue(testValueLevel * 1000));
            assertEquals(6L, biggerOther.getTotalCount());

            // Since we are auto-sized, trying to add a larger histogram into a smaller one should work if no
            // overflowing data is there:
            try
            {
                // This should throw:
                histogram.add(biggerOther);
            }
            catch (IndexOutOfRangeException e)
            {
                Assert.True(false, "Should of thown with out of bounds error");
            }

            // But trying to add smaller values to a larger histogram that actually uses it's range should throw an AIOOB:
            histogram.recordValue(1.0);
            other.recordValue(1.0);
            biggerOther.recordValue(trackableValueRangeSize * 8);

            try
            {
                // This should throw:
                biggerOther.add(histogram);
                Assert.True(false, "Should of thown with out of bounds error");
            }
            catch (IndexOutOfRangeException e)
            {
            }
        }


        [Fact]
        public void testSizeOfEquivalentValueRange()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(1.0);
            assertEquals("Size of equivalent range for value 1 is 1",
                1.0 / 1024.0, histogram.sizeOfEquivalentValueRange(1), 0.001);
            assertEquals("Size of equivalent range for value 2500 is 2",
                2, histogram.sizeOfEquivalentValueRange(2500), 0.001);
            assertEquals("Size of equivalent range for value 8191 is 4",
                4, histogram.sizeOfEquivalentValueRange(8191), 0.001);
            assertEquals("Size of equivalent range for value 8192 is 8",
                8, histogram.sizeOfEquivalentValueRange(8192), 0.001);
            assertEquals("Size of equivalent range for value 10000 is 8",
                8, histogram.sizeOfEquivalentValueRange(10000), 0.001);
        }

        [Fact]
        public void testLowestEquivalentValue()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(1.0);
            assertEquals("The lowest equivalent value to 10007 is 10000",
                10000, histogram.lowestEquivalentValue(10007), 0.001);
            assertEquals("The lowest equivalent value to 10009 is 10008",
                10008, histogram.lowestEquivalentValue(10009), 0.001);
        }

        [Fact]
        public void testHighestEquivalentValue()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(1.0);
            assertEquals("The highest equivalent value to 8180 is 8183",
                8183.99999, histogram.highestEquivalentValue(8180), 0.001);
            assertEquals("The highest equivalent value to 8187 is 8191",
                8191.99999, histogram.highestEquivalentValue(8191), 0.001);
            assertEquals("The highest equivalent value to 8193 is 8199",
                8199.99999, histogram.highestEquivalentValue(8193), 0.001);
            assertEquals("The highest equivalent value to 9995 is 9999",
                9999.99999, histogram.highestEquivalentValue(9995), 0.001);
            assertEquals("The highest equivalent value to 10007 is 10007",
                10007.99999, histogram.highestEquivalentValue(10007), 0.001);
            assertEquals("The highest equivalent value to 10008 is 10015",
                10015.99999, histogram.highestEquivalentValue(10008), 0.001);
        }

        [Fact]
        public void testMedianEquivalentValue()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(1.0);
            assertEquals("The median equivalent value to 4 is 4",
                4.002, histogram.medianEquivalentValue(4), 0.001);
            assertEquals("The median equivalent value to 5 is 5",
                5.002, histogram.medianEquivalentValue(5), 0.001);
            assertEquals("The median equivalent value to 4000 is 4001",
                4001, histogram.medianEquivalentValue(4000), 0.001);
            assertEquals("The median equivalent value to 8000 is 8002",
                8002, histogram.medianEquivalentValue(8000), 0.001);
            assertEquals("The median equivalent value to 10007 is 10004",
                10004, histogram.medianEquivalentValue(10007), 0.001);
        }

        [Fact]
        public void testNextNonEquivalentValue()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.Should().NotBeNull();
        }

        //private void testDoubleHistogramSerialization(DoubleHistogram histogram)
        //{
        //    histogram.recordValue(testValueLevel);
        //    histogram.recordValue(testValueLevel*10);
        //    histogram.recordValueWithExpectedInterval(histogram.getCurrentHighestTrackableValue() - 1, histogram.getCurrentHighestTrackableValue()/1000);
        //    ByteArrayOutputStream bos = new ByteArrayOutputStream();
        //    ObjectOutput out =
        //    null;
        //    ByteArrayInputStream bis = null;
        //    ObjectInput in =
        //    null;
        //    DoubleHistogram newHistogram = null;
        //    try
        //    {
        //    out =
        //        new ObjectOutputStream(bos);
        //        out.
        //        writeObject(histogram);
        //        Deflater compresser = new Deflater();
        //        compresser.setInput(bos.toByteArray());
        //        compresser.finish();
        //        byte[] compressedOutput = new byte[1024*1024];
        //        int compressedDataLength = compresser.deflate(compressedOutput);
        //        Trace.WriteLine("Serialized form of " + histogram.getClass() + " with internalHighestToLowestValueRatio = " +
        //                        histogram.getHighestToLowestValueRatio() + "\n and a numberOfSignificantValueDigits = " +
        //                        histogram.getNumberOfSignificantValueDigits() + " is " + bos.toByteArray().length +
        //                        " bytes long. Compressed form is " + compressedDataLength + " bytes long.");
        //        Trace.WriteLine("   (estimated footprint was " + histogram.getEstimatedFootprintInBytes() + " bytes)");
        //        bis = new ByteArrayInputStream(bos.toByteArray());
        //        in =
        //        new ObjectInputStream(bis);
        //        newHistogram = (DoubleHistogram)in.
        //        readObject();
        //    }
        //    finally
        //    {
        //        if (out !=
        //        null) out.
        //        close();
        //        bos.close();
        //        if (in !=
        //        null) in.
        //        close();
        //        if (bis != null) bis.close();
        //    }
        //    assertNotNull(newHistogram);
        //    assertEqual(histogram, newHistogram);
        //}

        private void assertEqual(DoubleHistogram expectedHistogram, DoubleHistogram actualHistogram)
        {
            actualHistogram.Equals(expectedHistogram).Should().BeTrue();
            Assert.Equal(
                expectedHistogram.getCountAtValue(testValueLevel),
                actualHistogram.getCountAtValue(testValueLevel));
            Assert.Equal(
                expectedHistogram.getCountAtValue(testValueLevel * 10),
                actualHistogram.getCountAtValue(testValueLevel * 10));
            Assert.Equal(
                expectedHistogram.getTotalCount(),
                actualHistogram.getTotalCount());
        }

        //[Fact]
        //public void testSerialization()
        //{
        //    DoubleHistogram histogram =
        //        new DoubleHistogram(trackableValueRangeSize, 3);
        //    testDoubleHistogramSerialization(histogram);
        //    DoubleHistogram withIntHistogram =
        //        new DoubleHistogram(trackableValueRangeSize, 3, typeof (IntCountsHistogram));
        //    testDoubleHistogramSerialization(withIntHistogram);
        //    DoubleHistogram withShortHistogram =
        //        new DoubleHistogram(trackableValueRangeSize, 3, typeof (ShortCountsHistogram));
        //    testDoubleHistogramSerialization(withShortHistogram);
        //    histogram = new DoubleHistogram(trackableValueRangeSize, 2, typeof (Histogram));
        //    testDoubleHistogramSerialization(histogram);
        //    withIntHistogram = new DoubleHistogram(trackableValueRangeSize, 2, typeof (IntCountsHistogram));
        //    testDoubleHistogramSerialization(withIntHistogram);
        //    withShortHistogram = new DoubleHistogram(trackableValueRangeSize, 2, typeof (ShortCountsHistogram));
        //    testDoubleHistogramSerialization(withShortHistogram);
        //}

        [Fact]
        public void testCopy()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.recordValue(testValueLevel * 10);
            histogram.recordValueWithExpectedInterval(histogram.getCurrentHighestTrackableValue() - 1, 31000);

            Trace.WriteLine("Testing copy of DoubleHistogram:");
            assertEqual(histogram, histogram.copy());

            DoubleHistogram withIntHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(IntCountsHistogram));

            withIntHistogram.recordValue(testValueLevel);
            withIntHistogram.recordValue(testValueLevel * 10);
            withIntHistogram.recordValueWithExpectedInterval(withIntHistogram.getCurrentHighestTrackableValue() - 1, 31000);

            Trace.WriteLine("Testing copy of DoubleHistogram backed by IntHistogram:");
            assertEqual(withIntHistogram, withIntHistogram.copy());

            DoubleHistogram withShortHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ShortCountsHistogram));
            withShortHistogram.recordValue(testValueLevel);
            withShortHistogram.recordValue(testValueLevel * 10);
            withShortHistogram.recordValueWithExpectedInterval(withShortHistogram.getCurrentHighestTrackableValue() - 1, 31000);

            Trace.WriteLine("Testing copy of DoubleHistogram backed by ShortHistogram:");
            assertEqual(withShortHistogram, withShortHistogram.copy());

            DoubleHistogram withConcurrentHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ConcurrentHistogram));
            withConcurrentHistogram.recordValue(testValueLevel);
            withConcurrentHistogram.recordValue(testValueLevel * 10);
            withConcurrentHistogram.recordValueWithExpectedInterval(withConcurrentHistogram.getCurrentHighestTrackableValue() - 1, 31000);

            Trace.WriteLine("Testing copy of DoubleHistogram backed by ConcurrentHistogram:");
            assertEqual(withConcurrentHistogram, withConcurrentHistogram.copy());

            DoubleHistogram withSyncHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(SynchronizedHistogram));
            withSyncHistogram.recordValue(testValueLevel);
            withSyncHistogram.recordValue(testValueLevel * 10);
            withSyncHistogram.recordValueWithExpectedInterval(withSyncHistogram.getCurrentHighestTrackableValue() - 1, 31000);

            Trace.WriteLine("Testing copy of DoubleHistogram backed by SynchronizedHistogram:");
            assertEqual(withSyncHistogram, withSyncHistogram.copy());
        }

        [Fact]
        public void testCopyInto()
        {
            DoubleHistogram histogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            DoubleHistogram targetHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            histogram.recordValue(testValueLevel);
            histogram.recordValue(testValueLevel * 10);
            histogram.recordValueWithExpectedInterval(histogram.getCurrentHighestTrackableValue() - 1,
                histogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for DoubleHistogram:");
            histogram.copyInto(targetHistogram);
            assertEqual(histogram, targetHistogram);

            histogram.recordValue(testValueLevel * 20);

            histogram.copyInto(targetHistogram);
            assertEqual(histogram, targetHistogram);


            DoubleHistogram withIntHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
               typeof(IntCountsHistogram));

            DoubleHistogram targetWithIntHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(IntCountsHistogram));

            withIntHistogram.recordValue(testValueLevel);
            withIntHistogram.recordValue(testValueLevel * 10);
            withIntHistogram.recordValueWithExpectedInterval(withIntHistogram.getCurrentHighestTrackableValue() - 1,
                withIntHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for DoubleHistogram backed by IntHistogram:");
            withIntHistogram.copyInto(targetWithIntHistogram);
            assertEqual(withIntHistogram, targetWithIntHistogram);

            withIntHistogram.recordValue(testValueLevel * 20);

            withIntHistogram.copyInto(targetWithIntHistogram);
            assertEqual(withIntHistogram, targetWithIntHistogram);


            DoubleHistogram withShortHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ShortCountsHistogram));
            DoubleHistogram targetWithShortHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ShortCountsHistogram));
            withShortHistogram.recordValue(testValueLevel);
            withShortHistogram.recordValue(testValueLevel * 10);
            withShortHistogram.recordValueWithExpectedInterval(withShortHistogram.getCurrentHighestTrackableValue() - 1,
                withShortHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for DoubleHistogram backed by a ShortHistogram:");
            withShortHistogram.copyInto(targetWithShortHistogram);
            assertEqual(withShortHistogram, targetWithShortHistogram);

            withShortHistogram.recordValue(testValueLevel * 20);

            withShortHistogram.copyInto(targetWithShortHistogram);
            assertEqual(withShortHistogram, targetWithShortHistogram);


            DoubleHistogram withConcurrentHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ConcurrentHistogram));
            DoubleHistogram targetWithConcurrentHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(ConcurrentHistogram));
            withConcurrentHistogram.recordValue(testValueLevel);
            withConcurrentHistogram.recordValue(testValueLevel * 10);
            withConcurrentHistogram.recordValueWithExpectedInterval(withConcurrentHistogram.getCurrentHighestTrackableValue() - 1,
                withConcurrentHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for DoubleHistogram backed by ConcurrentHistogram:");
            withConcurrentHistogram.copyInto(targetWithConcurrentHistogram);
            assertEqual(withConcurrentHistogram, targetWithConcurrentHistogram);

            withConcurrentHistogram.recordValue(testValueLevel * 20);

            withConcurrentHistogram.copyInto(targetWithConcurrentHistogram);
            assertEqual(withConcurrentHistogram, targetWithConcurrentHistogram);


            ConcurrentDoubleHistogram concurrentHistogram =
                new ConcurrentDoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            ConcurrentDoubleHistogram targetConcurrentHistogram =
                new ConcurrentDoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            concurrentHistogram.recordValue(testValueLevel);
            concurrentHistogram.recordValue(testValueLevel * 10);
            concurrentHistogram.recordValueWithExpectedInterval(concurrentHistogram.getCurrentHighestTrackableValue() - 1,
                concurrentHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for actual ConcurrentHistogram:");
            concurrentHistogram.copyInto(targetConcurrentHistogram);
            assertEqual(concurrentHistogram, targetConcurrentHistogram);

            concurrentHistogram.recordValue(testValueLevel * 20);

            concurrentHistogram.copyInto(targetConcurrentHistogram);
            assertEqual(concurrentHistogram, targetConcurrentHistogram);


            DoubleHistogram withSyncHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(SynchronizedHistogram));
            DoubleHistogram targetWithSyncHistogram = new DoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits,
                typeof(SynchronizedHistogram));
            withSyncHistogram.recordValue(testValueLevel);
            withSyncHistogram.recordValue(testValueLevel * 10);
            withSyncHistogram.recordValueWithExpectedInterval(withSyncHistogram.getCurrentHighestTrackableValue() - 1,
                withSyncHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for DoubleHistogram backed by SynchronizedHistogram:");
            withSyncHistogram.copyInto(targetWithSyncHistogram);
            assertEqual(withSyncHistogram, targetWithSyncHistogram);

            withSyncHistogram.recordValue(testValueLevel * 20);

            withSyncHistogram.copyInto(targetWithSyncHistogram);
            assertEqual(withSyncHistogram, targetWithSyncHistogram);

            SynchronizedDoubleHistogram syncHistogram =
                new SynchronizedDoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            SynchronizedDoubleHistogram targetSyncHistogram =
                new SynchronizedDoubleHistogram(trackableValueRangeSize, numberOfSignificantValueDigits);
            syncHistogram.recordValue(testValueLevel);
            syncHistogram.recordValue(testValueLevel * 10);
            syncHistogram.recordValueWithExpectedInterval(syncHistogram.getCurrentHighestTrackableValue() - 1,
                syncHistogram.getCurrentHighestTrackableValue() / 1000);

            Trace.WriteLine("Testing copyInto for actual SynchronizedDoubleHistogram:");
            syncHistogram.copyInto(targetSyncHistogram);
            assertEqual(syncHistogram, targetSyncHistogram);

            syncHistogram.recordValue(testValueLevel * 20);

            syncHistogram.copyInto(targetSyncHistogram);
            assertEqual(syncHistogram, targetSyncHistogram);
        }

        private int findContainingBinaryOrderOfMagnitude(long longNumber)
        {
            int pow2ceiling = 64 - MathUtils.NumberOfLeadingZeros(longNumber); // smallest power of 2 containing value
            pow2ceiling = Math.Min(pow2ceiling, 62);
            return pow2ceiling;
        }
    }
}
