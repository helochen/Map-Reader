using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapReader
{
 
    class MapFileReaderHelper
    {
        


        public void FileReader()
        {
            ReadGameMap readGameMap = new ReadGameMap();

            //readGameMap.LoadMap(@"E:\unity\project\2D TileMap Dev\Assets\Res\1002.map");
            readGameMap.LoadMap(@"E:\test\tes\1002.map");

            uint UnitTotal = readGameMap.m_SubMapTotal;
            uint m_MapWidth = readGameMap.m_MapWidth;
            uint m_MapHeight = readGameMap.m_MapHeight;
            uint m_SubMapWidth = readGameMap.m_SubMapWidth;
            uint m_SubMapHeight = readGameMap.m_SubMapHeight;

            uint m_SubMapRowNum = readGameMap.m_SubMapRowNum;
            uint m_SubMapColNum = readGameMap.m_SubMapColNum;

            uint m_SubMapTotal = readGameMap.m_SubMapTotal;

            Bmpoffset bo = new Bmpoffset(m_SubMapWidth * m_SubMapColNum, m_SubMapHeight * m_SubMapRowNum, m_SubMapWidth, m_SubMapHeight);
            uint bmpsize = m_SubMapWidth * m_SubMapHeight * m_SubMapTotal; // 得到地图的像素大小

            UInt16[] m_BmpData = new UInt16[bmpsize];

			Console.WriteLine("输出地图的宽度与高度：{0}， {1}\n 像素大小：{2}，{3}", m_SubMapRowNum, m_SubMapColNum,m_SubMapWidth , m_SubMapHeight );
            // 循环处理所有的地图单元
            for (uint i = 0; i < UnitTotal; i++)
            {
                readGameMap.ReadUint(i);
                MapData jd = readGameMap.m_jpeg;

                uint TempSize = 0;
                byte[] jpgdata;

				// 处理地图JPEG数据为标准的JPEG数据
				if (jd.direct)
				{
					jpgdata = jd.Data;
					TempSize = jd.Size;
				}
				else { 
					jpgdata = this.MapHandler(jd.Data, jd.Size, out TempSize);
				}

				if (true)
				{
					using (FileStream fileStream = File.Create(@"E:\test\"+i+@".jpg", (int)TempSize, FileOptions.WriteThrough))
					{
						fileStream.Write(jpgdata, 0, (int)TempSize);
						fileStream.Flush();
					}
				}

            }
        }

        private byte[] MapHandler(byte[] Buffer, uint inSize, out uint outSize)
        {
            // JPEG数据处理原理
            // 1、复制D8到D9的数据到缓冲区中
            // 2、删除第3、4个字节 FFA0
            // 3、修改FFDA的长度00 09 为 00 0C
            // 4、在FFDA数据的最后添加00 3F 00
            // 5、替换FFDA到FF D9之间的FF数据为FF 00

            uint TempNum = 0;                     // 临时变量，表示已读取的长度
            byte[] outBuffer;
            // TODO
            byte[] TempBuffer = new byte[inSize * 2];     // 临时变量，表示处理后的数据
           
            outBuffer = TempBuffer;                 // 已处理数据的开始地址
            UInt16 TempTimes = 0;                   // 临时变量，表示循环的次数
            uint Temp = 0;

			// 当已读取数据的长度小于总长度时继续
			int idx_buffer = 0;
			int idx_tempBuffer = 0;
			while (TempNum < inSize && Buffer[idx_buffer++] == 0xFF)
			{
				TempBuffer[idx_tempBuffer++] = 0xFF;
				TempNum++;
				byte[] tmp;
				switch (Buffer[idx_buffer])
				{
					case 0xD8:
						TempBuffer[idx_tempBuffer++] = 0xD8;
						idx_buffer++;
						TempNum++;
						break;
					case 0xA0:
						idx_buffer++;
						idx_tempBuffer--;
						TempNum++;
						break;
					case 0xC0:
						TempBuffer[idx_tempBuffer++] = 0xC0;
						idx_buffer++;
						TempNum++;

						tmp = new byte[2];
						tmp[0] = Buffer[idx_buffer + 1];
						tmp[1] = Buffer[idx_buffer];

						TempTimes = BitConverter.ToUInt16(tmp);//  读取长度,将长度转换为Intel顺序

						for (int i = 0; i < TempTimes; i++)
						{
							TempBuffer[idx_tempBuffer++] = Buffer[idx_buffer++];
							TempNum++;
						}

						break;
					case 0xC4:
						TempBuffer[idx_tempBuffer++] = 0xC4;
						idx_buffer++;
						TempNum++;

						tmp = new byte[2];
						tmp[0] = Buffer[idx_buffer + 1];
						tmp[1] = Buffer[idx_buffer];
						TempTimes = BitConverter.ToUInt16(tmp);//  读取长度,将长度转换为Intel顺序

						for (int i = 0; i < TempTimes; i++)
						{
							TempBuffer[idx_tempBuffer++] = Buffer[idx_buffer++];
							TempNum++;
						}
						break;
					case 0xDB:
						TempBuffer[idx_tempBuffer++] = 0xDB;
						idx_buffer++;
						TempNum++;

						tmp = new byte[2];
						tmp[0] = Buffer[idx_buffer + 1];
						tmp[1] = Buffer[idx_buffer];
						TempTimes = BitConverter.ToUInt16(tmp);//  读取长度,将长度转换为Intel顺序


						for (int i = 0; i < TempTimes; i++)
						{
							TempBuffer[idx_tempBuffer++] = Buffer[idx_buffer++];
							TempNum++;
						}
						break;
					case 0xDA:
						TempBuffer[idx_tempBuffer++] = 0xDA;
						TempBuffer[idx_tempBuffer++] = 0x00;
						TempBuffer[idx_tempBuffer++] = 0x0C;
						idx_buffer++;
						TempNum++;


						tmp = new byte[2];
						tmp[0] = Buffer[idx_buffer + 1];
						tmp[1] = Buffer[idx_buffer];
						TempTimes = BitConverter.ToUInt16(tmp);//  读取长度,将长度转换为Intel顺序

						idx_buffer++;
						TempNum++;
						idx_buffer++;

						for (int i = 2; i < TempTimes; i++)
						{
							TempBuffer[idx_tempBuffer++] = Buffer[idx_buffer++];
							TempNum++;
						}
						TempBuffer[idx_tempBuffer++] = 0x00;
						TempBuffer[idx_tempBuffer++] = 0x3F;
						TempBuffer[idx_tempBuffer++] = 0x00;
						Temp += 1; // 这里应该是+3的，因为前面的0xFFA0没有-2，所以这里只+1。

						// 循环处理0xFFDA到0xFFD9之间所有的0xFF替换为0xFF00
						for (; TempNum < inSize - 2;)
						{
							if (Buffer[idx_buffer] == 0xFF)
							{
								TempBuffer[idx_tempBuffer++] = 0xFF;
								TempBuffer[idx_tempBuffer++] = 0x00;
								idx_buffer++;
								TempNum++;
								Temp++;
							}
							else
							{
								TempBuffer[idx_tempBuffer++] = Buffer[idx_buffer++];
								TempNum++;
							}
						}
						// 直接在这里写上了0xFFD9结束Jpeg图片.
						Temp--; // 这里多了一个字节，所以减去。
						idx_tempBuffer--;
						TempBuffer[idx_tempBuffer--] = 0xD9;
						break;
					case 0xD9:
						// 算法问题，这里不会被执行，但结果一样。
						TempBuffer[idx_tempBuffer++] = 0xD9;
						TempNum++;
						break;
					default:
						break;
				}
			}
			Temp += inSize;
			outSize = Temp;
			return outBuffer;
		}
    }

}
