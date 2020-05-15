using Emux.GameBoy.Audio;
using Emux.NAudio;
using NAudio.Wave;
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

        public GameBoySpu spu { get; private set; }
        GameBoyNAudioMixer mixer;
        DirectSoundOut player;

        public Joypad pad { get; private set; }

        public DmgTimer timer { get; private set; }

        public StringBuilder Tty { get; private set; }

        public Bitmap FrameBuffer { get { return ppu.FrameBuffer; } }


        public Stopwatch EmulatorTimer { get; private set; }
        public Action OnFrame{ get; set;  }
        
        // Debugger hooks
        public Action<UInt32> OnFrameStart { get; set; }
        public Action<UInt32, bool> OnFrameEnd { get; set; }
        public Action<UInt32, UInt32, List<OamEntry>> OnOamSearchComplete { get; set; }

        public DmgSystem()
        {
            Tty = new StringBuilder(1024 * 256);
            EmulatorTimer = new Stopwatch();
        }


        public void PowerOn()
        {
            bootstrapRom = new BootRom("../../../../DMG_BootRom.bin");
            //rom = new Rom("../../../../roms/games/tetris.gb");
            //rom = new Rom("../../../../roms/games/Dr. Mario.gb");
            //rom = new Rom("../../../../roms/games/Bubble Ghost (J).gb");
            //rom = new Rom("../../../../roms/games/Super Mario Land.gb");
            //rom = new Rom("../../../../roms/games/Super Mario Land 2 - 6 Golden Coins (UE) (V1.2).gb");
            //rom = new Rom("../../../../roms/games/Wario Land - Super Mario Land 3.gb");            
            //rom = new Rom("../../../../roms/games/Bootleg Marioland 4.gb");
            //rom = new Rom("../../../../roms/games/Teenage Mutant Hero Turtles - Back from the Sewers (E).gb");
            //rom = new Rom("../../../../roms/games/Teenage Mutant Hero Turtles - Fall of the Foot Clan (E).gb");
            //rom = new Rom("../../../../roms/games/Teenage Mutant Hero Turtles III - Radical Rescue (E) [!].gb");
            rom = new Rom("../../../../roms/games/Legend of Zelda, The - Link's Awakening (U) (V1.2).gb");
            //rom = new Rom("../../../../roms/games/Pokemon - Blue.gb");
            //rom = new Rom("../../../../roms/games/Gargoyle's Quest - Ghosts'n Goblins.gb");
            //rom = new Rom("../../../../roms/games/Mega Man V.gb");
            //rom = new Rom("../../../../roms/games/Donkey Kong.gb");
            //rom = new Rom("../../../../roms/games/Donkey Kong Land (U) [S][!].gb");
            //rom = new Rom("../../../../roms/games/Donkey Kong Land 2 (UE) [S][!].gb");
            //rom = new Rom("../../../../roms/games/Donkey Kong Land III (U) [S][!].gb");
            //rom = new Rom("../../../../roms/games/X - Xekkusu.gb");
            //rom = new Rom("../../../../roms/games/Wave race.gb");
            //rom = new Rom("../../../../roms/games/F-1 Race.gb");
            //rom = new Rom("../../../../roms/games/Pinball Deluxe (U).gb");
            //rom = new Rom("../../../../roms/games/Prehistorik Man.gb");
            //rom = new Rom("../../../../roms/games/Amazing Spider-Man 3, The - Invasion of the Spider-Slayers (U) [!].gb");
            //rom = new Rom("../../../../roms/games/Tennis (JUE) [!].gb");

            //rom = new Rom("../../../../roms/bgbtest.gb");
            //rom = new Rom("../../../../roms/tellinglys.gb");                  //passes 

            // Blargg CPU tests
            //rom = new Rom("../../../../roms/cpu_instrs.gb");
            //rom = new Rom("../../../../roms/01-special.gb");                  // passes
            //rom = new Rom("../../../../roms/02-interrupts.gb");               // passes
            //rom = new Rom("../../../../roms/03-op sp,hl.gb");                 // passes
            //rom = new Rom("../../../../roms/04-op r,imm.gb");                 // passes
            //rom = new Rom("../../../../roms/05-op rp.gb");                    // passes
            //rom = new Rom("../../../../roms/06-ld r,r.gb");                   // passes
            //rom = new Rom("../../../../roms/07-jr,jp,call,ret,rst.gb");       // passes
            //rom = new Rom("../../../../roms/08-misc instrs.gb");              // passes
            //rom = new Rom("../../../../roms/09-op r,r.gb");                   // passes
            //rom = new Rom("../../../../roms/10-bit ops.gb");                  // passes
            //rom = new Rom("../../../../roms/11-op a,(hl).gb");                // passes

            //rom = new Rom("../../../../roms/instr_timing.gb");                // passes
            //rom = new Rom("../../../../roms/mem_timing.gb");                  // passes
            //rom = new Rom("../../../../roms/mem_timing2.gb");                 // passes
            //rom = new Rom("../../../../roms/interrupt_time.gb");              // fails (GBC only)
            //rom = new Rom("../../../../roms/halt_bug.gb");

            // Mooneye tests
            //rom = new Rom("../../../../roms/bits_bank1.gb");                    // pass

            if (rom.Type == Rom.RomType.UnSupported)
            {
                throw new InvalidDataException("Unsupported ROM type");
            }

            interrupts = new Interrupts(this);
            ppu = new Ppu(this);
            memory = new Memory(this);
            cpu = new Cpu(memory, interrupts, this);
            timer = new DmgTimer(this);
            pad = new Joypad(interrupts, this);
            spu = new GameBoySpu(this);
            spu.Initialize();

            mixer = new GameBoyNAudioMixer();
            mixer.Connect(spu);
            player = new DirectSoundOut();
            player.Init(mixer);
            player.Play();


            // yuck
            ppu.Memory = memory;

            cpu.Reset();
            ppu.Reset();
            interrupts.Reset();
            timer.Reset();
            pad.Reset();
            

            // Peek the first instruction (done this way so we can always see the next instruction)
            //cpu.PeekNextInstruction();

            EmulatorTimer.Reset();
            EmulatorTimer.Start();

            if(rom.Type == Rom.RomType.MBC1_Ram_Battery)
            {
                rom.LoadMbc1BatteryBackData();
            }


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
            spu.SpuStep();
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
                    
                    sw.Write(ppu.MemoryRegisters.ToString());

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


            foreach (var t in ppu.Tiles)
            {
                t.Value.Parse(memory.VRam);

                // 16 bytes per tile
                //offset += 16;            

                // Add one tiles pixels
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        image.SetPixel(x + (tileX * 8), y + (tileY * 8), palette[t.Value.renderTile[x, y]]);
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
       
            image.Save(string.Format("../../../../dump/tileset_{0}.png", rom.RomName));
        }        
    }
}
