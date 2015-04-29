﻿// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo

using System;
using System.Runtime.CompilerServices;
using HdrHistogram.ConcurrencyUtilities;

namespace HdrHistogram
{
    /**
     * Records floating point values, and provides stable interval {@link DoubleHistogram} samples from live recorded data
     * without interrupting or stalling active recording of values. Each interval histogram provided contains all
     * value counts accumulated since the previous interval histogram was taken.
     * <p>
     * This pattern is commonly used in logging interval histogram information while recording is ongoing.
     * <p>
     * {@link SingleWriterDoubleRecorder} expects only a single thread (the "single writer") to
     * call {@link SingleWriterDoubleRecorder#RecordValue} or
     * {@link SingleWriterDoubleRecorder#RecordValueWithExpectedInterval} at any point in time.
     * It DOES NOT support concurrent recording calls.
     *
     */

    public class SingleWriterDoubleRecorder
    {
        private static AtomicLong instanceIdSequencer = new AtomicLong(1);
        private readonly long instanceId = instanceIdSequencer.GetAndIncrement();

        private readonly WriterReaderPhaser recordingPhaser = new WriterReaderPhaser();

        private volatile InternalDoubleHistogram activeHistogram;
        private InternalDoubleHistogram inactiveHistogram;

        /**
         * Construct an auto-resizing {@link SingleWriterDoubleRecorder} using a precision stated as a
         * number of significant decimal digits.
         *
         * @param numberOfSignificantValueDigits Specifies the precision to use. This is the number of significant
         *                                       decimal digits to which the histogram will maintain value resolution
         *                                       and separation. Must be a non-negative integer between 0 and 5.
         */

        public SingleWriterDoubleRecorder(int numberOfSignificantValueDigits)
        {
            activeHistogram = new InternalDoubleHistogram(instanceId, numberOfSignificantValueDigits);
            inactiveHistogram = new InternalDoubleHistogram(instanceId, numberOfSignificantValueDigits);
            activeHistogram.setStartTimeStamp(Recorder.CurentTimeInMilis());
        }

        /**
         * Construct a {@link SingleWriterDoubleRecorder} dynamic range of values to cover and a number
         * of significant decimal digits.
         *
         * @param highestToLowestValueRatio specifies the dynamic range to use (as a ratio)
         * @param numberOfSignificantValueDigits Specifies the precision to use. This is the number of significant
         *                                       decimal digits to which the histogram will maintain value resolution
         *                                       and separation. Must be a non-negative integer between 0 and 5.
         */

        public SingleWriterDoubleRecorder(long highestToLowestValueRatio,
            int numberOfSignificantValueDigits)
        {
            activeHistogram = new InternalDoubleHistogram(
                instanceId, highestToLowestValueRatio, numberOfSignificantValueDigits);
            inactiveHistogram = new InternalDoubleHistogram(
                instanceId, highestToLowestValueRatio, numberOfSignificantValueDigits);
            activeHistogram.setStartTimeStamp(Recorder.CurentTimeInMilis());
        }

        /**
         * Record a value
         * @param value the value to record
         * @throws ArrayIndexOutOfBoundsException (may throw) if value is exceeds highestTrackableValue
         */

        public void recordValue(double value)
        {
            long criticalValueAtEnter = recordingPhaser.WriterCriticalSectionEnter();
            try
            {
                activeHistogram.recordValue(value);
            }
            finally
            {
                recordingPhaser.WriterCriticalSectionExit(criticalValueAtEnter);
            }
        }

        /**
         * Record a value
         * <p>
         * To compensate for the loss of sampled values when a recorded value is larger than the expected
         * interval between value samples, Histogram will auto-generate an additional series of decreasingly-smaller
         * (down to the expectedIntervalBetweenValueSamples) value records.
         * <p>
         * See related notes {@link org.HdrHistogram.DoubleHistogram#RecordValueWithExpectedInterval(double, double)}
         * for more explanations about coordinated omission and expected interval correction.
         *      *
         * @param value The value to record
         * @param expectedIntervalBetweenValueSamples If expectedIntervalBetweenValueSamples is larger than 0, add
         *                                           auto-generated value records as appropriate if value is larger
         *                                           than expectedIntervalBetweenValueSamples
         * @throws ArrayIndexOutOfBoundsException (may throw) if value is exceeds highestTrackableValue
         */

        public void recordValueWithExpectedInterval(double value, double expectedIntervalBetweenValueSamples)
        {
            long criticalValueAtEnter = recordingPhaser.WriterCriticalSectionEnter();
            try
            {
                activeHistogram.recordValueWithExpectedInterval(value, expectedIntervalBetweenValueSamples);
            }
            finally
            {
                recordingPhaser.WriterCriticalSectionExit(criticalValueAtEnter);
            }
        }

