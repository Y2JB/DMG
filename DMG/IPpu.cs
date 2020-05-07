using System;
using System.Collections.Generic;
using System.Drawing;

namespace DMG
{
    // Note the values of the enum matter as the state of the PPU is stored in a memory register
    public enum PpuMode
    {
        HBlank = 0,
        VBlank,
        OamSearch,
        PixelTransfer
    }

    public interface IPpu
    {
        public PpuMode Mode { get; }

        public void Enable(bool toggle);

        public GfxMemoryRegisters MemoryRegisters { get; }

        public byte CurrentScanline { get; }

        public Bitmap FrameBuffer { get; }

        public TileMap[] TileMaps { get; }
        public Dictionary<int, Tile> Tiles { get; }

        Tile GetTileByVRamAdrressFast(ushort address);
        Tile GetTileByVRamAdrressSlow(ushort address);
    }


}
