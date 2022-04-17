using System;
using System.IO;

namespace MapReader
{
    class FileByteReaderUtil
    {
        public static uint ReadUint32(Stream stream) {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, 4);
            return BitConverter.ToUInt32(buf);
        }
    }
}
