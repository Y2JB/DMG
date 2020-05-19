using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;

namespace DMG
{
    public class Ppu : IPpu
    {
        public const byte Screen_X_Resolution = 160;
        public const byte Screen_Y_Resolution = 144;

        public const int BG_Width_Pixels = 256;
        public const int BG_Height_Pixels = 256;
        public const int BG_Width_Tiles = 32;
        public const int BG_Height_Tiles = 32;

        public const UInt32 OAM_Length = 20;
        public const UInt32 Glitched_OAM_Length = 19;
        public const UInt32 PixelTransfer_Length = 43;
        public const UInt32 HBlank_Length = 51;
        public const UInt32 ScanLine_Length = OAM_Length + PixelTransfer_Length + HBlank_Length;
        public const UInt32 VBlank_Length = ScanLine_Length * 10;


        public const byte Max_Sprites = 40;

        // Tile Data is stored in VRAM at addresses $8000-97FF; with one tile being 16 bytes large, this area defines data for 384 Tiles
        public const ushort Max_Tiles = 384;

        //public const int Clocks_Per_Screen = 17556;
        public const int Clocks_Per_Screen = 70224;

        public Bitmap FrameBuffer { get; private set; }
        Bitmap drawBuffer;
        Bitmap frameBuffer0;
        Bitmap frameBuffer1;
 

        public PpuMode Mode { get; private set; }
   
        public PpuMemoryRegisters MemoryRegisters { get; private set; }

        public TileMap[] TileMaps { get; private set; }
        //public Tile[] Tiles { get; private set; }
        public Dictionary<int, Tile> Tiles { get; private set; }
        const int MaxSpritesPerLine = 10;
        public OamEntry[] Sprites { get; private set; }
        private List<OamEntry> oamSearchResults = new List<OamEntry>();

        public byte CurrentScanline { get; private set; }

        // Used to prevent cpu accessing vram and oam ram at incorrect times
        public bool PpuAccessingVram { get; private set; }

        // Midframe DMA to OAM ram can happen. If it does, it marks the ram dirty and we do not render sprites that line
        public bool OamDirty { get; set; }

        public DmgPalettes Palettes { get; set; }
        
        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;
        UInt32 vblankTicks;
        UInt32 frame;
        double lastFrameTime;

        DmgSystem dmg;


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

            MemoryRegisters = new PpuMemoryRegisters(this);
            MemoryRegisters.Reset();

            TileMaps = new TileMap[2];
            TileMaps[0] = new TileMap(this, dmg.memory, 0x9800);
            TileMaps[1] = new TileMap(this, dmg.memory, 0x9C00);
            Tiles = new Dictionary<int, Tile>();
            for (int i = 0; i < Max_Tiles; i++)
            {
                int address = (0x8000 + (i * 16));
                Tiles.Add(address, new Tile((ushort)address));
            }

            Sprites = new OamEntry[Max_Sprites];
            for(int i = 0; i < Max_Sprites; i++)
            {
                Sprites[i] = new OamEntry((ushort)(0xFE00 + (i * 4)), dmg.memory);
            }

            lastCpuTickCount = 0;
            elapsedTicks = 0;

            frame = 0;
            lastFrameTime = dmg.EmulatorTimer.Elapsed.TotalMilliseconds;

            Mode = PpuMode.HBlank;
            CurrentScanline = 0;
            // Debugger hooks
            if (dmg.OnFrameStart != null)
            {
                dmg.OnFrameStart(frame);
            }

            Palettes = new DmgPalettes();

            OamDirty = false;
            PpuAccessingVram = false;
        }




        // We clock our CPU at 1mhz so we use MCycles 

        // The GPU state machine works ayncronously to the CPU (in HW but not in the emulator). It works like this, measured in CPU cycles:
        // [OAM Search 20 cycles] -> [Pixel Transfer 43 cycles] -> [HBlank 51] = 114 clocks per line
        // [VBlank 1140 cycles]

