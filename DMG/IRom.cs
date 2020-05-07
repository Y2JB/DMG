using System;

namespace DMG
{
    public interface IRom : IMemoryReader
    {
        void BankSwitch(ushort address, byte value);
        
        byte ReadRamBankByte(ushort address);
        void WriteRamBankByte(ushort address, byte value);
    }
}
