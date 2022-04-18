using System;
using System.IO;
using System.Text;

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
}