        // 144 lines on screen + 10 lines vblank = 154 lines 

        // 114 * 154 = 17,556 clocks per screen
        // 1,048,576 / 17,556 = 59.72hz refresh rate
        public void Step()
        {
            UInt32 cpuTickCount = dmg.cpu.Ticks;

            // Here we monitor how many cycles the CPU has executed and we map the GPU state to match how the real hardware behaves. This allows
            // us to generate interupts at the right time

            if (MemoryRegisters.LCDC.LcdEnable == 0)
            {
                return;
            }

            UInt32 tickCount = (cpuTickCount - lastCpuTickCount);
            // Track how many cycles the CPU has done since we last changed states
            elapsedTicks += tickCount;
            lastCpuTickCount = cpuTickCount;

            LcdStatusRegister lcdStat = MemoryRegisters.STAT;

            switch (Mode)
            {
                // See the enable function for some details on this
                case PpuMode.Glitched_OAM:
                    if (elapsedTicks >= Glitched_OAM_Length)
                    {
                        Mode = PpuMode.PixelTransfer;
                        elapsedTicks -= Glitched_OAM_Length;
                    }
                    break;


                case PpuMode.OamSearch:
                    if (elapsedTicks >= OAM_Length)
                    {
                        OamSearch();

                        Mode = PpuMode.PixelTransfer;
                  
                        elapsedTicks -= OAM_Length;
                    }
                    break;


                // Transfers one scanline of pixels to the screen
                // The emulator must also work this way as the cpu is still running and many graphical effects change the state of the system between scanlines
                case PpuMode.PixelTransfer:
                    if (elapsedTicks >= PixelTransfer_Length)
                    {                      
                        RenderScanline();

                        Mode = PpuMode.HBlank;

                        if (lcdStat.HBlankInterruptEnable)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        elapsedTicks -= PixelTransfer_Length;
                    }
                    break;


                // PPU is idle during hblank
                case PpuMode.HBlank:
                    // 51 machine cycles ticks (mcycles) 
                    if (elapsedTicks >= HBlank_Length)
                    {                        
                        CurrentScanline++;

                        // Coincidence interrupt checks if the scanline is equal to the value in FF45
                        if(lcdStat.LycLyCoincidenceInterruptEnable &&
                            CurrentScanline == lcdStat.LYC)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        if (CurrentScanline == 144)
                        {
                            Mode = PpuMode.VBlank;

                            vblankTicks = (elapsedTicks - HBlank_Length);

                            //This will only fire the interrupt if IE for vblank is set
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_VBLANK);

                            if(lcdStat.VBlankInterruptEnable)
                            {
                                dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                            }


                            // We can set the renderer drawing the frame as soon as we enter vblank
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
                            while (dmg.EmulatorTimer.Elapsed.TotalMilliseconds - lastFrameTime < fps60)
                            { }

                            lastFrameTime = dmg.EmulatorTimer.Elapsed.TotalMilliseconds;

                            if (dmg.OnFrame != null)
                            {
                                dmg.OnFrame();
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
                        elapsedTicks -= HBlank_Length;
                    }
                    break;


                // PPU is idle during vblank
                // The vblank takes the equivilant of 10 scanlines
                case PpuMode.VBlank:
                    vblankTicks += tickCount;
                    if (elapsedTicks >= ScanLine_Length)
                    {
                        CurrentScanline++;

                        // Coincidence interrupt checks if the scanline is equal to the value in FF45
                        if (lcdStat.LycLyCoincidenceInterruptEnable &&
                            CurrentScanline == lcdStat.LYC)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        elapsedTicks -= ScanLine_Length;
                    }

                    if (CurrentScanline == 154)
                    {
                        CurrentScanline = 0;

                        // Coincidence interrupt checks if the scanline is equal to the value in FF45
                        if (lcdStat.LycLyCoincidenceInterruptEnable &&
                            CurrentScanline == lcdStat.LYC)
                        {
                            dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_LCDSTAT);
                        }

                        if ((vblankTicks - elapsedTicks) != VBlank_Length)
                        {
                            throw new Exception("PPU out of sync");
                        }

                        Mode = PpuMode.OamSearch;                        

                        // Debugger hooks
                        if (dmg.OnFrameEnd != null)
                        {
                            dmg.OnFrameEnd(frame, false);
                        }

                        frame++;

                        // Debugger hooks
                        if (dmg.OnFrameStart != null)
                        {
                            dmg.OnFrameStart(frame);
                        }
                    }                                       
                    break;
            }
        }


