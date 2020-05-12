using System;
using System.Collections.Generic;
using System.Text;

namespace DMG
{
    public class DmgTimer
    {
        readonly ushort TIMA = 0xFF05;              // Current Timer value 
        readonly ushort TMA = 0xFF06;               // Timer modulator, what value to we reset to when we overflow

        // 1048576 / 16384
        readonly UInt32 DivTimerFrequency = 64;


        // FF07 (TAC)
        byte tmc;
        public byte TimerControllerRegister 
        {  
            get { return tmc; }

            set
            {
                tmc = value;
                ExpireTimer();              
            }
        }

        public bool Enabled
        {
            get
            {
                return ((tmc & 0x04) != 0);
            }
        }


        public int TimerSelect()
        {
            return (tmc & 0x03);
        }


        // Continually counts up from 0 to 255 and then when it overflows it starts from 0 again. It does not cause an interupt when it overflows and it cannot be paused 
        // It is convenient to implement it alongside the timer. Games use this for a random number
        // It is implemented at Memory Address 0xFF04
        public byte DividerRegister { get; set; }


        // 1048576 / 
        // order is 4khz, 256khz, 64khz, 16khz
        uint[] timerTicksToFire = new uint[] { 256, 4, 16, 64 };


        UInt32 lastCpuTickCount;
        UInt32 elapsedDivTicks;
        public UInt32 elapsedTimaTicks { get; set; }

        UInt32 cyclesUntilTimerFires;

        DmgSystem dmg;


        public DmgTimer(DmgSystem dmg)
        {
            this.dmg = dmg;
        }


        public void Reset()
        {
            // 4mhz, and disabled
            TimerControllerRegister = 0x00;

            lastCpuTickCount = dmg.cpu.Ticks;

            // Another interesting discovery: the internal timer that is behind DIV starts ticking 2 M-cycles  before the first boot ROM instruction fetch even starts
            // I have no idea why or what implications this might have, but basically there's a delay of 2 M-cycles before it even starts fetching stuff
            elapsedDivTicks = 2;

            elapsedTimaTicks = 0;
        }


        public void ExpireTimer()
        {
            int timerNumber = TimerSelect();
            cyclesUntilTimerFires = timerTicksToFire[timerNumber];            
        }


        // https://gbdev.gg8.se/wiki/articles/Timer_Obscure_Behaviour
        public void Step()
        {
            UInt32 tickCount = (dmg.cpu.Ticks - lastCpuTickCount);
            lastCpuTickCount = dmg.cpu.Ticks;

            UpdateDividerRegister(tickCount);

            if (Enabled)
            {
                // Track how many cycles the CPU has done since we last changed states
                elapsedTimaTicks += tickCount;

                // Don't lose any ticks
                while (elapsedTimaTicks >= cyclesUntilTimerFires)
                {
                    elapsedTimaTicks -= cyclesUntilTimerFires;

                    // reset for next cycle count 
                    ExpireTimer();

                    // Increment the timer register 
                    byte tima = dmg.memory.ReadByte(TIMA);
                    if (tima == 0xFF)
                    {
                        // Timer about to overflow

                        // When TIMA overflows, the value from TMA is loaded and IF timer flag is set to 1, but this doesn't happen immediately. Timer interrupt is delayed 1 cycle (4 clocks) from the TIMA overflow. 

                        // The TMA reload to TIMA is also delayed. For one cycle, after overflowing TIMA, the value in TIMA is 00h, not TMA. This happens only when an overflow happens, not when 
                        // the upper bit goes from 1 to 0, it can't be done manually writing to TIMA, the timer has to increment itself.

                        tima = dmg.memory.ReadByte(TMA);
                        dmg.memory.WriteByte(TIMA, tima);
                        dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_TIMER);
                    }
                    else
                    {
                        tima++;
                    }
                    dmg.memory.WriteByte(TIMA, tima);
                }
            }           
        }


        public void ResetTIMACycles()
        {
            elapsedTimaTicks = 0;
            dmg.memory.WriteByte(TIMA, dmg.memory.ReadByte(TMA));
        }

        // This register is incremented at rate of 16384Hz, 256 times per second 
        private void UpdateDividerRegister(UInt32 elapsedTicks)
        {
            elapsedDivTicks += elapsedTicks;
            while (elapsedDivTicks >= DivTimerFrequency)
            {
                elapsedDivTicks -= DivTimerFrequency;

                DividerRegister++;
            }
        }
    }
    
}
