using System;
using System.Drawing;
using System.IO;

namespace DMG
{
    class Program
    {
        public enum Mode
        {
            Running,
            Halted,
            BreakPoint
        }


        static BootRom bootstrapRom;
        static Rom rom;
        static Memory memory;
        static Cpu cpu;
        static Gpu gpu;

        static void Main(string[] args)
        {
            bootstrapRom = new BootRom("../../../../DMG.bin");
            rom = new Rom("../../../../tetris.gb");
            gpu = new Gpu();
            memory = new Memory(bootstrapRom, rom, gpu);         
            cpu = new Cpu(memory);
            

            Console.WriteLine(String.Format("Running {0}", rom.RomName));

            Mode mode = Mode.BreakPoint;

            ConsoleKeyInfo key;
            
            // User keys
            Console.SetCursorPosition(0, 25);
            Console.Write(String.Format("[S]tep - [R]un - Rese[t] - [D]ump - E[x]it"));

            ushort[] breakpoints = new ushort[64];
            breakpoints[0] = 0xFC;
            breakpoints[1] = 0x40;
            //breakpoints[1] = 0x72;

            while (cpu.IsHalted == false)
            {
                cpu.OutputState();

                // Step the system
                if ( mode == Mode.BreakPoint ||
                    (mode == Mode.Running && Console.KeyAvailable))
                {
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

                if(mode == Mode.Running)
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


        static void Dump()
        {
            //TileDumpTxt(memory.VRam, 0x190, 16);
            DumpTileSet();
        }


        static void DumpTileSet()
        {
            Color[] palette = new Color[4] { Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), Color.FromArgb(0xFF, 0xC0, 0xC0, 0xC0), Color.FromArgb(0xFF, 0x60, 0x60, 0x60), Color.FromArgb(0xFF, 0x00, 0x00, 0x00) };          

            // map  - 16x16 (256) tiles 
            int tileMapX = 16;
            int tileMapY = 16;
            var image = new Bitmap(tileMapX * 8, tileMapY * 8);

            int tileX = 0;
            int tileY = 0;

            int offset =  0;

            Tile[] tiles = new Tile[tileMapX * tileMapY];
            for(int i=0; i < tiles.Length; i++)
            {
                tiles[i] = new Tile();
                Tile tile = tiles[i];
                tile.Parse(memory.VRam, offset);

                // 16 bytes per tile
                offset += 16;

                // Add one tiles pixels
                for (int y = 0; y < 8; y++)
                {
                    for (int x = 0; x < 8; x++)
                    {
                        image.SetPixel(x + (tileX * 8), y + (tileY * 8), palette[tile.renderTile[x, y]]);
                    }
                }                

                // Coordinates on the output image
                tileX++;
                if(tileX == tileMapX)
                {
                    tileX = 0;
                    tileY++;
                }
            }

            bool drawGrid = false;
            if(drawGrid)
            {
                Pen blackPen = new Pen(Color.Black, 0.5f);
                using (var graphics = Graphics.FromImage(image))
                {
                    for(int x = 0; x < tileMapX; x++)
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



            //byte[] tileData = new byte[16] { 0x3C, 0x00, 0x42, 0x00, 0xB9, 0x00, 0xA5, 0x00, 0xB9, 0x00, 0xA5, 0x00, 0x42, 0x00, 0x3C, 0x00 };
            //var tile = new Tile();
            //tile.Parse(tileData, 0);
            //tile.DumptToImageFile("../../../../dump/tile.png");


        }


        static void TileDumpTxt(byte[] array, int offset, int count)
        {
            using (FileStream fs = File.Open("../../../../dump/dump.txt", FileMode.Create))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {

                    for (int i = offset; i < (offset + count); i++)
                    {
                        sw.WriteLine(String.Format("{0:X2} ", array[i]));
                    }

                    /*
                    for (int i = offset; i < (offset + count); i += 2)
                    {
                        sw.WriteLine(String.Format("{0:X2} {1:X2}", array[i + 1], array[i]));
                    }

                    sw.WriteLine();
                    sw.WriteLine();

                    for (int i = offset; i < (offset + count); i += 2)
                    {
                        sw.WriteLine(String.Format("{0:X2}\n{1:X2}\n\n", Convert.ToString(array[i + 1], 2).PadLeft(8, '0'), Convert.ToString(array[i], 2).PadLeft(8, '0')));
                    }
                    */
                }
            }
        }



    }
}
