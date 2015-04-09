
using System.Collections.Generic;

namespace HdrHistogram
{
    public abstract class Iterable<T> : IEnumerable<T>
    {

        /**
         * Returns an iterator over a set of elements of type T.
         *
         * @return an Iterator.
         */
        protected abstract Iterator<T> iterator();

        public IEnumerator<T> GetEnumerator()
        {
            return this.iterator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
