using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DMG
{
    // Custom 8-bit Sharp LR35902 at 4.19 MHz
    public partial class Cpu
    {
        // 8 bit registers can be addressed together as 16 bit
        public byte A { get; private set; }
        public byte F { get; private set; }
        public ushort AF { get { return (ushort)((A << 8) | F); } private set { A = (byte)(value >> 8); B = (byte)(value & 0x00FF); } }

        public byte B { get; private set; }
        public byte C { get; private set; }
        public ushort BC { get { return (ushort)((B << 8) | C); } private set { B = (byte)(value >> 8); C = (byte)(value & 0x00FF); } }

        public byte D { get; private set; }
        public byte E { get; private set; }
        public ushort DE { get { return (ushort)((D << 8) | E); } private set { D = (byte)(value >> 8); E = (byte)(value & 0x00FF); } }

        public byte H { get; private set; }
        public byte L { get; private set; }
        public ushort HL { get { return (ushort)((H << 8) | L); } private set { H = (byte)(value >> 8); L = (byte)(value & 0x00FF); } }


        // Progrtam counter (16 bit)
        public ushort PC { get; private set; }

        // Stack Pointer (16 bit)
        public ushort SP { get; private set; }


        public enum Flags
        {
            Zero = 1 << 7,
            Negative = 1 << 6,
            HalfCarry = 1 << 5,
            Carry = 1 << 4,
        }

        private IMemoryReaderWriter memory;

        private Instruction[] instructions = new Instruction[256];
        private ExtendedInstruction[] extendedInstructions = new ExtendedInstruction[256];

        public bool IsHalted { get { return false; } }



        public Cpu(IMemoryReaderWriter memory)
        {
            this.memory = memory;

            RegisterInstructionHandlers();
            RegisterExtendedInstructionHandlers();

            Reset();
        }


        public void Reset()
        {
            //A = 0x01;
            //F = 0xb0;
            //B = 0x00;
            //C = 0x13;
            //D = 0x00;
            //E = 0xd8;
            //H = 0x01;
            //L = 0x4d;

            //PC = 0x100;       // Game code start
            //PC = 0x00;        // Boot ROM
            //SP = 0xFFFE;
        }


        public void Step()
        {
            byte opCode = memory.ReadByte(PC++);

            var instruction = instructions[opCode];
            if (instruction == null || instruction.Handler == null)
            {
                throw new ArgumentException(String.Format("Unsupported instruction 0x{0:X2} {1}", opCode, instruction == null ? "-" : instruction.Name));
            }

            ushort operandValue;
            if (instruction.OperandLength == 1) operandValue = memory.ReadByte(PC);
            else operandValue = memory.ReadShort(PC);
            PC += instruction.OperandLength;


            instruction.Handler(operandValue);

        }


        void StackPush(ushort value)
        {
            SP -= 2;
            memory.WriteShort(SP, value);
        }


        ushort StackPop()
        {
            ushort value = memory.ReadShort(SP);

            SP += 2;

            return value;
        }


        bool ZeroFlag
        {
            get
            {
                return (F & (byte)(Flags.Zero)) != 0;
            }
        }


        bool NegativeFlag
        {
            get
            {
                return (F & (byte)(Flags.Negative)) != 0;
            }
        }


        bool CarryFlag
        {
            get
            {
                return (F & (byte)(Flags.Carry)) != 0;
            }
        }


        bool HalfCarryFlag
        {
            get
            {
                return (F & (byte)(Flags.HalfCarry)) != 0;
            }
        }


        void SetFlag(Flags flag)
        {
            F |= (byte)flag;
        }


        void ClearFlag(Flags flag)
        {
            F &= (byte)~((byte)flag);
        }


        void extended(byte opCode)
        {
            extendedInstructions[opCode].Handler();
        }


        public void OutputState()
        {
            Console.ForegroundColor = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ConsoleColor.Black : ConsoleColor.White;

            Console.SetCursorPosition(0, 5);
            Console.Write("                    ");
            Console.SetCursorPosition(0, 5);
            Console.Write(String.Format("A: 0x{0:X2}", A));
            if (ZeroFlag) Console.Write(" Z");
            if (CarryFlag) Console.Write(" C");
            if (HalfCarryFlag) Console.Write(" H");
            if (NegativeFlag) Console.Write(" N");

            Console.SetCursorPosition(0, 6);
            Console.Write(String.Format("B: 0x{0:X2}", B));
            Console.SetCursorPosition(0, 7);
            Console.Write(String.Format("C: 0x{0:X2}", C));
            Console.SetCursorPosition(0, 8);
            Console.Write(String.Format("D: 0x{0:X2}", D));
            Console.SetCursorPosition(0, 9);
            Console.Write(String.Format("E: 0x{0:X2}", E));
            Console.SetCursorPosition(0, 10);
            Console.Write(String.Format("H: 0x{0:X2}", H));
            Console.SetCursorPosition(0, 11);
            Console.Write(String.Format("L: 0x{0:X2}", L));
            Console.SetCursorPosition(0, 12);
            Console.Write(String.Format("SP: 0x{0:X2}", SP));
            Console.SetCursorPosition(0, 13);
            Console.Write(String.Format("PC: 0x{0:X2}", PC));

            Console.SetCursorPosition(20, 5);
            Console.Write(String.Format("AF: 0x{0:X4}", AF));
            Console.SetCursorPosition(20, 6);
            Console.Write(String.Format("BC: 0x{0:X4}", BC));
            Console.SetCursorPosition(20, 7);
            Console.Write(String.Format("DE: 0x{0:X4}", DE));
            Console.SetCursorPosition(20, 8);
            Console.Write(String.Format("HL: 0x{0:X4}", HL));


            var ins = instructions[memory.ReadByte(PC)];
            if (ins != null)
            {
                Console.SetCursorPosition(0, 20);
                Console.Write(String.Format("Instruction: {0}                                ", ins.Name));
            }
            else
            {
                Console.Write("                                 ");
            }
        }


        void RegisterInstructionHandlers()
        {
            instructions[0x00] = new Instruction("NOP", 0x00, 0, (v) => this.NOP());
            instructions[0x03] = new Instruction("INC bc", 0x03, 0, (v) => this.INC_bc());
            instructions[0x04] = new Instruction("INC b", 0x04, 0, (v) => this.INC_b());
            instructions[0x05] = new Instruction("DEC b", 0x05, 0, (v) => this.DEC_b());
            instructions[0x06] = new Instruction("LD b n", 0x06, 1, (v) => this.LD_b_n((byte)v));
            instructions[0x08] = new Instruction("LD nn sp", 0x08, 2, (v) => this.LD_nn_sp(v));
            instructions[0x0B] = new Instruction("DEC bc", 0x0B, 0, (v) => this.DEC_bc());
            instructions[0x0C] = new Instruction("INC c", 0x0C, 0, (v) => this.INC_c());
            instructions[0x0D] = new Instruction("DEC c", 0x0D, 0, (v) => this.DEC_c());
            instructions[0x0E] = new Instruction("LD c n", 0x0E, 1, (v) => this.LD_c_n((byte)v));
            instructions[0x11] = new Instruction("LD DE nn", 0x11, 2, (v) => this.LD_de_nn(v));
            instructions[0x13] = new Instruction("INC de", 0x13, 0, (v) => this.INC_de());
            instructions[0x14] = new Instruction("INC d", 0x14, 0, (v) => this.INC_d());
            instructions[0x15] = new Instruction("DEC d", 0x15, 0, (v) => this.DEC_d());
            instructions[0x16] = new Instruction("LD d n", 0x16, 1, (v) => this.LD_d_n((byte)v));
            instructions[0x17] = new Instruction("RLA", 0x17, 0, (v) => this.RLA());
            instructions[0x18] = new Instruction("JR n", 0x18, 1, (v) => this.JR_n((sbyte) v));
            instructions[0x1A] = new Instruction("LD A (de)", 0x1A, 0, (v) => this.LD_a_dep());
            instructions[0x1B] = new Instruction("DEC de", 0x1B, 0, (v) => this.DEC_de());
            instructions[0x1C] = new Instruction("INC e", 0x1C, 0, (v) => this.INC_e());
            instructions[0x1D] = new Instruction("DEC e", 0x1D, 0, (v) => this.DEC_e());
            instructions[0x1E] = new Instruction("LD e n", 0x1E, 1, (v) => this.LD_e_n((byte)v));
            instructions[0x20] = new Instruction("JR NZ n", 0x20, 1, (v) => this.JR_NZ_n((sbyte)v));
            instructions[0x21] = new Instruction("LD hl nn", 0x21, 2, (v) => this.LD_hl_nn(v));
            instructions[0x22] = new Instruction("LDI (hl) a", 0x22, 0, (v) => this.LDI_hlp_a());
            instructions[0x23] = new Instruction("INC hl", 0x23, 0, (v) => this.INC_hl());
            instructions[0x24] = new Instruction("INC h", 0x24, 0, (v) => this.INC_h());
            instructions[0x25] = new Instruction("DEC h", 0x25, 0, (v) => this.DEC_h());
            instructions[0x26] = new Instruction("LD h n", 0x26, 1, (v) => this.LD_h_n((byte)v));
            instructions[0x28] = new Instruction("JR Z n", 0x28, 1, (v) => this.JR_Z_n((sbyte)v));
            instructions[0x2A] = new Instruction("LDI a (hl)", 0x2A, 0, (v) => this.LDI_a_hlp());
            instructions[0x2B] = new Instruction("DEC hl", 0x2B, 0, (v) => this.DEC_hl());
            instructions[0x2C] = new Instruction("INC l", 0x2C, 0, (v) => this.INC_l());
            instructions[0x2D] = new Instruction("DEC l", 0x2D, 0, (v) => this.DEC_l());
            instructions[0x2E] = new Instruction("LD l n", 0x2E, 1, (v) => this.LD_l_n((byte)v));
            instructions[0x30] = new Instruction("JR NC n", 0x30, 1, (v) => this.JR_NC_n((sbyte)v));
            instructions[0x31] = new Instruction("LD sp nn", 0x31, 2, (v) => this.LD_sp_nn(v));
            instructions[0x32] = new Instruction("LDD hl a", 0x32, 0, (v) => this.LDD_hl_a());
            instructions[0x33] = new Instruction("INC sp", 0x33, 0, (v) => this.INC_sp());
            instructions[0x34] = new Instruction("INC (hl)", 0x34, 0, (v) => this.INC_hlp());
            instructions[0x35] = new Instruction("DEC (hl)", 0x35, 0, (v) => this.DEC_hlp());
            instructions[0x38] = new Instruction("JR C n", 0x38, 1, (v) => this.JR_C_n((sbyte)v));
            instructions[0x3B] = new Instruction("DEC sp", 0x3B, 0, (v) => this.DEC_sp());
            instructions[0x3C] = new Instruction("INC a", 0x3C, 0, (v) => this.INC_a());
            instructions[0x3D] = new Instruction("DEC a", 0x3D, 0, (v) => this.DEC_a());
            instructions[0x3E] = new Instruction("LD a n", 0x3E, 1, (v) => this.LD_a_n((byte) v));

            instructions[0x40] = new Instruction("LD b b", 0x40, 0, (v) => this.LD_b_b());
            instructions[0x41] = new Instruction("LD b c", 0x41, 0, (v) => this.LD_b_c());
            instructions[0x42] = new Instruction("LD b d", 0x42, 0, (v) => this.LD_b_d());
            instructions[0x43] = new Instruction("LD b e", 0x43, 0, (v) => this.LD_b_e());
            instructions[0x44] = new Instruction("LD b h", 0x44, 0, (v) => this.LD_b_h());
            instructions[0x45] = new Instruction("LD b l", 0x45, 0, (v) => this.LD_b_l());
            instructions[0x46] = new Instruction("LD b (hl)", 0x46, 0, (v) => this.LD_b_hlp());
            instructions[0x47] = new Instruction("LD b a", 0x47, 0, (v) => this.LD_b_a());

            instructions[0x48] = new Instruction("LD c b", 0x48, 0, (v) => this.LD_c_b());
            instructions[0x49] = new Instruction("LD c c", 0x49, 0, (v) => this.LD_c_c());
            instructions[0x4A] = new Instruction("LD c d", 0x4A, 0, (v) => this.LD_c_d());
            instructions[0x4B] = new Instruction("LD c e", 0x4B, 0, (v) => this.LD_c_e());
            instructions[0x4C] = new Instruction("LD c h", 0x4C, 0, (v) => this.LD_c_h());
            instructions[0x4D] = new Instruction("LD c l", 0x4D, 0, (v) => this.LD_c_l());
            instructions[0x4E] = new Instruction("LD c (hl)", 0x4E, 0, (v) => this.LD_c_hlp());
            instructions[0x4F] = new Instruction("LD c a", 0x4F, 0, (v) => this.LD_c_a());

            instructions[0x50] = new Instruction("LD d b", 0x50, 0, (v) => this.LD_d_b());
            instructions[0x51] = new Instruction("LD d c", 0x51, 0, (v) => this.LD_d_c());
            instructions[0x52] = new Instruction("LD d d", 0x52, 0, (v) => this.LD_d_d());
            instructions[0x53] = new Instruction("LD d e", 0x53, 0, (v) => this.LD_d_e());
            instructions[0x54] = new Instruction("LD d h", 0x54, 0, (v) => this.LD_d_h());
            instructions[0x55] = new Instruction("LD d l", 0x55, 0, (v) => this.LD_d_l());
            instructions[0x56] = new Instruction("LD d (hl)", 0x56, 0, (v) => this.LD_d_hlp());
            instructions[0x57] = new Instruction("LD d a", 0x57, 0, (v) => this.LD_d_a());

            instructions[0x58] = new Instruction("LD e b", 0x58, 0, (v) => this.LD_e_b());
            instructions[0x59] = new Instruction("LD e c", 0x59, 0, (v) => this.LD_e_c());
            instructions[0x5A] = new Instruction("LD e d", 0x5A, 0, (v) => this.LD_e_d());
            instructions[0x5B] = new Instruction("LD e e", 0x5B, 0, (v) => this.LD_e_e());
            instructions[0x5C] = new Instruction("LD e h", 0x5C, 0, (v) => this.LD_e_h());
            instructions[0x5D] = new Instruction("LD e l", 0x5D, 0, (v) => this.LD_e_l());
            instructions[0x5E] = new Instruction("LD e (hl)", 0x5E, 0, (v) => this.LD_e_hlp());
            instructions[0x5F] = new Instruction("LD e a", 0x5F, 0, (v) => this.LD_e_a());

            instructions[0x60] = new Instruction("LD h b", 0x60, 0, (v) => this.LD_h_b());
            instructions[0x61] = new Instruction("LD h c", 0x61, 0, (v) => this.LD_h_c());
            instructions[0x62] = new Instruction("LD h d", 0x62, 0, (v) => this.LD_h_d());
            instructions[0x63] = new Instruction("LD h e", 0x63, 0, (v) => this.LD_h_e());
            instructions[0x64] = new Instruction("LD h h", 0x64, 0, (v) => this.LD_h_h());
            instructions[0x65] = new Instruction("LD h l", 0x65, 0, (v) => this.LD_h_l());
            instructions[0x66] = new Instruction("LD h (hl)", 0x66, 0, (v) => this.LD_h_hlp());
            instructions[0x67] = new Instruction("LD h a", 0x67, 0, (v) => this.LD_h_a());

            instructions[0x68] = new Instruction("LD l b", 0x68, 0, (v) => this.LD_l_b());
            instructions[0x69] = new Instruction("LD l c", 0x69, 0, (v) => this.LD_l_c());
            instructions[0x6A] = new Instruction("LD l d", 0x6A, 0, (v) => this.LD_l_d());
            instructions[0x6B] = new Instruction("LD l e", 0x6B, 0, (v) => this.LD_l_e());
            instructions[0x6C] = new Instruction("LD l h", 0x6C, 0, (v) => this.LD_l_h());
            instructions[0x6D] = new Instruction("LD l l", 0x6D, 0, (v) => this.LD_l_l());
            instructions[0x6E] = new Instruction("LD l (hl)", 0x6E, 0, (v) => this.LD_l_hlp());
            instructions[0x6F] = new Instruction("LD l a", 0x6F, 0, (v) => this.LD_l_a());

            instructions[0x70] = new Instruction("LD (hl) b", 0x70, 0, (v) => this.LD_hlp_b());
            instructions[0x71] = new Instruction("LD (hl) c", 0x71, 0, (v) => this.LD_hlp_b());
            instructions[0x72] = new Instruction("LD (hl) d", 0x72, 0, (v) => this.LD_hlp_b());
            instructions[0x73] = new Instruction("LD (hl) e", 0x73, 0, (v) => this.LD_hlp_b());
            instructions[0x74] = new Instruction("LD (hl) h", 0x74, 0, (v) => this.LD_hlp_b());
            instructions[0x75] = new Instruction("LD (hl) l", 0x75, 0, (v) => this.LD_hlp_b());
            instructions[0x76] = new Instruction("HALT", 0x76, 0, (v) => this.HALT());
            instructions[0x77] = new Instruction("LD (hl) a", 0x77, 0, (v) => this.LD_hlp_a());

            instructions[0x78] = new Instruction("LD a b", 0x78, 0, (v) => this.LD_a_b());
            instructions[0x79] = new Instruction("LD a c", 0x79, 0, (v) => this.LD_a_c());
            instructions[0x7A] = new Instruction("LD a d", 0x7A, 0, (v) => this.LD_a_d());
            instructions[0x7B] = new Instruction("LD a e", 0x7B, 0, (v) => this.LD_a_e());
            instructions[0x7C] = new Instruction("LD a h", 0x7C, 0, (v) => this.LD_a_h());
            instructions[0x7D] = new Instruction("LD a l", 0x7D, 0, (v) => this.LD_a_l());
            instructions[0x7E] = new Instruction("LD a (hl)", 0x7E, 0, (v) => this.LD_a_hlp());
            instructions[0x7F] = new Instruction("LD a a", 0x7F, 0, (v) => this.LD_a_a());

            instructions[0x80] = new Instruction("ADD a b", 0x80, 0, (v) => this.ADD_a_b());
            instructions[0x81] = new Instruction("ADD a c", 0x81, 0, (v) => this.ADD_a_c());
            instructions[0x82] = new Instruction("ADD a d", 0x82, 0, (v) => this.ADD_a_d());
            instructions[0x83] = new Instruction("ADD a e", 0x83, 0, (v) => this.ADD_a_e());
            instructions[0x84] = new Instruction("ADD a h", 0x84, 0, (v) => this.ADD_a_h());
            instructions[0x85] = new Instruction("ADD a l", 0x85, 0, (v) => this.ADD_a_l());
            instructions[0x86] = new Instruction("ADD a (hl)", 0x86, 0, (v) => this.ADD_a_hlp());
            instructions[0x87] = new Instruction("ADD a a", 0x87, 0, (v) => this.ADD_a_a());

            instructions[0x90] = new Instruction("SUB a b", 0x90, 0, (v) => this.SUB_a_b());
            instructions[0x91] = new Instruction("SUB a c", 0x91, 0, (v) => this.SUB_a_c());
            instructions[0x92] = new Instruction("SUB a d", 0x92, 0, (v) => this.SUB_a_d());
            instructions[0x93] = new Instruction("SUB a e", 0x93, 0, (v) => this.SUB_a_e());
            instructions[0x94] = new Instruction("SUB a h", 0x94, 0, (v) => this.SUB_a_h());
            instructions[0x95] = new Instruction("SUB a l", 0x95, 0, (v) => this.SUB_a_l());
            instructions[0x96] = new Instruction("SUB a (hl)", 0x96, 0, (v) => this.SUB_a_hlp());
            instructions[0x97] = new Instruction("SUB a a", 0x97, 0, (v) => this.SUB_a_a());

            instructions[0xA0] = new Instruction("AND b", 0xA0, 0, (v) => this.AND_b());
            instructions[0xA1] = new Instruction("AND c", 0xA1, 0, (v) => this.AND_c());
            instructions[0xA2] = new Instruction("AND d", 0xA2, 0, (v) => this.AND_d());
            instructions[0xA3] = new Instruction("AND e", 0xA3, 0, (v) => this.AND_e());
            instructions[0xA4] = new Instruction("AND h", 0xA4, 0, (v) => this.AND_h());
            instructions[0xA5] = new Instruction("AND l", 0xA5, 0, (v) => this.AND_l());
            instructions[0xA6] = new Instruction("AND (hl)", 0xA6, 0, (v) => this.AND_hlp());
            instructions[0xA7] = new Instruction("AND a", 0xA7, 0, (v) => this.AND_a());

            instructions[0xA8] = new Instruction("XOR b", 0xA8, 0, (v) => this.XOR_b());
            instructions[0xA9] = new Instruction("XOR c", 0xA9, 0, (v) => this.XOR_c());
            instructions[0xAA] = new Instruction("XOR d", 0xAA, 0, (v) => this.XOR_d());
            instructions[0xAB] = new Instruction("XOR e", 0xAB, 0, (v) => this.XOR_e());
            instructions[0xAC] = new Instruction("XOR h", 0xAC, 0, (v) => this.XOR_h());
            instructions[0xAD] = new Instruction("XOR l", 0xAD, 0, (v) => this.XOR_l());
            instructions[0xAE] = new Instruction("XOR (hl)", 0xAE, 0, (v) => this.XOR_hlp());
            instructions[0xAF] = new Instruction("XOR a", 0xAF, 0, (v) => this.XOR_a());

            instructions[0xB0] = new Instruction("OR b", 0xB0, 0, (v) => this.OR_b());
            instructions[0xB1] = new Instruction("OR c", 0xB1, 0, (v) => this.OR_c());
            instructions[0xB2] = new Instruction("OR d", 0xB2, 0, (v) => this.OR_d());
            instructions[0xB3] = new Instruction("OR e", 0xB3, 0, (v) => this.OR_e());
            instructions[0xB4] = new Instruction("OR h", 0xB4, 0, (v) => this.OR_h());
            instructions[0xB5] = new Instruction("OR l", 0xB5, 0, (v) => this.OR_l());
            instructions[0xB6] = new Instruction("OR (hl)", 0xB6, 0, (v) => this.OR_hlp());
            instructions[0xB7] = new Instruction("OR a", 0xB7, 0, (v) => this.OR_a());

            instructions[0xB8] = new Instruction("CP b", 0xB8, 0, (v) => this.CP_b());
            instructions[0xB9] = new Instruction("CP c", 0xB9, 0, (v) => this.CP_c());
            instructions[0xBA] = new Instruction("CP d", 0xBA, 0, (v) => this.CP_d());
            instructions[0xBB] = new Instruction("CP e", 0xBB, 0, (v) => this.CP_e());
            instructions[0xBC] = new Instruction("CP h", 0xBC, 0, (v) => this.CP_h());
            instructions[0xBD] = new Instruction("CP l", 0xBD, 0, (v) => this.CP_l());
            instructions[0xBE] = new Instruction("CP (hl)", 0xBE, 0, (v) => this.CP_hlp());
            instructions[0xBF] = new Instruction("CP a", 0xBF, 0, (v) => this.CP_a());

            instructions[0xC1] = new Instruction("POP bc", 0xC1, 0, (v) => this.POP_bc());
            instructions[0xC3] = new Instruction("JP nn", 0xC3, 2, (v) => this.JP_nn(v));
            instructions[0xC5] = new Instruction("PUSH bc", 0xC5, 0, (v) => this.PUSH_bc());
            instructions[0xC9] = new Instruction("RET", 0xC9, 0, (v) => this.RET());
            instructions[0xD1] = new Instruction("POP de", 0xD1, 0, (v) => this.POP_de());
            instructions[0xD5] = new Instruction("PUSH de", 0xD5, 0, (v) => this.PUSH_de());
            instructions[0xCB] = new Instruction("Extended Opcode", 0xCB, 1, (v) => this.extended((byte)v));
            instructions[0xCD] = new Instruction("CALL nn", 0xCD, 2, (v) => this.CALL_nn(v));
            instructions[0xE0] = new Instruction("LD (0xFF00 + n) a", 0xE0, 1, (v) => this.LD_ff_n_a((byte) v));
            instructions[0xE1] = new Instruction("POP hl", 0xE1, 0, (v) => this.POP_hl());
            instructions[0xE2] = new Instruction("LD (0xFF00 + C) a", 0xE2, 0, (v) => this.LD_ff_c_a());
            instructions[0xE5] = new Instruction("PUSH hl", 0xE5, 0, (v) => this.PUSH_hl());
            instructions[0xEA] = new Instruction("LD nn a", 0xEA, 2, (v) => this.LD_nn_a(v));
            instructions[0xF1] = new Instruction("POP af", 0xF1, 0, (v) => this.POP_af());
            instructions[0xF3] = new Instruction("DI", 0xF3, 0, (v) => this.DI());
            instructions[0xF5] = new Instruction("PUSH af", 0xF5, 0, (v) => this.PUSH_af());
            instructions[0xFE] = new Instruction("CP n", 0xFE, 1, (v) => this.CP_n((byte) v));


            // Check we don't have repeat id's (we made a type in the table above)
            for (int i = 0; i < 255; i++)
            {
                var instruction = instructions[i];
                if (instruction == null) continue;

                if (instruction.OpCode != i)
                {
                    throw new ArgumentException("Bad opcode");
                }

                for (int j = 0; j < 255; j++)
                {
                    if (i == j) continue;
                    var rhs = instructions[j];
                    if (rhs == null) continue;

                    if (instruction.OpCode == rhs.OpCode ||
                        instruction.Handler == rhs.Handler)
                    {
                        throw new ArgumentException("Bad opcode");
                    }
                }
            }
        }


        void RegisterExtendedInstructionHandlers()
        {
            extendedInstructions[0x00] = new ExtendedInstruction("RLC b", 0x00, () => this.RLC_b());
            extendedInstructions[0x01] = new ExtendedInstruction("RLC c", 0x01, () => this.RLC_c());
            extendedInstructions[0x02] = new ExtendedInstruction("RLC d", 0x02, () => this.RLC_d());
            extendedInstructions[0x03] = new ExtendedInstruction("RLC e", 0x03, () => this.RLC_e());
            extendedInstructions[0x04] = new ExtendedInstruction("RLC h", 0x04, () => this.RLC_h());
            extendedInstructions[0x05] = new ExtendedInstruction("RLC l", 0x05, () => this.RLC_l());
            extendedInstructions[0x06] = new ExtendedInstruction("RLC hlp", 0x06, () => this.RLC_hlp());
            extendedInstructions[0x07] = new ExtendedInstruction("RLC a", 0x07, () => this.RLC_a());

            extendedInstructions[0x08] = new ExtendedInstruction("RRC b", 0x08, () => this.RRC_b());
            extendedInstructions[0x09] = new ExtendedInstruction("RRC c", 0x09, () => this.RRC_c());
            extendedInstructions[0x0A] = new ExtendedInstruction("RRC d", 0x0A, () => this.RRC_d());
            extendedInstructions[0x0B] = new ExtendedInstruction("RRC e", 0x0B, () => this.RRC_e());
            extendedInstructions[0x0C] = new ExtendedInstruction("RRC h", 0x0C, () => this.RRC_h());
            extendedInstructions[0x0D] = new ExtendedInstruction("RRC l", 0x0D, () => this.RRC_l());
            extendedInstructions[0x0E] = new ExtendedInstruction("RRC hlp", 0x0E, () => this.RRC_hlp());
            extendedInstructions[0x0F] = new ExtendedInstruction("RRC a", 0x0F, () => this.RRC_a());

            extendedInstructions[0x10] = new ExtendedInstruction("RL b", 0x10, () => this.RL_b());
            extendedInstructions[0x11] = new ExtendedInstruction("RL c", 0x11, () => this.RL_c());
            extendedInstructions[0x12] = new ExtendedInstruction("RL d", 0x12, () => this.RL_d());
            extendedInstructions[0x13] = new ExtendedInstruction("RL e", 0x13, () => this.RL_e());
            extendedInstructions[0x14] = new ExtendedInstruction("RL h", 0x14, () => this.RL_h());
            extendedInstructions[0x15] = new ExtendedInstruction("RL l", 0x15, () => this.RL_l());
            extendedInstructions[0x16] = new ExtendedInstruction("RL hlp", 0x16, () => this.RL_hlp());
            extendedInstructions[0x17] = new ExtendedInstruction("RL a", 0x17, () => this.RL_a());

            extendedInstructions[0x18] = new ExtendedInstruction("RR b", 0x18, () => this.RR_b());
            extendedInstructions[0x19] = new ExtendedInstruction("RR c", 0x19, () => this.RR_c());
            extendedInstructions[0x1A] = new ExtendedInstruction("RR d", 0x1A, () => this.RR_d());
            extendedInstructions[0x1B] = new ExtendedInstruction("RR e", 0x1B, () => this.RR_e());
            extendedInstructions[0x1C] = new ExtendedInstruction("RR h", 0x1C, () => this.RR_h());
            extendedInstructions[0x1D] = new ExtendedInstruction("RR l", 0x1D, () => this.RR_l());
            extendedInstructions[0x1E] = new ExtendedInstruction("RR hlp", 0x1E, () => this.RR_hlp());
            extendedInstructions[0x1F] = new ExtendedInstruction("RR a", 0x1F, () => this.RR_a());

            extendedInstructions[0x20] = new ExtendedInstruction("SLA b", 0x20, () => this.SLA_b());
            extendedInstructions[0x21] = new ExtendedInstruction("SLA c", 0x21, () => this.SLA_c());
            extendedInstructions[0x22] = new ExtendedInstruction("SLA d", 0x22, () => this.SLA_d());
            extendedInstructions[0x23] = new ExtendedInstruction("SLA e", 0x23, () => this.SLA_e());
            extendedInstructions[0x24] = new ExtendedInstruction("SLA h", 0x24, () => this.SLA_h());
            extendedInstructions[0x25] = new ExtendedInstruction("SLA l", 0x25, () => this.SLA_l());
            extendedInstructions[0x26] = new ExtendedInstruction("SLA hlp", 0x26, () => this.SLA_hlp());
            extendedInstructions[0x27] = new ExtendedInstruction("SLA a", 0x27, () => this.SLA_a());

            extendedInstructions[0x28] = new ExtendedInstruction("SRA b", 0x28, () => this.SRA_b());
            extendedInstructions[0x29] = new ExtendedInstruction("SRA c", 0x29, () => this.SRA_c());
            extendedInstructions[0x2A] = new ExtendedInstruction("SRA d", 0x2A, () => this.SRA_d());
            extendedInstructions[0x2B] = new ExtendedInstruction("SRA e", 0x2B, () => this.SRA_e());
            extendedInstructions[0x2C] = new ExtendedInstruction("SRA h", 0x2C, () => this.SRA_h());
            extendedInstructions[0x2D] = new ExtendedInstruction("SRA l", 0x2D, () => this.SRA_l());
            extendedInstructions[0x2E] = new ExtendedInstruction("SRA hlp", 0x2E, () => this.SRA_hlp());
            extendedInstructions[0x2F] = new ExtendedInstruction("SRA a", 0x2F, () => this.SRA_a());

            extendedInstructions[0x30] = new ExtendedInstruction("SWAP b", 0x30, () => this.SWAP_b());
            extendedInstructions[0x31] = new ExtendedInstruction("SWAP c", 0x31, () => this.SWAP_c());
            extendedInstructions[0x32] = new ExtendedInstruction("SWAP d", 0x32, () => this.SWAP_d());
            extendedInstructions[0x33] = new ExtendedInstruction("SWAP e", 0x33, () => this.SWAP_e());
            extendedInstructions[0x34] = new ExtendedInstruction("SWAP h", 0x34, () => this.SWAP_h());
            extendedInstructions[0x35] = new ExtendedInstruction("SWAP l", 0x35, () => this.SWAP_l());
            extendedInstructions[0x36] = new ExtendedInstruction("SWAP hlp", 0x36, () => this.SWAP_hlp());
            extendedInstructions[0x37] = new ExtendedInstruction("SWAP a", 0x37, () => this.SWAP_a());

            extendedInstructions[0x38] = new ExtendedInstruction("SRL b", 0x38, () => this.SRL_b());
            extendedInstructions[0x39] = new ExtendedInstruction("SRL c", 0x39, () => this.SRL_c());
            extendedInstructions[0x3A] = new ExtendedInstruction("SRL d", 0x3A, () => this.SRL_d());
            extendedInstructions[0x3B] = new ExtendedInstruction("SRL e", 0x3B, () => this.SRL_e());
            extendedInstructions[0x3C] = new ExtendedInstruction("SRL h", 0x3C, () => this.SRL_h());
            extendedInstructions[0x3D] = new ExtendedInstruction("SRL l", 0x3D, () => this.SRL_l());
            extendedInstructions[0x3E] = new ExtendedInstruction("SRL hlp", 0x3E, () => this.SRL_hlp());
            extendedInstructions[0x3F] = new ExtendedInstruction("SRL a", 0x3F, () => this.SRL_a());

            extendedInstructions[0x40] = new ExtendedInstruction("BIT 0 b", 0x40, () => this.BIT_0_b());
            extendedInstructions[0x41] = new ExtendedInstruction("BIT 0 c", 0x41, () => this.BIT_0_c());
            extendedInstructions[0x42] = new ExtendedInstruction("BIT 0 d", 0x42, () => this.BIT_0_d());
            extendedInstructions[0x43] = new ExtendedInstruction("BIT 0 e", 0x43, () => this.BIT_0_e());
            extendedInstructions[0x44] = new ExtendedInstruction("BIT 0 h", 0x44, () => this.BIT_0_h());
            extendedInstructions[0x45] = new ExtendedInstruction("BIT 0 l", 0x45, () => this.BIT_0_l());
            extendedInstructions[0x46] = new ExtendedInstruction("BIT 0 $(HL)", 0x46, () => this.BIT_0_hlp());
            extendedInstructions[0x47] = new ExtendedInstruction("BIT 0 a", 0x47, () => this.BIT_0_a());

            extendedInstructions[0x48] = new ExtendedInstruction("BIT 1 b", 0x48, () => this.BIT_1_b());
            extendedInstructions[0x49] = new ExtendedInstruction("BIT 1 c", 0x49, () => this.BIT_1_c());
            extendedInstructions[0x4A] = new ExtendedInstruction("BIT 1 d", 0x4A, () => this.BIT_1_d());
            extendedInstructions[0x4B] = new ExtendedInstruction("BIT 1 e", 0x4B, () => this.BIT_1_e());
            extendedInstructions[0x4C] = new ExtendedInstruction("BIT 1 h", 0x4C, () => this.BIT_1_h());
            extendedInstructions[0x4D] = new ExtendedInstruction("BIT 1 l", 0x4D, () => this.BIT_1_l());
            extendedInstructions[0x4E] = new ExtendedInstruction("BIT 1 $(HL)", 0x4E, () => this.BIT_1_hlp());
            extendedInstructions[0x4F] = new ExtendedInstruction("BIT 1 a", 0x4F, () => this.BIT_1_a());

            extendedInstructions[0x50] = new ExtendedInstruction("BIT 2 b", 0x50, () => this.BIT_2_b());
            extendedInstructions[0x51] = new ExtendedInstruction("BIT 2 c", 0x51, () => this.BIT_2_c());
            extendedInstructions[0x52] = new ExtendedInstruction("BIT 2 d", 0x52, () => this.BIT_2_d());
            extendedInstructions[0x53] = new ExtendedInstruction("BIT 2 e", 0x53, () => this.BIT_2_e());
            extendedInstructions[0x54] = new ExtendedInstruction("BIT 2 h", 0x54, () => this.BIT_2_h());
            extendedInstructions[0x55] = new ExtendedInstruction("BIT 2 l", 0x55, () => this.BIT_2_l());
            extendedInstructions[0x56] = new ExtendedInstruction("BIT 2 $(HL)", 0x56, () => this.BIT_2_hlp());
            extendedInstructions[0x57] = new ExtendedInstruction("BIT 2 a", 0x57, () => this.BIT_2_a());

            extendedInstructions[0x58] = new ExtendedInstruction("BIT 3 b", 0x58, () => this.BIT_3_b());
            extendedInstructions[0x59] = new ExtendedInstruction("BIT 3 c", 0x59, () => this.BIT_3_c());
            extendedInstructions[0x5A] = new ExtendedInstruction("BIT 3 d", 0x5A, () => this.BIT_3_d());
            extendedInstructions[0x5B] = new ExtendedInstruction("BIT 3 e", 0x5B, () => this.BIT_3_e());
            extendedInstructions[0x5C] = new ExtendedInstruction("BIT 3 h", 0x5C, () => this.BIT_3_h());
            extendedInstructions[0x5D] = new ExtendedInstruction("BIT 3 l", 0x5D, () => this.BIT_3_l());
            extendedInstructions[0x5E] = new ExtendedInstruction("BIT 3 $(HL)", 0x5E, () => this.BIT_3_hlp());
            extendedInstructions[0x5F] = new ExtendedInstruction("BIT 3 a", 0x5F, () => this.BIT_3_a());

            extendedInstructions[0x60] = new ExtendedInstruction("BIT 4 b", 0x60, () => this.BIT_4_b());
            extendedInstructions[0x61] = new ExtendedInstruction("BIT 4 c", 0x61, () => this.BIT_4_c());
            extendedInstructions[0x62] = new ExtendedInstruction("BIT 4 d", 0x62, () => this.BIT_4_d());
            extendedInstructions[0x63] = new ExtendedInstruction("BIT 4 e", 0x63, () => this.BIT_4_e());
            extendedInstructions[0x64] = new ExtendedInstruction("BIT 4 h", 0x64, () => this.BIT_4_h());
            extendedInstructions[0x65] = new ExtendedInstruction("BIT 4 l", 0x65, () => this.BIT_4_l());
            extendedInstructions[0x66] = new ExtendedInstruction("BIT 4 $(HL)", 0x66, () => this.BIT_4_hlp());
            extendedInstructions[0x67] = new ExtendedInstruction("BIT 4 a", 0x67, () => this.BIT_4_a());

            extendedInstructions[0x68] = new ExtendedInstruction("BIT 5 b", 0x68, () => this.BIT_5_b());
            extendedInstructions[0x69] = new ExtendedInstruction("BIT 5 c", 0x69, () => this.BIT_5_c());
            extendedInstructions[0x6A] = new ExtendedInstruction("BIT 5 d", 0x6A, () => this.BIT_5_d());
            extendedInstructions[0x6B] = new ExtendedInstruction("BIT 5 e", 0x6B, () => this.BIT_5_e());
            extendedInstructions[0x6C] = new ExtendedInstruction("BIT 5 h", 0x6C, () => this.BIT_5_h());
            extendedInstructions[0x6D] = new ExtendedInstruction("BIT 5 l", 0x6D, () => this.BIT_5_l());
            extendedInstructions[0x6E] = new ExtendedInstruction("BIT 5 $(HL)", 0x6E, () => this.BIT_5_hlp());
            extendedInstructions[0x6F] = new ExtendedInstruction("BIT 5 a", 0x6F, () => this.BIT_5_a());

            extendedInstructions[0x70] = new ExtendedInstruction("BIT 6 b", 0x70, () => this.BIT_6_b());
            extendedInstructions[0x71] = new ExtendedInstruction("BIT 6 c", 0x71, () => this.BIT_6_c());
            extendedInstructions[0x72] = new ExtendedInstruction("BIT 6 d", 0x72, () => this.BIT_6_d());
            extendedInstructions[0x73] = new ExtendedInstruction("BIT 6 e", 0x73, () => this.BIT_6_e());
            extendedInstructions[0x74] = new ExtendedInstruction("BIT 6 h", 0x74, () => this.BIT_6_h());
            extendedInstructions[0x75] = new ExtendedInstruction("BIT 6 l", 0x75, () => this.BIT_6_l());
            extendedInstructions[0x76] = new ExtendedInstruction("BIT 6 $(HL)", 0x76, () => this.BIT_6_hlp());
            extendedInstructions[0x77] = new ExtendedInstruction("BIT 6 a", 0x77, () => this.BIT_6_a());

            extendedInstructions[0x78] = new ExtendedInstruction("BIT 7 b", 0x78, () => this.BIT_7_b());
            extendedInstructions[0x79] = new ExtendedInstruction("BIT 7 c", 0x79, () => this.BIT_7_c());
            extendedInstructions[0x7A] = new ExtendedInstruction("BIT 7 d", 0x7A, () => this.BIT_7_d());
            extendedInstructions[0x7B] = new ExtendedInstruction("BIT 7 e", 0x7B, () => this.BIT_7_e());
            extendedInstructions[0x7C] = new ExtendedInstruction("BIT 7 h", 0x7C, () => this.BIT_7_h());
            extendedInstructions[0x7D] = new ExtendedInstruction("BIT 7 l", 0x7D, () => this.BIT_7_l());
            extendedInstructions[0x7E] = new ExtendedInstruction("BIT 7 $(HL)", 0x7E, () => this.BIT_7_hlp());
            extendedInstructions[0x7F] = new ExtendedInstruction("BIT 7 a", 0x7F, () => this.BIT_7_a());


            // Check we don't have repeat id's (we made a type in the table above)
            for (int i = 0; i < 255; i++)
            {
                var instruction = extendedInstructions[i];

                if (instruction == null) continue;

                if (instruction.OpCode != i)
                {
                    throw new ArgumentException("Bad extended opcode");
                }                                  

                for (int j = 0; j < 255; j++)
                {
                    if (i == j) continue;
                    var rhs = extendedInstructions[j];
                    if (rhs == null) continue;

                    if (instruction.OpCode == rhs.OpCode ||
                        instruction.Handler == rhs.Handler)
                    {
                        throw new ArgumentException("Dupe extended opcode");
                    }
                }
            }
        }
    }   
}
