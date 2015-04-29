
using System;

namespace HdrHistogram.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Histogram h = new Histogram(100000000000, 3);

            h.RecordValue(100);

            var x = h.getValueAtPercentile(50);

            h.OutputPercentileDistribution(Console.Out, 1000);

            Console.ReadKey();
        }
    }
}
