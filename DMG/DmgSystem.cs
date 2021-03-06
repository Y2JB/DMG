﻿using Emux.GameBoy.Audio;
using Emux.NAudio;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;

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

        public Bitmap FrameBuffer { get { return ppu.FrameBuffer.Bitmap; } }

        long oneSecondTimer;
        public Stopwatch EmulatorTimer { get; private set; }
        public Action OnFrame{ get; set;  }
        
        // Debugger hooks
        public Action<UInt32> OnFrameStart { get; set; }
        public Action<UInt32, bool> OnFrameEnd { get; set; }
        public Action<UInt32, UInt32, List<OamEntry>> OnOamSearchComplete { get; set; }

        int secondsSinceLastSave;
        uint ticksPerSecond;

        public bool PoweredOn { get; private set; }

        public DmgSystem()
        {
            Tty = new StringBuilder(1024 * 256);
            EmulatorTimer = new Stopwatch();
            PoweredOn = false;
        }


        public void PowerOn(string romName)
        {
            PoweredOn = true;

            bootstrapRom = new BootRom("../../../../DMG_BootRom.bin");
            rom = new Rom(romName); 
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
            //rom = new Rom("../../../../roms/games/Legend of Zelda, The - Link's Awakening (U) (V1.2).gb");
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

            cpu.Reset();
            ppu.Reset();
            interrupts.Reset();
            timer.Reset();
            pad.Reset();   

            EmulatorTimer.Reset();
            EmulatorTimer.Start();

            if(rom.Type == Rom.RomType.MBC1_Ram_Battery)
            {
                rom.LoadMbc1BatteryBackData();
            }

            ticksPerSecond = 0;
            secondsSinceLastSave = 0;
        }


        public void Step()
        {
            if (PoweredOn == false)
            {
                Thread.Sleep(10);
                return;           
            }

            cpu.Step();
            ppu.Step();
            timer.Step();
            pad.Step();
            interrupts.Step();
            spu.SpuStep();

            if(EmulatorTimer.ElapsedMilliseconds - oneSecondTimer >= 1000)
            {
                oneSecondTimer = EmulatorTimer.ElapsedMilliseconds;
                secondsSinceLastSave++;

                // Keep track of how many ticks per second the cpu has executed. The cpu itself has no concept of time beyond ticks so we calculate this here. 
                cpu.CyclesPerSecond = ((cpu.Ticks) - ticksPerSecond);
                ticksPerSecond = cpu.Ticks;
            }

            if(secondsSinceLastSave >= 120)
            {
                // Save the game every couple of minutes
                rom.SaveMbc1BatteryBackData();
                secondsSinceLastSave = 0;
            }
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


        // TODO: Hook up a global exception handler to write this out in if we crash
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
      
                // Add one tiles' pixels
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
