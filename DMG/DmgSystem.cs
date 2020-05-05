using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;

namespace DMG
{


    public class DmgSystem
    {
        public BootRom bootstrapRom { get; private set; }
        public Rom rom { get; private set; }
        public Memory memory { get; private set; }
        public Cpu cpu { get; private set; }
        public Ppu ppu { get; private set; }
        public Interrupts interrupts { get; private set; }

        public Joypad pad { get; private set; }

        public Timer timer { get; private set; }

        public StringBuilder Tty { get; private set; }

        public Bitmap FrameBuffer { get { return ppu.FrameBuffer; } }

        public Action OnFrame{ get; set;  }

        public DmgSystem()
        {
            Tty = new StringBuilder(1024 * 256);
        }


        public void PowerOn()
        {
            bootstrapRom = new BootRom("../../../../DMG.bin");
            //rom = new Rom("../../../../roms/tetris.gb");
            //rom = new Rom("../../../../roms/Dr. Mario.gb");

            //rom = new Rom("../../../../roms/bgbtest.gb");
            rom = new Rom("../../../../roms/tellinglys.gb");
            

            //rom = new Rom("../../../../roms/Tetris (World).gb");

            //rom = new Rom("../../../..//roms/cpu_instrs.gb");

            //rom = new Rom("../../../../roms/cpu_instrs.gb");
            //rom = new Rom("../../../../roms/01-special.gb");                  // passes
            //rom = new Rom("../../../../roms/02-interrupts.gb");                 // passes
            //rom = new Rom("../../../../roms/03-op sp,hl.gb");                 // passes
            //rom = new Rom("../../../../roms/04-op r,imm.gb");                 // passes
            //rom = new Rom("../../../../roms/05-op rp.gb");                    // passes
            //rom = new Rom("../../../../roms/06-ld r,r.gb");                   // passes
            //rom = new Rom("../../../../roms/07-jr,jp,call,ret,rst.gb");       // passes
            //rom = new Rom("../../../../roms/08-misc instrs.gb");                // passes

            // Fails at 0xC9FB reading current scanline from 0xFF44
            //rom = new Rom("../../../../roms/09-op r,r.gb");                     // fail

            //rom = new Rom("../../../../roms/10-bit ops.gb");                  // fail
            //rom = new Rom("../../../../roms/11-op a,(hl).gb");                  // fail

            //rom = new Rom("../../../../roms/bits_bank1.gb");

            interrupts = new Interrupts(this);
            ppu = new Ppu(this);
            memory = new Memory(this);
            cpu = new Cpu(memory, interrupts);
            timer = new Timer(this);
            pad = new Joypad(interrupts, this);


            // yuck
            ppu.Memory = memory;

            cpu.Reset();
            ppu.Reset();
            interrupts.Reset();
            timer.Reset();
            pad.Reset();

            // Peek the first instruction (done this way so we can always see the next instruction)
            cpu.PeekNextInstruction();

            //Mode mode = Mode.Running;


            /*
            ConsoleKeyInfo key;

            // User keys
            Console.SetCursorPosition(0, 25);
            Console.Write(String.Format("[S]tep - [R]un - Rese[t] - [D]ump - E[x]it"));

            List<ushort> breakpoints = new List<ushort>()
            {
                0xFC
                //0xDEF8
            };
            //breakpoints[0] = 0xFC;
            //breakpoints[1] = 0xDEF9;
            //breakpoints[1] = 0x55;
            //breakpoints[2] = 0x8F;
            //breakpoints[3] = 0xF9;
            //breakpoints[1] = 0x28;
            //breakpoints[2] = 0x99;
            //breakpoints[3] = 0x68;

            //breakpoints[1] = 0x72;

            var timer = new Stopwatch();
            timer.Start();
            long elapsed = timer.ElapsedMilliseconds;
            bool keyAvailable = false;
            try
            {
                while (cpu.IsHalted == false)
                {
                    // Only poll the key every ms or the program slows to a crawl
                    if(timer.ElapsedMilliseconds - elapsed >= 1)
                    {
                        elapsed = timer.ElapsedMilliseconds;
                        keyAvailable = Console.KeyAvailable;
                    }

                    if (mode == Mode.BreakPoint) cpu.OutputState();

                    // Step the system
                    if (mode == Mode.BreakPoint ||
                        (mode == Mode.Running && keyAvailable))
                    {
                        keyAvailable = false;
                        key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.S:
                                mode = Mode.BreakPoint;
                                cpu.Step();
                                gpu.Step(cpu.Ticks);
                                break;

                            case ConsoleKey.R:
                                mode = Mode.Running;
                                break;


                            case ConsoleKey.D:
                                Dump();
                                break;

                            case ConsoleKey.X:
                                return;
                        }
                    }

                    if (mode == Mode.Running)
                    {
                        cpu.Step();
                        gpu.Step(cpu.Ticks);
                    }

                    foreach (var breakpoint in breakpoints)
                    {
                        if (cpu.PC == breakpoint)
                        {
                            mode = Mode.BreakPoint;
                            break;
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                DumpSystemState(ex);

                cpu.OutputState();

                DumpTty();

                DumpTileSet();
            }   
            
            */
        }

        public void Step()
        {
            cpu.Step();
            ppu.Step();
            timer.Step();
            pad.Step();
            interrupts.Step();
        }


        void Dump()
        {
            ppu.DumpFrameBufferToPng();

            DumpTty();

            //TileDumpTxt(memory.VRam, 0x190, 16);
            DumpTileSet();
        }

        public void DumpTty()
        {
            using (FileStream fs = File.Open("../../../../dump/tty.txt", FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(Tty.ToString());                 
                }
            }
        }

        void DumpSystemState(Exception ex)
        {
            using (FileStream fs = File.Open("../../../../dump/crashdump.txt", FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(cpu.ToString());

                    sw.Write("\n\n");

                    if (ex != null)
                    {
                        sw.Write(ex.ToString());
                    }
                }
            }
        }


        public void DumpTileSet()
        {
            Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };

            // map  - 16x24 (384) tiles 
            int tileMapX = 16;
            int tileMapY = 24;
            var image = new Bitmap(tileMapX * 8, tileMapY * 8);

            int tileX = 0;
            int tileY = 0;

            //int offset =  0;

            Tile[] tiles = ppu.Tiles; // new Tile[tileMapX * tileMapY];
            for (int i = 0; i < tiles.Length; i++)
            {
                //tiles[i] = new Tile((ushort)(0x8000 + offset));
                //Tile tile = tiles[i];
                tiles[i].Parse(memory.VRam);

                // 16 bytes per tile
                //offset += 16;            

                // Add one tiles pixels
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        image.SetPixel(x + (tileX * 8), y + (tileY * 8), palette[tiles[i].renderTile[x, y]]);
                    }
                }

                // Coordinates on the output image
                tileX++;
                if (tileX == tileMapX)
                {
                    tileX = 0;
                    tileY++;
                }
            }

            bool drawGrid = false;
            if (drawGrid)
            {
                Pen blackPen = new Pen(Color.Black, 0.5f);
                using (var graphics = Graphics.FromImage(image))
                {
                    for (int x = 0; x < tileMapX; x++)
                    {
                        graphics.DrawLine(blackPen, x * 8, 0, x * 8, tileMapY * 8);
                    }

                    for (int y = 0; y < tileMapY; y++)
                    {
                        graphics.DrawLine(blackPen, 0, y * 8, tileMapX * 8, y * 8);
                    }

                }
            }

            image.Save("../../../../dump/tileset.png");
        }        
    }
}
