using System;
namespace DMG
{
    public class Interupts
    {
        readonly byte INTERRUPTS_VBLANK =  (1 << 0);
        readonly byte INTERRUPTS_LCDSTAT = (1 << 1);
        readonly byte INTERRUPTS_TIMER =   (1 << 2);
        readonly byte INTERRUPTS_SERIAL =  (1 << 3);
        readonly byte INTERRUPTS_JOYPAD =  (1 << 4);

        public bool InteruptsMasterEnable { get; set; }


        // I think the next two fields are how it works!!

        // Bitfield to tell us which interupts are enabled
        public byte InteruptEnableRegister { get; set; }

        // https://gbdev.gg8.se/wiki/articles/Interrupts
        // When an interrupt signal changes from low to high, then the corresponding bit in the IF register becomes set.
        public byte InteruptFlags { get; set; }

        DmgSystem dmg;

        public Interupts(DmgSystem dmg)
        {
            this.dmg = dmg;            
        }

        public void Reset()
        {
            InteruptsMasterEnable = true;
            InteruptFlags = 0;
            InteruptEnableRegister = 0;
        }

        public void Step()
        {
            if (InteruptsMasterEnable && InteruptEnableRegister != 0 && InteruptFlags != 0)
            {
                byte fire = (byte) (InteruptEnableRegister & InteruptFlags);

                if ((fire & INTERRUPTS_VBLANK) != 0)
                {
                    InteruptFlags &= (byte) ~INTERRUPTS_VBLANK;
                    vblank();
                }

                if ((fire & INTERRUPTS_LCDSTAT) != 0)
                {
                    InteruptFlags &= (byte)~INTERRUPTS_LCDSTAT;
                    lcdStat();
                }

                if ((fire & INTERRUPTS_TIMER) != 0)
                {
                    InteruptFlags &= (byte)~INTERRUPTS_TIMER;
                    timer();
                }

                if ((fire & INTERRUPTS_SERIAL) != 0)
                {
                    InteruptFlags &= (byte)~INTERRUPTS_SERIAL;
                    serial();
                }

                if ((fire & INTERRUPTS_JOYPAD) != 0)
                {
                    InteruptFlags &= (byte)~INTERRUPTS_JOYPAD;
                    joypad();
                }
            }
        }



        void vblank()
        {
            //drawFramebuffer();
            //if (dmg.OnFrame != null)
            //{
            //    dmg.OnFrame();
            //}

            InteruptsMasterEnable = false;

            dmg.cpu.StackPush(dmg.cpu.PC);
            dmg.cpu.PC = 0x40;


            dmg.cpu.Ticks += 12;            
        }

        void lcdStat()
        {
            InteruptsMasterEnable = false;
            dmg.cpu.StackPush(dmg.cpu.PC);
            dmg.cpu.PC = 0x48;

            dmg.cpu.Ticks += 12;
        }

        void timer()
        {
            InteruptsMasterEnable = false;
            dmg.cpu.StackPush(dmg.cpu.PC);
            dmg.cpu.PC = 0x50;

            dmg.cpu.Ticks += 12;
        }

        void serial()
        {
            InteruptsMasterEnable = false;
            dmg.cpu.StackPush(dmg.cpu.PC);
            dmg.cpu.PC = 0x58;

            dmg.cpu.Ticks += 12;
        }

        void joypad()
        {
            InteruptsMasterEnable = false;
            dmg.cpu.StackPush(dmg.cpu.PC);
            dmg.cpu.PC = 0x60;

            dmg.cpu.Ticks += 12;
        }

        public void ReturnFromInterrupt()
        {
            InteruptsMasterEnable = true;
            dmg.cpu.PC = dmg.cpu.StackPop();
        }
    }
}
