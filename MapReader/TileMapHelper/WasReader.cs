using OpenCvSharp;
using System;
using System.IO;
using System.Text;

namespace MapReader.TileMapHelper
{
    class WasReader
    {
        private static String WAS_FILE_FLAT = "SP";
        private static int TCP_HEADER_SIZE = 12;

        private const int TYPE_ALPHA = 0x00;// 前2位

        private const int TYPE_ALPHA_PIXEL = 0x20;// 前3位 0010 0000

        private const int TYPE_ALPHA_REPEAT = 0x00;// 前3位

        private const int TYPE_FLAG = 0xC0;// 2进制前2位 1100 0000

        private const int TYPE_PIXELS = 0x40;// 以下前2位 0100 0000

        private const int TYPE_REPEAT = 0x80;// 1000 0000

        private const int TYPE_SKIP = 0xC0; // 1100 0000

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

                        // 绘制图片
                        int[] pixels = this.ParsePixels(fs, offset, lineOffsets, frameWidth, frameHeight, headerSize);

                        this.CreateImage(frameX, frameY, pixels, width, height);
                        break;
                    }
                }
            }

        }

        private void CreateImage(int frameX, int frameY, int[] pixels, int width, int height) {
            Mat textMat = new Mat(height, width, MatType.CV_8UC4, new Scalar(0, 0, 0, 0));
            // TODO 写入像素
            for (int y1 = 0; y1 < height && y1 + frameY < height; y1++)
            {
                for (int x1 = 0; x1 < width && x1 + frameX < width; x1++)
                {
                    int r = ((pixels[y1 * width + x1] >> 11) & 0x1F) << 3;
                    int g = ((pixels[y1 * width + x1] >> 5) & 0x3F) << 2;
                    int b = (pixels[y1 * width + x1] & 0x1F) << 3;
                    //RGBA
                    Vec3i vec4 = new Vec3i(r,g,b);
                    textMat.Set<Vec3i>( x1, y1,vec4);
                }
            }
            textMat.SaveImage(@"E:\test\tes\decode\player.png");
        }

        private int[] ParsePixels(FileStream fs, int frameOffset, int[] lineOffsets, int frameWidth, int frameHeight, int headerSize)
        {
            int[] pixels = new int[frameHeight * frameWidth];

            int b, x, c;
            int index;
            int count;

            for (int y = 0; y < frameHeight; y++)
            {
                x = 0;
                fs.Seek(lineOffsets[y] + frameOffset + headerSize + 4, SeekOrigin.Begin);
                while (x < frameWidth) {
                    b = fs.ReadByte();
                    switch ((b & TYPE_FLAG))
                    {
                        case TYPE_ALPHA:
                            if ((b & TYPE_ALPHA_PIXEL) > 0)
                            {
                                index = fs.ReadByte();
                                c = palette[index];
                            }
                            else if (b != 0)
                            {
                                count = b & 0x1F;
                                b = fs.ReadByte();
                                index = fs.ReadByte();
                                c = palette[index];

                                for (int i = 0; i < count; i++)
                                {
                                    pixels[y * frameWidth + x++] = c + ((c & 0x1F) << 16);
                                }
                            }
                            else
                            {
                                if (x > frameWidth)
                                {
                                    Console.Error.WriteLine("block end error: [{0}][{1} / {2}]", y, x, frameWidth);
                                    continue;
                                }else if (x == 0)
                                {
                                    Console.Error.WriteLine("x == 0");
                                }else
                                {
                                    x = frameWidth;
                                }
                            }
                            break;
                        case TYPE_PIXELS:
                            count = b & 0x3F;
                            for (int i = 0; i < count; i++)
                            {
                                index = fs.ReadByte();
                                pixels[y * frameWidth + x++] = palette[index] + (0x1F << 16);
                            }
                            break;
                        case TYPE_REPEAT:
                            count = b & 0x3F;
                            index = fs.ReadByte();
                            c = palette[index];

                            for (int i = 0; i < count; i++)
                            {
                                pixels[y * frameWidth + x++] = c + (0x1F << 16);
                            }
                            break;
                        case TYPE_SKIP:
                            count = b & 0x3F;
                            x += count;
                            break;
                    }
                }
                if(x > frameWidth) {
                    Console.Error.WriteLine("block end error: [{0}][{1} / {2}]", y, x, frameWidth);
                }
            }

            return pixels;
        }
    }
}
