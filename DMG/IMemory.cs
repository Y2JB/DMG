using System;

namespace DMG
{
    public interface IMemoryReader
    {
        byte ReadByte(ushort address);
        ushort ReadShort(ushort address);
    }

    public interface IMemoryWriter
    {
        void WriteByte(ushort address, byte value);
        void WriteShort(ushort address, ushort value);
    }


    public interface IMemoryReaderWriter : IMemoryReader, IMemoryWriter
    {
    }


    public interface IDmgMemoryReaderWriter : IMemoryReaderWriter
    {
        public byte ReadByteAndCycle(ushort address);
        public ushort ReadShortAndCycle(ushort address);

        public void WriteByteAndCycle(ushort address, byte value);
        public void WriteShortAndCycle(ushort address, ushort value);

        public byte[] Ram { get; }
        public byte[] VRam { get; }
        public byte[] Io { get; }
        public byte[] HRam { get; }
    }

}
