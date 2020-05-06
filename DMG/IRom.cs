using System;

namespace DMG
{
    public interface IRom : IMemoryReader
    {
        void BankSwitch(ushort address, byte value);
    }
}
