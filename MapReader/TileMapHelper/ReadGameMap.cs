using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using OpenCvSharp;

namespace MapReader
{
    class ReadGameMap
    {

        public string m_FileName; // 地图的文件名称

        public uint m_FileType;// 文件类型

        public uint m_SubMapWidth;        // 子地图的宽度
        public uint m_SubMapHeight;        // 子地图的高度

        public uint m_FileFlag; // 文件标志
        public uint m_MapWidth; // 地图宽
        public uint m_MapHeight; // 地图高

        public uint m_SubMapRowNum; // 子地图列数量
        public uint m_SubMapColNum; // 子地图行数量

        public uint m_SubMapTotal; // 子地图的总数

        public UnitHeader m_ErrorUnit; // 错误的单元标志

        public uint m_MapSize; // 地图的大小

        public uint m_PointHeight; // 坐标的高度
        public uint m_PointWidth; // 坐标的宽度

        public uint m_UnitIndexNum { get; private set; } // 单元的索引位置
        public uint[] m_UnitOffsetList { get; private set; } // 单元的偏移列表
        public uint m_MaskTemp { get; private set; } // MASK临时变量
        public bool m_isImag { get; private set; } // 受否有此数据
        public bool m_isMask { get; private set; }
        public bool m_isJpeg { get; private set; }
        public bool m_isBlock { get; private set; }
        public bool m_isCell { get; private set; }
        public bool m_isBrig { get; private set; }
        public bool m_isLight { get; private set; }
        public bool m_isBlok { get; private set; }
        public uint[] m_UnitHeadData { get; private set; }  // 单元的头数据
        public bool m_isLigt { get; private set; }

        public MapData m_imag; // IMAG数据
        private MapData[] m_mask = new MapData[128]; // MASK 数据 ;
        public MapData m_jpeg;// JPEG 数据

        private MapData m_blok;// BLOK 数据
        private MapData m_cell;// CELL 数据
        private MapData m_brig;// BRIG 数据
        private MapData m_ligt;// LIGT 数据

