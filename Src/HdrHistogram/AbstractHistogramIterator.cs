﻿/**
* Written by Gil Tene of Azul Systems, and released to the public domain,
* as explained at http://creativecommons.org/publicdomain/zero/1.0/
*
* @author Gil Tene
*/

using System;

namespace HdrHistogram
{

    /**
     * Used for iterating through histogram values.
     */
    public abstract class AbstractHistogramIterator : Iterator<HistogramIterationValue>
    {
        protected AbstractHistogram histogram;
        protected long savedHistogramTotalRawCount;

        protected int currentIndex;
        protected long currentValueAtIndex;

        protected long nextValueAtIndex;

        long prevValueIteratedTo;
        long totalCountToPrevIndex;

        protected long totalCountToCurrentIndex;
        long totalValueToCurrentIndex;

        protected long arrayTotalCount;
        protected long countAtThisValue;

        private bool freshSubBucket;
        HistogramIterationValue currentIterationValue = new HistogramIterationValue();

        private double integerToDoubleValueConversionRatio;

        protected void resetIterator(AbstractHistogram histogram)
        {
            this.histogram = histogram;
            this.savedHistogramTotalRawCount = histogram.getTotalCount();
            this.arrayTotalCount = histogram.getTotalCount();
            this.integerToDoubleValueConversionRatio = histogram.getIntegerToDoubleValueConversionRatio();
            this.currentIndex = 0;
            this.currentValueAtIndex = 0;
            this.nextValueAtIndex = 1 << histogram.unitMagnitude;
            this.prevValueIteratedTo = 0;
            this.totalCountToPrevIndex = 0;
            this.totalCountToCurrentIndex = 0;
            this.totalValueToCurrentIndex = 0;
            this.countAtThisValue = 0;
            this.freshSubBucket = true;
            currentIterationValue.reset();
        }

        /**
         * Returns true if the iteration has more elements. (In other words, returns true if next would return an
         * element rather than throwing an exception.)
         *
         * @return true if the iterator has more elements.
         */

        public override bool hasNext()
        {
            if (histogram.getTotalCount() != savedHistogramTotalRawCount)
            {
                throw new InvalidOperationException("ConcurrentModificationException");
            }
            return (totalCountToCurrentIndex < arrayTotalCount);
        }

        /**
         * Returns the next element in the iteration.
         *
         * @return the {@link HistogramIterationValue} associated with the next element in the iteration.
         */

        public override HistogramIterationValue next()
        {
            // Move through the sub buckets and buckets until we hit the next reporting level:
            while (!exhaustedSubBuckets())
            {
                countAtThisValue = histogram.getCountAtIndex(currentIndex);
                if (freshSubBucket)
                { // Don't add unless we've incremented since last bucket...
                    totalCountToCurrentIndex += countAtThisValue;
                    totalValueToCurrentIndex += countAtThisValue * histogram.highestEquivalentValue(currentValueAtIndex);
                    freshSubBucket = false;
                }
                if (reachedIterationLevel())
                {
                    long valueIteratedTo = getValueIteratedTo();
                    currentIterationValue.set(valueIteratedTo, prevValueIteratedTo, countAtThisValue,
                            (totalCountToCurrentIndex - totalCountToPrevIndex), totalCountToCurrentIndex,
                            totalValueToCurrentIndex, ((100.0 * totalCountToCurrentIndex) / arrayTotalCount),
                            getPercentileIteratedTo(), integerToDoubleValueConversionRatio);
                    prevValueIteratedTo = valueIteratedTo;
                    totalCountToPrevIndex = totalCountToCurrentIndex;
                    // move the next iteration level forward:
                    incrementIterationLevel();
                    if (histogram.getTotalCount() != savedHistogramTotalRawCount)
                    {
                        throw new InvalidOperationException("ConcurrentModificationException");
                    }
                    return currentIterationValue;
                }
                incrementSubBucket();
            }
            // Should not reach here. But possible for overflowed histograms under certain conditions
            throw new IndexOutOfRangeException();
        }

        /**
         * Not supported. Will throw an {@link UnsupportedOperationException}.
         */

        protected override void remove()
        {
            throw new NotSupportedException();
        }

        protected abstract void incrementIterationLevel();

        protected abstract bool reachedIterationLevel();

        double getPercentileIteratedTo()
        {
            return (100.0 * (double)totalCountToCurrentIndex) / arrayTotalCount;
        }

        double getPercentileIteratedFrom()
        {
            return (100.0 * (double)totalCountToPrevIndex) / arrayTotalCount;
        }

        long getValueIteratedTo()
        {
            return histogram.highestEquivalentValue(currentValueAtIndex);
        }

        private bool exhaustedSubBuckets()
        {
            return (currentIndex >= histogram.countsArrayLength);
        }

        void incrementSubBucket()
        {
            freshSubBucket = true;
            // Take on the next index:
            currentIndex++;
            currentValueAtIndex = histogram.valueFromIndex(currentIndex);
            // Figure out the value at the next index (used by some iterators):
            nextValueAtIndex = histogram.valueFromIndex(currentIndex + 1);
        }

    }

}
