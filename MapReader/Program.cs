using System;

namespace MapReader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!"); 
            MapFileReaderHelper helper = new MapFileReaderHelper();
            helper.FileReader();

        }
    }
}
