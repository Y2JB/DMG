using System;
namespace DMG
{
    public class GfxMemoryRegisters
    {
        // 0xFF40
        public LcdControlRegister LCDC { get; private set; }

        // 0xFF41
        public LcdStatusRegister STAT { get; set; }

        // 0xFF42
        public byte BgScrollY { get; set; }

        // 0xFF43
        public byte BgScrollX { get; set; }

        // 0xFF4A
        public byte WindowY { get; set; }

        // 0xFF4B
        public byte WindowX { get; set; }

        public GfxMemoryRegisters()
        {
            LCDC = new LcdControlRegister();
            STAT = new LcdStatusRegister();
        }

        public void Reset()
        {
            LCDC.Register = 0;
            //LCDC.LcdEnable = true;  // do we set these to sensible things????
        }
    }


    public class LcdControlRegister
    {

        public LcdControlRegister()
        {
        }

        public byte Register { get; set; }

        // Bit 7 - LCD Display Enable(0=Off, 1=On)
        public byte LcdEnable { get { return (Register & (byte)(1 << 7)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 6 - Window Tile Map Display Select(0=9800-9BFF, 1=9C00-9FFF)
        public byte WindowTileMapSelect { get { return (Register & (byte)(1 << 6)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 5 - Window Display Enable(0=Off, 1=On)
        public byte WindowDisplay { get { return (Register & (byte)(1 << 5)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 4 - BG & Window Tile Data Select(0=8800-97FF, 1=8000-8FFF)
        public byte BgAndWindowTileAddressingMode { get { return (Register & (byte)(1 << 4)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 3 - BG Tile Map Display Select(0=9800-9BFF, 1=9C00-9FFF)
        public byte BgTileMapSelect { get { return ((Register & (byte)(1 << 3))) == 0 ? (byte) 0 : (byte) 1; } }

        // Bit 2 - OBJ(Sprite) Size(0=8x8, 1=8x16)
        public byte SpriteHeight { get { return (Register & (byte)(1 << 2)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 1 - OBJ(Sprite) Display Enable(0=Off, 1=On)
        public byte SpritesDisplay { get { return (Register & (byte)(1 << 1)) == 0 ? (byte)0 : (byte)1; } }

        // Bit 0 - BG/Window Display/Priority(0=Off, 1=On)
        public byte BgDisplay { get { return (Register & (byte)(1 << 0)) == 0 ? (byte)0 : (byte)1; } }
    }


    // Bit 6 - LYC=LY Coincidence Interrupt(1=Enable) (Read/Write)
    // Bit 5 - Mode 2 OAM Interrupt(1=Enable) (Read/Write)
    // Bit 4 - Mode 1 V-Blank Interrupt(1=Enable) (Read/Write)
    // Bit 3 - Mode 0 H-Blank Interrupt(1=Enable) (Read/Write)
    // Bit 2 - Coincidence Flag(0:LYC<> LY, 1:LYC= LY) (Read Only)
    // Bit 1-0 - Mode Flag(Mode 0-3, see below) (Read Only)
    //       0: During H-Blank
    //       1: During V-Blank
    //       2: During Searching OAM
    //       3: During Transferring Data to LCD Driver
    public class LcdStatusRegister 
    {
        public LcdStatusRegister()
        {
        }

        public byte Register { get; set; }

        public bool VBlamkInterruptEnable { get { return (Register & (byte)(1 << 4)) != 0; } }
    }


}
