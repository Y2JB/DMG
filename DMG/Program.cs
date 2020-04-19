using System;

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

        static void Main(string[] args)
        {
            var bootstrapRom = new BootRom("../../../../DMG.bin");
            var rom = new Rom("../../../../tetris.gb");
            var memory = new Memory(bootstrapRom, rom);


            Console.WriteLine(String.Format("Running {0}", rom.RomName));
            var cpu = new Cpu(memory);



            
            Mode mode = Mode.BreakPoint;

            ConsoleKeyInfo key;

            // User keys
            Console.SetCursorPosition(0, 25);
            Console.Write(String.Format("[S]tep - [R]un - Rese[t] - [D]isplay - E[x]it"));

            ushort[] breakpoints = new ushort[64];
            breakpoints[0] = 0xC;
            breakpoints[1] = 0x13;

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
                            break;

                        case ConsoleKey.R:
                            mode = Mode.Running;
                            break;

                        case ConsoleKey.X:
                            return;
                    }
                }

                if(mode == Mode.Running)
                {
                    cpu.Step();
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
    }
}
