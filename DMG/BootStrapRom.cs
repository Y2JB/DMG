using System;
using System.IO;
using System.Text;

namespace DMG
{
    public class BootRom : IMemoryReader
    {
        private byte[] romData;

        public BootRom(string fn)
        {
            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();
        }


        public byte ReadByte(ushort address)
        {
            return romData[address];
        }


        public ushort ReadShort(ushort address)
        {
            // NB: Little Endian
            return (ushort)((romData[address+1] << 8) | romData[address]);
        }
    }
}
