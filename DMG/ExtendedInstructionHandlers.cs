using System;

namespace DMG
{
    public partial class Cpu
    {
        void TestBit(byte bit, byte value)
        {
            /*
            if ((value & bit) == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);           

            ClearFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);
            */


            if (((value >> bit) & 0x01) == 0)
            {
                SetFlag(Flags.Zero);
            }
            else
            {
                ClearFlag(Flags.Carry);
            }
            ClearFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);
        }


        // Rotates register to the left with the carry's value put into bit 0 and bit 7 is put into the carry.
        byte Rl(byte value, bool isRegisterA)
        {
            /*
            int carry = CarryFlag ? 1 : 0;

            if ((value & 0x80) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            value <<= 1;
            value += (byte) carry;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);
            */

            byte carry = CarryFlag ? (byte) 1 : (byte) 0;
            byte result = value;
            ClearAllFlags();
            if ((result & 0x80) != 0) SetFlag(Flags.Carry);

            result <<= 1;
            result |= carry;

            if (!isRegisterA)
            {
                if (result == 0) SetFlag(Flags.Zero);
                else ClearFlag(Flags.Zero);
            }

            return result;
        }


        // Rotates to the right with the carry put in bit 7 and bit 0 put into the carry.
        byte Rr(byte value, bool isRegisterA)
        {
            /*
            value >>= 1;
            if (CarryFlag) value |= 0x80;

            if ((value & 0x01) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);
            */

            byte carry = CarryFlag ? (byte) 0x80 : (byte) 0x00;
            byte result = value;

            ClearAllFlags();

            if ((result & 0x01) != 0) SetFlag(Flags.Carry);
            result >>= 1;
            result |= carry;

            if (!isRegisterA)
            {
                if (result == 0) SetFlag(Flags.Zero);
                else ClearFlag(Flags.Zero);
            }

            return result;
        }


        // Rotates to the left with bit 7 being moved to bit 0 and also stored into the carry.
        byte Rlc(byte value)
        {
            int carry = (value & 0x80) >> 7;

            if ((value & 0x80) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            value <<= 1;
            value += (byte) carry;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);

            return value;
        }


        // Rotates to the right with bit 0 moved to bit 7 and also stored into the carry.
        byte Rrc(byte value)
        {
            int carry = (value & 0x01);

            value >>= 1;

            if (carry != 0)
            {
                SetFlag(Flags.Carry);
                value |= 0x80;
            }
            else ClearFlag(Flags.Carry);

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);

