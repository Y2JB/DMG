using System;
using System.Collections.Generic;
using System.Text;

namespace DMG
{
    public class Timer
    {
        public bool Enabled { get; set; }

        readonly ushort TIMA = 0xFF05;              // Current Timer value 
        readonly ushort TMA = 0xFF06;               // Timer modulator, what value to we reset to when we overflow

        byte tmc;
        public byte TimerControllerRegister {  get { return tmc; }

            set
            {
                tmc = value;
                Enabled = ((tmc & 0x04) != 0);
                byte freq = (byte) (tmc & 0x03);
                if (freq == 0) SetFrequency(TimerFreq.Hz4096);
                else if (freq == 1) SetFrequency(TimerFreq.Hz262144);
                else if (freq == 2) SetFrequency(TimerFreq.Hz65536);
                else if (freq == 3) SetFrequency(TimerFreq.Hz16384);
            }
        }

        // Continually counts up from 0 to 255 and then when it overflows it starts from 0 again. It does not cause an interupt when it overflows and it cannot be paused 
        // It is convenient to implement it alongside the timer. Games use this for a random number
        // It is implemented at Memory Address 0xFF04
        public byte DividerRegister { get; private set; }
        UInt32 dividerRegisterElapsedTicks;

        public enum TimerFreq
        {
            Hz4096 = 4096,
            Hz16384 = 16384,
            Hz65536 = 65536,
            Hz262144 = 262144
        }

        public TimerFreq Freq { get; private set; }

        UInt32 lastCpuTickCount;
        Int32 cyclesUntilTimerFires;

        DmgSystem dmg;

        public Timer(DmgSystem dmg)
        {
            this.dmg = dmg;
        }

        public void SetFrequency(TimerFreq freq)
        {
            Freq = freq;
            cyclesUntilTimerFires = (Int32) (dmg.cpu.ClockSpeedHz / (Int32)(freq));
        }

        public void Reset()
        {
            // 4mhz, and disabled
            TimerControllerRegister = 0x00;

            lastCpuTickCount = 0;
            dividerRegisterElapsedTicks = 0;
        }

        public void Step()
        {
            UInt32 cpuTickCount = dmg.cpu.Ticks;


            // TODO : SOMETIMES ELAPSED TICKS == 0 !!!!!!!!!!!!!!XXXXXXXXXXXXXXXXXXXXXXXXXXXX

            // Track how many cycles the CPU has done since we last updated
            UInt32 elapsedTicks = (UInt32) (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            UpdateDividerRegister(elapsedTicks);

            if (Enabled)
            {              
                cyclesUntilTimerFires -= (int) elapsedTicks;

                if (cyclesUntilTimerFires <= 0)
                {
                    // reset for next cycle count 
                    SetFrequency(Freq);

                    // timer about to overflow
                    if (dmg.memory.ReadByte(TIMA) == 0xFF)
                    {
                        dmg.memory.WriteByte(TIMA, dmg.memory.ReadByte(TMA));
                        
                        dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_TIMER);
                    }
                    else
                    {
                        dmg.memory.WriteByte(TIMA, (byte) (dmg.memory.ReadByte(TIMA) + 1));
                    }
                }
            }
        }

        private void UpdateDividerRegister(UInt32 cycles)
        {
            dividerRegisterElapsedTicks += cycles;
            if (dividerRegisterElapsedTicks >= 255)
            {
                dividerRegisterElapsedTicks -= 255;

                DividerRegister++;
            }
        }
    }
    
}
