using System;
namespace DMG
{
    public class Instruction
    {
        public string Name { get; }
        public byte OpCode { get; }
        public byte OperandLength { get; }
        public Action<ushort> Handler { get; }

        // These 2 operand unftions are only used when peeking the instruction, not when exectuing as then the data needs to be fetched 
        public bool HasOperand { get { return OperandLength != 0;  } }
        public ushort Operand { get; set; }

        // NB: I'm not setting the handler as this is purely for debugging!
        public Instruction DeepCopy()
        {
            return new Instruction(this.Name, this.OpCode, this.OperandLength, null)
            {
                Operand = this.Operand
            };
            
        }

        public Instruction(string name, byte opCode, byte operandLength, Action<ushort> handler)
        {
            Name = name;
            OpCode = opCode;
            OperandLength = operandLength;
            Handler = handler;
        }


        public override String ToString()
        {
            return String.Format("0x{0:X2}  ->  {1}", OpCode, Name);
        }
    }
}
