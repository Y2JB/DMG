using System;
using System.Collections.Generic;

namespace DMG
{
    // Custom 8-bit Sharp LR35902 at 4.19 MHz
    public class Cpu
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

        private IMemoryReader memory;

        private Instruction[] instructions = new Instruction[256];

        public bool IsHalted { get { return false; } }

        public Cpu(IMemoryReader memory)
        {
            this.memory = memory;

            RegisterInstructionHandlers();

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


        private bool FlagsIsZero()
        {
            return (F & (byte)(Flags.Zero)) != 0;
        }


        private bool FlagsIsNegative()
        {
            return (F & (byte)(Flags.Negative)) != 0;
        }


        private bool FlagsIsCarry()
        {
            return (F & (byte)(Flags.Carry)) != 0;
        }


        private bool FlagsIsHalfCarry()
        {
            return (F & (byte)(Flags.HalfCarry)) != 0;
        }


        private void SetFlag(Flags flag)
        {
            F |= (byte)flag;
        }


        private void ClearFlag(Flags flag)
        {
            F &= (byte)~((byte)flag);
        }


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
            //Memory.
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


        private void RegisterInstructionHandlers()
        {
            instructions[0x00] = new Instruction("NOP", 0x00, 0, (v) => this.NOP());
            instructions[0x06] = new Instruction("LD b n", 0x06, 1, (v) => this.LD_b_n((byte)v));
            instructions[0x0E] = new Instruction("LD c n", 0x0E, 1, (v) => this.LD_c_n((byte)v));
            instructions[0x14] = new Instruction("INC d", 0x14, 0, (v) => this.INC_d());
            instructions[0x16] = new Instruction("LD d n", 0x16, 1, (v) => this.LD_d_n((byte)v));
            instructions[0x1E] = new Instruction("LD e n", 0x1E, 1, (v) => this.LD_e_n((byte)v));
            instructions[0x21] = new Instruction("LD hl nn", 0x21, 2, (v) => this.LD_hl(v));
            instructions[0x26] = new Instruction("LD h n", 0x26, 1, (v) => this.LD_h_n((byte)v));
            instructions[0x2E] = new Instruction("LD l n", 0x2E, 1, (v) => this.LD_l_n((byte)v));
            instructions[0x31] = new Instruction("LD sp nn", 0x31, 2, (v) => this.LD_sp_nn(v));
            instructions[0x32] = new Instruction("LDD hl a", 0x32, 0, (v) => this.LDD_hl_a());
            instructions[0xAF] = new Instruction("XOR a", 0xAF, 0, (v) => this.XOR_a());
            instructions[0xC3] = new Instruction("JP nn", 0xC3, 2, (v) => this.JP_nn(v));


            // Check we don't have repeat id's (we made a type in the table above)
            for(int i=0; i < 255; i++)
            {
                var instruction = instructions[i];
                if (instruction == null) continue;

                for (int j = 0; j < 255; j++)
                {
                    if (i == j) continue;
                    var rhs = instructions[j];
                    if (rhs == null) continue;

                    if (instruction.OpCode == rhs.OpCode)
                    {
                        throw new ArgumentException("Bad opcode");
                    }
                }
            }
        }
    }
}
