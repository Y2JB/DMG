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
        public byte[] Ram { get; }
        public byte[] VRam { get; }
        public byte[] Io { get; }
        public byte[] HRam { get; }
    }
}