            return value;
        }


        // Shifts register to the left with bit 7 moved to the carry flag and bit 0 reset (zeroed).
        byte Sla(byte value)
        {
            if ((value & 0x80) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            value <<= 1;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);

            return value;
        }


        byte Sra(byte value)
        {
            if((value & 0x01) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            value = (byte) ((byte)(value & 0x80) | (byte)(value >> 1));

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);

            return value;
        }


        byte Srl(byte value)
        {
            /*
            if ((byte) (value & 0x01) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            value >>= 1;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);
            */


            byte result = value;

            ClearAllFlags();

            if ((result & 0x01) != 0) SetFlag(Flags.Carry);
            result >>= 1;

            if (result == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            return result;
        }


        // Swap upper & lower nibles 
        byte Swap(byte value)
        {
            /*
            value = (byte)( (byte)(((value & 0xf) << 4)) | (byte) (((value & 0xf0) >> 4)));


            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.Carry);
            ClearFlag(Flags.HalfCarry);
            */


            byte low_half = (byte) (value & 0x0F);
            byte high_half = (byte) ((value >> 4) & 0x0F);
            byte result = (byte)((low_half << 4) + high_half);

            ClearAllFlags();

            if (result == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            return value;
        }



        // ********************
        // Instruction Handlers 
        // ********************


        //0x00
        void RLC_b()
        {
            B = Rlc(B);
        }

        //0x01
        void RLC_c()
        {
            C = Rlc(C);
        }

        //0x02
        void RLC_d()
        {
            D = Rlc(D);
        }

        //0x03
        void RLC_e()
        {
            E = Rlc(E);
        }

        //0x04
        void RLC_h()
        {
            H = Rlc(H);
        }

        //0x05
        void RLC_l()
        {
            L = Rlc(L);
        }

        //0x06
        void RLC_hlp()
        {
            memory.WriteByte(HL, Rlc(memory.ReadByte(HL)));
        }

        //0x07
        void RLC_a()
        {
            A = Rlc(A);
        }

        //0x08
        void RRC_b()
        {
            B = Rrc(B);
        }

        //0x09
        void RRC_c()
        {
            C = Rrc(C);
        }

        //0x0A
        void RRC_d()
        {
            D = Rrc(D);
        }

        //0x0B
        void RRC_e()
        {
            E = Rrc(E);
        }

        //0x0C
        void RRC_h()
        {
            H = Rrc(H);
        }

        //0x0D
        void RRC_l()
        {
            L = Rrc(L);
        }

        //0x0E
        void RRC_hlp()
        {
            memory.WriteByte(HL, Rrc(memory.ReadByte(HL)));
        }

        //0x0F
        void RRC_a()
        {
            A = Rrc(A);
        }

        //0x10
        void RL_b()
        {
            B = Rl(B, false);
        }

        //0x11
        void RL_c()
        {
            C = Rl(C, false);
        }

        //0x12
        void RL_d()
        {
            D = Rl(D, false);
        }

        //0x13
        void RL_e()
        {
            E = Rl(E, false);
        }

        //0x14
        void RL_h()
        {
            H = Rl(H, false);
        }

        //0x15
        void RL_l()
        {
            L = Rl(L, false);
        }

        //0x16
        void RL_hlp()
        {
            memory.WriteByte(HL, Rl(memory.ReadByte(HL), false));
        }

        //0x17
        void RL_a()
        {
            A = Rl(A, true);
        }

        //0x18
        void RR_b()
        {
            B = Rr(B, false);
        }

        //0x19
        void RR_c()
        {
            C = Rr(C, false);
        }

        //0x1A
        void RR_d()
        {
            D = Rr(D, false);
        }

        //0x1B
        void RR_e()
        {
            E = Rr(E, false);
        }

        //0x1C
        void RR_h()
        {
            H = Rr(H, false);
        }

        //0x1D
        void RR_l()
        {
            L = Rr(L, false);
        }

        //0x1E
        void RR_hlp()
        {
            memory.WriteByte(HL, Rr(memory.ReadByte(HL), false));
        }

        //0x1F
        void RR_a()
        {
            A = Rr(A, true);
        }

        //0x20
        void SLA_b()
        {
            B = Sla(B);
        }

        //0x21
        void SLA_c()
        {
            C = Sla(C);
        }

        //0x22
        void SLA_d()
        {
            D = Sla(D);
        }

        //0x23
        void SLA_e()
        {
            E = Sla(E);
        }

        //0x24
        void SLA_h()
        {
            H = Sla(H);
        }

        //0x25
        void SLA_l()
        {
            L = Sla(L);
        }

        //0x26
        void SLA_hlp()
        {
            memory.WriteByte(HL, Sla(memory.ReadByte(HL)));
        }

        //0x27
        void SLA_a()
        {
            A = Sla(A);
        }

        //0x28
        void SRA_b()
        {
            B = Sra(B);
        }

        //0x29
        void SRA_c()
        {
            C = Sra(C);
        }

        //0x2A
        void SRA_d()
        {
            D = Sra(D);
        }

        //0x2B
        void SRA_e()
        {
            E = Sra(E);
        }

        //0x2C
        void SRA_h()
        {
            H = Sra(H);
        }

        //0x2D
        void SRA_l()
        {
            L = Sra(L);
        }

        //0x2E
        void SRA_hlp()
        {
            memory.WriteByte(HL, Sra(memory.ReadByte(HL)));
        }

        //0x2F
        void SRA_a()
        {
            A = Sra(A);
        }

        //0x30
        void SWAP_b()
        {
            B = Swap(B);
        }

        //0x31
        void SWAP_c()
        {
            C = Swap(C);
        }

        //0x32
        void SWAP_d()
        {
            D = Swap(D);
        }

        //0x33
        void SWAP_e()
        {
            E = Swap(E);
        }

        //0x34
        void SWAP_h()
        {
            H = Swap(H);
        }

        //0x35
        void SWAP_l()
        {
            L = Swap(L);
        }

        //0x36
        void SWAP_hlp()
        {
            memory.WriteByte(HL, Swap(memory.ReadByte(HL)));
        }

        //0x37
        void SWAP_a()
        {
            A = Swap(A);
        }

        //0x38
        void SRL_b()
        {
            B = Srl(B);
        }

        //0x39
        void SRL_c()
        {
            C = Srl(C);
        }

        //0x3A
        void SRL_d()
        {
            D = Srl(D);
        }

        //0x3B
        void SRL_e()
        {
            E = Srl(E);
        }

        //0x3C
        void SRL_h()
        {
            H = Srl(H);
        }

        //0x3D
        void SRL_l()
        {
            L = Srl(L);
        }

        //0x3E
        void SRL_hlp()
        {
            memory.WriteByte(HL, Srl(memory.ReadByte(HL)));
        }

        //0x3F
        void SRL_a()
        {
            A = Srl(A);
        }


        //0x40
        void BIT_0_b()
        {
            TestBit(1 << 0, B);
        }

        // 0x41
        void BIT_0_c()
        {
            TestBit(1 << 0, C);
        }

        void BIT_0_d()
        {
            TestBit(1 << 0, D);
        }

        void BIT_0_e()
        {
            TestBit(1 << 0, E);
        }

        void BIT_0_h()
        {
            TestBit(1 << 0, H);
        }

        void BIT_0_l()
        {
            TestBit(1 << 0, L);
        }

        void BIT_0_hlp()
        {
            TestBit(1 << 0, memory.ReadByte(HL));
        }

        void BIT_0_a()
        {
            TestBit(1 << 0, A);
        }

        // 0x48
        void BIT_1_b()
        {
            TestBit(1 << 1, B);
        }

        void BIT_1_c()
        {
            TestBit(1 << 1, C);
        }

        void BIT_1_d()
        {
            TestBit(1 << 1, D);
        }

        void BIT_1_e()
        {
            TestBit(1 << 1, E);
        }

        void BIT_1_h()
        {
            TestBit(1 << 1, H);
        }

        void BIT_1_l()
        {
            TestBit(1 << 1, L);
        }

        void BIT_1_hlp()
        {
            TestBit(1 << 1, memory.ReadByte(HL));
        }

        void BIT_1_a()
        {
            TestBit(1 << 1, A);
        }

        void BIT_2_b()
        {
            TestBit(1 << 2, B);
        }

        void BIT_2_c()
        {
            TestBit(1 << 2, C);
        }

        void BIT_2_d()
        {
            TestBit(1 << 2, D);
        }

        void BIT_2_e()
        {
            TestBit(1 << 2, E);
        }

        void BIT_2_h()
        {
            TestBit(1 << 2, H);
        }

        void BIT_2_l()
        {
            TestBit(1 << 2, L);
        }

        void BIT_2_hlp()
        {
            TestBit(1 << 2, memory.ReadByte(HL));
        }

        void BIT_2_a()
        {
            TestBit(1 << 2, A);
        }

        void BIT_3_b()
        {
            TestBit(1 << 3, B);
        }

        void BIT_3_c()
        {
            TestBit(1 << 3, C);
        }

        void BIT_3_d()
        {
            TestBit(1 << 3, D);
        }

        void BIT_3_e()
        {
            TestBit(1 << 3, E);
        }

        void BIT_3_h()
        {
            TestBit(1 << 3, H);
        }

        void BIT_3_l()
        {
            TestBit(1 << 3, L);
        }

        void BIT_3_hlp()
        {
            TestBit(1 << 3, memory.ReadByte(HL));
        }

        void BIT_3_a()
        {
            TestBit(1 << 3, A);
        }

        void BIT_4_b()
        {
            TestBit(1 << 4, B);
        }

        void BIT_4_c()
        {
            TestBit(1 << 4, C);
        }

        void BIT_4_d()
        {
            TestBit(1 << 4, D);
        }

        void BIT_4_e()
        {
            TestBit(1 << 4, E);
        }

        void BIT_4_h()
        {
            TestBit(1 << 4, H);
        }

        void BIT_4_l()
        {
            TestBit(1 << 4, L);
        }

        void BIT_4_hlp()
        {
            TestBit(1 << 4, memory.ReadByte(HL));
        }

        void BIT_4_a()
        {
            TestBit(1 << 4, A);
        }

        void BIT_5_b()
        {
            TestBit(1 << 5, B);
        }

        void BIT_5_c()
        {
            TestBit(1 << 5, C);
        }

        void BIT_5_d()
        {
            TestBit(1 << 5, D);
        }

        void BIT_5_e()
        {
            TestBit(1 << 5, E);
        }

        void BIT_5_h()
        {
            TestBit(1 << 5, H);
        }

        void BIT_5_l()
        {
            TestBit(1 << 5, L);
        }

        void BIT_5_hlp()
        {
            TestBit(1 << 5, memory.ReadByte(HL));
        }

        void BIT_5_a()
        {
            TestBit(1 << 5, A);
        }

        void BIT_6_b()
        {
            TestBit(1 << 6, B);
        }

        void BIT_6_c()
        {
            TestBit(1 << 6, C);
        }

        void BIT_6_d()
        {
            TestBit(1 << 6, D);
        }

        void BIT_6_e()
        {
            TestBit(1 << 6, E);
        }

        void BIT_6_h()
        {
            TestBit(1 << 6, H);
        }

        void BIT_6_l()
        {
            TestBit(1 << 6, L);
        }

        void BIT_6_hlp()
        {
            TestBit(1 << 6, memory.ReadByte(HL));
        }

        void BIT_6_a()
        {
            TestBit(1 << 6, A);
        }

        void BIT_7_b()
        {
            TestBit(1 << 7, B);
        }

        void BIT_7_c()
        {
            TestBit(1 << 7, C);
        }

        void BIT_7_d()
        {
            TestBit(1 << 7, D);
        }

        void BIT_7_e()
        {
            TestBit(1 << 7, E);
        }

        void BIT_7_h()
        {
            TestBit(1 << 7, H);
        }

        void BIT_7_l()
        {
            TestBit(1 << 7, L);
        }

        void BIT_7_hlp()
        {
            TestBit(1 << 7, memory.ReadByte(HL));
        }

        void BIT_7_a()
        {
            TestBit(1 << 7, A);
        }
    }
}
