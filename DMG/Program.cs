using System;

namespace DMG
{
    class Program
    {
        static void Main(string[] args)
        {
            var bootstrapRom = new BootRom("../../../../DMG.bin");
            var rom = new Rom("../../../../tetris.gb");
            var memory = new Memory(bootstrapRom, rom);


            Console.WriteLine(String.Format("Running {0}", rom.RomName));
            var cpu = new Cpu(memory);

            while (cpu.IsHalted == false)
            {
                cpu.Step();
            }
        }
    }
}
