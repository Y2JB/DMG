using System;
namespace DMG
{
    public class Instruction
    {
        public string Name { get; }
        public byte OpCode { get; }
        public byte OperandLength { get; }
        public Action<ushort> Handler { get; }

        public Instruction(string name, byte opCode, byte operandLength, Action<ushort> handler)
        {
            Name = name;
            OpCode = opCode;
            OperandLength = operandLength;
            Handler = handler;
        }
    }
}
