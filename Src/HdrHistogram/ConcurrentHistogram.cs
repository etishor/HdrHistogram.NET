using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HdrHistogram
{
    public class ConcurrentHistogram : Histogram
    {
        public ConcurrentHistogram(int numberOfSignificantValueDigits) : base(numberOfSignificantValueDigits)
        {
        }

        public ConcurrentHistogram(long highestTrackableValue, int numberOfSignificantValueDigits) : base(highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public ConcurrentHistogram(long lowestDiscernibleValue, long highestTrackableValue, int numberOfSignificantValueDigits) : base(lowestDiscernibleValue, highestTrackableValue, numberOfSignificantValueDigits)
        {
        }

        public ConcurrentHistogram(AbstractHistogram source) : base(source)
        {
        }
    }
}
