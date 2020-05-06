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


        public GfxMemoryRegisters(IPpu ppu)
        {
            LCDC = new LcdControlRegister(ppu);
            STAT = new LcdStatusRegister(ppu);
        }

        public void Reset()
        {
            BgScrollX = 0;
            BgScrollY = 0;
            WindowX = 0;
            WindowY = 0;
            LCDC.Register = 0;
            STAT.Register = 0;
        }
    }


    public class LcdControlRegister
    {
        byte register;
        public byte Register 
        {
            get { return register; }
            set
            {
                bool lcdState = ((register & (byte)(1 << 7)) != 0);
                bool lcdNewState = ((value & (byte)(1 << 7)) != 0);

                register = value;

                if(lcdState != lcdNewState)
                {
                    ppu.Enable(lcdNewState);
                }
            }                
        }

        IPpu ppu;

        public LcdControlRegister(IPpu ppu)
        {
            this.ppu = ppu;
        }

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
    // Bit 1-0 - Mode Flag(Mode 0-3) (Read Only)
    //       0: During H-Blank
    //       1: During V-Blank
    //       2: During Searching OAM
    //       3: During Transferring Data to LCD Driver
    // 0xFF41
    public class LcdStatusRegister 
    {
        byte register;
        public byte Register 
        {
            get
            {
                byte low3Bits = 0;
                byte ppuMode = (byte)ppu.Mode;
                low3Bits |= ppuMode;

                // Bit 2 (Coincidence Flag) is set to 1 if register(0xFF44) is the same value as (0xFF45) otherwise it is set to 0
                if(ppu.CurrentScanline == LYC)
                {
                    low3Bits |= 0x04;
                }

                return (byte) (register | low3Bits);
            }

            set
            {
                // Mask off the read only bits
                register = (byte) (value & 0xF8);


                //if (lcdStat.OamInterruptEnable)
                //{
                //    dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                //}
            }

        }

        // Set at 0xFF45 by the program to drive the coincidence interrupt
        public byte LYC { get; set; }


        IPpu ppu;

        public LcdStatusRegister(IPpu ppu)
        {
            this.ppu = ppu;
        }

        public bool LycLyCoincidenceInterruptEnable { get { return (Register & (byte)(1 << 6)) != 0; } }
        public bool OamInterruptEnable { get { return (Register & (byte)(1 << 5)) != 0; } }
        public bool VBlankInterruptEnable { get { return (Register & (byte)(1 << 4)) != 0; } }
        public bool HBlankInterruptEnable { get { return (Register & (byte)(1 << 3)) != 0; } }

        public byte CoincidenceFlag { get { return (byte) ((Register & (byte)(1 << 2)) == 0 ? 0 : 1); } }

        public byte ModeFlag { get { return (byte)(Register & (byte)(0x3)); } }
    }


}
