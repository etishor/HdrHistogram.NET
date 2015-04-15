// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace HdrHistogram.Tests
{

    [Trait("Category", "Performance")]
    public class HistogramPerfTest
    {
        static readonly long highestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units
        static readonly int numberOfSignificantValueDigits = 3;
        static readonly long testValueLevel = 12340;
        static readonly long warmupLoopLength = 50000;
        static readonly long rawTimingLoopCount = 500000000L;
        static readonly long rawDoubleTimingLoopCount = 300000000L;
        static readonly long singleWriterIntervalTimingLoopCount = 100000000L;
        static readonly long singleWriterDoubleIntervalTimingLoopCount = 100000000L;
        static readonly long intervalTimingLoopCount = 40000000L;
        static readonly long synchronizedTimingLoopCount = 180000000L;
        static readonly long atomicTimingLoopCount = 80000000L;
        static readonly long concurrentTimingLoopCount = 50000000L;

        private static readonly long factor = 1000L * 1000L * 1000L / Stopwatch.Frequency;
        private static long SystemNanoTime()
        {
            return Stopwatch.GetTimestamp() * factor;
        }

        void recordLoopWithExpectedInterval(AbstractHistogram histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        void recordLoopWithExpectedInterval(Recorder histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        void recordLoopWithExpectedInterval(SingleWriterRecorder histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        void recordLoopWithExpectedInterval(DoubleRecorder histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        void recordLoopWithExpectedInterval(SingleWriterDoubleRecorder histogram, long loopCount, long expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        void recordLoopDoubleWithExpectedInterval(DoubleHistogram histogram, long loopCount, double expectedInterval)
        {
            for (long i = 0; i < loopCount; i++)
                histogram.recordValueWithExpectedInterval(testValueLevel + (i & 0x8000), expectedInterval);
        }

        long LeadingZerosSpeedLoop(long loopCount)
        {
            long sum = 0;
            for (long i = 0; i < loopCount; i++)
            {
                // long val = testValueLevel + (i & 0x8000);
                long val = testValueLevel;
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
                sum += MathUtils.NumberOfLeadingZeros(val);
            }
            return sum;
        }

        public void testRawRecordingSpeedAtExpectedInterval(String label, AbstractHistogram histogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(histogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            histogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(histogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        public void testRawRecordingSpeedAtExpectedInterval(String label, Recorder intervalHistogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            intervalHistogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            Histogram histogram = intervalHistogram.getIntervalHistogram();
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        public void testRawRecordingSpeedAtExpectedInterval(String label, SingleWriterRecorder intervalHistogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            intervalHistogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            Histogram histogram = intervalHistogram.getIntervalHistogram();
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        public void testRawRecordingSpeedAtExpectedInterval(String label, SingleWriterDoubleRecorder intervalHistogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            intervalHistogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            DoubleHistogram histogram = intervalHistogram.getIntervalHistogram();
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        public void testRawRecordingSpeedAtExpectedInterval(String label, DoubleRecorder intervalHistogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            intervalHistogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopWithExpectedInterval(intervalHistogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            DoubleHistogram histogram = intervalHistogram.getIntervalHistogram();
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        public void testRawDoubleRecordingSpeedAtExpectedInterval(String label, DoubleHistogram histogram,
                                                            long expectedInterval, long timingLoopCount)
        {
            Console.WriteLine("\nTiming recording speed with expectedInterval = " + expectedInterval + " :");
            // Warm up:
            long startTime = SystemNanoTime();
            recordLoopDoubleWithExpectedInterval(histogram, warmupLoopLength, expectedInterval);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine(label + "Warmup: " + warmupLoopLength + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            histogram.reset();
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            recordLoopDoubleWithExpectedInterval(histogram, timingLoopCount, expectedInterval);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * timingLoopCount / deltaUsec;
            Console.WriteLine(label + "Hot code timing:");
            Console.WriteLine(label + timingLoopCount + " value recordings completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            rate = 1000000 * histogram.getTotalCount() / deltaUsec;
            Console.WriteLine(label + histogram.getTotalCount() + " raw recorded entries completed in " +
                    deltaUsec + " usec, rate = " + rate + " recorded values per sec.");
        }

        [Fact]
        public void testRawRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming Histogram:");
            testRawRecordingSpeedAtExpectedInterval("Histogram: ", histogram, 1000000000, rawTimingLoopCount);
        }

        [Fact]
        public void testSingleWriterIntervalRecordingSpeed()
        {
            SingleWriterRecorder histogramRecorder;
            histogramRecorder = new SingleWriterRecorder(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming SingleWriterIntervalHistogramRecorder:");
            testRawRecordingSpeedAtExpectedInterval("SingleWriterRecorder: ", histogramRecorder, 1000000000, singleWriterIntervalTimingLoopCount);
        }

        [Fact]
        public void testIntervalRecordingSpeed()
        {
            Recorder histogramRecorder;
            histogramRecorder = new Recorder(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming IntervalHistogramRecorder:");
            testRawRecordingSpeedAtExpectedInterval("Recorder: ", histogramRecorder, 1000000000, intervalTimingLoopCount);
        }

        [Fact]
        public void testRawDoubleRecordingSpeed()
        {
            DoubleHistogram histogram;
            histogram = new DoubleHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming DoubleHistogram:");
            testRawDoubleRecordingSpeedAtExpectedInterval("DoubleHistogram: ", histogram, 1000000000, rawDoubleTimingLoopCount);
        }

        [Fact]
        public void testDoubleIntervalRecordingSpeed()
        {
            DoubleRecorder histogramRecorder;
            histogramRecorder = new DoubleRecorder(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming IntervalDoubleHistogramRecorder:");
            testRawRecordingSpeedAtExpectedInterval("DoubleRecorder: ", histogramRecorder, 1000000000, intervalTimingLoopCount);
        }

        [Fact]
        public void testSingleWriterDoubleIntervalRecordingSpeed()
        {
            SingleWriterDoubleRecorder histogramRecorder;
            histogramRecorder = new SingleWriterDoubleRecorder(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming SingleWriterIntervalDoubleHistogramRecorder:");
            testRawRecordingSpeedAtExpectedInterval("SingleWriterDoubleRecorder: ", histogramRecorder, 1000000000, singleWriterDoubleIntervalTimingLoopCount);
        }

        [Fact]
        public void testRawSyncronizedRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new SynchronizedHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming SynchronizedHistogram:");
            testRawRecordingSpeedAtExpectedInterval("SynchronizedHistogram: ", histogram, 1000000000, synchronizedTimingLoopCount);
        }

        [Fact]
        public void testRawAtomicRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new AtomicHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming AtomicHistogram:");
            testRawRecordingSpeedAtExpectedInterval("AtomicHistogram: ", histogram, 1000000000, atomicTimingLoopCount);
        }


        [Fact]
        public void testRawConcurrentRecordingSpeed()
        {
            AbstractHistogram histogram;
            histogram = new ConcurrentHistogram(highestTrackableValue, numberOfSignificantValueDigits);
            Console.WriteLine("\n\nTiming ConcurrentHistogram:");
            testRawRecordingSpeedAtExpectedInterval("AtomicHistogram: ", histogram, 1000000000, concurrentTimingLoopCount);
        }

        [Fact]
        public void testLeadingZerosSpeed()
        {
            Console.WriteLine("\nTiming LeadingZerosSpeed :");
            long startTime = SystemNanoTime();
            LeadingZerosSpeedLoop(warmupLoopLength);
            long endTime = SystemNanoTime();
            long deltaUsec = (endTime - startTime) / 1000L;
            long rate = 1000000 * warmupLoopLength / deltaUsec;
            Console.WriteLine("Warmup:\n" + warmupLoopLength + " Leading Zero loops completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
            // Wait a bit to make sure compiler had a cache to do it's stuff:
            Thread.Sleep(1000);

            startTime = SystemNanoTime();
            LeadingZerosSpeedLoop(rawTimingLoopCount);
            endTime = SystemNanoTime();
            deltaUsec = (endTime - startTime) / 1000L;
            rate = 1000000 * rawTimingLoopCount / deltaUsec;
            Console.WriteLine("Hot code timing:");
            Console.WriteLine(rawTimingLoopCount + " Leading Zero loops completed in " +
                    deltaUsec + " usec, rate = " + rate + " value recording calls per sec.");
        }

        public static void main(String[] args)
        {
            try
            {
                HistogramPerfTest test = new HistogramPerfTest();
                test.testLeadingZerosSpeed();
                Thread.Sleep(1000000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e);
            }
        }

    }

}