        public UInt32 ElapsedTicks()
        {
            switch (Mode)
            {
                case PpuMode.Glitched_OAM:
                case PpuMode.OamSearch:
                case PpuMode.PixelTransfer:
                case PpuMode.HBlank:
                    return elapsedTicks;
                case PpuMode.VBlank:
                    return vblankTicks;
            }

            throw new ArgumentException("bad mode");
        }


        public UInt32 TotalTicksForState()
        {
            switch (Mode)
            {
                case PpuMode.Glitched_OAM:
                    return Glitched_OAM_Length;
                case PpuMode.OamSearch:
                    return OAM_Length;
                case PpuMode.PixelTransfer:
                    return PixelTransfer_Length;
                case PpuMode.HBlank:
                    return HBlank_Length;
                case PpuMode.VBlank:
                    return VBlank_Length;
            }

            throw new ArgumentException("bad mode");
        }


        // Gameboy can display 10 sprites per pixel line. mOnce 10 is exceeded no more will be drawn. The order is simply the first 10
        // it comes across as we iteratre the oam table
        void OamSearch()
        {
            PpuAccessingVram = true;

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
            oamSearchResults = oamSearchResults.OrderByDescending(o => o.X).ToList();

            PpuAccessingVram = false;
            OamDirty = false;


            // Debugger hooks
            // This will get very expensive. Remember to disconnect the ppu profiler if you are not using it
            if (dmg.OnOamSearchComplete != null)
            {
                dmg.OnOamSearchComplete(frame, CurrentScanline, oamSearchResults);
            }
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

            PpuAccessingVram = true;

            // Render the BG
            // Total BG size in VRam is 32x32 tiles
            // Viewport is 20x18 tiles
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
            if (MemoryRegisters.LCDC.WindowDisplay == 1 &&
                MemoryRegisters.WindowX < Screen_X_Resolution &&
                MemoryRegisters.WindowY < Screen_Y_Resolution)
            {
                if (CurrentScanline >= MemoryRegisters.WindowY)
                {
                    TileMap tileMap = TileMaps[MemoryRegisters.LCDC.WindowTileMapSelect];

                    // Window X, Y tell you where on screen to start drawing the tiles found at 0,0 in the tilemap.
                    // The Window DOES NOT WRAP
                    // WindowX draws -7 pixels from its actual value                    
                    byte windowScreenSpaceX = MemoryRegisters.WindowX;
                    byte windowScreenSpaceY = MemoryRegisters.WindowY;
                    int windowScreenSpaceXAdjusted = windowScreenSpaceX - 7;              

                    // These track the X&Y in the tile map;
                    byte windowDataX = 0;
                    byte windowDataY = (byte) (CurrentScanline - windowScreenSpaceY);

                    int tilePixelY = (windowDataY % 8);

                    for (int x = windowScreenSpaceXAdjusted; x < Screen_X_Resolution; x++)
                    {
                        // Remember, this is window X adjusted by 7, so this is not wrapping
                        if(windowScreenSpaceXAdjusted < 0)
                        {
                            // Because of -7, the window can be offscreen for a few pixels
                            windowScreenSpaceXAdjusted++;                     
                            continue;
                        }
                        // What column are we rendering within a tile?
                        byte tilePixelX = (byte)(windowDataX % 8);             

                        Tile tile = tileMap.TileFromXY(windowDataX, windowDataY);
                        drawBuffer.SetPixel(x, CurrentScanline, Palettes.BackgroundPalette[tile.renderTile[tilePixelX, tilePixelY]]);

                        windowScreenSpaceXAdjusted++;
                        windowDataX++;
                    }
                }
            }


            // Skip sprite rendering this line if a dma transfer has occured midframe and stomped all over the OAM entries.
            // OAM entries will become 'clean' after next OAM search (next line).
            if (OamDirty == false)
            {
                // Render Sprites, we already know that they all are visible on this scanline and they are already ordered so that the right most is first
                foreach (var sprite in oamSearchResults)
                {
                    byte sx = sprite.X;
                    byte sy = sprite.Y;

                    // can be negative 
                    int spriteXScreenSpace = sx - 8;
                    int spriteYScreenSpace = sy - 16;

                    // Which row of the sprite is being rendered on this line?
                    int spriteYLine = CurrentScanline - spriteYScreenSpace;

                    Color[] palette = Palettes.ObjPalette0;
                    if (sprite.PaletteNumber == 1) palette = Palettes.ObjPalette1;

                    Tile tile = null;
                    if (MemoryRegisters.LCDC.SpriteHeight == 0)
                    {
                        tile = GetSpriteTileByIndex(sprite.TileIndex);
                    }
                    // if using 16 pixel high sprites (2 tiles) then potentially adjust to the next tile and fix up the line if we are drawing the sewcond tile
                    // The tiles themselves also index opposite when Y flipped 
                    else
                    {
                        if (spriteYLine >= 8)
                        {
                            if (sprite.YFlip == false) tile = GetSpriteTileByIndex((byte)(sprite.TileIndex + 1));
                            else tile = GetSpriteTileByIndex((byte)(sprite.TileIndex));
                            spriteYLine -= 8;
                        }
                        else
                        {
                            if (sprite.YFlip == false) tile = GetSpriteTileByIndex((byte)(sprite.TileIndex));
                            else tile = GetSpriteTileByIndex((byte)(sprite.TileIndex + 1));
                        }
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        // Offscreen 
                        if (spriteXScreenSpace + i >= Screen_X_Resolution)
                        {
                            break;
                        }

                        if (spriteXScreenSpace + i < 0)
                        {
                            continue;
                        }



                        int sprPixelX = i;
                        int sprPixelY = spriteYLine;
                        if (sprite.XFlip) sprPixelX = 7 - sprPixelX;
                        if (sprite.YFlip) sprPixelY = 7 - sprPixelY;
                        byte paletteIndex = tile.renderTile[sprPixelX, sprPixelY];

                        // If the priority is 0, sprites redner on top. If it is 1 then sprite pixels only render on top of 'white' otherwise they are obscured 
                        if (sprite.ObjPriority == 1)
                        {
                            Color pixel = drawBuffer.GetPixel(spriteXScreenSpace + i, CurrentScanline);
                            if (pixel != Palettes.BackgroundPalette[0])
                            {
                                continue;
                            }
                        }

                        // Palette entry 0 == translucent for sprites
                        if (paletteIndex != 0)
                        {
                            drawBuffer.SetPixel(spriteXScreenSpace + i, CurrentScanline, palette[paletteIndex]);
                        }
                    }
                }
            }

            oamSearchResults.Clear();
            PpuAccessingVram = false;
        }


