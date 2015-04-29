
namespace HdrHistogram.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.DefaultMaxThreads = 1;
            BenchmarkRunner.DefaultTotalSeconds = 5;

            //BenchmarkRunner.Run("Noop", () => { });

            var histogram = new Histogram(100, 2);
            BenchmarkRunner.Run("Histogram", () => histogram.recordValue(1));

        }
    }
}
