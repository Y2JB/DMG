using System;
namespace DMG
{
    public class PpuMemoryRegisters
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


        IPpu ppu;

        public PpuMemoryRegisters(IPpu ppu)
        {
            this.ppu = ppu;
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


        public override string ToString()
        {                  
            return String.Format("{0}{1}Scanline: {2}{3}Window X,Y: {4} , {5}{6}BG Scroll X, Y: {7} , {8}{9}", 
                LCDC.ToString(), 
                STAT.ToString(),            
                ppu.CurrentScanline, Environment.NewLine,
                WindowX, WindowY, Environment.NewLine, 
                BgScrollX, BgScrollY, Environment.NewLine);
        }

    }


    // 0xFF40
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

        // Bit 0 - BG/Window Display(0=Off, 1=On)
        // When Bit 0 is cleared, both background and window become blank(white), and the Window Display Bit is ignored in that case. Only Sprites may still be displayed(if enabled in Bit 1).
        public byte BgWinDisplay { get { return (Register & (byte)(1 << 0)) == 0 ? (byte)0 : (byte)1; } }

        public override string ToString()
        {
            string sprHeight = SpriteHeight == 0 ? "8x8" : "8x16";
            string bgTileMap = BgTileMapSelect == 0 ? "9800-9BFF" : "9C00-9FFF";
            string bgAddressingMode = BgAndWindowTileAddressingMode == 0 ? "8800-9BFF" : "8000-8FFF";
            string windowTileMap = WindowTileMapSelect == 0 ? "9800-9BFF" : "9C00-9FFF";

            return String.Format("LCDC:{0}LCD Enabled: {1}{2}BGWin Display: {3}{4}Sprites Enabled: {5}{6}Sprite Height: {7}{8}BG Tilemap: {9}{10}BG/Window Addressing Mode: {11}{12}Window Enabled: {13}{14}Window Tilemap: {15}{16}", 
                Environment.NewLine, LcdEnable.ToString(), Environment.NewLine, BgWinDisplay.ToString(), Environment.NewLine, SpritesDisplay.ToString(), Environment.NewLine, sprHeight, Environment.NewLine,
                bgTileMap, Environment.NewLine, bgAddressingMode, Environment.NewLine, WindowDisplay.ToString(), Environment.NewLine, windowTileMap, Environment.NewLine);
        }
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

                // See the ppu.Enable function for some details on this
                byte mode = (byte) (ppu.Mode == PpuMode.Glitched_OAM ? PpuMode.HBlank : ppu.Mode);
                byte ppuMode = mode; 
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
                
                // Set the unused bit
                register |= 0x80;


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


        public override string ToString()
        {
            // See the ppu.Enable function for some details on this
            string mode = (ppu.Mode == PpuMode.Glitched_OAM ? PpuMode.HBlank.ToString() : ppu.Mode.ToString());

            return String.Format("STAT:{0}Current Mode: {1}{2}LYC Flag: {3}{4}HBlank IRQ: {5}{6}VBlank IRQ: {7}{8}OAM IRQ: {9}{10}LYC IRQ: {11}{12}LYC: {13}{14}", 
                Environment.NewLine, mode, Environment.NewLine, CoincidenceFlag.ToString(), Environment.NewLine, HBlankInterruptEnable.ToString(), 
                Environment.NewLine, VBlankInterruptEnable.ToString(), Environment.NewLine, OamInterruptEnable.ToString(), Environment.NewLine, 
                LycLyCoincidenceInterruptEnable.ToString(), Environment.NewLine, LYC.ToString(), Environment.NewLine);
        }
    }


}
