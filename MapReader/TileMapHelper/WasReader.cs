using System;
using System.IO;
using System.Text;

namespace MapReader.TileMapHelper
{
    class WasReader
    {
        private static String WAS_FILE_FLAT = "SP";
        private static int TCP_HEADER_SIZE = 12;

        private ushort[] palette;

        public void DecoderWas(String path)
        {
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] buf = new byte[2];
                fs.Read(buf, 0, 2);
                String flag = Encoding.GetEncoding("utf-8").GetString(buf);


                ushort headerSize = FileByteReaderUtil.ReadUshort16(fs);
                ushort animCount = FileByteReaderUtil.ReadUshort16(fs);
                ushort frameCount = FileByteReaderUtil.ReadUshort16(fs);
                ushort width = FileByteReaderUtil.ReadUshort16(fs);
                ushort height = FileByteReaderUtil.ReadUshort16(fs);

                ushort refPixelX = FileByteReaderUtil.ReadUshort16(fs);
                ushort refPixelY = FileByteReaderUtil.ReadUshort16(fs);


                Console.WriteLine("flag:{0},headerSize:{1},animCount:{2}" +
                    ",frameCount:{3},width,height:{4},{5},x,y:{6},{7}",
                    flag, headerSize, animCount, frameCount, width, height
                    , refPixelX, refPixelY);
                // 读取帧延时信息
                int delay = headerSize - TCP_HEADER_SIZE;

                // 调色版
                palette = new ushort[256];
                for (int i = 0; i < 256; i++)
                {
                    palette[i] = FileByteReaderUtil.ReadUshort16(fs);
                }

                // TODO 复制调色板,应该是为了变色用

                // 帧偏移量
                fs.Seek(headerSize + 4 + 512, SeekOrigin.Begin);

                int[] frameOffsets = new int[animCount * frameCount];
                for (int i = 0; i < animCount; i++)
                {
                    for (int j = 0; j < frameCount; j++)
                    {
                        frameOffsets[i * frameCount + j] = FileByteReaderUtil.ReadInt32(fs);
                    }
                }

                // 帧信息
                int frameX, frameY, frameWidth, frameHeight;
                for (int i = 0; i < animCount; i++)
                {
                    for (int j = 0; j < frameCount; j++)
                    {
                        int offset = frameOffsets[i * frameCount + j];
                        if (offset == 0) {
                            continue;
                        }
                        fs.Seek(offset + headerSize + 4 , SeekOrigin.Begin);
                        frameX = FileByteReaderUtil.ReadInt32(fs);
                        frameY = FileByteReaderUtil.ReadInt32(fs);
                        frameWidth = FileByteReaderUtil.ReadInt32(fs);
                        frameHeight = FileByteReaderUtil.ReadInt32(fs);

                        // 行像素数据偏移
                        int[] lineOffsets = new int[frameHeight];
                        for (int l = 0; l < frameHeight; l++)
                        {
                            lineOffsets[l] = FileByteReaderUtil.ReadInt32(fs);
                        }
                    }
                }
            }

        }
    }
}
