// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{

    public class HistogramDataAccessTest
    {
        static long highestTrackableValue = 3600L * 1000 * 1000; // 1 hour in usec units
        static int numberOfSignificantValueDigits = 3; // Maintain at least 3 decimal points of accuracy
        static Histogram histogram;
        static Histogram scaledHistogram;
        static Histogram rawHistogram;
        static Histogram scaledRawHistogram;
        static Histogram postCorrectedHistogram;
        static Histogram postCorrectedScaledHistogram;

        static HistogramDataAccessTest()
        {

            histogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            scaledHistogram = new Histogram(1000, highestTrackableValue * 512, numberOfSignificantValueDigits);
            rawHistogram = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            scaledRawHistogram = new Histogram(1000, highestTrackableValue * 512, numberOfSignificantValueDigits);
            // Log hypothetical scenario: 100 seconds of "perfect" 1msec results, sampled
            // 100 times per second (10,000 results), followed by a 100 second pause with
            // a single (100 second) recorded result. Recording is done indicating an expected
            // interval between samples of 10 msec:
            for (int i = 0; i < 10000; i++)
            {
                histogram.RecordValueWithExpectedInterval(1000 /* 1 msec */, 10000 /* 10 msec expected interval */);
                scaledHistogram.RecordValueWithExpectedInterval(1000 * 512 /* 1 msec */, 10000 * 512 /* 10 msec expected interval */);
                rawHistogram.RecordValue(1000 /* 1 msec */);
                scaledRawHistogram.RecordValue(1000 * 512/* 1 msec */);
            }
            histogram.RecordValueWithExpectedInterval(100000000L /* 100 sec */, 10000 /* 10 msec expected interval */);
            scaledHistogram.RecordValueWithExpectedInterval(100000000L * 512 /* 100 sec */, 10000 * 512 /* 10 msec expected interval */);
            rawHistogram.RecordValue(100000000L /* 100 sec */);
            scaledRawHistogram.RecordValue(100000000L * 512 /* 100 sec */);

            postCorrectedHistogram = rawHistogram.copyCorrectedForCoordinatedOmission(10000 /* 10 msec expected interval */) as Histogram;
            postCorrectedScaledHistogram = scaledRawHistogram.copyCorrectedForCoordinatedOmission(10000 * 512 /* 10 msec expected interval */) as Histogram;
        }

        private void AssertEqual(string because, double expected, double value, double epsilon)
        {
            value.Should().BeApproximately(expected, epsilon, because);
        }

        private void AssertEqual(string because, long expected, long value)
        {
            value.Should().Be(expected, because);
        }

        [Fact]
        public void testScalingEquivalence()
        {
            AssertEqual("averages should be equivalent",
                    histogram.getMean() * 512,
                    scaledHistogram.getMean(), scaledHistogram.getMean() * 0.000001);
            AssertEqual("total count should be the same",
                    histogram.getTotalCount(),
                    scaledHistogram.getTotalCount());
            AssertEqual("99%'iles should be equivalent",
                    scaledHistogram.highestEquivalentValue(histogram.getValueAtPercentile(99.0) * 512),
                    scaledHistogram.highestEquivalentValue(scaledHistogram.getValueAtPercentile(99.0)));
            AssertEqual("Max should be equivalent",
                    scaledHistogram.highestEquivalentValue(histogram.getMaxValue() * 512),
                    scaledHistogram.getMaxValue());
            // Same for post-corrected:
            AssertEqual("averages should be equivalent",
                    histogram.getMean() * 512,
                    scaledHistogram.getMean(), scaledHistogram.getMean() * 0.000001);
            AssertEqual("total count should be the same",
                    postCorrectedHistogram.getTotalCount(),
                    postCorrectedScaledHistogram.getTotalCount());
            AssertEqual("99%'iles should be equivalent",
                    postCorrectedHistogram.lowestEquivalentValue(postCorrectedHistogram.getValueAtPercentile(99.0)) * 512,
                    postCorrectedScaledHistogram.lowestEquivalentValue(postCorrectedScaledHistogram.getValueAtPercentile(99.0)));
            AssertEqual("Max should be equivalent",
                    postCorrectedScaledHistogram.highestEquivalentValue(postCorrectedHistogram.getMaxValue() * 512),
                    postCorrectedScaledHistogram.getMaxValue());
        }

        [Fact]
        public void testPreVsPostCorrectionValues()
        {
            // Loop both ways (one would be enough, but good practice just for fun:

            AssertEqual("pre and post corrected count totals ",
                    histogram.getTotalCount(), postCorrectedHistogram.getTotalCount());

            // The following comparison loops would have worked in a perfect accuracy world, but since post
            // correction is done based on the value extracted from the bucket, and the during-recording is done
            // based on the actual (not pixelized) value, there will be subtle differences due to roundoffs:

            //        for (HistogramIterationValue v : histogram.allValues()) {
            //            long preCorrectedCount = v.getCountAtValueIteratedTo();
            //            long postCorrectedCount = postCorrectedHistogram.getCountAtValue(v.getValueIteratedTo());
            //            AssertEqual("pre and post corrected count at value " + v.getValueIteratedTo(),
            //                    preCorrectedCount, postCorrectedCount);
            //        }
            //
            //        for (HistogramIterationValue v : postCorrectedHistogram.allValues()) {
            //            long preCorrectedCount = v.getCountAtValueIteratedTo();
            //            long postCorrectedCount = histogram.getCountAtValue(v.getValueIteratedTo());
            //            AssertEqual("pre and post corrected count at value " + v.getValueIteratedTo(),
            //                    preCorrectedCount, postCorrectedCount);
            //        }

        }

        [Fact]
        public void testGetTotalCount()
        {
            // The overflow value should count in the total count:
            AssertEqual("Raw total count is 10,001",
                    10001L, rawHistogram.getTotalCount());
            AssertEqual("Total count is 20,000",
                    20000L, histogram.getTotalCount());
        }

        [Fact]
        public void testGetMaxValue()
        {
            Assert.True(
                    histogram.valuesAreEquivalent(100L * 1000 * 1000,
                            histogram.getMaxValue()));
        }

        [Fact]
        public void testGetMinValue()
        {
            Assert.True(
                    histogram.valuesAreEquivalent(1000,
                            histogram.getMinValue()));
        }

        [Fact]
        public void testGetMean()
        {
            double expectedRawMean = ((10000.0 * 1000) + (1.0 * 100000000)) / 10001; /* direct avg. of raw results */
            double expectedMean = (1000.0 + 50000000.0) / 2; /* avg. 1 msec for half the time, and 50 sec for other half */
            // We expect to see the mean to be accurate to ~3 decimal points (~0.1%):
            AssertEqual("Raw mean is " + expectedRawMean + " +/- 0.1%",
                    expectedRawMean, rawHistogram.getMean(), expectedRawMean * 0.001);
            AssertEqual("Mean is " + expectedMean + " +/- 0.1%",
                    expectedMean, histogram.getMean(), expectedMean * 0.001);
        }

        [Fact]
        public void testGetStdDeviation()
        {
            double expectedRawMean = ((10000.0 * 1000) + (1.0 * 100000000)) / 10001; /* direct avg. of raw results */
            double expectedRawStdDev =
                    Math.Sqrt(
                        ((10000.0 * Math.Pow((1000.0 - expectedRawMean), 2)) +
                                Math.Pow((100000000.0 - expectedRawMean), 2)) /
                                10001);

            double expectedMean = (1000.0 + 50000000.0) / 2; /* avg. 1 msec for half the time, and 50 sec for other half */
            double expectedSquareDeviationSum = 10000 * Math.Pow((1000.0 - expectedMean), 2);
            for (long value = 10000; value <= 100000000; value += 10000)
            {
                expectedSquareDeviationSum += Math.Pow((value - expectedMean), 2);
            }
            double expectedStdDev = Math.Sqrt(expectedSquareDeviationSum / 20000);

            // We expect to see the standard deviations to be accurate to ~3 decimal points (~0.1%):
            AssertEqual("Raw standard deviation is " + expectedRawStdDev + " +/- 0.1%",
                    expectedRawStdDev, rawHistogram.getStdDeviation(), expectedRawStdDev * 0.001);
            AssertEqual("Standard deviation is " + expectedStdDev + " +/- 0.1%",
                    expectedStdDev, histogram.getStdDeviation(), expectedStdDev * 0.001);
        }

        [Fact]
        public void testGetValueAtPercentile()
        {
            AssertEqual("raw 30%'ile is 1 msec +/- 0.1%",
                    1000.0, (double)rawHistogram.getValueAtPercentile(30.0),
                    1000.0 * 0.001);
            AssertEqual("raw 99%'ile is 1 msec +/- 0.1%",
                    1000.0, (double)rawHistogram.getValueAtPercentile(99.0),
                    1000.0 * 0.001);
            AssertEqual("raw 99.99%'ile is 1 msec +/- 0.1%",
                    1000.0, (double)rawHistogram.getValueAtPercentile(99.99)
                    , 1000.0 * 0.001);
            AssertEqual("raw 99.999%'ile is 100 sec +/- 0.1%",
                    100000000.0, (double)rawHistogram.getValueAtPercentile(99.999),
                    100000000.0 * 0.001);
            AssertEqual("raw 100%'ile is 100 sec +/- 0.1%",
                    100000000.0, (double)rawHistogram.getValueAtPercentile(100.0),
                    100000000.0 * 0.001);

            AssertEqual("30%'ile is 1 msec +/- 0.1%",
                    1000.0, (double)histogram.getValueAtPercentile(30.0),
                    1000.0 * 0.001);
            AssertEqual("50%'ile is 1 msec +/- 0.1%",
                    1000.0, (double)histogram.getValueAtPercentile(50.0),
                    1000.0 * 0.001);
            AssertEqual("75%'ile is 50 sec +/- 0.1%",
                    50000000.0, (double)histogram.getValueAtPercentile(75.0),
                    50000000.0 * 0.001);
            AssertEqual("90%'ile is 80 sec +/- 0.1%",
                    80000000.0, (double)histogram.getValueAtPercentile(90.0),
                    80000000.0 * 0.001);
            AssertEqual("99%'ile is 98 sec +/- 0.1%",
                    98000000.0, (double)histogram.getValueAtPercentile(99.0),
                    98000000.0 * 0.001);
            AssertEqual("99.999%'ile is 100 sec +/- 0.1%",
                    100000000.0, (double)histogram.getValueAtPercentile(99.999),
                    100000000.0 * 0.001);
            AssertEqual("100%'ile is 100 sec +/- 0.1%",
                    100000000.0, (double)histogram.getValueAtPercentile(100.0),
                    100000000.0 * 0.001);
        }

        [Fact]
        public void testGetValueAtPercentileForLargeHistogram()
        {
            long largestValue = 1000000000000L;
            Histogram h = new Histogram(largestValue, 5);
            h.RecordValue(largestValue);

            Assert.True(h.getValueAtPercentile(100.0) > 0);
        }


        [Fact]
        public void testGetPercentileAtOrBelowValue()
        {
            AssertEqual("Raw percentile at or below 5 msec is 99.99% +/- 0.0001",
                    99.99,
                    rawHistogram.getPercentileAtOrBelowValue(5000), 0.0001);
            AssertEqual("Percentile at or below 5 msec is 50% +/- 0.0001%",
                    50.0,
                    histogram.getPercentileAtOrBelowValue(5000), 0.0001);
            AssertEqual("Percentile at or below 100 sec is 100% +/- 0.0001%",
                    100.0,
                    histogram.getPercentileAtOrBelowValue(100000000L), 0.0001);
        }

        [Fact]
        public void testGetCountBetweenValues()
        {
            AssertEqual("Count of raw values between 1 msec and 1 msec is 1",
                    10000, rawHistogram.getCountBetweenValues(1000L, 1000L));
            AssertEqual("Count of raw values between 5 msec and 150 sec is 1",
                    1, rawHistogram.getCountBetweenValues(5000L, 150000000L));
            AssertEqual("Count of values between 5 msec and 150 sec is 10,000",
                    10000, histogram.getCountBetweenValues(5000L, 150000000L));
        }

        [Fact]
        public void testGetCountAtValue()
        {
            AssertEqual("Count of raw values at 10 msec is 0",
                    0, rawHistogram.getCountBetweenValues(10000L, 10010L));
            AssertEqual("Count of values at 10 msec is 0",
                    1, histogram.getCountBetweenValues(10000L, 10010L));
            AssertEqual("Count of raw values at 1 msec is 10,000",
                    10000, rawHistogram.getCountAtValue(1000L));
            AssertEqual("Count of values at 1 msec is 10,000",
                    10000, histogram.getCountAtValue(1000L));
        }

        [Fact]
        public void testPercentiles()
        {
            foreach (HistogramIterationValue v in histogram.Percentiles(5 /* ticks per half */))
            {
                AssertEqual("Value at Iterated-to Percentile is the same as the matching getValueAtPercentile():\n" +
                        "getPercentileLevelIteratedTo = " + v.getPercentileLevelIteratedTo() +
                        "\ngetValueIteratedTo = " + v.getValueIteratedTo() +
                        "\ngetValueIteratedFrom = " + v.getValueIteratedFrom() +
                        "\ngetValueAtPercentile(getPercentileLevelIteratedTo()) = " +
                        histogram.getValueAtPercentile(v.getPercentileLevelIteratedTo()) +
                        "\ngetPercentile = " + v.getPercentile() +
                        "\ngetValueAtPercentile(getPercentile())" +
                        histogram.getValueAtPercentile(v.getPercentile()) +
                        "\nequivalent1 = " +
                        histogram.highestEquivalentValue(histogram.getValueAtPercentile(v.getPercentileLevelIteratedTo())) +
                        "\nequivalent2 = " +
                        histogram.highestEquivalentValue(histogram.getValueAtPercentile(v.getPercentile())) +
                        "\n"
                        ,
                        v.getValueIteratedTo(),
                        histogram.highestEquivalentValue(histogram.getValueAtPercentile(v.getPercentile())));
            }
        }

        [Fact]
        public void testLinearBucketValues()
        {
            int index = 0;
            // Note that using linear buckets should work "as expected" as long as the number of linear buckets
            // is lower than the resolution level determined by largestValueWithSingleUnitResolution
            // (2000 in this case). Above that count, some of the linear buckets can end up rounded up in size
            // (to the nearest local resolution unit level), which can result in a smaller number of buckets that
            // expected covering the range.

            // Iterate raw data using linear buckets of 100 msec each.
            foreach (HistogramIterationValue v in rawHistogram.LinearBucketValues(100000))
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Raw Linear 100 msec bucket # 0 added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                else if (index == 999)
                {
                    AssertEqual("Raw Linear 100 msec bucket # 999 added a count of 1",
                            1, countAddedInThisBucket);
                }
                else
                {
                    AssertEqual("Raw Linear 100 msec bucket # " + index + " added a count of 0",
                            0, countAddedInThisBucket);
                }
                index++;
            }
            index.Should().Be(1000);

            index = 0;
            long totalAddedCounts = 0;
            // Iterate data using linear buckets of 10 msec each.
            foreach (HistogramIterationValue v in histogram.LinearBucketValues(10000))
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Linear 1 sec bucket # 0 [" +
                            v.getValueIteratedFrom() + ".." + v.getValueIteratedTo() +
                            "] added a count of 10001",
                            10001, countAddedInThisBucket);
                }
                // Because value resolution is low enough (3 digits) that multiple linear buckets will end up
                // residing in a single value-equivalent range, some linear buckets will have counts of 2 or
                // more, and some will have 0 (when the first bucket in the equivalent range was the one that
                // got the total count bump).
                // However, we can still verify the sum of counts added in all the buckets...
                totalAddedCounts += v.getCountAddedInThisIterationStep();
                index++;
            }
            AssertEqual("There should be 10000 linear buckets of size 10000 usec between 0 and 100 sec.",
                    10000, index);
            AssertEqual("Total added counts should be 20000", 20000, totalAddedCounts);

            index = 0;
            totalAddedCounts = 0;
            // Iterate data using linear buckets of 1 msec each.
            foreach (HistogramIterationValue v in histogram.LinearBucketValues(1000))
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Linear 1 sec bucket # 0 [" +
                                 v.getValueIteratedFrom() + ".." + v.getValueIteratedTo() +
                                 "] added a count of 10000",
                        10000, countAddedInThisBucket);
                }
                // Because value resolution is low enough (3 digits) that multiple linear buckets will end up
                // residing in a single value-equivalent range, some linear buckets will have counts of 2 or
                // more, and some will have 0 (when the first bucket in the equivalent range was the one that
                // got the total count bump).
                // However, we can still verify the sum of counts added in all the buckets...
                totalAddedCounts += v.getCountAddedInThisIterationStep();
                index++;
            }
            // You may ask "why 100007 and not 100000?" for the value below? The answer is that at this fine
            // a linear stepping resolution, the populated sub-bucket (at 100 seconds with 3 decimal
            // point resolution) is larger than our liner stepping, and holds more than one linear 1 msec
            // step in it.
            // Since we only know we're done with linear iteration when the next iteration step will step
            // out of the last populated bucket, there is not way to tell if the iteration should stop at
            // 100000 or 100007 steps. The proper thing to do is to run to the end of the sub-bucket quanta...
            AssertEqual("There should be 100007 linear buckets of size 1000 usec between 0 and 100 sec.",
                    100007, index);
            AssertEqual("Total added counts should be 20000", 20000, totalAddedCounts);


        }

        [Fact]
        public void testLogarithmicBucketValues()
        {
            int index = 0;
            // Iterate raw data using logarithmic buckets starting at 10 msec.
            foreach (HistogramIterationValue v in rawHistogram.LogarithmicBucketValues(10000, 2))
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Raw Logarithmic 10 msec bucket # 0 added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                else if (index == 14)
                {
                    AssertEqual("Raw Logarithmic 10 msec bucket # 14 added a count of 1",
                            1, countAddedInThisBucket);
                }
                else
                {
                    AssertEqual("Raw Logarithmic 100 msec bucket # " + index + " added a count of 0",
                            0, countAddedInThisBucket);
                }
                index++;
            }
            (index - 1).Should().Be(14);

            index = 0;
            long totalAddedCounts = 0;
            foreach (HistogramIterationValue v in histogram.LogarithmicBucketValues(10000, 2))
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Logarithmic 10 msec bucket # 0 [" +
                            v.getValueIteratedFrom() + ".." + v.getValueIteratedTo() +
                            "] added a count of 10001",
                            10001, countAddedInThisBucket);
                }
                totalAddedCounts += v.getCountAddedInThisIterationStep();
                index++;
            }
            AssertEqual("There should be 14 Logarithmic buckets of size 10000 usec between 0 and 100 sec.",
                    14, index - 1);
            AssertEqual("Total added counts should be 20000", 20000, totalAddedCounts);
        }

        [Fact]
        public void testRecordedValues()
        {
            int index = 0;
            // Iterate raw data by stepping through every value that has a count recorded:
            foreach (HistogramIterationValue v in rawHistogram.RecordedValues())
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Raw recorded value bucket # 0 added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                else
                {
                    AssertEqual("Raw recorded value bucket # " + index + " added a count of 1",
                            1, countAddedInThisBucket);
                }
                index++;
            }
            index.Should().Be(2);

            index = 0;
            long totalAddedCounts = 0;
            foreach (HistogramIterationValue v in histogram.RecordedValues())
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 0)
                {
                    AssertEqual("Recorded bucket # 0 [" +
                            v.getValueIteratedFrom() + ".." + v.getValueIteratedTo() +
                            "] added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                Assert.True(v.getCountAtValueIteratedTo() != 0, "The count in recorded bucket #" + index + " is not 0");
                AssertEqual("The count in recorded bucket #" + index +
                        " is exactly the amount added since the last iteration ",
                        v.getCountAtValueIteratedTo(), v.getCountAddedInThisIterationStep());
                totalAddedCounts += v.getCountAddedInThisIterationStep();
                index++;
            }
            AssertEqual("Total added counts should be 20000", 20000, totalAddedCounts);
        }

        [Fact]
        public void testAllValues()
        {
            int index = 0;
            long latestValueAtIndex = 0;
            long totalCountToThisPoint = 0;
            long totalValueToThisPoint = 0;
            // Iterate raw data by stepping through every value that has a count recorded:
            foreach (HistogramIterationValue v in rawHistogram.AllValues())
            {
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 1000)
                {
                    AssertEqual("Raw allValues bucket # 0 added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                else if (histogram.valuesAreEquivalent(v.getValueIteratedTo(), 100000000))
                {
                    AssertEqual("Raw allValues value bucket # " + index + " added a count of 1",
                            1, countAddedInThisBucket);
                }
                else
                {
                    AssertEqual("Raw allValues value bucket # " + index + " added a count of 0",
                            0, countAddedInThisBucket);
                }
                latestValueAtIndex = v.getValueIteratedTo();
                totalCountToThisPoint += v.getCountAtValueIteratedTo();
                AssertEqual("total Count should match", totalCountToThisPoint, v.getTotalCountToThisValue());
                totalValueToThisPoint += v.getCountAtValueIteratedTo() * latestValueAtIndex;
                AssertEqual("total Value should match", totalValueToThisPoint, v.getTotalValueToThisValue());
                index++;
            }
            AssertEqual("index should be equal to countsArrayLength", histogram.countsArrayLength, index);

            index = 0;
            long totalAddedCounts = 0;
            HistogramIterationValue v1 = null;
            foreach (HistogramIterationValue v in histogram.AllValues())
            {
                v1 = v;
                long countAddedInThisBucket = v.getCountAddedInThisIterationStep();
                if (index == 1000)
                {
                    AssertEqual("AllValues bucket # 0 [" +
                            v.getValueIteratedFrom() + ".." + v.getValueIteratedTo() +
                            "] added a count of 10000",
                            10000, countAddedInThisBucket);
                }
                AssertEqual("The count in AllValues bucket #" + index +
                        " is exactly the amount added since the last iteration ",
                        v.getCountAtValueIteratedTo(), v.getCountAddedInThisIterationStep());
                totalAddedCounts += v.getCountAddedInThisIterationStep();
                Assert.True(histogram.valuesAreEquivalent(histogram.valueFromIndex(index), v.getValueIteratedTo()),
                    "valueFromIndex(index) should be equal to getValueIteratedTo()");
                index++;
            }
            AssertEqual("index should be equal to countsArrayLength", histogram.countsArrayLength, index);
            AssertEqual("Total added counts should be 20000", 20000, totalAddedCounts);
        }

        [Fact]
        public void testVerifyManualAllValuesDuplication()
        {
            var histogram1 = histogram.copy();

            var values = histogram1.AllValues();
            List<long> ranges = new List<long>();
            List<long> counts = new List<long>();
            int index = 0;
            foreach (HistogramIterationValue value in values)
            {
                if (value.getCountAddedInThisIterationStep() > 0)
                {
                    ranges.Add(value.getValueIteratedTo());
                    counts.Add(value.getCountAddedInThisIterationStep());
                }
                index++;
            }
            AssertEqual("index should be equal to countsArrayLength", histogram1.countsArrayLength, index);

            AbstractHistogram histogram2 = new Histogram(highestTrackableValue, numberOfSignificantValueDigits);
            for (int i = 0; i < ranges.Count; ++i)
            {
                histogram2.RecordValueWithCount(ranges[i], counts[i]);
            }

            Assert.True(histogram1.Equals(histogram2), "Histograms should be equal");
        }
    }

}
