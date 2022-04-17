using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapReader
{
    // 偏移量对应表
    struct LIST
    {
        public UInt32 OffsetBmp; // 大地图的偏移量
        public UInt32 OffsetSub; // 小地图的偏移量
    }
    class Bmpoffset
    {

        public UInt32 m_BmpWidth; // 
        public UInt32 m_BmpHeight;//
        public UInt32 m_SubWidth; // 
        public UInt32 m_SubHeight; //

        private UInt32 m_BmpType; // 地图的类型, 0 为整数 1为非整数
        private LIST[] m_OffsetList; // 大小地图的偏移量对照表

        // 大地图宽度 , 大地图高度 , 子地图宽度, 子地图高度
        public Bmpoffset(UInt32 BmpWidth, UInt32 BmpHeight, UInt32 SubWidth, UInt32 SubHeight)
        {
            this.m_BmpWidth = BmpWidth;
            this.m_BmpHeight = BmpHeight;
            this.m_SubWidth = SubWidth;
            this.m_SubHeight = SubHeight;

            // 取趋于大的值,即不管小数点后面多少都进位
            uint row = (uint)MathF.Ceiling(BmpHeight / SubHeight);
            uint col = (uint)MathF.Ceiling(BmpWidth / SubWidth);
            uint bmps = row * col; //子地图总数量

            // 整除
            uint tmp_row = (uint)(BmpHeight / SubHeight);
            uint tmp_col = (uint)(BmpWidth / SubWidth);
            uint tmp_bmps = tmp_row * tmp_col;

            if (bmps != tmp_bmps)
            {
                m_BmpType = 1; // 大地图不能整除小地图
            }
            else {
                m_BmpType = 0; // 大地图整除小地图
            }

            m_OffsetList = new LIST[bmps];

            // 顺序: 从左到右,从上到小的规则
            for (uint j = 0; j < row; j++)
            {
                for (uint k = 0; k < col; k++)
                {
                    // 大地图的偏移量 = (大地图的宽度* 子地图的高度) * 行数 + 子地图的宽度 * 列数
                    m_OffsetList[k + j * col].OffsetBmp = (BmpWidth * SubHeight) * j + SubWidth * k;
                    // 小地图的偏移量 = 子地图的宽度* 子地图的高度* (列数 + 行数 ＊　行总数）
                    m_OffsetList[k + j * col].OffsetSub = SubWidth * SubHeight * (k + j * col);
                }
            }
        }
    }
}
