//#define THREADED_PIXEL_TRANSFER

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace DMG
{
    public class Ppu : IPpu
    {
#if THREADED_PIXEL_TRANSFER
        bool drawLine = false;
        Thread pixelWriterThread;
#endif 

        const byte Screen_X_Resolution = 160;
        const byte Screen_Y_Resolution = 144;
        
        const byte Max_Sprites = 40;

        // Tile Data is stored in VRAM at addresses $8000-97FF; with one tile being 16 bytes large, this area defines data for 384 Tiles
        const ushort MaxTiles = 384;

        public Bitmap FrameBuffer { get; private set; }
        Bitmap drawBuffer;
        Bitmap frameBuffer0;
        Bitmap frameBuffer1;
 

        public PpuMode Mode { get; private set; }

        public IMemoryReaderWriter Memory { get; set; }
   
        public GfxMemoryRegisters MemoryRegisters { get; private set; }

        public TileMap[] TileMaps { get; private set; }
        //public Tile[] Tiles { get; private set; }
        public Dictionary<int, Tile> Tiles { get; private set; }
        const int MaxSpritesPerLine = 10;
        public OamEntry[] Sprites { get; private set; }
        private List<OamEntry> oamSearchResults = new List<OamEntry>();

        public byte CurrentScanline { get; private set; }

        public DmgPalettes Palettes { get; set; }
        
        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;

        int frame;
        double lastFrameTime;

        DmgSystem dmg;

        // temp palette
        //Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };


        public Ppu(DmgSystem dmg)
        {
            this.dmg = dmg;
        }


        public void Reset()
        {
            frameBuffer0 = new Bitmap(Screen_X_Resolution, Screen_Y_Resolution); 
            frameBuffer1 = new Bitmap(Screen_X_Resolution, Screen_Y_Resolution); 
            FrameBuffer = frameBuffer0;
            drawBuffer = frameBuffer1;

            MemoryRegisters = new GfxMemoryRegisters(this);
            MemoryRegisters.Reset();

            TileMaps = new TileMap[2];
            TileMaps[0] = new TileMap(this, Memory, 0x9800);
            TileMaps[1] = new TileMap(this, Memory, 0x9C00);
            Tiles = new Dictionary<int, Tile>();
            for (int i = 0; i < MaxTiles; i++)
            {
                int address = (0x8000 + (i * 16));
                Tiles.Add(address, new Tile((ushort)address));
            }

            Sprites = new OamEntry[Max_Sprites];
            for(int i = 0; i < Max_Sprites; i++)
            {
                Sprites[i] = new OamEntry((ushort)(0xFE00 + (i * 4)), Memory);
            }

            lastCpuTickCount = 0;
            elapsedTicks = 0;

            frame = 0;
            lastFrameTime = dmg.EmulatorTimer.Elapsed.TotalMilliseconds;

            Mode = PpuMode.VBlank;

            Palettes = new DmgPalettes();

#if THREADED_PIXEL_TRANSFER
            drawLine = false;
            pixelWriterThread = new Thread(new ThreadStart(PixelThread));
            pixelWriterThread.Start();
#endif 

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

            LcdStatusRegister lcdStat = MemoryRegisters.STAT;

            switch (Mode)
            {
                case PpuMode.OamSearch:
                    if (elapsedTicks >= 80)
                    {
                        // In theory this could be async while waiting for the ticks
                        OamSearch();

                        Mode = PpuMode.PixelTransfer;

#if THREADED_PIXEL_TRANSFER                        
                        drawLine = true;
#endif
                        elapsedTicks -= 80;
                    }
                    break;


                // Transfers one scanline of pixels to the screen
                // The emulator must also work this way as the cpu is still running and many graphical effects change the state of the system between scanlines
                case PpuMode.PixelTransfer:
                    if (elapsedTicks >= 172)
                    {                      
#if THREADED_PIXEL_TRANSFER
                        // Wait for line to finish
                        while (drawLine)
                        {
                        }
#else
                        // In theory this could be async while waiting for the ticks
                        RenderScanline();
#endif

                        Mode = PpuMode.HBlank;

                        if (lcdStat.HBlankInterruptEnable)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        elapsedTicks -= 172;
                    }
                    break;


                // PPU is idle during hblank
                case PpuMode.HBlank:
                    // 51 machine cycles ticks (1mhz vs 4mhz mean ours are 4x the value found in some documents)
                    if (elapsedTicks >= 204)
                    {                        
                        CurrentScanline++;

                        // Coincidence interrupt checks if the scanline is equal to the value in FF45
                        if(lcdStat.LycLyCoincidenceInterruptEnable &&
                            CurrentScanline == lcdStat.LYC)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        // TODO: Shouldn't this be 144??????? Not 143!
                        if (CurrentScanline == 144)
                        {
                            Mode = PpuMode.VBlank;

                            //This will only fire the interrupt if IE for vblank is set
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_VBLANK);

                            if(lcdStat.VBlankInterruptEnable)
                            {
                                dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                            }
                            
                        }
                        else
                        {
                            Mode = PpuMode.OamSearch;

                            if (lcdStat.OamInterruptEnable)
                            {
                                dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                            }
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

                        if (CurrentScanline == 154)
                        {
                            CurrentScanline = 0;
                            Mode = PpuMode.OamSearch;

                            frame++;

                            lock (FrameBuffer)
                            {
                                // Flip frames 
                                if (FrameBuffer == frameBuffer0)
                                {
                                    FrameBuffer = frameBuffer1;
                                    drawBuffer = frameBuffer0;
                                }
                                else
                                {
                                    FrameBuffer = frameBuffer0;
                                    drawBuffer = frameBuffer1;
                                }                              
                            }

                            // lock to 60fps
                            double fps60 = 1000 / 60.0;
                            while(dmg.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime < fps60)
                            { }

                            lastFrameTime = dmg.EmulatorTimer.Elapsed.TotalMilliseconds;

                            if (dmg.OnFrame != null)
                            {
                                dmg.OnFrame();
                            }

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

#if THREADED_PIXEL_TRANSFER
        void PixelThread()
        {
            while(true)
            {
                if(drawLine)
                {
                    RenderScanline();
                    drawLine = false;
                }
            }
        }
#endif

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

            // TODO: NOT SURE THIS FLAG IS BEING USED RIGHT HERE!!!
            if (MemoryRegisters.LCDC.BgWinDisplay == 1)
            {
                TileMap tileMap = TileMaps[MemoryRegisters.LCDC.BgTileMapSelect];

                byte screenY = CurrentScanline;

                // Where are we viewing the logical 256x256 tile map?
                byte viewPortX = MemoryRegisters.BgScrollX;
                byte viewPortY = MemoryRegisters.BgScrollY;

                int bgY = viewPortY + screenY;
                if (bgY >= 256) bgY -= 256;

                // What row are we rendering within a tile?
                byte tilePixelY = (byte) (bgY % 8);

                for (byte screenX = 0; screenX < Screen_X_Resolution; screenX++)
                {
                    int bgX = viewPortX + screenX;
                    if (bgX >= 256) bgX -= 256;

                    // What column are we rendering within a tile?
                    byte tilePixelX = (byte)(bgX % 8);

                    Tile tile = tileMap.TileFromXY((byte)(bgX), (byte)(bgY));

                    drawBuffer.SetPixel(screenX, screenY, Palettes.BackgroundPalette[tile.renderTile[tilePixelX, tilePixelY]]);
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

                    int tilePixelY = (windowYPos % 8);

                    // WindowX should always be used -7
                    for (byte x = (byte) (MemoryRegisters.WindowX - 7); x < Screen_X_Resolution; x++)
                    {
                        // What column are we rendering within a tile?
                        byte tilePixelX = (byte)(windowXPos % 8);             

                        Tile tile = tileMap.TileFromXY(windowXPos, windowYPos);
                        drawBuffer.SetPixel(x, y, Palettes.BackgroundPalette[tile.renderTile[tilePixelX, tilePixelY]]);

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

                Color[] palette = Palettes.ObjPalette0;
                if (sprite.PaletteNumber == 1) palette = Palettes.ObjPalette1;
                
                Tile tile = GetSpriteTileByIndex(sprite.TileIndex);

                // if using 16 pixel high sprites (2 tiles) then adjust to the next tile and fix up the line
                if (MemoryRegisters.LCDC.SpriteHeight == 1 && spriteYLine >= 8)
                {
                    tile = GetSpriteTileByIndex((byte) (sprite.TileIndex + 1));
                    spriteYLine -= 8;
                }

                for (int i = 0; i < 8; i++)
                {                 
                    // Offscreen 
                    if (spriteXScreenSpace + i >= Screen_X_Resolution)
                    {
                        break;
                    }

                    if(spriteXScreenSpace + i < 0)
                    {
                        continue;
                    }

                    int sprPixelX = i;
                    int sprPixelY = spriteYLine;
                    if (sprite.XFlip) sprPixelX = 7 - sprPixelX;
                    if (sprite.YFlip) sprPixelY = 7 - sprPixelY;
                    byte paletteIndex = tile.renderTile[sprPixelX, sprPixelY];

                    // Palette entry 0 == translucent for sprites
                    if (paletteIndex != 0)
                    {
                        drawBuffer.SetPixel(spriteXScreenSpace + i, CurrentScanline, palette[paletteIndex]);
                    }
                }

                // TODO : obj/BG priority, BG can render on top??
                
            }
        }


        public void Enable(bool toggle)
        {
            if(toggle == false)
            {
                Mode = PpuMode.OamSearch;
                CurrentScanline = 0;
                elapsedTicks = 0;                
            }
            else
            {
                lastCpuTickCount = dmg.cpu.Ticks;
            }
        }


        public Tile GetTileByVRamAdrressFast(ushort address)
        {
            return Tiles[address];
        }


        // TODO: This could be faster 
        public Tile GetTileByVRamAdrressSlow(ushort address)
        {
            foreach (var t in Tiles)
            {
                if (address >= t.Value.VRamAddress && address < (t.Value.VRamAddress + 16))
                {
                    return t.Value;
                }
            }

            throw new ArgumentException("Bad tile address");
        }


        public Tile GetSpriteTileByIndex(byte index)
        {
            return GetTileByVRamAdrressFast((ushort) (0x8000 + (index * 16)));
        }


        public void DumpFrameBufferToPng()
        {
            string fn = string.Format("../../../../dump/screen.png");
            lock (FrameBuffer)
            {
                FrameBuffer.Save(fn);
            }
        }
    }
}
