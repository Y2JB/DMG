using System;
using System.Collections.Generic;

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
            A = 0x01;
            F = 0xb0;
            B = 0x00;
            C = 0x13;
            D = 0x00;
            E = 0xd8;
            H = 0x01;
            L = 0x4d;

            //PC = 0x100;   // Game code start
            PC = 0x00;      // Boot ROM
            SP = 0xFFFE;
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


        void RegisterInstructionHandlers()
        {
            instructions[0x00] = new Instruction("NOP", 0x00, 0, (v) => this.NOP());
            instructions[0x06] = new Instruction("LD b n", 0x06, 1, (v) => this.LD_b_n((byte)v));
            instructions[0x0E] = new Instruction("LD c n", 0x0E, 1, (v) => this.LD_c_n((byte)v));
            instructions[0x14] = new Instruction("INC d", 0x14, 0, (v) => this.INC_d());
            instructions[0x16] = new Instruction("LD d n", 0x16, 1, (v) => this.LD_d_n((byte)v));
            instructions[0x1E] = new Instruction("LD e n", 0x1E, 1, (v) => this.LD_e_n((byte)v));
            instructions[0x20] = new Instruction("JR NZ n", 0x20, 1, (v) => this.JR_NZ_n((byte)v));
            instructions[0x21] = new Instruction("LD hl nn", 0x21, 2, (v) => this.LD_hl(v));
            instructions[0x26] = new Instruction("LD h n", 0x26, 1, (v) => this.LD_h_n((byte)v));
            instructions[0x2E] = new Instruction("LD l n", 0x2E, 1, (v) => this.LD_l_n((byte)v));
            instructions[0x31] = new Instruction("LD sp nn", 0x31, 2, (v) => this.LD_sp_nn(v));
            instructions[0x32] = new Instruction("LDD hl a", 0x32, 0, (v) => this.LDD_hl_a());
            instructions[0x66] = new Instruction("LD h (hl)", 0x66, 0, (v) => this.LD_h_hlp());
            instructions[0xAF] = new Instruction("XOR a", 0xAF, 0, (v) => this.XOR_a());
            instructions[0xC3] = new Instruction("JP nn", 0xC3, 2, (v) => this.JP_nn(v));
            instructions[0xCB] = new Instruction("Extended Opcode", 0xCB, 1, (v) => this.extended((byte)v));


            // Check we don't have repeat id's (we made a type in the table above)
            for (int i = 0; i < 255; i++)
            {
                var instruction = instructions[i];
                if (instruction == null) continue;

                if (instruction.OpCode != i)
                {
                    throw new ArgumentException("Bad extended opcode");
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
