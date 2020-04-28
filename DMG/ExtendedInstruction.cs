using System;
namespace DMG
{
    public class ExtendedInstruction
    {
        public string Name { get; }
        public byte OpCode { get; }
        public Action Handler { get; }

        public ExtendedInstruction(string name, byte opCode, Action handler)
        {
            Name = name;
            OpCode = opCode;
            Handler = handler;
        }

        public ExtendedInstruction DeepCopy()
        {
            return new ExtendedInstruction(this.Name, this.OpCode, null);           
        }
    }
}
