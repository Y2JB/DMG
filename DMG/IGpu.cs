using System;

namespace DMG
{
    public interface IGpu
    {
        public byte BgScrollX { get; set; }
        public byte BgScrollY { get; set; }

        public byte CurrentScanline { get; }

        public byte[,] FrameBuffer { get; }
    }


}
