using MapReader.TileMapHelper;
using System;

namespace MapReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //MapFileReaderHelper helper = new MapFileReaderHelper();
            //helper.FileReader();


            // 读取WAS文件
            WasReader reader = new WasReader();
            reader.DecoderWas(@"E:\test\was\attack.tcp");


        }
    }
}
