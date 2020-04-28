using System;
namespace DMG
{
    public class Instruction
    {
        public string Name { get; }
        public byte OpCode { get; }
        public byte OperandLength { get; }
        public Action<ushort> Handler { get; }

        // These methods are only used when peeking the instruction, not when exectuing as then the data needs to be fetched 
        public bool HasOperand { get { return OperandLength != 0;  } }
        public ushort Operand { get; set; }

        public ExtendedInstruction extendedInstruction { get; set; }

        // NB: I'm not setting the handler as this is purely for debugging!
        public Instruction DeepCopy()
        {
            return new Instruction(this.Name, this.OpCode, this.OperandLength, null)
            {
                Operand = this.Operand,
                extendedInstruction = this.extendedInstruction
            };       
        }

        public Instruction(string name, byte opCode, byte operandLength, Action<ushort> handler)
        {
            Name = name;
            OpCode = opCode;
            OperandLength = operandLength;
            Handler = handler;
            extendedInstruction = null;
        }


        public override String ToString()
        {
            if (extendedInstruction != null)
            {
                return String.Format("0xCB - 0x{0:X}  ->  {1}", extendedInstruction.OpCode, extendedInstruction.Name);
            }
            else
            { 
                if (HasOperand)
                {
                    return String.Format("0x{0:X}  ->  {1} 0x{2:X}", OpCode, Name, Operand);

                }
                else
                {
                    return String.Format("0x{0:X}  ->  {1}", OpCode, Name);
                }
            }
        }
    }
}
