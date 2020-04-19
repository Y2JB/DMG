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

            if (value == 0) SetFlag(Flags.Zero); 
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);

            return value;
        }


        byte Add(byte lhs, byte rhs)
        {
            UInt16 result = (UInt16) (lhs + rhs);

            if ((result & 0xFF00) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            byte destination = (byte)(result & 0xFF);

            if (destination == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            if (((destination & 0x0F) + (rhs & 0x0F)) > 0x0F) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);

            ClearFlag(Flags.Negative);

            return destination;
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

        void INC_a()
        {
            A = Inc(A);
        }

        void INC_b()
        {
            B = Inc(B);
        }

        void INC_c()
        {
            C = Inc(C);
        }

        void INC_d()
        {
            D = Inc(D);
        }

        void INC_e()
        {
            E = Inc(E);
        }

        void INC_h()
        {
            H = Inc(H);
        }

        void INC_l()
        {
            L = Inc(L);
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

        void JR_NZ_n(sbyte n)
        {
            if(ZeroFlag == false)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;
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

        void LD_a_n(byte n)
        {
            A = n;
        }

        void LD_h_hlp()
        {
            H = memory.ReadByte(HL);
        }

        void ADD_a_b()
        {
            A = Add(A, B);
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


        // 0xE2
        void LD_ff_c_a()
        {
            memory.WriteByte((ushort)((ushort) 0xFF00 + (ushort) C), A);
        }
    }
}
