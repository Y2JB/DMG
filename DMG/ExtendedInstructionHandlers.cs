using System;
using System.Runtime.CompilerServices;

namespace DMG
{
    public partial class Cpu
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void TestBit(byte bit, byte value)
        {          
            if ((value & bit) == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);           

            ClearFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);       
        }


        // Rotates register to the left with the carry's value put into bit 0 and bit 7 is put into the carry.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Rl(byte value, bool isRegisterA)
        {
            byte carry = CarryFlag ? (byte) 1 : (byte) 0;
            byte result = value;
            
            if ((result & 0x80) != 0)
            {
                ClearAllFlags();
                SetFlag(Flags.Carry);
            }
            else
            {
                ClearAllFlags();
            }

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Rr(byte value, bool isRegisterA)
        {
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Rlc(byte value, bool isRegisterA)
        {
            byte result = value;
            if ((result & 0x80) != 0)
            {
                ClearAllFlags();
                SetFlag(Flags.Carry);
                result <<= 1;
                result |= 0x1;
            }
            else
            {
                ClearAllFlags();
                result <<= 1;
            }

            if (!isRegisterA)
            {
                if (result == 0) SetFlag(Flags.Zero);
                else ClearFlag(Flags.Zero);
            }

            return result;
        }


        // Rotates to the right with bit 0 moved to bit 7 and also stored into the carry.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Rrc(byte value, bool isRegisterA)
        {
            byte  result = value;
            if ((result & 0x01) != 0)
            {
                ClearAllFlags();
                SetFlag(Flags.Carry);
                result >>= 1;
                result |= 0x80;
            }
            else
            {
                ClearAllFlags();
                result >>= 1;
            }

            if (!isRegisterA)
            {
                if (result == 0) SetFlag(Flags.Zero);
                else ClearFlag(Flags.Zero);
            }
            return result;
        }


        // Shifts register to the left with bit 7 moved to the carry flag and bit 0 reset (zeroed).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Sla(byte value)
        {
            ClearAllFlags();
            if((value & 0x80) != 0) SetFlag(Flags.Carry);

            byte result = (byte) (value << 1);

            if (result == 0) SetFlag(Flags.Zero);

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Sra(byte value)
        {
            byte result = value;
            ClearAllFlags();
            if ((result & 0x01) != 0) SetFlag(Flags.Carry);

            if ((result & 0x80) != 0)
            {
                result >>= 1;
                result |= 0x80;
            }
            else
            {
                result >>= 1;
            }

            if (result == 0) SetFlag(Flags.Zero);

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Srl(byte value)
        {
            byte result = value;

            ClearAllFlags();

            if ((result & 0x01) != 0) SetFlag(Flags.Carry);
            result >>= 1;

            if (result == 0) SetFlag(Flags.Zero);

            return result;
        }


        // Swap upper & lower nibles 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Swap(byte value)
        {
            byte low_half = (byte) (value & 0x0F);
            byte high_half = (byte) ((value >> 4) & 0x0F);
            byte result = (byte)((low_half << 4) + high_half);

            ClearAllFlags();

            if (result == 0) SetFlag(Flags.Zero);

            return result;
        }


        // TODO: Set and Reset could easily not be functions!


        // Reset bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Res(byte bit, byte value)
        {
            return (byte) (value & (~bit));
        }


        // Set bit
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Set(byte bit, byte value)
        {
            return (byte)(value | bit);
        }

        // ********************
        // Instruction Handlers 
        // ********************


        //0x00
        void RLC_b()
        {
            B = Rlc(B, false);
        }

        //0x01
        void RLC_c()
        {
            C = Rlc(C, false);
        }

        //0x02
        void RLC_d()
        {
            D = Rlc(D, false);
        }

        //0x03
        void RLC_e()
        {
            E = Rlc(E, false);
        }

        //0x04
        void RLC_h()
        {
            H = Rlc(H, false);
        }

        //0x05
        void RLC_l()
        {
            L = Rlc(L, false);
        }

        //0x06
        void RLC_hlp()
        {
            memory.WriteByteAndCycle(HL, Rlc(memory.ReadByteAndCycle(HL), false));
        }

        //0x07
        void RLC_a()
        {
            // Pass false here and the RLCA instruction (0x07) passes true...
            A = Rlc(A, false);
        }

        //0x08
        void RRC_b()
        {
            B = Rrc(B, false);
        }

        //0x09
        void RRC_c()
        {
            C = Rrc(C, false);
        }

        //0x0A
        void RRC_d()
        {
            D = Rrc(D, false);
        }

        //0x0B
        void RRC_e()
        {
            E = Rrc(E, false);
        }

        //0x0C
        void RRC_h()
        {
            H = Rrc(H, false);
        }

        //0x0D
        void RRC_l()
        {
            L = Rrc(L, false);
        }

        //0x0E
        void RRC_hlp()
        {
            memory.WriteByteAndCycle(HL, Rrc(memory.ReadByteAndCycle(HL), false));
        }

        //0x0F
        void RRC_a()
        {
            A = Rrc(A, false);
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
            memory.WriteByteAndCycle(HL, Rl(memory.ReadByteAndCycle(HL), false));
        }

        //0x17
        void RL_a()
        {
            A = Rl(A, false);
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
            memory.WriteByteAndCycle(HL, Rr(memory.ReadByteAndCycle(HL), false));
        }

        //0x1F
        void RR_a()
        {
            A = Rr(A, false);
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
            memory.WriteByteAndCycle(HL, Sla(memory.ReadByteAndCycle(HL)));
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
            memory.WriteByteAndCycle(HL, Sra(memory.ReadByteAndCycle(HL)));
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
            memory.WriteByteAndCycle(HL, Swap(memory.ReadByteAndCycle(HL)));
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
            memory.WriteByteAndCycle(HL, Srl(memory.ReadByteAndCycle(HL)));
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
            TestBit(1 << 0, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 1, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 2, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 3, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 4, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 5, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 6, memory.ReadByteAndCycle(HL));
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
            TestBit(1 << 7, memory.ReadByteAndCycle(HL));
        }

        void BIT_7_a()
        {
            TestBit(1 << 7, A);
        }

        //0x80
        void RES_0_b()
        {
           B = Res(1 << 0, B);
        }

        // 0x81
        void RES_0_c()
        {
            C = Res(1 << 0, C);
        }

        void RES_0_d()
        {
            D = Res(1 << 0, D);
        }

        void RES_0_e()
        {
            E = Res(1 << 0, E);
        }

        void RES_0_h()
        {
            H = Res(1 << 0, H);
        }

        void RES_0_l()
        {
            L = Res(1 << 0, L);
        }

        void RES_0_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 0, memory.ReadByteAndCycle(HL)));
        }

        void RES_0_a()
        {
            A = Res(1 << 0, A);
        }

        //0x88
        void RES_1_b()
        {
            B = Res(1 << 1, B);
        }

        // 0x89
        void RES_1_c()
        {
            C = Res(1 << 1, C);
        }

        void RES_1_d()
        {
            D = Res(1 << 1, D);
        }

        void RES_1_e()
        {
            E = Res(1 << 1, E);
        }

        void RES_1_h()
        {
            H = Res(1 << 1, H);
        }

        void RES_1_l()
        {
           L =  Res(1 << 1, L);
        }

        void RES_1_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 1, memory.ReadByteAndCycle(HL)));
        }

        //0x8F
        void RES_1_a()
        {
            A = Res(1 << 1, A);
        }


        //0x90
        void RES_2_b()
        {
            B = Res(1 << 2, B);
        }

        // 0x91
        void RES_2_c()
        {
            C = Res(1 << 2, C);
        }

        void RES_2_d()
        {
            D = Res(1 << 2, D);
        }

        void RES_2_e()
        {
            E = Res(1 << 2, E);
        }

        void RES_2_h()
        {
            H = Res(1 << 2, H);
        }

        void RES_2_l()
        {
            L = Res(1 << 2, L);
        }

        void RES_2_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 2, memory.ReadByteAndCycle(HL)));
        }

        void RES_2_a()
        {
            A = Res(1 << 2, A);
        }

        //0x98
        void RES_3_b()
        {
            B = Res(1 << 3, B);
        }

        // 0x99
        void RES_3_c()
        {
            C = Res(1 << 3, C);
        }

        void RES_3_d()
        {
            D = Res(1 << 3, D);
        }

        void RES_3_e()
        {
            E = Res(1 << 3, E);
        }

        void RES_3_h()
        {
            H = Res(1 << 3, H);
        }

        void RES_3_l()
        {
            L = Res(1 << 3, L);
        }

        void RES_3_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 3, memory.ReadByteAndCycle(HL)));
        }

        //0x9F
        void RES_3_a()
        {
            A = Res(1 << 3, A);
        }

        //0xA0
        void RES_4_b()
        {
            B = Res(1 << 4, B);
        }

        // 0xA1
        void RES_4_c()
        {
            C = Res(1 << 4, C);
        }

        void RES_4_d()
        {
            D = Res(1 << 4, D);
        }

        void RES_4_e()
        {
            E = Res(1 << 4, E);
        }

        void RES_4_h()
        {
            H = Res(1 << 4, H);
        }

        void RES_4_l()
        {
            L = Res(1 << 4, L);
        }

        void RES_4_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 4, memory.ReadByteAndCycle(HL)));
        }

        void RES_4_a()
        {
            A = Res(1 << 4, A);
        }

        //0xA8
        void RES_5_b()
        {
            B = Res(1 << 5, B);
        }

        // 0xA9
        void RES_5_c()
        {
            C = Res(1 << 5, C);
        }

        void RES_5_d()
        {
            D = Res(1 << 5, D);
        }

        void RES_5_e()
        {
            E = Res(1 << 5, E);
        }

        void RES_5_h()
        {
            H = Res(1 << 5, H);
        }

        void RES_5_l()
        {
            L = Res(1 << 5, L);
        }

        void RES_5_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 5, memory.ReadByteAndCycle(HL)));
        }

        //0x8F
        void RES_5_a()
        {
            A = Res(1 << 5, A);
        }

        //0xB0
        void RES_6_b()
        {
            B = Res(1 << 6, B);
        }

        // 0xB1
        void RES_6_c()
        {
            C = Res(1 << 6, C);
        }

        void RES_6_d()
        {
            D = Res(1 << 6, D);
        }

        void RES_6_e()
        {
            E = Res(1 << 6, E);
        }

        void RES_6_h()
        {
            H = Res(1 << 6, H);
        }

        void RES_6_l()
        {
            L = Res(1 << 6, L);
        }

        void RES_6_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 6, memory.ReadByteAndCycle(HL)));
        }

        void RES_6_a()
        {
            A = Res(1 << 6, A);
        }

        //0xB8
        void RES_7_b()
        {
            B = Res(1 << 7, B);
        }

        // 0xB9
        void RES_7_c()
        {
            C = Res(1 << 7, C);
        }

        void RES_7_d()
        {
            D = Res(1 << 7, D);
        }

        void RES_7_e()
        {
            E = Res(1 << 7, E);
        }

        void RES_7_h()
        {
            H = Res(1 << 7, H);
        }

        void RES_7_l()
        {
            L = Res(1 << 7, L);
        }

        void RES_7_hlp()
        {
            memory.WriteByteAndCycle(HL, Res(1 << 7, memory.ReadByteAndCycle(HL)));
        }

        //0xBF
        void RES_7_a()
        {
            A = Res(1 << 7, A);
        }

        //0xC0
        void SET_0_b()
        {
            B = Set(1 << 0, B);
        }

        // 0xC1
        void SET_0_c()
        {
            C = Set(1 << 0, C);
        }

        void SET_0_d()
        {
            D = Set(1 << 0, D);
        }

        void SET_0_e()
        {
            E = Set(1 << 0, E);
        }

        void SET_0_h()
        {
            H = Set(1 << 0, H);
        }

        void SET_0_l()
        {
            L = Set(1 << 0, L);
        }

        void SET_0_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 0, memory.ReadByteAndCycle(HL)));
        }

        void SET_0_a()
        {
            A = Set(1 << 0, A);
        }

        //0xC8
        void SET_1_b()
        {
            B = Set(1 << 1, B);
        }

        // 0xC9
        void SET_1_c()
        {
            C = Set(1 << 1, C);
        }

        void SET_1_d()
        {
            D = Set(1 << 1, D);
        }

        void SET_1_e()
        {
            E = Set(1 << 1, E);
        }

        void SET_1_h()
        {
            H = Set(1 << 1, H);
        }

        void SET_1_l()
        {
            L = Set(1 << 1, L);
        }

        void SET_1_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 1, memory.ReadByteAndCycle(HL)));
        }

        //0xCF
        void SET_1_a()
        {
            A = Set(1 << 1, A);
        }

        //0xD0
        void SET_2_b()
        {
            B = Set(1 << 2, B);
        }

        // 0xD1
        void SET_2_c()
        {
            C = Set(1 << 2, C);
        }

        void SET_2_d()
        {
            D = Set(1 << 2, D);
        }

        void SET_2_e()
        {
            E = Set(1 << 2, E);
        }

        void SET_2_h()
        {
            H = Set(1 << 2, H);
        }

        void SET_2_l()
        {
            L = Set(1 << 2, L);
        }

        void SET_2_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 2, memory.ReadByteAndCycle(HL)));
        }

        void SET_2_a()
        {
            A = Set(1 << 2, A);
        }

        //0xD8
        void SET_3_b()
        {
            B = Set(1 << 3, B);
        }

        // 0xD9
        void SET_3_c()
        {
            C = Set(1 << 3, C);
        }

        void SET_3_d()
        {
            D = Set(1 << 3, D);
        }

        void SET_3_e()
        {
            E = Set(1 << 3, E);
        }

        void SET_3_h()
        {
            H = Set(1 << 3, H);
        }

        void SET_3_l()
        {
            L = Set(1 << 3, L);
        }

        void SET_3_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 3, memory.ReadByteAndCycle(HL)));
        }

        //0xDF
        void SET_3_a()
        {
            A = Set(1 << 3, A);
        }

        //0xE0
        void SET_4_b()
        {
            B = Set(1 << 4, B);
        }

        // 0xE1
        void SET_4_c()
        {
            C = Set(1 << 4, C);
        }

        void SET_4_d()
        {
            D = Set(1 << 4, D);
        }

        void SET_4_e()
        {
            E = Set(1 << 4, E);
        }

        void SET_4_h()
        {
            H = Set(1 << 4, H);
        }

        void SET_4_l()
        {
            L = Set(1 << 4, L);
        }

        void SET_4_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 4, memory.ReadByteAndCycle(HL)));
        }

        void SET_4_a()
        {
            A = Set(1 << 4, A);
        }

        //0xE8
        void SET_5_b()
        {
            B = Set(1 << 5, B);
        }

        // 0xE9
        void SET_5_c()
        {
            C = Set(1 << 5, C);
        }

        void SET_5_d()
        {
            D = Set(1 << 5, D);
        }

        void SET_5_e()
        {
            E = Set(1 << 5, E);
        }

        void SET_5_h()
        {
            H = Set(1 << 5, H);
        }

        void SET_5_l()
        {
            L = Set(1 << 5, L);
        }

        void SET_5_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 5, memory.ReadByteAndCycle(HL)));
        }

        //0xEF
        void SET_5_a()
        {
            A = Set(1 << 5, A);
        }

        //0xF0
        void SET_6_b()
        {
            B = Set(1 << 6, B);
        }

        // 0xF1
        void SET_6_c()
        {
            C = Set(1 << 6, C);
        }

        void SET_6_d()
        {
            D = Set(1 << 6, D);
        }

        void SET_6_e()
        {
            E = Set(1 << 6, E);
        }

        void SET_6_h()
        {
            H = Set(1 << 6, H);
        }

        void SET_6_l()
        {
            L = Set(1 << 6, L);
        }

        void SET_6_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 6, memory.ReadByteAndCycle(HL)));
        }

        void SET_6_a()
        {
            A = Set(1 << 6, A);
        }

        //0xF8
        void SET_7_b()
        {
            B = Set(1 << 7, B);
        }

        // 0xF9
        void SET_7_c()
        {
            C = Set(1 << 7, C);
        }

        void SET_7_d()
        {
            D = Set(1 << 7, D);
        }

        void SET_7_e()
        {
            E = Set(1 << 7, E);
        }

        void SET_7_h()
        {
            H = Set(1 << 7, H);
        }

        void SET_7_l()
        {
            L = Set(1 << 7, L);
        }

        void SET_7_hlp()
        {
            memory.WriteByteAndCycle(HL, Set(1 << 7, memory.ReadByteAndCycle(HL)));
        }

        //0xFF
        void SET_7_a()
        {
            A = Set(1 << 7, A);
        }




    }
}
