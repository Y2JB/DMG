using System;

using DMG;

namespace DmgDebugger
{
    // We bolt onto the instruction the state it had when it executed - pc, operand etc
    public class StoredInstruction : Instruction
    {
        // These methods are only used when peeking the instruction, not when exectuing as then the data needs to be fetched 
        public bool HasOperand { get { return OperandLength != 0; } }
        public ushort Operand { get; set; }
        public ushort PC { get; set; }

        // NB: I'm not setting the handler as this is purely for debugging!
        public static StoredInstruction DeepCopy(Instruction instruction)
        {
            return new StoredInstruction(instruction.Name, instruction.OpCode, instruction.OperandLength, null)
            {
                //Operand = instruction.Operand,
                //extendedInstruction = instruction.extendedInstruction
            };       
        }

        public static StoredInstruction DeepCopy(StoredInstruction instruction)
        {
            return new StoredInstruction(instruction.Name, instruction.OpCode, instruction.OperandLength, null)
            {
                Operand = instruction.Operand,
                PC = instruction.PC,
                extendedInstruction = instruction.extendedInstruction
            };
        }

        public StoredInstruction(string name, byte opCode, byte operandLength, Action<ushort> handler) : base(name, opCode, operandLength, null)
        {
        }

        public override String ToString()
        {
            if (extendedInstruction != null)
            {
                return String.Format("({0:x2})  ->  {1}", PC, extendedInstruction.Name);
            }
            else
            {
                if (HasOperand)
                {
                    return String.Format("({0:X2})  ->  {1} 0x{2:X2}", PC, Name, Operand);

                }
                else
                {
                    return String.Format("({0:X2})  ->  {1}", PC, Name);
                }
            }
        }
    }
}
