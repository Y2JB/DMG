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
            instructions[0x04] = new Instruction("INC b", 0x04, 0, (v) => this.INC_b());
            instructions[0x06] = new Instruction("LD b n", 0x06, 1, (v) => this.LD_b_n((byte)v));
            instructions[0x0C] = new Instruction("INC c", 0x0C, 0, (v) => this.INC_c());
            instructions[0x0E] = new Instruction("LD c n", 0x0E, 1, (v) => this.LD_c_n((byte)v));
            instructions[0x11] = new Instruction("LD DE nn", 0x11, 2, (v) => this.LD_de_nn(v));
            instructions[0x14] = new Instruction("INC d", 0x14, 0, (v) => this.INC_d());
            instructions[0x16] = new Instruction("LD d n", 0x16, 1, (v) => this.LD_d_n((byte)v));
            instructions[0x1A] = new Instruction("LD A (de)", 0x1A, 0, (v) => this.LD_a_dep());
            instructions[0x1C] = new Instruction("INC e", 0x1C, 0, (v) => this.INC_e());
            instructions[0x1E] = new Instruction("LD e n", 0x1E, 1, (v) => this.LD_e_n((byte)v));
            instructions[0x20] = new Instruction("JR NZ n", 0x20, 1, (v) => this.JR_NZ_n((sbyte)v));
            instructions[0x21] = new Instruction("LD hl nn", 0x21, 2, (v) => this.LD_hl_nn(v));
            instructions[0x24] = new Instruction("INC h", 0x24, 0, (v) => this.INC_h());
            instructions[0x26] = new Instruction("LD h n", 0x26, 1, (v) => this.LD_h_n((byte)v));
            instructions[0x2C] = new Instruction("INC l", 0x2C, 0, (v) => this.INC_l());
            instructions[0x2E] = new Instruction("LD l n", 0x2E, 1, (v) => this.LD_l_n((byte)v));
            instructions[0x31] = new Instruction("LD sp nn", 0x31, 2, (v) => this.LD_sp_nn(v));
            instructions[0x32] = new Instruction("LDD hl a", 0x32, 0, (v) => this.LDD_hl_a());
            instructions[0x3C] = new Instruction("INC a", 0x3C, 0, (v) => this.INC_a());
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

            instructions[0xC3] = new Instruction("JP nn", 0xC3, 2, (v) => this.JP_nn(v));
            instructions[0xCB] = new Instruction("Extended Opcode", 0xCB, 1, (v) => this.extended((byte)v));
            instructions[0xCD] = new Instruction("CALL nn", 0xCd, 2, (v) => this.CALL_nn(v));
            instructions[0xE0] = new Instruction("LD (0xFF00 + n) a", 0xE0, 1, (v) => this.LD_ff_n_a((byte) v));
            instructions[0xE2] = new Instruction("LD (0xFF00 + C) a", 0xE2, 0, (v) => this.LD_ff_c_a());
            instructions[0xF3] = new Instruction("DI", 0xF3, 0, (v) => this.DI());


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
            extendedInstructions[0x40] = new ExtendedInstruction("bit 0 b", 0x40, () => this.BIT_0_b());
            extendedInstructions[0x41] = new ExtendedInstruction("bit 0 c", 0x41, () => this.BIT_0_c());
            extendedInstructions[0x42] = new ExtendedInstruction("bit 0 d", 0x42, () => this.BIT_0_d());
            extendedInstructions[0x43] = new ExtendedInstruction("bit 0 e", 0x43, () => this.BIT_0_e());
            extendedInstructions[0x44] = new ExtendedInstruction("bit 0 h", 0x44, () => this.BIT_0_h());
            extendedInstructions[0x45] = new ExtendedInstruction("bit 0 l", 0x45, () => this.BIT_0_l());
            extendedInstructions[0x46] = new ExtendedInstruction("bit 0 $(HL)", 0x46, () => this.BIT_0_hlp());
            extendedInstructions[0x47] = new ExtendedInstruction("bit 0 a", 0x47, () => this.BIT_0_a());

            extendedInstructions[0x48] = new ExtendedInstruction("bit 1 b", 0x48, () => this.BIT_1_b());
            extendedInstructions[0x49] = new ExtendedInstruction("bit 1 c", 0x49, () => this.BIT_1_c());
            extendedInstructions[0x4A] = new ExtendedInstruction("bit 1 d", 0x4A, () => this.BIT_1_d());
            extendedInstructions[0x4B] = new ExtendedInstruction("bit 1 e", 0x4B, () => this.BIT_1_e());
            extendedInstructions[0x4C] = new ExtendedInstruction("bit 1 h", 0x4C, () => this.BIT_1_h());
            extendedInstructions[0x4D] = new ExtendedInstruction("bit 1 l", 0x4D, () => this.BIT_1_l());
            extendedInstructions[0x4E] = new ExtendedInstruction("bit 1 $(HL)", 0x4E, () => this.BIT_1_hlp());
            extendedInstructions[0x4F] = new ExtendedInstruction("bit 1 a", 0x4F, () => this.BIT_1_a());

            extendedInstructions[0x50] = new ExtendedInstruction("bit 2 b", 0x50, () => this.BIT_2_b());
            extendedInstructions[0x51] = new ExtendedInstruction("bit 2 c", 0x51, () => this.BIT_2_c());
            extendedInstructions[0x52] = new ExtendedInstruction("bit 2 d", 0x52, () => this.BIT_2_d());
            extendedInstructions[0x53] = new ExtendedInstruction("bit 2 e", 0x53, () => this.BIT_2_e());
            extendedInstructions[0x54] = new ExtendedInstruction("bit 2 h", 0x54, () => this.BIT_2_h());
            extendedInstructions[0x55] = new ExtendedInstruction("bit 2 l", 0x55, () => this.BIT_2_l());
            extendedInstructions[0x56] = new ExtendedInstruction("bit 2 $(HL)", 0x56, () => this.BIT_2_hlp());
            extendedInstructions[0x57] = new ExtendedInstruction("bit 2 a", 0x57, () => this.BIT_2_a());

            extendedInstructions[0x58] = new ExtendedInstruction("bit 3 b", 0x58, () => this.BIT_3_b());
            extendedInstructions[0x59] = new ExtendedInstruction("bit 3 c", 0x59, () => this.BIT_3_c());
            extendedInstructions[0x5A] = new ExtendedInstruction("bit 3 d", 0x5A, () => this.BIT_3_d());
            extendedInstructions[0x5B] = new ExtendedInstruction("bit 3 e", 0x5B, () => this.BIT_3_e());
            extendedInstructions[0x5C] = new ExtendedInstruction("bit 3 h", 0x5C, () => this.BIT_3_h());
            extendedInstructions[0x5D] = new ExtendedInstruction("bit 3 l", 0x5D, () => this.BIT_3_l());
            extendedInstructions[0x5E] = new ExtendedInstruction("bit 3 $(HL)", 0x5E, () => this.BIT_3_hlp());
            extendedInstructions[0x5F] = new ExtendedInstruction("bit 3 a", 0x5F, () => this.BIT_3_a());

            extendedInstructions[0x60] = new ExtendedInstruction("bit 4 b", 0x60, () => this.BIT_4_b());
            extendedInstructions[0x61] = new ExtendedInstruction("bit 4 c", 0x61, () => this.BIT_4_c());
            extendedInstructions[0x62] = new ExtendedInstruction("bit 4 d", 0x62, () => this.BIT_4_d());
            extendedInstructions[0x63] = new ExtendedInstruction("bit 4 e", 0x63, () => this.BIT_4_e());
            extendedInstructions[0x64] = new ExtendedInstruction("bit 4 h", 0x64, () => this.BIT_4_h());
            extendedInstructions[0x65] = new ExtendedInstruction("bit 4 l", 0x65, () => this.BIT_4_l());
            extendedInstructions[0x66] = new ExtendedInstruction("bit 4 $(HL)", 0x66, () => this.BIT_4_hlp());
            extendedInstructions[0x67] = new ExtendedInstruction("bit 4 a", 0x67, () => this.BIT_4_a());

            extendedInstructions[0x68] = new ExtendedInstruction("bit 5 b", 0x68, () => this.BIT_5_b());
            extendedInstructions[0x69] = new ExtendedInstruction("bit 5 c", 0x69, () => this.BIT_5_c());
            extendedInstructions[0x6A] = new ExtendedInstruction("bit 5 d", 0x6A, () => this.BIT_5_d());
            extendedInstructions[0x6B] = new ExtendedInstruction("bit 5 e", 0x6B, () => this.BIT_5_e());
            extendedInstructions[0x6C] = new ExtendedInstruction("bit 5 h", 0x6C, () => this.BIT_5_h());
            extendedInstructions[0x6D] = new ExtendedInstruction("bit 5 l", 0x6D, () => this.BIT_5_l());
            extendedInstructions[0x6E] = new ExtendedInstruction("bit 5 $(HL)", 0x6E, () => this.BIT_5_hlp());
            extendedInstructions[0x6F] = new ExtendedInstruction("bit 5 a", 0x6F, () => this.BIT_5_a());

            extendedInstructions[0x70] = new ExtendedInstruction("bit 6 b", 0x70, () => this.BIT_6_b());
            extendedInstructions[0x71] = new ExtendedInstruction("bit 6 c", 0x71, () => this.BIT_6_c());
            extendedInstructions[0x72] = new ExtendedInstruction("bit 6 d", 0x72, () => this.BIT_6_d());
            extendedInstructions[0x73] = new ExtendedInstruction("bit 6 e", 0x73, () => this.BIT_6_e());
            extendedInstructions[0x74] = new ExtendedInstruction("bit 6 h", 0x74, () => this.BIT_6_h());
            extendedInstructions[0x75] = new ExtendedInstruction("bit 6 l", 0x75, () => this.BIT_6_l());
            extendedInstructions[0x76] = new ExtendedInstruction("bit 6 $(HL)", 0x76, () => this.BIT_6_hlp());
            extendedInstructions[0x77] = new ExtendedInstruction("bit 6 a", 0x77, () => this.BIT_6_a());

            extendedInstructions[0x78] = new ExtendedInstruction("bit 7 b", 0x78, () => this.BIT_7_b());
            extendedInstructions[0x79] = new ExtendedInstruction("bit 7 c", 0x79, () => this.BIT_7_c());
            extendedInstructions[0x7A] = new ExtendedInstruction("bit 7 d", 0x7A, () => this.BIT_7_d());
            extendedInstructions[0x7B] = new ExtendedInstruction("bit 7 e", 0x7B, () => this.BIT_7_e());
            extendedInstructions[0x7C] = new ExtendedInstruction("bit 7 h", 0x7C, () => this.BIT_7_h());
            extendedInstructions[0x7D] = new ExtendedInstruction("bit 7 l", 0x7D, () => this.BIT_7_l());
            extendedInstructions[0x7E] = new ExtendedInstruction("bit 7 $(HL)", 0x7E, () => this.BIT_7_hlp());
            extendedInstructions[0x7F] = new ExtendedInstruction("bit 7 a", 0x7F, () => this.BIT_7_a());


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
