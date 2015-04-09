
namespace HdrHistogram.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Histogram h = new Histogram(100000000000, 3);

            h.recordValue(100);

            var x = h.getValueAtPercentile(50);
        }
    }
}
