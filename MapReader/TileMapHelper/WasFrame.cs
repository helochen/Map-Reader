using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapReader.TileMapHelper
{
    class WasFrame
    {
        private int frameOffset;

        private int[] lineOffsets;

        private int delay = 1; // 延时帧数

        private int height;

        private int width;

        private int x;
        private int y;

        /**
         * 图像原始数据<br>
         * 0-15位RGB颜色(565)<br>
         * 16-20为alpha值<br>
         * pixels[x+y*width]
         */
        private int[] pixels;

        public WasFrame(int x, int y, int width, int height, int delay,
            int frameOffset, int[] lineOffset)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.delay = delay;
            this.frameOffset = frameOffset;
            this.lineOffsets = lineOffset;
        }
    }
}