        public bool LoadMap(String path)
        {
            this.m_FileName = path;

            using (FileStream fs = File.OpenRead(path))
            {
                byte[] buf = new byte[4];
                fs.Read(buf, 0, 4);
                string valid = Encoding.UTF8.GetString(buf);

                fs.Seek(0, SeekOrigin.Begin);

                // 文件类型标志
                int fileFalg = BitConverter.ToInt32(buf);


                // m_FileType 地图文件类型（1为旧的，2为新的，3为大话3内测版， 4为大话3正式版
                this.m_FileType = 0;
                switch (fileFalg)
                {
                    case 0x4D415058:
                        m_FileType = 1;
                        Console.WriteLine("大话2旧地图格式");
                        break;
                    case 0x4d312e30:
                        m_FileType = 2;
                        Console.WriteLine("大话2新地图、梦幻地图格式");
                        break;
                    case 0x4D322E35:
                        m_FileType = 3;
                        Console.WriteLine("大话3内测版地图格式");
                        break;
                    case 0x4D332E30:
                        m_FileType = 4;
                        Console.WriteLine("大话3正式版地图格式");
                        break;
                    case 0x524F4C30:
                        m_FileType = 5;
                        Console.WriteLine("大话3地图背景文件格式");
                        break;
                    default:
                        Console.WriteLine("未知旧地图格式");
                        break;
                }

                if ((m_FileType == 1) | (m_FileType == 2))
                {

                    this.m_SubMapWidth = 320;
                    this.m_SubMapHeight = 240;

                    // 地图的文件头信息（梦幻，大话2）
                    fs.Read(buf, 0, 4);
                    m_FileFlag = BitConverter.ToUInt32(buf);
                    fs.Read(buf, 0, 4);
                    m_MapWidth = BitConverter.ToUInt32(buf);
                    fs.Read(buf, 0, 4);
                    m_MapHeight = BitConverter.ToUInt32(buf);


                }
                // TODO 等待补充其它类型的代码

                // 注：因为有些地图的尺寸并不一定被小地图尺寸整除，所以需要趋向最大取整
                this.m_SubMapRowNum = (uint)MathF.Ceiling(m_MapHeight / m_SubMapHeight); // 计算子地图中的行数量
                this.m_SubMapColNum = (uint)MathF.Ceiling(m_MapWidth / m_SubMapWidth); // 计算子地图中的列数量

                int m_MapBmpWidth = (int)(m_SubMapColNum * 320);
                int m_MapBmpHeight = (int)(m_SubMapRowNum * 240);


                this.m_SubMapTotal = m_SubMapRowNum * m_SubMapColNum; // 计算地图中总的子地图数量

                this.m_UnitIndexNum = m_SubMapTotal;

                // 读取单元的偏移值列表
                this.m_UnitOffsetList = new UInt32[m_UnitIndexNum]; // 自动分配列表空间
                for (int i = 0; i < m_UnitIndexNum; i++)
                {
                    fs.Read(buf, 0, 4);
                    this.m_UnitOffsetList[i] = BitConverter.ToUInt32(buf);
                }
                // MASK遮罩列表位置
                fs.Read(buf, 0, 4);
                int maskPos = BitConverter.ToInt32(buf);
                fs.Seek(maskPos, SeekOrigin.Begin); // 移动到mask位置
                // 遮罩的数据长度
                fs.Read(buf, 0, 4);
                int maskLength = BitConverter.ToInt32(buf);
                Console.WriteLine("遮罩层的数据量：{0}", maskLength);

                if (maskLength > 0)
                {
                    uint[] TmpMaskList = new uint[maskLength];
                    for (int i = 0; i < maskLength; i++)
                    {
                        fs.Read(buf, 0, 4);
                        TmpMaskList[i] = BitConverter.ToUInt32(buf);
                    }
                    // 读取MASK数据
                    for (int idx = 0; idx < 1; idx++)
                    {
                        fs.Seek(TmpMaskList[idx], SeekOrigin.Begin);
                        Console.WriteLine("数据偏移量:{0} ", TmpMaskList[idx]);

                        MaskData mask = new MaskData();

                        fs.Read(buf, 0, 4);
                        mask.X = BitConverter.ToUInt32(buf);
                        fs.Read(buf, 0, 4);
                        mask.Y = BitConverter.ToUInt32(buf);
                        fs.Read(buf, 0, 4);
                        mask.Width = BitConverter.ToUInt32(buf);
                        fs.Read(buf, 0, 4);
                        mask.Height = BitConverter.ToUInt32(buf);
                        fs.Read(buf, 0, 4);
                        mask.Size = BitConverter.ToInt32(buf);

                        byte[] encodeData = new byte[mask.Size];
                        fs.Read(encodeData, 0, mask.Size);



                        int delta = (mask.Width % 4 != 0 ? 1 : 0);
                        int alignWidth = (int)((mask.Width / 4 + delta) * 4);//以4对齐的宽度
                        byte[] decodeData = new byte[alignWidth * mask.Height / 4];
                        int maskResult = 0;

                        maskResult = this.DecodeMaskData(encodeData, decodeData);
                        Console.WriteLine("读取完成:{0}, {1}", mask, maskResult);
                        // 获取整个图片的像素信息
                        Mat wholeImg = OpenCvSharp.Cv2.ImRead(@"E:\test\cv\whole.jpg");
                        Mat textMat = new Mat((int)mask.Height, (int)mask.Width, MatType.CV_8UC3, new Scalar(0,0,0));
                        

                        // 输出MASK到TGA图像
                        uint pixel_num = mask.Width * mask.Height;
                        uint[] pOutMaskBmp = new uint[pixel_num];

                        for (int h = 0; h < mask.Height; h++)
                        {
                            for (int w = 0; w < mask.Width; w++)
                            {
                                int maskIndex = (h * alignWidth + w) * 2;
                                byte maskValue = decodeData[maskIndex / 8];
                                if ((maskValue & 3) == 3)
                                {
                                    int mapX = (int)(mask.X + w); // 地图图像中的X位置
                                    int mapY = (int)(mask.Y + h); //地图图像中的Y位置

                                    Vec3b vec = wholeImg.Get<Vec3b>(mapY, mapX);
                                    textMat.Set<Vec3b>(h, w, vec);

                                }
                            }
                        }
                        textMat.SaveImage(@"E:\test\tes\decode\t.jpg");
                    }
                }
                // TODO 仅大话3地图使用



            }
            return true;
        }


