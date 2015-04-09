﻿
using System;

namespace HdrHistogram
{
    /**
 * Written by Gil Tene of Azul Systems, and released to the public domain,
 * as explained at http://creativecommons.org/publicdomain/zero/1.0/
 *
 * @author Gil Tene
 */


    /**
     * Used for iterating through histogram values according to percentile levels. The iteration is
     * performed in steps that start at 0% and reduce their distance to 100% according to the
     * <i>percentileTicksPerHalfDistance</i> parameter, ultimately reaching 100% when all recorded histogram
     * values are exhausted.
    */
    public class PercentileIterator : AbstractHistogramIterator
    {
        int percentileTicksPerHalfDistance;
        double percentileLevelToIterateTo;
        double percentileLevelToIterateFrom;
        bool reachedLastRecordedValue;

        /**
         * Reset iterator for re-use in a fresh iteration over the same histogram data set.
         *
         * @param percentileTicksPerHalfDistance The number of iteration steps per half-distance to 100%.
         */
        public void reset(int percentileTicksPerHalfDistance)
        {
            reset(histogram, percentileTicksPerHalfDistance);
        }

        private void reset(AbstractHistogram histogram, int percentileTicksPerHalfDistance)
        {
            base.resetIterator(histogram);
            this.percentileTicksPerHalfDistance = percentileTicksPerHalfDistance;
            this.percentileLevelToIterateTo = 0.0;
            this.percentileLevelToIterateFrom = 0.0;
            this.reachedLastRecordedValue = false;
        }

        /**
         * @param histogram The histogram this iterator will operate on
         * @param percentileTicksPerHalfDistance The number of iteration steps per half-distance to 100%.
         */
        public PercentileIterator(AbstractHistogram histogram, int percentileTicksPerHalfDistance)
        {
            reset(histogram, percentileTicksPerHalfDistance);
        }

        public override bool hasNext()
        {
            if (base.hasNext())
                return true;
            // We want one additional last step to 100%
            if (!reachedLastRecordedValue && (arrayTotalCount > 0))
            {
                percentileLevelToIterateTo = 100.0;
                reachedLastRecordedValue = true;
                return true;
            }
            return false;
        }

        protected override void incrementIterationLevel()
        {
            percentileLevelToIterateFrom = percentileLevelToIterateTo;
            long percentileReportingTicks =
                    percentileTicksPerHalfDistance *
                            (long)Math.Pow(2,
                                    (long)(Math.Log(100.0 / (100.0 - (percentileLevelToIterateTo))) / Math.Log(2)) + 1);
            percentileLevelToIterateTo += 100.0 / percentileReportingTicks;
        }

        protected override bool reachedIterationLevel()
        {
            if (countAtThisValue == 0)
                return false;
            double currentPercentile = (100.0 * (double)totalCountToCurrentIndex) / arrayTotalCount;
            return (currentPercentile >= percentileLevelToIterateTo);
        }

        double getPercentileIteratedTo()
        {
            return percentileLevelToIterateTo;
        }

        double getPercentileIteratedFrom()
        {
            return percentileLevelToIterateFrom;
        }
    }

}