        /**
         * Get a new instance of an interval histogram, which will include a stable, consistent view of all value
         * counts accumulated since the last interval histogram was taken.
         * <p>
         * Calling {@link SingleWriterDoubleRecorder#GetIntervalHistogram()} will reset
         * the value counts, and start accumulating value counts for the next interval.
         *
         * @return a histogram containing the value counts accumulated since the last interval histogram was taken.
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DoubleHistogram getIntervalHistogram()
        {
            return getIntervalHistogram(null);
        }

        /**
         * Get an interval histogram, which will include a stable, consistent view of all value counts
         * accumulated since the last interval histogram was taken.
         * <p>
         * {@link SingleWriterDoubleRecorder#GetIntervalHistogram(DoubleHistogram histogramToRecycle)
         * GetIntervalHistogram(histogramToRecycle)}
         * accepts a previously returned interval histogram that can be recycled internally to avoid allocation
         * and content copying operations, and is therefore significantly more efficient for repeated use than
         * {@link SingleWriterDoubleRecorder#GetIntervalHistogram()} and
         * {@link SingleWriterDoubleRecorder#getIntervalHistogramInto getIntervalHistogramInto()}. The
         * provided {@code histogramToRecycle} must
         * be either be null or an interval histogram returned by a previous call to
         * {@link SingleWriterDoubleRecorder#GetIntervalHistogram(DoubleHistogram histogramToRecycle)
         * GetIntervalHistogram(histogramToRecycle)} or
         * {@link SingleWriterDoubleRecorder#GetIntervalHistogram()}.
         * <p>
         * NOTE: The caller is responsible for not recycling the same returned interval histogram more than once. If
         * the same interval histogram instance is recycled more than once, behavior is undefined.
         * <p>
         * Calling
         * {@link SingleWriterDoubleRecorder#GetIntervalHistogram(DoubleHistogram histogramToRecycle)
         * GetIntervalHistogram(histogramToRecycle)} will reset the value counts, and start accumulating value
         * counts for the next interval
         *
         * @param histogramToRecycle a previously returned interval histogram that may be recycled to avoid allocation and
         *                           copy operations.
         * @return a histogram containing the value counts accumulated since the last interval histogram was taken.
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public DoubleHistogram getIntervalHistogram(DoubleHistogram histogramToRecycle)
        {
            if (histogramToRecycle == null)
            {
                histogramToRecycle = new InternalDoubleHistogram(inactiveHistogram);
            }
            // Verify that replacement histogram can validly be used as an inactive histogram replacement:
            validateFitAsReplacementHistogram(histogramToRecycle);
            try
            {
                recordingPhaser.ReaderLock();
                inactiveHistogram = (InternalDoubleHistogram)histogramToRecycle;
                performIntervalSample();
                return inactiveHistogram;
            }
            finally
            {
                recordingPhaser.ReaderUnlock();
            }
        }

        /**
         * Place a copy of the value counts accumulated since accumulated (since the last interval histogram
         * was taken) into {@code targetHistogram}.
         *
         * Calling {@link SingleWriterDoubleRecorder#getIntervalHistogramInto}() will
         * reset the value counts, and start accumulating value counts for the next interval.
         *
         * @param targetHistogram the histogram into which the interval histogram's data should be copied
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void getIntervalHistogramInto(DoubleHistogram targetHistogram)
        {
            performIntervalSample();
            inactiveHistogram.copyInto(targetHistogram);
        }

        /**
         * Reset any value counts accumulated thus far.
         */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void reset()
        {
            // the currently inactive histogram is reset each time we flip. So flipping twice resets both:
            performIntervalSample();
            performIntervalSample();
        }

        private void performIntervalSample()
        {
            inactiveHistogram.reset();
            try
            {
                recordingPhaser.ReaderLock();

                // Swap active and inactive histograms:
                InternalDoubleHistogram tempHistogram = inactiveHistogram;
                inactiveHistogram = activeHistogram;
                activeHistogram = tempHistogram;

                // Mark end time of previous interval and start time of new one:
                long now = Recorder.CurentTimeInMilis();
                activeHistogram.setStartTimeStamp(now);
                inactiveHistogram.setEndTimeStamp(now);

                // Make sure we are not in the middle of recording a value on the previously active histogram:

                // Flip phase to make sure no recordings that were in flight pre-flip are still active:
                recordingPhaser.FlipPhase(500000L /* yield in 0.5 msec units if needed */);
            }
            finally
            {
                recordingPhaser.ReaderUnlock();
            }
        }

        private class InternalDoubleHistogram : DoubleHistogram
        {
            public readonly long containingInstanceId;

            public InternalDoubleHistogram(long id, int numberOfSignificantValueDigits)
                : base(numberOfSignificantValueDigits)
            {
                this.containingInstanceId = id;
            }

            public InternalDoubleHistogram(long id,
                long highestToLowestValueRatio,
                int numberOfSignificantValueDigits)
                : base(highestToLowestValueRatio, numberOfSignificantValueDigits)
            {
                this.containingInstanceId = id;
            }

            public InternalDoubleHistogram(InternalDoubleHistogram source)
                : base(source)
            {
                this.containingInstanceId = source.containingInstanceId;
            }
        }

        private void validateFitAsReplacementHistogram(DoubleHistogram replacementHistogram)
        {
            var internalHistogram = replacementHistogram as InternalDoubleHistogram;
            if (internalHistogram == null || internalHistogram.containingInstanceId != activeHistogram.containingInstanceId)
            {
                throw new ArgumentException("replacement histogram must have been obtained via a previous" +
                                            "GetIntervalHistogram() call from this " + this.GetType().Name + " instance");
            }
        }
    }
}