        internal bool ReadUint(uint UnitNum)
        {
            using (FileStream fs = File.OpenRead(m_FileName))
            {
                long seek; //跳转
                bool Result = true; // 结果
                bool loop = true; // 是否循环

                m_MaskTemp = 1;
                // 
                m_isImag = false;
                m_isMask = false;
                m_isJpeg = false;
                m_isBlock = false;
                m_isCell = false;
                m_isBrig = false;
                m_isLight = false;

                seek = m_UnitOffsetList[UnitNum];
                fs.Seek(seek, SeekOrigin.Begin); // 跳转到单元开始的位置

                if ((m_FileType == 1) | (m_FileType == 2))
                {

                    uint Num = 0; // 单元开始的头4个字节

                    Num = FileByteReaderUtil.ReadUint32(fs);

                    m_UnitHeadData = new uint[Num];

                    for (int i = 0; i < Num; i++)
                    {
                        m_UnitHeadData[i] = FileByteReaderUtil.ReadUint32(fs);
                    }

                    UnitHeader header = new UnitHeader();

                    while (loop)
                    {
                        header.Flag = FileByteReaderUtil.ReadUint32(fs);
                        header.Size = FileByteReaderUtil.ReadUint32(fs);

                        switch (header.Flag)
                        {
                            // GAMI
                            case 0x494D4147:
                                Result = this.ReadIMAG(fs, header.Flag, header.Size);
                                break;

                            // 2KSM
                            case 0x4D534B32:
                            // KSAM
                            case 0x4D41534B:
                                Result = this.ReadMASK(fs, header.Flag, header.Size);
                                break;

                            // GEPJ
                            case 0x4A504547:
                                Result = this.ReadJPEG(fs, header.Flag, header.Size);
                                if (m_FileFlag == 0x524F4C30) // 是否为ROL文件
                                {
                                    loop = false;
                                }
                                break;

                            // KOLB
                            case 0x424C4F4B:
                                Result = this.ReadBLOK(fs, header.Flag, header.Size);
                                break;
                            // LLEC
                            case 0x43454C4C:
                                Result = this.ReadCELL(fs, header.Flag, header.Size);
                                break;
                            // GIRB
                            case 0x42524947:
                                Result = this.ReadBRIG(fs, header.Flag, header.Size);
                                break;

                            // TGIL
                            case 0x4C494754:
                                Result = this.ReadLIGT(fs, header.Flag, header.Size);
                                break;
                            // DNE
                            case 0x454E4420:
                                Result = this.ReadEND(fs, header.Flag, header.Size);
                                loop = false;
                                break;
                            // DIRECT jpeg图形
                            case 0x4A504732:
                                Result = this.ReadDirect(fs, header.Flag, header.Size);
                                break;
                            // 默认处理
                            default:
                                Console.WriteLine("图形问题：{0}. {1}", header.Flag, header.Size);
                                m_ErrorUnit.Flag = header.Flag;
                                m_ErrorUnit.Size = header.Size;
                                loop = false;
                                break;
                        }
                    }
                }
                return Result;
            }
        }

