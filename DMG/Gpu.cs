using System;
using System.Drawing;

namespace DMG
{
    public class Gpu : IGpu
    {
        // Note the values of the enum matter as the state of the GPU is stored in a memory register
        public enum Mode
        {
            HBlank = 0,
            VBlank,
            OamSearch,
            PixelTransfer
        }

        const byte Screen_X_Resolution = 160;
        const byte Screen_Y_Resolution = 144;

        // Tile Data is stored in VRAM at addresses $8000-97FF; with one tile being 16 bytes large, this area defines data for 384 Tiles
        const ushort MaxTiles = 384;

        public GfxMemoryRegisters MemoryRegisters { get; private set; }

        public Bitmap FrameBuffer { get; private set; }

        public TileMap[] TileMaps { get; private set; }
        public Tile[] Tiles { get; private set; }

        public byte CurrentScanline { get; private set; }

        public byte GpuMode { get { return (byte)mode; } }

        public IMemoryReaderWriter Memory { get; set; }

        Mode mode { get; set; }

        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;

        int frame;

        DmgSystem dmg;

        // temp palette
        Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };

        public Gpu(DmgSystem dmg)
        {
            this.dmg = dmg;
        }


        public void Reset()
        {
            FrameBuffer = new Bitmap(Screen_X_Resolution, Screen_Y_Resolution); // new Color[Screen_X_Resolution * Screen_Y_Resolution];

            MemoryRegisters = new GfxMemoryRegisters();
            MemoryRegisters.Reset();

            TileMaps = new TileMap[2];
            TileMaps[0] = new TileMap(this, Memory, 0x9800);
            TileMaps[1] = new TileMap(this, Memory, 0x9C00);
            Tiles = new Tile[MaxTiles];
            for (int i = 0; i < Tiles.Length; i++)
            {
                Tiles[i] = new Tile((ushort)(0x8000 + (i * 16)));
            }

            lastCpuTickCount = 0;
            elapsedTicks = 0;

            mode = Mode.HBlank;
        }




        // We clock our CPU at 4mhz so for our values are 4x higher than you will see in some documents

        // The GPU state machine works ayncronously to the CPU (in HW but not in the emulator). It works like this, measured in CPU cycles:
        // [OAM Search 80 cycles] -> [Pixel Transfer 172 cycles] -> [HBlank 204] * 144 times (Y resolution)
        // [VBlank 456 cycles]

        // At our 4mhz our cpu ticks:
        // 80 + 172 + 204 = 456 ticks to render 1 scanline
        // 456 * 154 lines = 70,224 ticks to render 1 screen
        // Our 4hmz CPU = 4,194,304 ticks per second
        // 4,194,304 / 70,224 = 59.72 frames per second
        public void Step()
        {
            UInt32 cpuTickCount = dmg.cpu.Ticks;

            // Here we monitor how many cycles the CPU has executed and we map the GPU state to match how the real hardware behaves. This allows
            // us to generate interupts at the right time

            if (MemoryRegisters.LCDC.LcdEnable == 0)
            {
                return;
            }
                
            // Track how many cycles the CPU has done since we last changed states
            elapsedTicks += (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            switch (mode)
            {
                case Mode.OamSearch:
                    if (elapsedTicks >= 80)
                    {
                        // In theory this could be async while waiting for the ticks
                        OamSearch();

                        mode = Mode.PixelTransfer;

                        elapsedTicks -= 80;
                    }
                    break;


                // Transfers one scanline of pixels to the screen
                // The emulator must also work this way as the cpu is still running and many graphical effects change the state of the system between scanlines
                case Mode.PixelTransfer:
                    if (elapsedTicks >= 172)
                    {
                        mode = Mode.HBlank;

                        // In theory this could be async while waiting for the ticks
                        RenderScanline();

                        elapsedTicks -= 172;
                    }
                    break;


                // PPU is idle during hblank
                case Mode.HBlank:
                    // 51 machine cycles ticks (1mhz vs 4mhz mean ours are 4x the value found in some documents)
                    if (elapsedTicks >= 204)
                    {
                        CurrentScanline++;

                        // TODO: Shouldn't this be 144???????

                        if (CurrentScanline == 143)
                        {
                            //This will only fire the interrupt if IE for vblank is set
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_VBLANK);

                            mode = Mode.VBlank;
                        }
                        else
                        {
                            mode = Mode.OamSearch;
                        }

                        // Don't lose any ticks, cannot set to zero
                        elapsedTicks -= 204;
                    }
                    break;


                // PPU is idle during vblank
                // The vblank takes the equivilant of 10 scanlines
                case Mode.VBlank:
                    if (elapsedTicks >= 456)
                    {
                        CurrentScanline++;

                        if (CurrentScanline > 153)
                        {
                            CurrentScanline = 0;
                            mode = Mode.OamSearch;

                            frame++;

                            if (dmg.OnFrame != null)
                            {
                                dmg.OnFrame();
                            }
                            //DumpFrameBufferToPng();
                        }

                        elapsedTicks -= 456;
                    }
                    break;
            }
        }


        void OamSearch()
        {

        }


        // Gamebopy screen resolution = 160x144
        void RenderScanline()
        {
            /*
            // Very temporary
            int offset = 0;
            foreach (Tile t in Tiles)
            {
                t.Parse(Memory.VRam, offset);
                offset += 16;
            }
            */

            // Render the BG
            // Total BG size in VRam is 32x32 tiles
            // Viewport is 20x18 tiles
            TileMap tileMap = TileMaps[MemoryRegisters.LCDC.BgTileMapSelect];

            byte y = CurrentScanline;

            // What row are we rendering within a tile?
            int tilePixelY = ((y + MemoryRegisters.BgScrollY) % 8);        

            for (byte x = 0; x < Screen_X_Resolution; x++)
            {
                // What column are we rendering within a tile?
                byte tilePixelX = (byte)((x + MemoryRegisters.BgScrollX) % 8);

                Tile tile = tileMap.TileFromXY((byte) (x + MemoryRegisters.BgScrollX), (byte) (y + MemoryRegisters.BgScrollY));
                FrameBuffer.SetPixel(x, y, palette[tile.renderTile[tilePixelX, tilePixelY]]);
            }



            // Render Sprites


            // Render Window
        }


        public Tile GetTileByVRamAdrress(ushort address)
        {
            foreach (Tile t in Tiles)
            {
                if (address >= t.VRamAddress && address < (t.VRamAddress + 16))
                {
                    return t;
                }
            }

            throw new ArgumentException("Bad tile address");
        }


        public void DumpFrameBufferToPng()
        {
            string fn = string.Format("../../../../dump/screens/{0}.png", frame);
            FrameBuffer.Save(fn);
        }
    }
}
