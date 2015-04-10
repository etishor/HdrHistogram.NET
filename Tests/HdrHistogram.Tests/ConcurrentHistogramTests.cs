// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo

using System;
using System.Threading;
using FluentAssertions;
using Xunit;

namespace HdrHistogram.Tests
{
    public class ConcurrentHistogramTests
    {
        static readonly long highestTrackableValue = 3600L * 1000 * 1000 * 1000; // e.g. for 1 hr in usec units
        static volatile bool doRun = true;
        static volatile bool waitToGo = true;

        [Fact]
        public void testConcurrentAutoSizedRecording()
        {
            doConcurrentRecordValues();
        }

        private void doConcurrentRecordValues()
        {
            ConcurrentHistogram histogram = new ConcurrentHistogram(2);
            ValueRecorder[] valueRecorders = new ValueRecorder[24];
            doRun = true;
            waitToGo = true;
            for (int i = 0; i < valueRecorders.Length; i++)
            {
                valueRecorders[i] = new ValueRecorder(histogram);
                valueRecorders[i].Start();
            }

            long sumOfCounts;

            // TODO: investigate why 500 takes too long to complete (it is probably normal)
            for (int i = 0; i < 50; i++)
            {

                // Ready:
                sumOfCounts = 0;
                foreach (ValueRecorder v in valueRecorders)
                {
                    v.readySem.WaitOne();
                    sumOfCounts += v.count;
                }

                histogram.getTotalCount().Should().Be(sumOfCounts, "totalCount must be equal to sum of counts");

                // Set:
                waitToGo = true;
                histogram = new ConcurrentHistogram(2);
                foreach (ValueRecorder v in valueRecorders)
                {
                    v.histogram = histogram;
                    v.count = 0;
                    v.setSem.Release();
                }

                Thread.Sleep(1);

                // Go! :
                waitToGo = false;
            }
            doRun = false;
        }

        static AtomicLong valueRecorderId = new AtomicLong(42);

        private class ValueRecorder
        {
            private readonly Thread thread;
            public ConcurrentHistogram histogram;
            public long count = 0;
            public Semaphore readySem = new Semaphore(0, 1);
            public Semaphore setSem = new Semaphore(0, 1);

            private long id;
            private Random random;

            public ValueRecorder(ConcurrentHistogram histogram)
            {
                this.id = valueRecorderId.GetAndIncrement();
                this.random = new Random((int)id);
                this.histogram = histogram;
                this.thread = new Thread(run);
            }

            public void Start()
            {
                this.thread.Start();
            }

            public void run()
            {
                long nextValue = 0;
                for (int i = 0; i < id; i++)
                {
                    nextValue = (long)(highestTrackableValue * random.NextDouble());
                }
                while (doRun)
                {
                    readySem.Release();
                    setSem.WaitOne();
                    while (waitToGo)
                    {
                        // wait for doRun to be set.
                    }
                    histogram.resize(nextValue);
                    histogram.recordValue(nextValue);
                    count++;
                }
            }
        }

    }

}
