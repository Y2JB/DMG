using System;

namespace DMG
{
    public partial class Cpu
    {
        private void Xor(byte value)
        {
            A ^= value;

            if (A != 0) ClearFlag(Flags.Zero);
            else SetFlag(Flags.Zero);

            ClearFlag(Flags.Carry);
            ClearFlag(Flags.HalfCarry);
            ClearFlag(Flags.Negative);
        }


        private byte Inc(byte value)
        {
            if ((value & 0x0f) == 0x0f) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);

            value++;

            if (value == 0) ClearFlag(Flags.Zero);
            else SetFlag(Flags.Zero);

            ClearFlag(Flags.Negative);

            return value;
        }


        // ********************
        // Instruction Handlers 
        // ********************

        void NOP()
        {
        }

        void LD_b_n(byte n)
        {
            B = n;
        }

        void LD_c_n(byte n)
        {
            C = n;
        }

        void LD_d_n(byte n)
        {
            D = n;
        }

        void LD_e_n(byte n)
        {
            E = n;
        }

        void JR_NZ_n(byte n)
        {
            if(ZeroFlag == false)
            {
                PC += n;
            }
        }

        void LD_h_n(byte n)
        {
            H = n;
        }

        void LD_l_n(byte n)
        {
            L = n;
        }

        void LD_sp_nn(ushort nn)
        {
            SP = nn;
        }

        void LDD_hl_a()
        {
            // Put A into memory address HL. Decrement HL.
            memory.WriteByte(HL, A);
            HL--;
        }

        void LD_h_hlp()
        {
            H = memory.ReadByte(HL);
        }

        void INC_d()
        {
            D = Inc(D);
        }


        void JP_nn(ushort nn)
        {
            PC = nn;
        }


        void XOR_a()
        {
            Xor(A);
        }


        void LD_hl(ushort operand)
        {
            HL = operand;
        }
    }
}
