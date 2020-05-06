using System;
using System.IO;
using System.Text;

namespace DMG
{
    public class Rom : IMemoryReader
    {
        private byte[] romData;
        private readonly int RomNameOffset = 0x134;
        private readonly int RomBankingOffset = 0x147;

        public string RomName { get; private set; }

//#define ROM_OFFSET_NAME 0x134
//#define ROM_OFFSET_TYPE 0x147
//#define ROM_OFFSET_ROM_SIZE 0x148
//#define ROM_OFFSET_RAM_SIZE 0x149
         
        public enum RomType
        {
            UnSupported = -1,
            RomOnly = 0x00,
            MBC1    = 0x01,
            MBC1_Ram = 0x02,
            MBC1_Ram_Battery = 0x03,
            MBC2 = 0x05,
            MBC2_Battery = 0x06
        }

        public RomType Type 
        { 
            get 
            {
                RomType romType;

                byte type = romData[RomBankingOffset];

                if (type > 6)
                {
                    romType = RomType.UnSupported;
                }
                else
                {
                    romType = (RomType)type;
                }
                return romType;
            } 
        }

        public Rom(string fn)
        {
            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();

            RomName = Encoding.UTF8.GetString(romData, RomNameOffset, 16).TrimEnd((Char)0);
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