        private bool ReadDirect(FileStream fs, uint flag, uint Size)
        {
            if (flag == 0x4A504732)
            {
                m_jpeg.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_jpeg.Data, 0, (int)Size); // 读取单元JPEG的数据
                m_jpeg.Size = Size;
                m_jpeg.direct = true;
                // 测试读取后面的字节信息
                uint header = FileByteReaderUtil.ReadUint32(fs);
                uint size = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("读取到CELL的数据： {0} , {1}", header, size);
                fs.Seek(size, SeekOrigin.Current);

                header = FileByteReaderUtil.ReadUint32(fs);
                size = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("读取到GIRB的数据： {0} , {1}", header, size);
                fs.Seek(size, SeekOrigin.Current);

                // 结束字节信息
                uint end = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("结束信息:{0}", end);
            }
            else
            {
                Console.Write("直接JPEG图片标志错误！\n");
                return false;
            }
            return true;
        }

        // 读取地图END 的数据
        private bool ReadEND(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x454E4420)
            {
                // 不需要处理数据
            }
            else
            {
                Console.Write("END 标志错误！\n");
                return false;
            }
            return true;
        }

        // 读取地图LIGT的数据
        private bool ReadLIGT(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x4C494754)
            {
                m_isLigt = true;
                m_ligt.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_ligt.Data, 0, (int)Size); // 读取单元JPEG的数据
                m_ligt.Size = Size;
            }
            else
            {
                Console.Write("LIGT标志错误！\n");
                m_isLigt = false;
                return false;
            }
            return true;
        }
        // 读取地图BRIG的数据
        private bool ReadBRIG(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x42524947)
            {
                m_isBrig = true;
                m_brig.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_brig.Data, 0, (int)Size);
                m_brig.Size = Size;
            }
            else
            {
                Console.Write("BRIG标志错误！\n");
                m_isCell = false;
                return false;
            }
            return true;
        }

        private bool ReadCELL(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x43454C4C)
            {
                m_isCell = true;
                m_cell.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_cell.Data, 0, (int)Size); // 读取单元CELL的数据
                m_cell.Size = Size;
            }
            else
            {
                Console.WriteLine("CELL标志错误！\n");
                m_isCell = false;
                return false;
            }
            return true;
        }

        // 读取地图BLOK的数据
        private bool ReadBLOK(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x424C4F4B)
            {
                m_isBlok = true;
                m_blok.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_blok.Data, 0, (int)Size); // 读取单元BLOK的数据
                m_blok.Size = Size;
            }
            else
            {
                Console.WriteLine("BLOK标志错误！\n");
                m_isBlok = false;
                return false;
            }
            return true;
        }
        // 读取地图JPEG的数据
        private bool ReadJPEG(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x4A504547)
            {
                m_isJpeg = true;
                m_jpeg.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_jpeg.Data, 0, (int)Size);// 读取单元JPEG的数据
                m_jpeg.Size = Size;
                m_jpeg.direct = false;
                // 测试读取后面的字节信息
                uint header = FileByteReaderUtil.ReadUint32(fs);
                uint size = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("读取到CELL的数据： {0} , {1}", header, size);
                fs.Seek(size, SeekOrigin.Current);

                header = FileByteReaderUtil.ReadUint32(fs);
                size = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("读取到GIRB的数据： {0} , {1}", header, size);
                fs.Seek(size, SeekOrigin.Current);

                // 结束字节信息
                uint end = FileByteReaderUtil.ReadUint32(fs);
                Console.WriteLine("结束信息:{0}", end);
            }
            else
            {
                Console.WriteLine("JPEG标志错误！");
                m_isJpeg = false;
                return false;
            }
            return true;
        }

        // 读取地图MASK的数据
        private bool ReadMASK(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x4D41534B || Flag == 0x4D534B32)
            {
                // 这个处理可能存在问题,缺少循环标志
                m_isMask = true;
                if ((m_FileType == 1) | (m_FileType == 2))
                {
                    m_mask[0].Data = new byte[Size]; // 分配单元数据的内存空间
                    fs.Read(m_mask[0].Data, 0, ((int)Size));// 读取单元IMAG的数据
                    m_mask[0].Size = Size;
                }
                if ((m_FileType == 3) | (m_FileType == 4))
                {
                    for (int i = 0; i < Size; i++)
                    {
                        m_mask[i].Data = new byte[4];
                        fs.Read(m_mask[i].Data, 0, 4);
                    }
                }
            }
            else
            {
                Console.WriteLine("MASK标志错误！");
                m_isMask = false;
                return false;
            }
            return true;
        }

        private bool ReadIMAG(FileStream fs, uint Flag, uint Size)
        {
            if (Flag == 0x494D4147)
            {
                m_isImag = true;
                m_imag.Data = new byte[Size]; // 分配单元数据的内存空间
                fs.Read(m_imag.Data, 0, m_imag.Data.Length);// 读取单元IMAG的数据

                m_imag.Size = Size;
            }
            else
            {
                Console.WriteLine("IMAG标志错误！");
                m_isImag = false;
                return false;
            }
            return true;
        }

        // 解压MASK的代码
        private int DecodeMaskData(byte[] ip, byte[] op)
        {
            int t = 0, o = 0, i = 0, m = 0;
            int run = 1;
            if (ip[i] > 17)
            {
                t = ip[i++] - 17;
                if (t < 4)
                {
                    //goto match_next;
                    run = -1;
                }
                do
                {
                    op[o++] = ip[i++];
                } while (--t > 0);
                //goto first_literal_run;
                run = -2;
            }
            // 第一个循环
            while (true)
            {
                if (run != -1)
                {
                    if (run != -2)
                    {

                        t = ip[i++];
                        if (t >= 16)
                        {
                            //goto match;
                            run = -3;
                        }
                        if (run != -3)
                        {

                            if (t == 0)
                            {
                                while (ip[i++] == 0)
                                {
                                    t += 255;
                                }

                                t = t + 15 + ip[i++];
                            }
                            // 4个byte的赋值
                            for (int skip = 0; skip < 4; ++skip)
                            {
                                op[o + skip] = ip[i + skip];
                            }
                            o += 4;
                            i += 4;

                            if (--t > 0)
                            {
                                if (t >= 4)
                                {
                                    do
                                    {
                                        for (int skip = 0; skip < 4; ++skip)
                                        {
                                            op[o + skip] = ip[i + skip];
                                        }
                                        o += 4;
                                        i += 4;
                                        t -= 4;
                                    } while (t >= 4);
                                    if (t > 0)
                                    {
                                        do
                                        {
                                            op[o++] = ip[i++];
                                        } while (--t > 0);
                                    }
                                }
                                else
                                {
                                    do
                                    {
                                        op[o++] = ip[i++];
                                    } while (--t > 0);
                                }
                            }

                        }
                    }
                first_literal_run:
                    if (run == -2)
                    {
                        run = 1;
                    }
                    if (run != -3)
                    {
                        t = ip[i++];
                        if (t >= 16)
                        {
                            //goto match;
                            run = -4;
                        }
                        if (run != -4)
                        {
                            m = o - 0x801;
                            m -= t >> 2;
                            m -= ip[i] << 2;
                            ++i;
                            op[o++] = op[m++];
                            op[o++] = op[m++];
                            op[o++] = op[m];

                            //goto match_done:
                            run = -5;
                        }
                    }

                }
                // 第二个死循环
                while (true)
                {
                    if (run != -1 || run != -5)
                    {

                    match:
                        if (run == -3 || run == -4)
                        {
                            run = 1;
                        }
                        if (t >= 64)
                        {
                            m = o - 1;
                            m -= ((t >> 2) & 7);
                            m -= ip[i] << 3;
                            ++i;
                            t = (t >> 5) - 1;
                            //goto copy_match;
                            run = -6;
                        }
                        else if (t >= 32)
                        {
                            t &= 31;
                            if (t == 0)
                            {
                                while (ip[i] == 0)
                                {
                                    t += 255;
                                    i++;
                                }
                                t += 31 + ip[i++];
                            }

                            m = o - 1;
                            byte[] tmpIp = new byte[2];
                            Array.Copy(ip, i, tmpIp, 0, 2);
                            ushort s = BitConverter.ToUInt16(tmpIp);
                            m -= s >> 2;
                            i += 2;

                        }
                        else if (t >= 16)
                        {
                            m = o;
                            m -= (t & 8) << 11;
                            t &= 7;
                            if (t == 0)
                            {
                                while (ip[i] == 0)
                                {
                                    t += 255;
                                    i++;
                                }
                                t += 7 + ip[i++];
                            }
                            byte[] tmpIp = new byte[2];
                            Array.Copy(ip, i, tmpIp, 0, 2);
                            ushort s = BitConverter.ToUInt16(tmpIp);
                            m -= s >> 2;
                            i += 2;
                            if (m == o)
                            {
                                goto eof_found;
                            }
                            o -= 0x4000;
                        }
                        else
                        {
                            m = o - 1;
                            m -= t >> 2;
                            m -= ip[i] << 2;
                            ++i;

                            op[o++] = op[m++];
                            op[o++] = op[m];
                            goto match_done;
                            //run = -7;

                        }
                    }
                    if ((t >= 6 && (o - m) >= 4) && run != -1 && run != -5 && run != -6)
                    {

                        for (int skip = 0; skip < 4; ++skip)
                        {
                            op[o + skip] = op[m + skip];
                        }
                        o += 4;
                        m += 4;
                        t -= 2;
                        do
                        {
                            for (int skip = 0; skip < 4; ++skip)
                            {
                                op[o + skip] = op[m + skip];
                            }
                            o += 4;
                            m += 4;
                            t -= 4;
                        } while (t >= 4);

                        if (t > 0)
                        {
                            do
                            {
                                op[o++] = op[m++];
                            } while (--t > 0);
                        }

                    }
                    else
                    {
                        if (run != -1)
                        {
                            if (run != -5 && run != -7)
                            {
                            copy_match:
                                if (run == -6)
                                {
                                    run = 1;
                                }
                                op[o++] = op[m++];
                                op[o++] = op[m++];

                                do
                                {
                                    op[o++] = op[m++];
                                } while (--t > 0);
                            }

                        }
                    }
                match_done:
                    if (run == -5 || run == -7)
                    {
                        run = 1;
                    }
                    t = ip[i - 2] & 3;
                    if (t == 0) break;
                    match_next:
                    do
                    {
                        op[o++] = ip[i++];
                        if (run == -1)
                        {
                            run = 1;
                        }
                    } while (--t > 0);
                    t = ip[i++];
                }
            }

        eof_found:
            return o;
        }


    }



    // 地图的数据
    public struct MapData
    {
        public uint Size; // 数据的大小
        public byte[] Data; // 数据内容
        public bool direct; // 是否需要特殊处理标识
    }


    // 地图的单元头
    public struct UnitHeader
    {
        public uint Flag; // 单元标志
        public uint Size; // 单元大小
    }

    // MASK的数据结构（推测）
    public struct MaskData
    {
        public uint X;
        public uint Y;
        public uint Width;
        public uint Height;
        public int Size;
    }

    // TGA文件头
    public struct STGA_HEADER
    {
        // TGA像素顺序：B G R A
        public byte IdLength;              // 图像信息字段(默认:0)
        public byte ColorMapType;          // 颜色表的类型（0或者1，0表示没有颜色表,1表示颜色表存在.格式 2,3,10 是无颜色表的，故一般为0）
        public byte ImageType;             // 图像类型码(2-未压缩的RGB图像，3-未压缩的黑白图像，10-runlength编码的RGB图像)
        public ushort ColorMapFirstIndex;        // 颜色表的索引(默认:0)
        public ushort ColorMapLength;            // 颜色表的长度(默认:0)
        public byte ColorMapEntrySize;     // 颜色表表项的为数(默认:0，支持16/24/32)
        public ushort XOrigin;               // 图像X坐标的起始位置(默认:0)
        public ushort YOrigin;               // 图像Y坐标的起始位置(默认:0)
        public ushort ImageWidth;                // 图像的宽度
        public ushort ImageHeight;           // 图像的高度
        public byte PixelBits;             // 像素位数
        public byte ImageDescruptor;       // 图像描述字符字节(默认:0，不支持16bit 565格式)
    }
}
