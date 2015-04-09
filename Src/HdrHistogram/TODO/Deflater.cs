using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HdrHistogram
{
    class Deflater
    {
        private int compressionLevel;

        public Deflater(int compressionLevel)
        {
            // TODO: Complete member initialization
            this.compressionLevel = compressionLevel;
        }

        internal void setInput(object p1, int p2, int uncompressedLength)
        {
            throw new NotImplementedException();
        }

        internal void finish()
        {
            throw new NotImplementedException();
        }

        internal int deflate(byte[] targetArray, int compressedTargetOffset, int p)
        {
            throw new NotImplementedException();
        }

        internal void end()
        {
            throw new NotImplementedException();
        }
    }
}
