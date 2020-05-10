using System;
using System.Collections.Generic;
using System.Text;



namespace DMG
{
    public class Joypad
    {
        // When a key is pressed, its state on a GB is 0
        byte register = 0xFF;

        // Bits 5 & 6 select if we are reading buttons or the pad. The program sets 5 & 6 to tell us what it wants

        public byte Register
        {
            get
            {
                UpdateRegister();
                return register;
            }
            set
            {
                register = value;                
            }
        }


        public enum GbKey
        {
            Up = 0,
            Down,
            Left,
            Right, 
            A,
            B,
            Start,
            Select
        }


        public enum GbKeyBits
        {
            Right_Bit   = 1 << 0,
            A_Bit       = 1 << 0,
            Left_Bit    = 1 << 1,
            B_Bit       = 1 << 1,
            Up_Bit      = 1 << 2,
            Select_Bit  = 1 << 2,
            Down_Bit    = 1 << 3,
            Start_Bit   = 1 << 3
        }


        // We capture key state the 'right' way, true == pressed
        bool[] keys = new bool[8];

        DmgSystem dmg;
        Interrupts interrupts;

        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;


        public Joypad(Interrupts interrupts, DmgSystem dmg)
        {
            this.dmg = dmg;
            this.interrupts = interrupts;
        }


        public void Reset()
        {
            register = 0xFF;

            for (int i=0; i < 8; i++)
            {
                keys[i] = false;
            }
        }


        public void Step()
        {
            UInt32 cpuTickCount = dmg.cpu.Ticks;

            // Track how many cycles the CPU has done since we last changed states
            elapsedTicks += (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            // Joypad Poll Speed (64 Hz)
            //if (elapsedTicks >= 65536)        // tcycles
            if (elapsedTicks >= 16384)          // mcycles
            {
                elapsedTicks -= 16384;
                UpdateRegister();
            }
        }


        // Bit 7 - Not used
        // Bit 6 - Not used
        // Bit 5 - P15 Select Button Keys(0=Select)
        // Bit 4 - P14 Select Direction Keys(0=Select)
        // Bit 3 - P13 Input Down or Start(0=Pressed) (Read Only)
        // Bit 2 - P12 Input Up or Select(0=Pressed) (Read Only)
        // Bit 1 - P11 Input Left or Button B(0=Pressed) (Read Only)
        // Bit 0 - P10 Input Right or Button A(0=Pressed) (Read Only)
        void UpdateRegister()
        {
            // NB: The bits to select which keys to read are active low! See that we flip the opposite select bit below
            if((register & (byte) 0x20) == 0)
            {
                // Buttons requested                
                if (keys[(int)GbKey.A] == false) register |= (byte)(GbKeyBits.A_Bit);
                if (keys[(int)GbKey.B] == false) register |= (byte)(GbKeyBits.B_Bit);
                if (keys[(int)GbKey.Start] == false) register |= (byte)(GbKeyBits.Start_Bit);
                if (keys[(int)GbKey.Select] == false) register |= (byte)(GbKeyBits.Select_Bit);
            }
            else if ((register & (byte)0x10) == 0)
            {
                // Pad requested
                if (keys[(int)GbKey.Up] == false) register |= (byte)(GbKeyBits.Up_Bit);
                if (keys[(int)GbKey.Down] == false) register |= (byte)(GbKeyBits.Down_Bit);
                if (keys[(int)GbKey.Left] == false) register |= (byte)(GbKeyBits.Left_Bit);
                if (keys[(int)GbKey.Right] == false) register |= (byte)(GbKeyBits.Right_Bit);
            }                    
        }


        public void UpdateKeyState(GbKey key, bool state)
        {
            // Interrupt occurs when a button becomes pressed
            bool fireInterrupt = false;
            if(keys[(int)key] == false && state == true)
            {
                fireInterrupt = true;
            }

            keys[(int)key] = state;

            if (fireInterrupt)
            {
                interrupts.RequestInterrupt(Interrupts.Interrupt.INTERRUPTS_JOYPAD);
            }
        }
    }
}
