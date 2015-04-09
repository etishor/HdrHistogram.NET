using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HdrHistogram
{
    public sealed class AtomicLongArray
    {
        internal long get(int index)
        {
            throw new NotImplementedException();
        }

        internal void getAndIncrement(int index)
        {
            throw new NotImplementedException();
        }

        internal void getAndAdd(int index, long value)
        {
            throw new NotImplementedException();
        }

        internal void lazySet(int index, long value)
        {
            throw new NotImplementedException();
        }

        public int Length { get; set; }
    }
}
