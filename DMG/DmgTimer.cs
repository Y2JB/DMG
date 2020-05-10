using System;
using System.Collections.Generic;
using System.Text;

namespace DMG
{
    public class DmgTimer
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
        public byte DividerRegister { get; set; }
        UInt32 dividerRegisterElapsedTicks;

        public enum TimerFreq
        {
//            Hz4096 = 4096,
//            Hz16384 = 16384,
//            Hz65536 = 65536,
//            Hz262144 = 262144
            // mcycle values 
            Hz4096 = 1024,
            Hz16384 = 4096,
            Hz65536 = 16384,
            Hz262144 = 65536
        }

        //4194304

        public TimerFreq Freq { get; private set; }

        UInt32 lastCpuTickCount;
        Int32 cyclesUntilTimerFires;

        DmgSystem dmg;


        public DmgTimer(DmgSystem dmg)
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

        UInt32 m_iDIVCycles = 0;
        UInt32 m_iTIMACycles = 0;

        public void Step()
        {
            UInt32 cpuTickCount = dmg.cpu.Ticks;
            UInt32 elapsedTicks = (UInt32)(cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            //elapsedTicks *= 4;

            m_iDIVCycles += elapsedTicks;

            UInt32 div_cycles = 256;

            while (m_iDIVCycles >= div_cycles)
            {
                m_iDIVCycles -= div_cycles;
                byte div = dmg.memory.ReadByte(0xFF04);
                div++;
                dmg.memory.WriteByte(0xFF04, div);
                //dmg.timer.DividerRegister = div;
            }

            byte tac = dmg.memory.ReadByte(0xFF07);

            // if tima is running
            if ((tac & 0x04) != 0)
            {
                m_iTIMACycles += elapsedTicks;

                UInt32 freq = 0;

                switch (tac & 0x03)
                {
                    case 0:
                        freq = 1024;
                        break;
                    case 1:
                        freq = 16;
                        break;
                    case 2:
                        freq = 64;
                        break;
                    case 3:
                        freq = 256;
                        break;
                }

                while (m_iTIMACycles >= freq)
                {
                    m_iTIMACycles -= freq;
                    byte tima = dmg.memory.ReadByte(0xFF05);

                    if (tima == 0xFF)
                    {
                        tima = dmg.memory.ReadByte(0xFF06);
                        dmg.interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_TIMER);
                    }
                    else
                        tima++;

                    dmg.memory.WriteByte(0xFF05, tima);
                }
            }


            /*
            UInt32 cpuTickCount = dmg.cpu.Ticks;


            // TODO : SOMETIMES ELAPSED TICKS == 0 !!!!!!!!!!!!!!XXXXXXXXXXXXXXXXXXXXXXXXXXXX

            // Track how many cycles the CPU has done since we last updated
            UInt32 elapsedTicks = (UInt32) (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            if(elapsedTicks == 0)
            {
                throw new ArgumentException("wtf");
            }

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
            */
        }

        private void UpdateDividerRegister(UInt32 cycles)
        {
            // We use Mcycles so *4 the cycles
            dividerRegisterElapsedTicks += (cycles * 4);
            if (dividerRegisterElapsedTicks >= 255)
            {
                dividerRegisterElapsedTicks -= 255;

                DividerRegister++;
            }
        }
    }
    
}
