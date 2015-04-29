// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo

using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{

    public class RecorderTests
    {
        static long highestTrackableValue = 3600L * 1000 * 1000; // e.g. for 1 hr in usec units

        [Fact]
        public void testIntervalRecording()
        {
            Histogram histogram = new Histogram(highestTrackableValue, 3);
            DoubleHistogram doubleHistogram = new DoubleHistogram(highestTrackableValue * 1000, 3);
            Recorder recorder1 =
                    new Recorder(highestTrackableValue, 3);
            Recorder recorder2 =
                    new Recorder(highestTrackableValue, 3);
            DoubleRecorder doubleRecorder1 =
                    new DoubleRecorder(highestTrackableValue * 1000, 3);
            DoubleRecorder doubleRecorder2 =
                    new DoubleRecorder(highestTrackableValue * 1000, 3);


            for (int i = 0; i < 10000; i++)
            {
                histogram.RecordValue(3000 * i);
                recorder1.RecordValue(3000 * i);
                recorder2.RecordValue(3000 * i);
                doubleHistogram.recordValue(5000 * i);
                doubleRecorder1.recordValue(5000 * i);
                doubleRecorder2.recordValue(5000 * i);
                doubleHistogram.recordValue(0.001); // Makes some internal shifts happen.
                doubleRecorder1.recordValue(0.001); // Makes some internal shifts happen.
                doubleRecorder2.recordValue(0.001); // Makes some internal shifts happen.
            }

            Histogram histogram2 = recorder1.GetIntervalHistogram();
            histogram2.Equals(histogram).Should().BeTrue();

            recorder2.GetIntervalHistogramInto(histogram2);
            histogram2.Equals(histogram).Should().BeTrue();

            DoubleHistogram doubleHistogram2 = doubleRecorder1.getIntervalHistogram();
            doubleHistogram2.Equals(doubleHistogram).Should().BeTrue();

            doubleRecorder2.getIntervalHistogramInto(doubleHistogram2);
            doubleHistogram2.Equals(doubleHistogram).Should().BeTrue();

            for (int i = 0; i < 5000; i++)
            {
                histogram.RecordValue(3000 * i);
                recorder1.RecordValue(3000 * i);
                recorder2.RecordValue(3000 * i);
                doubleHistogram.recordValue(5000 * i);
                doubleRecorder1.recordValue(5000 * i);
                doubleRecorder2.recordValue(5000 * i);
                doubleHistogram.recordValue(0.001);
                doubleRecorder1.recordValue(0.001);
                doubleRecorder2.recordValue(0.001);
            }

            Histogram histogram3 = recorder1.GetIntervalHistogram();

            Histogram sumHistogram = histogram2.copy() as Histogram;
            sumHistogram.add(histogram3);
            sumHistogram.Equals(histogram).Should().BeTrue();

            DoubleHistogram doubleHistogram3 = doubleRecorder1.getIntervalHistogram();

            DoubleHistogram sumDoubleHistogram = doubleHistogram2.copy();
            sumDoubleHistogram.add(doubleHistogram3);
            sumDoubleHistogram.Equals(doubleHistogram).Should().BeTrue();

            recorder2.GetIntervalHistogram();
            doubleRecorder2.getIntervalHistogram();

            for (int i = 5000; i < 10000; i++)
            {
                histogram.RecordValue(3000 * i);
                recorder1.RecordValue(3000 * i);
                recorder2.RecordValue(3000 * i);
                doubleHistogram.recordValue(5000 * i);
                doubleRecorder1.recordValue(5000 * i);
                doubleRecorder2.recordValue(5000 * i);
                doubleHistogram.recordValue(0.001);
                doubleRecorder1.recordValue(0.001);
                doubleRecorder2.recordValue(0.001);
            }

            Histogram histogram4 = recorder1.GetIntervalHistogram();
            histogram4.add(histogram3);
            Assert.Equal(histogram4, histogram2);

            recorder2.GetIntervalHistogramInto(histogram4);
            histogram4.add(histogram3);
            Assert.Equal(histogram4, histogram2);

            DoubleHistogram doubleHistogram4 = doubleRecorder1.getIntervalHistogram();
            doubleHistogram4.add(doubleHistogram3);
            doubleHistogram2.Equals(doubleHistogram4).Should().BeTrue();

            doubleHistogram4.reset();
            doubleRecorder2.getIntervalHistogramInto(doubleHistogram4);
            doubleHistogram4.add(doubleHistogram3);
            doubleHistogram2.Equals(doubleHistogram4).Should().BeTrue();
        }

        [Fact]
        public void testSimpleAutosizingRecorder()
        {
            Recorder recorder = new Recorder(3);
            Histogram histogram = recorder.GetIntervalHistogram();
            histogram.Should().NotBeNull();
        }

    }

}
