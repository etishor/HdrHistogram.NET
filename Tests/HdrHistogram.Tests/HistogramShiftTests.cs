// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
using System;
using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{
    public class HistogramShiftTest
    {
        private static readonly long highestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units

        [Fact]
        public void testHistogramShift()
        {
            Histogram histogram = new Histogram(highestTrackableValue, 3);
            testShiftLowestBucket(histogram);
            testShiftNonLowestBucket(histogram);
        }

        [Fact]
        public void testIntHistogramShift()
        {
            IntCountsHistogram intCountsHistogram = new IntCountsHistogram(highestTrackableValue, 3);
            testShiftLowestBucket(intCountsHistogram);
            testShiftNonLowestBucket(intCountsHistogram);
        }

        [Fact]
        public void testShortHistogramShift()
        {
            ShortCountsHistogram shortCountsHistogram = new ShortCountsHistogram(highestTrackableValue, 3);
            testShiftLowestBucket(shortCountsHistogram);
            testShiftNonLowestBucket(shortCountsHistogram);
        }


        [Fact]
        public void testSynchronizedHistogramShift()
        {
            SynchronizedHistogram synchronizedHistogram = new SynchronizedHistogram(highestTrackableValue, 3);
            testShiftLowestBucket(synchronizedHistogram);
            testShiftNonLowestBucket(synchronizedHistogram);
        }

        [Fact]
        public void testAtomicHistogramShift()
        {
            Action action = () =>
            {
                AtomicHistogram atomicHistogram = new AtomicHistogram(highestTrackableValue, 3);
                testShiftLowestBucket(atomicHistogram);
                testShiftNonLowestBucket(atomicHistogram);
            };

            action.ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void testConcurrentHistogramShift()
        {
            ConcurrentHistogram concurrentHistogram = new ConcurrentHistogram(highestTrackableValue, 3);
            testShiftLowestBucket(concurrentHistogram);
            testShiftNonLowestBucket(concurrentHistogram);
        }

        private void testShiftLowestBucket(AbstractHistogram histogram)
        {
            for (int shiftAmount = 0; shiftAmount < 10; shiftAmount++)
            {
                histogram.reset();
                histogram.RecordValueWithCount(0, 500);
                histogram.RecordValue(2);
                histogram.RecordValue(4);
                histogram.RecordValue(5);
                histogram.RecordValue(511);
                histogram.RecordValue(512);
                histogram.RecordValue(1023);
                histogram.RecordValue(1024);
                histogram.RecordValue(1025);

                AbstractHistogram histogram2 = histogram.copy();

                histogram2.reset();
                histogram2.RecordValueWithCount(0, 500);
                histogram2.RecordValue(2 << shiftAmount);
                histogram2.RecordValue(4 << shiftAmount);
                histogram2.RecordValue(5 << shiftAmount);
                histogram2.RecordValue(511 << shiftAmount);
                histogram2.RecordValue(512 << shiftAmount);
                histogram2.RecordValue(1023 << shiftAmount);
                histogram2.RecordValue(1024 << shiftAmount);
                histogram2.RecordValue(1025 << shiftAmount);

                histogram.shiftValuesLeft(shiftAmount);

                histogram.Equals(histogram2).Should().BeTrue("Not Equal for shift of " + shiftAmount);
            }
        }

        private void testShiftNonLowestBucket(AbstractHistogram histogram)
        {
            for (int shiftAmount = 0; shiftAmount < 10; shiftAmount++)
            {
                histogram.reset();
                histogram.RecordValueWithCount(0, 500);
                histogram.RecordValue(2 << 10);
                histogram.RecordValue(4 << 10);
                histogram.RecordValue(5 << 10);
                histogram.RecordValue(511 << 10);
                histogram.RecordValue(512 << 10);
                histogram.RecordValue(1023 << 10);
                histogram.RecordValue(1024 << 10);
                histogram.RecordValue(1025 << 10);

                AbstractHistogram origHistogram = histogram.copy();
                AbstractHistogram histogram2 = histogram.copy();

                histogram2.reset();
                histogram2.RecordValueWithCount(0, 500);
                histogram2.RecordValue((2 << 10) << shiftAmount);
                histogram2.RecordValue((4 << 10) << shiftAmount);
                histogram2.RecordValue((5 << 10) << shiftAmount);
                histogram2.RecordValue((511 << 10) << shiftAmount);
                histogram2.RecordValue((512 << 10) << shiftAmount);
                histogram2.RecordValue((1023 << 10) << shiftAmount);
                histogram2.RecordValue((1024 << 10) << shiftAmount);
                histogram2.RecordValue((1025 << 10) << shiftAmount);

                histogram.shiftValuesLeft(shiftAmount);

                histogram.Equals(histogram2).Should().BeTrue("Not Equal for shift of " + shiftAmount);

                histogram.shiftValuesRight(shiftAmount);

                histogram.Equals(origHistogram).Should().BeTrue();
            }
        }
    }
}
