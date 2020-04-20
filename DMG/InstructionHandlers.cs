﻿using System;

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


        void Or(byte value)
        {
            A |= value;

            if(A != 0) ClearFlag(Flags.Zero);
            else SetFlag(Flags.Zero);

            ClearFlag(Flags.Carry);
            ClearFlag(Flags.HalfCarry);
            ClearFlag(Flags.Negative);
        }


        void And(byte value)
        {
            A &= value;

            if (A == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Carry);
            ClearFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);
        }


        byte Inc(byte value)
        {
            if ((value & 0x0f) == 0x0f) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);

            value++;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            ClearFlag(Flags.Negative);

            return value;
        }


        byte Dec(byte value)
        {
            if ((byte)(value & 0x0f) != 0) ClearFlag(Flags.HalfCarry);
            else SetFlag(Flags.HalfCarry);

            value--;

            if (value == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            SetFlag(Flags.Negative);

            return value;
        }



        byte Add(byte lhs, byte rhs)
        {
            UInt16 result = (UInt16)(lhs + rhs);

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


        void Sub(byte value)
        {
            SetFlag(Flags.Negative);

            if (value > A) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            if ((value & 0x0F) > (A & 0x0f)) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);

            A -= value;

            if (A == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);
        }


        void Cmp(byte value)
        {
            if (A == value) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            if (value > A) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            if ((byte)(value & 0x0F) > (byte)(A & 0x0F)) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);

            SetFlag(Flags.Negative);
        }


        // ********************
        // Instruction Handlers 
        // ********************

        // 0x00
        void NOP()
        {
        }


        // 0x03
        void INC_bc()
        {
            BC++;
        }

        // 0x06
        void LD_b_n(byte n)
        {
            B = n;
        }

        // 0x08
        void LD_nn_sp(ushort nn)
        {
            memory.WriteShort(nn, SP);
        }

        // 0x38
        void JR_C_n(sbyte n)
        {
            if (CarryFlag)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;
            }
        }

        // 0x3B
        void DEC_sp()
        {
            SP--;
        }

        // 0x3C
        void INC_a()
        {
            A = Inc(A);
        }

        void INC_b()
        {
            B = Inc(B);
        }

        // 0x05
        void DEC_b()
        {
            B = Dec(B);
        }

        // 0x0B
        void DEC_bc()
        {
            BC--;
        }

        // 0x0C
        void INC_c()
        {
            C = Inc(C);
        }

        // 0x0D
        void DEC_c()
        {
            C = Dec(C);
        }

        void INC_d()
        {
            D = Inc(D);
        }

        // 0x15
        void DEC_d()
        {
            D = Dec(D);
        }

        // 0x1B
        void DEC_de()
        {
            DE--;
        }

        // 0x1C
        void INC_e()
        {
            E = Inc(E);
        }

        // 0x22
        void LDI_hlp_a()
        {
            memory.WriteByte(HL, A);
            HL++;
        }

        // 0x23       
        void INC_hl()
        {
            HL++;
        }

        // 0x24
        void INC_h()
        {
            H = Inc(H);
        }

        // 0x25
        void DEC_h()
        {
            H = Dec(H);
        }

        // 0x28
        void JR_Z_n(sbyte n)
        {
            if (ZeroFlag)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;
            }
        }

        // 0x2A
        void LDI_a_hlp()
        {
            A = memory.ReadByte(HL);
            HL++;
        }

        // 0x2B
        void DEC_hl()
        {
            HL--;
        }

        // 0x2C
        void INC_l()
        {
            L = Inc(L);
        }

        void LD_c_n(byte n)
        {
            C = n;
        }

        void LD_de_nn(ushort nn)
        {
            DE = nn;
        }

        // 0x13       
        void INC_de()
        {
            DE++;
        }

        void LD_d_n(byte n)
        {
            D = n;
        }

        // 0x17
        void RLA()
        {
            int carry = CarryFlag ? 1 : 0;

            if ((A & 0x80) != 0) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            A <<= 1;
            A += (byte) carry;

            ClearFlag(Flags.HalfCarry);
            ClearFlag(Flags.Negative);
            ClearFlag(Flags.Zero);
        }

        // 0x18
        void JR_n(sbyte n)
        {
            int pc = (int)(PC) + n;
            PC = (ushort)pc;
        }

        // 0x1A
        void LD_a_dep()
        {
            A = memory.ReadByte(DE);
        }

        // 0x1D
        void DEC_e()
        {
            E = Dec(E);
        }

        // 0x1E
        void LD_e_n(byte n)
        {
            E = n;
        }

        // 0x20
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

        // 0x2D
        void DEC_l()
        {
            L = Dec(L);
        }

        // 0x2E
        void LD_l_n(byte n)
        {
            L = n;
        }


        // 0x30
        void JR_NC_n(sbyte n)
        {
            if (CarryFlag)
            {
                //ticks += 8;
            }
            else
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;

                //ticks += 12;
            }
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

        // 0x33       
        void INC_sp()
        {
            SP++;
        }

        // 0x34
        void INC_hlp()
        {
            memory.WriteByte(HL, Inc(memory.ReadByte(HL)));
        }

        // 0x35
        void DEC_hlp()
        {
            memory.WriteByte(HL, Dec(memory.ReadByte(HL)));
        }

        // 0x3D
        void DEC_a()
        {
            A = Dec(A);
        }

        // 0x3E
        void LD_a_n(byte n)
        {
            A = n;
        }



        //0x40        
        void LD_b_b()
        {
            B = B;
        }

        //0x41     
        void LD_b_c()
        {
            B = C;
        }

        //0x42       
        void LD_b_d()
        {
            B = D;
        }

        //0x43        
        void LD_b_e()
        {
            B = E;
        }

        //0x44        
        void LD_b_h()
        {
            B = H;
        }

        //0x45        
        void LD_b_l()
        {
            B = L;
        }

        //0x46        
        void LD_b_hlp()
        {
            B = memory.ReadByte(HL);
        }

        //0x47        
        void LD_b_a()
        {
            B = A;
        }

        //0x48        
        void LD_c_b()
        {
            C = B;
        }

        //0x49     
        void LD_c_c()
        {
            C = C;
        }

        //0x4A       
        void LD_c_d()
        {
            C = D;
        }

        //0x4B        
        void LD_c_e()
        {
            C = E;
        }

        //0x4C        
        void LD_c_h()
        {
            C = H;
        }

        //0x4D        
        void LD_c_l()
        {
            C = L;
        }

        //0x4E        
        void LD_c_hlp()
        {
            C = memory.ReadByte(HL);
        }

        // 0x4F
        void LD_c_a()
        {
            C = A;
        }

        //0x50        
        void LD_d_b()
        {
            D = B;
        }

        //0x51     
        void LD_d_c()
        {
            D = C;
        }

        //0x52       
        void LD_d_d()
        {
            D = D;
        }

        //0x53        
        void LD_d_e()
        {
            D = E;
        }

        //0x54        
        void LD_d_h()
        {
            D = H;
        }

        //0x55        
        void LD_d_l()
        {
            D = L;
        }

        //0x56        
        void LD_d_hlp()
        {
            D = memory.ReadByte(HL);
        }

        //0x57        
        void LD_d_a()
        {
            D = A;
        }

        //0x58        
        void LD_e_b()
        {
            E = B;
        }

        //0x59     
        void LD_e_c()
        {
            E = C;
        }

        //0x5A       
        void LD_e_d()
        {
            E = D;
        }

        //0x5B        
        void LD_e_e()
        {
            E = E;
        }

        //0x5C        
        void LD_e_h()
        {
            E = H;
        }

        //0x5D        
        void LD_e_l()
        {
            E = L;
        }

        //0x5E        
        void LD_e_hlp()
        {
            E = memory.ReadByte(HL);
        }

        // 0x5F
        void LD_e_a()
        {
            E = A;
        }

        //0x60        
        void LD_h_b()
        {
            H = B;
        }

        //0x61     
        void LD_h_c()
        {
            H = C;
        }

        //0x62       
        void LD_h_d()
        {
            H = D;
        }

        //0x63        
        void LD_h_e()
        {
            H = E;
        }

        //0x64        
        void LD_h_h()
        {
            H = H;
        }

        //0x65        
        void LD_h_l()
        {
            H = L;
        }

        //0x66        
        void LD_h_hlp()
        {
            H = memory.ReadByte(HL);
        }

        //0x67        
        void LD_h_a()
        {
            H = A;
        }

        //0x68        
        void LD_l_b()
        {
            L = B;
        }

        //0x69     
        void LD_l_c()
        {
            L = C;
        }

        //0x6A       
        void LD_l_d()
        {
            L = D;
        }

        //0x6B        
        void LD_l_e()
        {
            L = E;
        }

        //0x6C        
        void LD_l_h()
        {
            L = H;
        }

        //0x6D        
        void LD_l_l()
        {
            L = L;
        }

        //0x6E        
        void LD_l_hlp()
        {
            L = memory.ReadByte(HL);
        }

        // 0x6F
        void LD_l_a()
        {
            L = A;
        }

        // 0x70
        void LD_hlp_b()
        {
            memory.WriteByte(HL, B);
        }

        // 0x71
        void LD_hlp_c()
        {
            memory.WriteByte(HL, C);
        }

        // 0x72
        void LD_hlp_d()
        {
            memory.WriteByte(HL, D);
        }
        // 0x73
        void LD_hlp_e()
        {
            memory.WriteByte(HL, E);
        }

        // 0x74
        void LD_hlp_h()
        {
            memory.WriteByte(HL, H);
        }

        // 0x75
        void LD_hlp_l()
        {
            memory.WriteByte(HL, L);
        }

        // 0x76
        void HALT()
        {
            throw new NotImplementedException();
        }

        // 0x77
        void LD_hlp_a()
        {
            memory.WriteByte(HL, A);
        }


        //0x78        
        void LD_a_b()
        {
            A = B;
        }

        //0x79     
        void LD_a_c()
        {
            A = C;
        }

        //0x7A       
        void LD_a_d()
        {
            A = D;
        }

        //0x7B        
        void LD_a_e()
        {
            A = E;
        }

        //0x7C        
        void LD_a_h()
        {
            A = H;
        }

        //0x7D        
        void LD_a_l()
        {
            A = L;
        }

        //0x7E        
        void LD_a_hlp()
        {
            A = memory.ReadByte(HL);
        }

        // 0x7F
        void LD_a_a()
        {
            A = A;
        }

        //0x80
        void ADD_a_b()
        {
            A = Add(A, B);
        }

        //0x81
        void ADD_a_c()
        {
            A = Add(A, C);
        }

        //0x82
        void ADD_a_d()
        {
            A = Add(A, D);
        }

        //0x83
        void ADD_a_e()
        {
            A = Add(A, E);
        }

        //0x84
        void ADD_a_h()
        {
            A = Add(A, H);
        }

        //0x85
        void ADD_a_l()
        {
            A = Add(A, L);
        }

        //0x86
        void ADD_a_hlp()
        {
            A = Add(A, memory.ReadByte(HL));
        }

        //0x87
        void ADD_a_a()
        {
            A = Add(A, A);
        }

        //0x90
        void SUB_a_b()
        {
            Sub(B);
        }

        //0x91
        void SUB_a_c()
        {
            Sub(C);
        }

        //0x92
        void SUB_a_d()
        {
            Sub(D);
        }

        //0x93
        void SUB_a_e()
        {
            Sub(E);
        }

        //0x94
        void SUB_a_h()
        {
            Sub(H);
        }

        //0x95
        void SUB_a_l()
        {
            Sub(L);
        }

        //0x96
        void SUB_a_hlp()
        {
            Sub(memory.ReadByte(HL));
        }

        //0x97
        void SUB_a_a()
        {
            Sub(A);
        }

        //0xA0
        void AND_b()
        {
            And(B);
        }

        //0xA1
        void AND_c()
        {
            And(C);
        }

        //0xA2
        void AND_d()
        {
            And(D);
        }

        //0xA3
        void AND_e()
        {
            And(E);
        }

        //0xA4
        void AND_h()
        {
            And(H);
        }

        //0xA5
        void AND_l()
        {
            And(L);
        }

        //0xA6
        void AND_hlp()
        {
            And(memory.ReadByte(HL));
        }

        //0xA7
        void AND_a()
        {
            And(A);
        }


        //0xA8
        void XOR_b()
        {
            Xor(B);
        }

        //0xA9
        void XOR_c()
        {
            Xor(C);
        }

        //0xAA
        void XOR_d()
        {
            Xor(D);
        }

        //0xAB
        void XOR_e()
        {
            Xor(E);
        }

        //0xAC
        void XOR_h()
        {
            Xor(H);
        }

        //0xAD
        void XOR_l()
        {
            Xor(L);
        }

        //0xAE
        void XOR_hlp()
        {
            Xor(memory.ReadByte(HL));
        }

        //0xAF
        void XOR_a()
        {
            Xor(A);
        }


        //0xB0
        void OR_b()
        {
            Or(B);
        }

        //0xB1
        void OR_c()
        {
            Or(C);
        }

        //0xB2
        void OR_d()
        {
            Or(D);
        }

        //0xB3
        void OR_e()
        {
            Or(E);
        }

        //0xB4
        void OR_h()
        {
            Or(H);
        }

        //0xB5
        void OR_l()
        {
            Or(L);
        }

        //0xB6
        void OR_hlp()
        {
            Or(memory.ReadByte(HL));
        }

        //0xB7
        void OR_a()
        {
            Or(A);
        }

        // 0xB8
        void CP_b()
        {
            Cmp(B);
        }

        // 0xB9
        void CP_c()
        {
            Cmp(C);
        }

        // 0xBA
        void CP_d()
        {
            Cmp(D);
        }

        // 0xBB
        void CP_e()
        {
            Cmp(E);
        }

        // 0xBC
        void CP_h()
        {
            Cmp(H);
        }

        // 0xBD
        void CP_l()
        {
            Cmp(L);
        }

        // 0xBE
        void CP_hlp()
        {
            Cmp(memory.ReadByte(HL));
        }

        // 0xBF
        void CP_a()
        {
            Cmp(A);
        }

        //0xC1
        void POP_bc()
        {
            BC = StackPop();
        }

        void JP_nn(ushort nn)
        {
            PC = nn;
        }


        // 0xC5
        void PUSH_bc()
        {
            StackPush(BC);
        }

        // 0xCD
        void CALL_nn(ushort nn)
        {
            StackPush(PC);
            PC = nn;
        }

        // 0xC9
        void RET()
        {
            PC = StackPop();
        }

        //0xD1
        void POP_de()
        {
            DE = StackPop();
        }

        // 0xD5
        void PUSH_de()
        {
            StackPush(DE);
        }

        void LD_hl_nn(ushort nn)
        {
            HL = nn;
        }

        void LD_ff_n_a(byte n)
        {
            memory.WriteByte((ushort)((ushort)0xFF00 + (ushort)n), A);
        }

        // 0xE1
        void POP_hl()
        {
            HL = StackPop();
        }

        // 0xE2
        void LD_ff_c_a()
        {
            memory.WriteByte((ushort)((ushort) 0xFF00 + (ushort) C), A);
        }


        // 0xE5
        void PUSH_hl()
        {
            StackPush(HL);
        }

        // 0xEA
        void LD_nn_a(ushort nn)
        {
            memory.WriteByte(nn, A);
        }

        // 0xF1
        void POP_af()
        {
            AF = StackPop();
        }

        // F3
        void DI()
        {
            // TODO DISABLE INTERUPTS!!!!
            throw new NotImplementedException();
        }


        // 0xF5
        void PUSH_af()
        {
            StackPush(AF);
        }

        // 0xFE
        void CP_n(byte n)
        {
            SetFlag(Flags.Negative);

            if (A == n) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            if (n > A) SetFlag(Flags.Carry);
            else ClearFlag(Flags.Carry);

            if ((n & 0x0f) > (A & 0x0f)) SetFlag(Flags.HalfCarry);
            else ClearFlag(Flags.HalfCarry);
        }
    }
}
