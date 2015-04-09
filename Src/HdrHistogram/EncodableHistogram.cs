﻿// Written by Gil Tene of Azul Systems, and released to the public domain,
// as explained at http://creativecommons.org/publicdomain/zero/1.0/
// 
// Ported to .NET by Iulian Margarintescu under the same license and terms as the java version
// Java Version repo: https://github.com/HdrHistogram/HdrHistogram
// Latest ported version is available in the Java submodule in the root of the repo
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HdrHistogram
{
    
/**
 * A base class for all encodable (and decodable) histogram classes. Log readers and writers
 * will generally use this base class to provide common log processing across the integer value
 * based AbstractHistogram subclasses and the double value based DoubleHistogram class.
 *
 */

    public abstract class EncodableHistogram
    {

        public abstract int getNeededByteBufferCapacity();

        public abstract int encodeIntoCompressedByteBuffer(ByteBuffer targetBuffer, int compressionLevel);

        public abstract long getStartTimeStamp();

        public abstract void setStartTimeStamp(long startTimeStamp);

        public abstract long getEndTimeStamp();

        public abstract void setEndTimeStamp(long endTimestamp);

        public abstract double getMaxValueAsDouble();

        /**
     * Decode a {@EncodableHistogram} from a compressed byte buffer. Will return either a
     * {@link org.HdrHistogram.Histogram} or {@link org.HdrHistogram.DoubleHistogram} depending
     * on the format found in the supplied buffer.
     *
     * @param buffer The input buffer to decode from.
     * @param minBarForHighestTrackableValue A lower bound either on the highestTrackableValue of
     *                                       the created Histogram, or on the HighestToLowestValueRatio
     *                                       of the created DoubleHistogram.
     * @return The decoded {@link org.HdrHistogram.Histogram} or {@link org.HdrHistogram.DoubleHistogram}
     * @throws DataFormatException on errors in decoding the buffer compression.
     */

        private static EncodableHistogram decodeFromCompressedByteBuffer(ByteBuffer buffer,
            long minBarForHighestTrackableValue)
        {
            // Peek iun buffer to see the cookie:
            int cookie = buffer.getInt(buffer.position());

            // TODO: DoubleHistogram
            //if (DoubleHistogram.isDoubleHistogramCookie(cookie)) 
            //{
            //    return DoubleHistogram.decodeFromCompressedByteBuffer(buffer, minBarForHighestTrackableValue);
            //}
            //else 
            //{
            return Histogram.decodeFromCompressedByteBuffer(buffer, minBarForHighestTrackableValue);
            //}
        }
    }

}
