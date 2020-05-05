using System;
using System.Drawing;

namespace DMG
{
    public interface IPpu
    {
        public GfxMemoryRegisters MemoryRegisters { get; }

        public byte CurrentScanline { get; }

        public Bitmap FrameBuffer { get; }

        public TileMap[] TileMaps { get; }
        public Tile[] Tiles { get; }
        Tile GetTileByVRamAdrress(ushort address);
    }


}
