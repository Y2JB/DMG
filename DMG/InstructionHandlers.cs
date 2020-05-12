using System;
using System.Runtime.CompilerServices;

namespace DMG
{
    public partial class Cpu
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Xor(byte value)
        {
            A ^= value;
            ClearAllFlags();
            if (A == 0) SetFlag(Flags.Zero);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Or(byte value)
        {
            A |= value;
            ClearAllFlags();
            if (A == 0) SetFlag(Flags.Zero);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void And(byte value)
        {
            A &= value;
            ClearAllFlags();
            SetFlag(Flags.HalfCarry);
            if (A == 0) SetFlag(Flags.Zero);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Inc(byte value)
        {
            byte result = (byte) (value + 1);

            if (CarryFlag)
            {
                ClearAllFlags();
                SetFlag(Flags.Carry);
            }
            else
            {
                ClearAllFlags();
            }

            if (result == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            if ((result & 0x0F) == 0x00)
            {
                SetFlag(Flags.HalfCarry);
            }

            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Add(byte value)
        {
            int result = A + value;
            int carrybits = A ^ value ^ result;
            A = (byte)result;

            ClearAllFlags();

            if (A == 0) SetFlag(Flags.Zero);

            if ((carrybits & 0x100) != 0)
            {
                SetFlag(Flags.Carry);
            }
            if ((carrybits & 0x10) != 0)
            {
                SetFlag(Flags.HalfCarry);
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void AddHL(ushort value)
        {
            int result = HL + value;

            if(ZeroFlag)
            {
                ClearAllFlags();
                SetFlag(Flags.Zero);
            }
            else
            {
                ClearAllFlags();
            }


            if ((result & 0x10000) != 0)
            {
                SetFlag(Flags.Carry);
            }
            if (((HL ^ value ^ (result & 0xFFFF)) & 0x1000) != 0)
            {
                SetFlag(Flags.HalfCarry);
            }

            HL = (ushort) result;
        }


        // Add and then add Carry
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Adc(byte value)
        {
            int carry = CarryFlag ? 1 : 0;
            int result = A + value + carry;
            ClearAllFlags();

            // Note that result must be cast to a byte to do the zero check!
            if ((byte) (result) == 0) SetFlag(Flags.Zero);

            if (result > 0xFF)
            {
                SetFlag(Flags.Carry);
            }
            if (((A & 0x0F) + (value & 0x0F) + carry) > 0x0F)
            {
                SetFlag(Flags.HalfCarry);
            }
            A = (byte)result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Sub(byte value)
        {
            int result = A - value;
            int carrybits = A ^ value ^ result;
            A = (byte)result;

            ClearAllFlags();
            SetFlag(Flags.Negative);

            if (A == 0) SetFlag(Flags.Zero);

            if ((carrybits & 0x100) != 0) SetFlag(Flags.Carry);            
            
            if ((carrybits & 0x10) != 0) SetFlag(Flags.HalfCarry);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte Dec(byte value)
        {
            byte result = (byte) (value - 1);

            if (CarryFlag)
            {
                ClearAllFlags();
                SetFlag(Flags.Carry);
            }
            else
            {
                ClearAllFlags();
            }
   
            SetFlag(Flags.Negative);
            
            if (result == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);

            if ((result & 0x0F) == 0x0F)
            {
                SetFlag(Flags.HalfCarry);
            }
            return result;

        }


        // Subrtact and then subtract Carry
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Sbc(byte value)
        {
            int carry = CarryFlag ? 1 : 0;
            int result = A - value - carry;

            ClearAllFlags();
            SetFlag(Flags.Negative);

            // Note that result must be cast to a byte to do the zero check!
            if ((byte) (result) == 0) SetFlag(Flags.Zero);

            if (result < 0) SetFlag(Flags.Carry);
            
            if (((A & 0x0F) - (value & 0x0F) - carry) < 0) SetFlag(Flags.HalfCarry);

            A = (byte)(result);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Cmp(byte value)
        {
            ClearAllFlags();
            SetFlag(Flags.Negative);

            if (A < value)
            {
                SetFlag(Flags.Carry);
            }
            if (A == value)
            {
                SetFlag(Flags.Zero);
            }
            if (((A - value) & 0xF) > (A & 0xF))
            {
                SetFlag(Flags.HalfCarry);
            }
        }



        // ********************
        // Instruction Handlers 
        // ********************

        // 0x00
        void NOP()
        {
        }

        // 0x01
        void LD_bc_nn(ushort nn)
        {
            BC = nn;
        }

        // 0x02
        void LD_bcp_a()
        {
            memory.WriteByteAndCycle(BC, A);
        }

        // 0x03
        void INC_bc()
        {
            BC++;
        }

        // 0x04
        void INC_b()
        {
            B = Inc(B);
        }

        // 0x05
        void DEC_b()
        {
            B = Dec(B);
        }

        // 0x06
        void LD_b_n(byte n)
        {
            B = n;
        }

        //0x07
        void RLCA()
        {
            A = Rlc(A, true);
        }

        // 0x08
        void LD_nn_sp(ushort nn)
        {
            memory.WriteShortAndCycle(nn, SP);
        }        

        
        // 0x09
        void ADD_hl_bc()
        {
            AddHL(BC);
        }

        // 0x0A
        void LD_a_bcp()
        {
            A = memory.ReadByteAndCycle(BC);
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

        // 0x0E
        void LD_c_n(byte n)
        {
            C = n;
        }

        // 0x0F
        void RRCA()
        {
            A = Rrc(A, true);
        }

        // 0x10
        void STOP()
        {
            // Halt CPU & LCD display until button pressed.

            //IsStopped = true;
        }

        // 0x11
        void LD_de_nn(ushort nn)
        {
            DE = nn;
        }


        // 0x12
        void LD_dep_a()
        {
            memory.WriteByteAndCycle(DE, A);
        }

        // 0x13       
        void INC_de()
        {
            DE++;
        }

        // 0x14
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

        // 0x16
        void LD_d_n(byte n)
        {
            D = n;
        }

        // 0x17
        void RLA()
        {
            A = Rl(A, true);
        }

        // 0x18
        void JR_n(sbyte n)
        {
            int pc = (int)(PC) + n;
            PC = (ushort)pc;
        }


        // 0x19
        void ADD_hl_de()
        {
            AddHL(DE);
        }

        // 0x1A
        void LD_a_dep()
        {
            A = memory.ReadByteAndCycle(DE);
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

        // 0x1F
        void RRA()
        {
            A = Rr(A, true);
        }

        // 0x20
        void JR_NZ_n(sbyte n)
        {
            if(ZeroFlag == false)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;
                
                dmg.cpu.CycleCpu(1);
            }
        }


        // 0x21
        void LD_hl_nn(ushort nn)
        {
            HL = nn;
        }

        // 0x22
        void LDI_hlp_a()
        {
            memory.WriteByteAndCycle(HL, A);
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

        // 0x26
        void LD_h_n(byte n)
        {
            H = n;
        }

        // 0x27
        void DAA()
        {
            int a = A;

            if (!NegativeFlag)
            {
                if (HalfCarryFlag || ((a & 0xF) > 9))
                    a += 0x06;

                if (CarryFlag || (a > 0x9F))
                    a += 0x60;
            }
            else
            {
                if (HalfCarryFlag)
                    a = (a - 6) & 0xFF;

                if (CarryFlag)
                    a -= 0x60;
            }

            ClearFlag(Flags.HalfCarry);
            ClearFlag(Flags.Zero);

            if ((a & 0x100) == 0x100)
                SetFlag(Flags.Carry);

            a &= 0xFF;

            if (a == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);            

            A = (byte) a;
        }

        // 0x28
        void JR_Z_n(sbyte n)
        {
            if (ZeroFlag)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;

                dmg.cpu.CycleCpu(1);
            }
        }

        //0x29
        void ADD_hl_hl()
        {
            AddHL(HL);
        }

        // 0x2A
        void LDI_a_hlp()
        {
            A = memory.ReadByteAndCycle(HL);
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

        //0x2F
        void CPL()
        {
            //flip all bits
            A = (byte)(~A);

            SetFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);
        }

        // 0x30
        void JR_NC_n(sbyte n)
        {
            if (CarryFlag == false)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;

                dmg.cpu.CycleCpu(1);
            }
        }

        void LD_sp_nn(ushort nn)
        {
            SP = nn;
        }

        // 0x32
        void LDD_hlp_a()
        {
            // Put A into memory address HL. Decrement HL.
            memory.WriteByteAndCycle(HL, A);
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
            memory.WriteByteAndCycle(HL, Inc(memory.ReadByteAndCycle(HL)));
        }

        // 0x35
        void DEC_hlp()
        {
            memory.WriteByteAndCycle(HL, Dec(memory.ReadByteAndCycle(HL)));
        }

        // 0x36
        void LD_hlp_n(byte n)
        {
            memory.WriteByteAndCycle(HL, n);
        }

        // 0x37
        void SCF()
        {
            SetFlag(Flags.Carry);
            ClearFlag(Flags.HalfCarry);
            ClearFlag(Flags.Negative);
        }

        // 0x38
        void JR_C_n(sbyte n)
        {
            if (CarryFlag)
            {
                int pc = (int)(PC) + n;
                PC = (ushort)pc;

                dmg.cpu.CycleCpu(1);
            }
        }        

        //0x39
        void ADD_hl_sp()
        {
            AddHL(SP);
        }

        // 0x3A
        void LDD_a_hlp()
        {
            A = memory.ReadByteAndCycle(HL);
            HL--;
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

        // 0x3F
        void CCF()
        {
            if (CarryFlag) ClearFlag(Flags.Carry);
            else SetFlag(Flags.Carry);

            ClearFlag(Flags.Negative);
            ClearFlag(Flags.HalfCarry);
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
            B = memory.ReadByteAndCycle(HL);
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
            C = memory.ReadByteAndCycle(HL);
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
            D = memory.ReadByteAndCycle(HL);
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
            E = memory.ReadByteAndCycle(HL);
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
            H = memory.ReadByteAndCycle(HL);
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
            L = memory.ReadByteAndCycle(HL);
        }

        // 0x6F
        void LD_l_a()
        {
            L = A;
        }

        // 0x70
        void LD_hlp_b()
        {
            memory.WriteByteAndCycle(HL, B);
        }

        // 0x71
        void LD_hlp_c()
        {
            memory.WriteByteAndCycle(HL, C);
        }

        // 0x72
        void LD_hlp_d()
        {
            memory.WriteByteAndCycle(HL, D);
        }
        // 0x73
        void LD_hlp_e()
        {
            memory.WriteByteAndCycle(HL, E);
        }

        // 0x74
        void LD_hlp_h()
        {
            memory.WriteByteAndCycle(HL, H);
        }

        // 0x75
        void LD_hlp_l()
        {
            memory.WriteByteAndCycle(HL, L);
        }

        // 0x76
        void HALT()
        {
            // http://rednex.github.io/rgbds/gbz80.7.html#HALT
            if (interrupts.InterruptsMasterEnable)
            {
                // The CPU enters low-power mode until after an interrupt is about to be serviced. The handler is executed normally, and the CPU resumes 
                // execution after the HALT when that returns.
                IsHalted = true;

                //JB: account for the cycles below, we use M cycles 
                dmg.cpu.CycleCpu(1);

                /*
                while halted:
                    sleep 2 T cycles
                    check for interrupts
                    sleep 2 T cycles
                    handle interrupt if needed
                */


            }
            else
            {
                if (interrupts.IsAnInterruptPending())
                {
                    // The CPU continues execution after the HALT, but the byte after it is read twice in a row (PC is not incremented, due to a hardware bug).
                    PC += 1;
                    PeekNextInstruction();              
                }
                else
                {
                    // As soon as an interrupt becomes pending, the CPU resumes execution. This is like the above, except that the handler is not called.
                    IsHalted = true;
                    interrupts.ResumeCpuWhenInterruptBecomesPending = true;
                }
            }
        }

        // 0x77
        void LD_hlp_a()
        {
            memory.WriteByteAndCycle(HL, A);
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
            A = memory.ReadByteAndCycle(HL);
        }

        // 0x7F
        void LD_a_a()
        {
            A = A;
        }

        //0x80
        void ADD_a_b()
        {
            Add(B);
        }

        //0x81
        void ADD_a_c()
        {
            Add(C);
        }

        //0x82
        void ADD_a_d()
        {
            Add(D);
        }

        //0x83
        void ADD_a_e()
        {
            Add(E);
        }

        //0x84
        void ADD_a_h()
        {
            Add(H);
        }

        //0x85
        void ADD_a_l()
        {
            Add(L);
        }

        //0x86
        void ADD_a_hlp()
        {
            Add(memory.ReadByteAndCycle(HL));
        }

        //0x87
        void ADD_a_a()
        {
            Add(A);
        }

        // 0x88
        void ADC_a_b()
        {
            Adc(B);
        }

        // 0x89
        void ADC_a_c()
        {
            Adc(C);
        }

        // 0x8A
        void ADC_a_d()
        {
            Adc(D);
        }

        // 0x8B
        void ADC_a_e()
        {
            Adc(E);
        }

        // 0x8C
        void ADC_a_h()
        {
            Adc(H);
        }

        // 0x8D
        void ADC_a_l()
        {
            Adc(L);
        }

        // 0x8E
        void ADC_a_hlp()
        {
            Adc(memory.ReadByteAndCycle(HL));
        }

        // 0x8F
        void ADC_a_a()
        {
            Adc(A);
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
            Sub(memory.ReadByteAndCycle(HL));
        }

        //0x97
        void SUB_a_a()
        {
            Sub(A);
        }


        // 0x98
        void SBC_a_b()
        {
            Sbc(B);
        }

        // 0x99
        void SBC_a_c()
        {
            Sbc(C);
        }

        // 0x9A
        void SBC_a_d()
        {
            Sbc(D);
        }

        // 0x9B
        void SBC_a_e()
        {
            Sbc(E);
        }

        // 0x9C
        void SBC_a_h()
        {
            Sbc(H);
        }

        // 0x9D
        void SBC_a_l()
        {
            Sbc(L);
        }

        // 0x9E
        void SBC_a_hlp()
        {
            Sbc(memory.ReadByteAndCycle(HL));
        }

        // 0x9F
        void SBC_a_a()
        {
            Sbc(A);
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
            And(memory.ReadByteAndCycle(HL));
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
            Xor(memory.ReadByteAndCycle(HL));
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
            Or(memory.ReadByteAndCycle(HL));
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
            Cmp(memory.ReadByteAndCycle(HL));
        }

        // 0xBF
        void CP_a()
        {
            Cmp(A);
        }

        // 0xC0
        void RET_NZ()
        {
            if (ZeroFlag == false)
            {
                PC = StackPop();
                dmg.cpu.CycleCpu(2);
            }
            else
            {
                dmg.cpu.CycleCpu(1);
            }
        }
        
        //0xC1
        void POP_bc()
        {
            BC = StackPop();
        }
        
        // C2
        void JP_NZ_nn(ushort nn)
        {
            if (ZeroFlag == false)
            {
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // C3
        void JP_nn(ushort nn)
        {
            PC = nn;
        }

        // 0xC4
        void CALL_NZ_nn(ushort nn)
        {
            if (ZeroFlag == false)
            {
                StackPush(PC);
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xC5
        void PUSH_bc()
        {
            StackPush(BC);
        }

        // 0xC6
        void ADD_a_n(byte n)
        {
            Add(n);
        }

        // 0xC7
        void RST_0()
        {
            StackPush(PC);
            PC = 0x0000;
        }

        // 0xC8
        void RET_Z()
        {
            if (ZeroFlag)
            {
                PC = StackPop();
                dmg.cpu.CycleCpu(2);
            }
            else
            {
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xC9
        void RET()
        {
            PC = StackPop();
        }

        // 0xCA
        void JP_Z_nn(ushort nn)
        {
            if (ZeroFlag)
            {
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xCC
        void CALL_Z_nn(ushort nn)
        {
            if (ZeroFlag)
            {
                StackPush(PC);
                PC = nn;
                dmg.cpu.CycleCpu(1);           
            }
        }

        // 0xCD
        void CALL_nn(ushort nn)
        {
            StackPush(PC);
            PC = nn;
        }

        // 0xCE
        void ADC_a_n(byte n)
        {
            Adc(n);
        }

        // 0xCF
        void RST_8()
        {
            StackPush(PC);
            PC = 0x0008;
        }

        // 0xD0
        void RET_NC()
        {
            if (CarryFlag == false)
            {
                PC = StackPop();

                dmg.cpu.CycleCpu(2);
            }
            else
            {
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xD1
        void POP_de()
        {
            DE = StackPop();
        }

        // 0xD2
        void JP_NC_nn(ushort nn)
        {
            if (CarryFlag == false)
            {
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xD4
        void CALL_NC_nn(ushort nn)
        {
            if (CarryFlag == false)
            {
                StackPush(PC);
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xD5
        void PUSH_de()
        {
            StackPush(DE);
        }

        // 0xD6
        void SUB_a_n(byte n)
        {
            Sub(n);
        }

        // 0xD7
        void RST_10()
        {
            StackPush(PC);
            PC = 0x0010;
        }

        // 0xD8
        void RET_C()
        {
            if (CarryFlag)
            {
                PC = StackPop();
                dmg.cpu.CycleCpu(2);
            }
            else
            {
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xD9
        void RETI()
        {      
            dmg.interrupts.InterruptsMasterEnable = true;

            dmg.cpu.PC = dmg.cpu.StackPop();
            dmg.cpu.CycleCpu(1);
            dmg.cpu.PeekNextInstruction();
        }


        // 0xDA
        void JP_C_nn(ushort nn)
        {
            if (CarryFlag)
            {
                PC = nn;
                dmg.cpu.CycleCpu(1);
            }
        }

        // 0xDC
        void CALL_C_nn(ushort nn)
        {
            // TODO: Look at this instruction here:
            // https://izik1.github.io/gbops/
            // Claims 6 mcycles but if you add it up, would be 7.
            // We are not cycle accurate on PC / SP writes!
            if (CarryFlag)
            {
                StackPush(PC);
                PC = nn;
                dmg.cpu.CycleCpu(1);                
            }
        }

        // 0xDE
        void SBC_a_n(byte n)
        {
            Sbc(n);
        }

        // 0xDF
        void RST_18()
        {
            StackPush(PC);
            PC = 0x0018;
        }

        // 0xE0
        void LDH_ff_n_a(byte n)
        {
            memory.WriteByteAndCycle((ushort)((ushort)0xFF00 + (ushort)n), A);
        }

        // 0xE1
        void POP_hl()
        {
            HL = StackPop();
        }

        // 0xE2
        void LDH_ff_c_a()
        {
            memory.WriteByteAndCycle((ushort)((ushort) 0xFF00 + (ushort) C), A);
        }


        // 0xE5
        void PUSH_hl()
        {
            StackPush(HL);
        }

        // 0xE6
        void AND_n(byte n)
        {
            /*
            A &= n;

            ClearFlag(Flags.Carry);
            ClearFlag(Flags.Negative);

            SetFlag(Flags.HalfCarry);

            if (A == 0) SetFlag(Flags.Zero);
            else ClearFlag(Flags.Zero);
            */
            And(n);
        }

        // 0xE7
        void RST_20()
        {
            StackPush(PC);
            PC = 0x0020;
        }

        // 0xE6
        void ADD_sp_n(sbyte n)
        {
            int result = SP + n;
            ClearAllFlags();
            if (((SP ^ n ^ (result & 0xFFFF)) & 0x100) == 0x100)
            {
                SetFlag(Flags.Carry);
            }
            if (((SP ^ n ^ (result & 0xFFFF)) & 0x10) == 0x10)
            {
                SetFlag(Flags.HalfCarry);
            }
            SP = (ushort) result;
        }

        // 0xE9
        // **** The documentation looks like this should jump to what HL points at but then it contradicts itself in the description ****
        // **** If this becomes problematic, check this! ****
        void JP_hl()
        {
            PC = HL;
        }
        
        // 0xEA
        void LD_nnp_a(ushort nn)
        {
            memory.WriteByteAndCycle(nn, A);
        }

        // 0xEE
        void XOR_n(byte n)
        {
            Xor(n);
        }

        // 0xEF
        void RST_28()
        {
            StackPush(PC);
            PC = 0x0028;
        }

        // 0xF0
        void LDH_a_ff_n(byte n)
        {
            ushort address = (ushort) (0xFF00 + n);
            A = memory.ReadByteAndCycle(address);
        }

        // 0xF1
        void POP_af()
        {
            AF = StackPop();

            // Make sure we don't set impossible flags on F, See Blargg's PUSH AF test.
            F &= 0xF0;
        }

        // 0xF2
        void LD_a_ff_c()
        {
            A = memory.ReadByteAndCycle((ushort)(0xFF00 + C));
        }

        // F3
        void DI()
        {
            interrupts.InterruptsMasterEnable = false;
        }

        // 0xF5
        void PUSH_af()
        {
            StackPush(AF);
        }

        // 0xF6
        void OR_n(byte n)
        {
            Or(n);
        }

        // 0xF7
        void RST_30()
        {
            StackPush(PC);
            PC = 0x0030;
        }

        // 0xF8
        void LDH_hl_sp_n(sbyte n)
        {
            // LD HL,SP+n
            ushort result = (ushort) (SP + n);
            ClearAllFlags();
            if (((SP ^ n ^ result) & 0x100) == 0x100)
                SetFlag(Flags.Carry);
            if (((SP ^ n ^ result) & 0x10) == 0x10)
                SetFlag(Flags.HalfCarry);
            HL = result;           
        }

        // 0xF9
        void LD_sp_hl()
        {
            SP = HL;
        }

        // 0xFA
        void LD_a_nnp(ushort nn)
        {
            A = memory.ReadByteAndCycle(nn);
        }

        // 0xFB
        void EI()
        {
            enableInterruptsNextCycle = true;
        }

        // 0xFE
        void CP_n(byte n)
        {
            Cmp(n);
        }

        // 0xFF
        void RST_38()
        {
            StackPush(PC);
            PC = 0x0038;
        }
    }
}
