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

        public static int ReadInt32(Stream stream)
        {
            byte[] buf = new byte[4];
            stream.Read(buf, 0, 4);
            return BitConverter.ToInt32(buf);
        }

        public static ushort ReadUshort16(Stream stream) {
            byte[] buf = new byte[2];
            stream.Read(buf, 0, 2);
            return BitConverter.ToUInt16(buf);
        }
    }
}