        public void Enable(bool toggle)
        {
            if(toggle == false)
            {
                // Debugger hooks
                if (dmg.OnFrameEnd != null)
                {
                    dmg.OnFrameEnd(frame, true);
                }

                // Not modelling this and setting it here was causing the 'Mario World' Level on marioland 2 to not enable the LCD!
                Mode = PpuMode.Glitched_OAM;
                CurrentScanline = 0;
                elapsedTicks = 0;
            }
            else
            {
                // When you first turn on the LCD, the very first line in the very first frame doesn't have a proper OAM mode
                // The STAT register will read HBlank instead of OAM and the OAM region will not be locked(and therefore will remain inaccessible to the PPU)
                Mode = PpuMode.Glitched_OAM;
                CurrentScanline = 0;
                elapsedTicks = 0;

                frame++;
                // Debugger hooks
                if (dmg.OnFrameStart != null)
                {
                    dmg.OnFrameStart(frame);
                }
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


        public void DumpFullCurrentBgToPng(bool renderViewPortBox)
        {
            Bitmap png = new Bitmap(256, 256);

            int bgSelect = 0;
            RenderFullBgToImage(png, renderViewPortBox, bgSelect);
            
            string tileMapLocation = bgSelect == 0 ? "9800" : "9C00";
            png.Save(string.Format("../../../../dump/BG_{0}.png", tileMapLocation));

            bgSelect = 1;
            RenderFullBgToImage(png, renderViewPortBox, bgSelect);

            tileMapLocation = bgSelect == 0 ? "9800" : "9C00";
            png.Save(string.Format("../../../../dump/BG_{0}.png", tileMapLocation));
        }

            
        // Debug only
        public void RenderFullBgToImage(Bitmap bmp, bool renderViewPortBox, int bgSelect)
        {        
            if(bgSelect == -1)
            {
                bgSelect = MemoryRegisters.LCDC.BgTileMapSelect;
            }

            TileMap tileMap = TileMaps[bgSelect];
            for (int y = 0; y < 256; y++)
            {
                for (int x = 0; x < 256; x++)
                {
                    Tile tile = tileMap.TileFromXY((byte)(x), (byte)(y));
                        
                    bmp.SetPixel(x, y, Palettes.BackgroundPalette[tile.renderTile[x % 8, y % 8]]);
                }
            }

            if (renderViewPortBox)
            {
                byte viewPortX;
                byte viewPortY;
                
                // Where are we viewing the logical 256x256 tile map?
                viewPortX = MemoryRegisters.BgScrollX;
                viewPortY = MemoryRegisters.BgScrollY;
                
                Pen pen = new Pen(Color.RoyalBlue, 1.0f);
                using (var graphics = Graphics.FromImage(bmp))
                {
                    int x1 = viewPortX;
                    int x2 = viewPortX + Screen_X_Resolution;
 
                    int y1 = viewPortY;
                    int y2 = viewPortY + Screen_Y_Resolution;
 
                    // Each side can take 2 lines to draw if it wraps
                    
                    int adjustX2 = x2;
                    if (x2 >= BG_Width_Pixels) adjustX2 = x2 - BG_Width_Pixels;

                    int adjustY2 = y2;
                    if (y2 >= BG_Height_Pixels) adjustY2 = y2 - BG_Height_Pixels;

                    // Top of rect (can go off end of image)
                    graphics.DrawLine(pen, x1, y1, x2, y1);
                    if (x2 != adjustX2) graphics.DrawLine(pen, 0, y1, adjustX2, y1);

                    // Bottom of rect
                    graphics.DrawLine(pen, x1, adjustY2, x2, adjustY2);
                    if (x2 != adjustX2) graphics.DrawLine(pen, 0, adjustY2, adjustX2, adjustY2);

                    // Left of rect (can go off end of image)
                    graphics.DrawLine(pen, x1, y1, x1, y2);
                    if (y2 != adjustY2) graphics.DrawLine(pen, x1, 0, x1, adjustY2);

                    // Right
                    graphics.DrawLine(pen, adjustX2, y1, adjustX2, y2);
                    if (y2 != adjustY2) graphics.DrawLine(pen, adjustX2, 0, adjustX2, adjustY2);
                }
            }
        }
    }
}
