using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace DMG
{
    public class Ppu : IPpu
    {
        // Note the values of the enum matter as the state of the PPU is stored in a memory register
        public enum PpuMode
        {
            HBlank = 0,
            VBlank,
            OamSearch,
            PixelTransfer
        }

        const byte Screen_X_Resolution = 160;
        const byte Screen_Y_Resolution = 144;
        
        const byte Max_Sprites = 40;

        // Tile Data is stored in VRAM at addresses $8000-97FF; with one tile being 16 bytes large, this area defines data for 384 Tiles
        const ushort MaxTiles = 384;

        public Bitmap FrameBuffer { get; private set; }

        PpuMode Mode { get; set; }

        public IMemoryReaderWriter Memory { get; set; }

        
        public GfxMemoryRegisters MemoryRegisters { get; private set; }

        public TileMap[] TileMaps { get; private set; }
        public Tile[] Tiles { get; private set; }

        const int MaxSpritesPerLine = 10;
        public OamEntry[] Sprites { get; private set; }
        private List<OamEntry> oamSearchResults = new List<OamEntry>();

        public byte CurrentScanline { get; private set; }

        
        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;

        int frame;

        DmgSystem dmg;

        // temp palette
        Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };

        public Ppu(DmgSystem dmg)
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

            Sprites = new OamEntry[Max_Sprites];
            for(int i = 0; i < Max_Sprites; i++)
            {
                Sprites[i] = new OamEntry((ushort)(0xFE00 + (i * 4)), Memory);
            }

            lastCpuTickCount = 0;
            elapsedTicks = 0;

            Mode = PpuMode.HBlank;
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

            switch (Mode)
            {
                case PpuMode.OamSearch:
                    if (elapsedTicks >= 80)
                    {
                        // In theory this could be async while waiting for the ticks
                        OamSearch();

                        Mode = PpuMode.PixelTransfer;

                        elapsedTicks -= 80;
                    }
                    break;


                // Transfers one scanline of pixels to the screen
                // The emulator must also work this way as the cpu is still running and many graphical effects change the state of the system between scanlines
                case PpuMode.PixelTransfer:
                    if (elapsedTicks >= 172)
                    {
                        Mode = PpuMode.HBlank;

                        // In theory this could be async while waiting for the ticks
                        RenderScanline();

                        elapsedTicks -= 172;
                    }
                    break;


                // PPU is idle during hblank
                case PpuMode.HBlank:
                    // 51 machine cycles ticks (1mhz vs 4mhz mean ours are 4x the value found in some documents)
                    if (elapsedTicks >= 204)
                    {
                        CurrentScanline++;

                        // TODO: Shouldn't this be 144???????

                        if (CurrentScanline == 143)
                        {
                            //This will only fire the interrupt if IE for vblank is set
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_VBLANK);

                            Mode = PpuMode.VBlank;
                        }
                        else
                        {
                            Mode = PpuMode.OamSearch;
                        }

                        // Don't lose any ticks, cannot set to zero
                        elapsedTicks -= 204;
                    }
                    break;


                // PPU is idle during vblank
                // The vblank takes the equivilant of 10 scanlines
                case PpuMode.VBlank:
                    if (elapsedTicks >= 456)
                    {
                        CurrentScanline++;

                        if (CurrentScanline > 153)
                        {
                            CurrentScanline = 0;
                            Mode = PpuMode.OamSearch;

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


        // Gameboy can display 10 sprites per pixel line. mOnce 10 is exceeded no more will be drawn. The order is simply the first 10
        // it comes across as we iteratre the oam table
        void OamSearch()
        {
            oamSearchResults.Clear();
            int spriteHeight = 8;
            if (MemoryRegisters.LCDC.SpriteHeight == 1) spriteHeight = 16;

            int spritesRendered = 0;
            foreach(var sprite in Sprites)
            {
                // Not X & Y are specified as 0 == fully off screen. Lcd Pixel 0,0 == X==8, Y==16. (as you can have 8 or 16 pixel high sprites but always 8 width)
                byte x = sprite.X;
                byte y = sprite.Y;

                // Fully off screen top/left == 0
                if (x == 0) continue;
                if (y == 0) continue;

                // These can be negative
                int spriteXScreenSpace = x - 8;
                int spriteYScreenSpace = y - 16;

                // Fully off right/bottom screen entirely 
                if (spriteXScreenSpace >= Screen_X_Resolution) continue;
                if (spriteYScreenSpace >= Screen_Y_Resolution) continue;

                // To get here, we know that part of the sprite is somewhere along the visable X axis, now we just need to check the Y
                // Is this sprite part of this scanline?
                if ( (CurrentScanline >= (spriteYScreenSpace)) && 
                     (CurrentScanline < (spriteYScreenSpace + spriteHeight)))
                {
                    spritesRendered++;
                    oamSearchResults.Add(sprite);

                    // Only 10 per row can be rendered, then we simply stop
                    if (spritesRendered == MaxSpritesPerLine)
                    {
                        break;
                    }
                }
            }

            // order the list by X decending (we will draw right to left so that lowest X sprite wins)
            oamSearchResults.OrderByDescending(o => o.X).ToList();
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
            if (MemoryRegisters.LCDC.BgDisplay == 1)
            {
                TileMap tileMap = TileMaps[MemoryRegisters.LCDC.BgTileMapSelect];

                byte y = CurrentScanline;

                // What row are we rendering within a tile?
                int tilePixelY = ((y + MemoryRegisters.BgScrollY) % 8);

                for (byte x = 0; x < Screen_X_Resolution; x++)
                {
                    // What column are we rendering within a tile?
                    byte tilePixelX = (byte)((x + MemoryRegisters.BgScrollX) % 8);

                    Tile tile = tileMap.TileFromXY((byte)(x + MemoryRegisters.BgScrollX), (byte)(y + MemoryRegisters.BgScrollY));
                    FrameBuffer.SetPixel(x, y, palette[tile.renderTile[tilePixelX, tilePixelY]]);
                }
            }


            // Render Window
            if (MemoryRegisters.LCDC.WindowDisplay == 1)
            {
                byte y = CurrentScanline;

                if (y >= MemoryRegisters.WindowY)
                {
                    TileMap tileMap = TileMaps[MemoryRegisters.LCDC.WindowTileMapSelect];

                    // Regardless of where on the screen we are drawing the Window, the drawing of the window data starts from window x = 0
                    byte windowXPos = 0;
                    byte windowYPos = (byte)(y - MemoryRegisters.WindowY);

                    int tilePixelY = (y % 8);

                    // WindowX should always be used -7
                    for (byte x = (byte) (MemoryRegisters.WindowX - 7); x < Screen_X_Resolution; x++)
                    {
                        // What column are we rendering within a tile?
                        byte tilePixelX = (byte)(windowXPos % 8);             

                        Tile tile = tileMap.TileFromXY(windowXPos, windowYPos);
                        FrameBuffer.SetPixel(x, y, palette[tile.renderTile[tilePixelX, tilePixelY]]);

                        windowXPos++;
                    }
                }
            }


            // Render Sprites, we already know that they all are visible on this scanline and they are already ordered so that the right most is first
            foreach (var sprite in oamSearchResults)
            {
                byte sx = sprite.X;
                byte sy = sprite.Y;

                // can be negative 
                int spriteXScreenSpace = sx - 8;
                int spriteYScreenSpace = sy - 16;

                // Which row of the sprite is being rendered on this line?
                int spriteYLine =  CurrentScanline - spriteYScreenSpace;

                Tile tile = GetSpriteTileByIndex(sprite.TileIndex);

                for (int i = 0; i < 8; i++)
                {
                    byte paletteIndex = tile.renderTile[i, spriteYLine];

                    if (paletteIndex != 0)
                    {
                        FrameBuffer.SetPixel(spriteXScreenSpace + i, CurrentScanline, palette[paletteIndex]);
                    }
                }

                // TODO: obj/obj priority (sprite wil smallest x position draws on top)

                // TODO : obj/BG priority, BG can render on top??

                // TODO: Sprites have two palettes, use the right one!

                // Palette entry 0 == translucent for sprites
            }



 

        }



        // TODO: This could be faster 
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


        public Tile GetSpriteTileByIndex(byte index)
        {
            return GetTileByVRamAdrress((ushort) (0x8000 + (index * 16)));
        }


        public void DumpFrameBufferToPng()
        {
            string fn = string.Format("../../../../dump/screen.png");
            FrameBuffer.Save(fn);
        }
    }
}
