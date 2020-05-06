using System;
using System.IO;
using System.Text;

namespace DMG
{
    public class Rom : IRom
    {
        private byte[] romData;
        private readonly int RomNameOffset = 0x134;
        private readonly int RomBankingOffset = 0x147;

        public string RomName { get; private set; }

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

        public byte CurrentRomBank { get; private set;  }
        bool IsRomBanking { get; set; }

        public Rom(string fn)
        {
            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();

            RomName = Encoding.UTF8.GetString(romData, RomNameOffset, 16).TrimEnd((Char)0);

            // RomBank 0 is classed as the first 16K of the ROM which is always available. Therefore the current ROM bank is always 1 or more.
            CurrentRomBank = 1;
            IsRomBanking = false;
        }


        public byte ReadByte(ushort address)
        {
            if(address < 0x4000)
            {
                return romData[address];
            }
            // Read from ROM memory bank
            else if ((address >= 0x4000) && (address <= 0x7FFF))
            {
                ushort newAddress = (ushort) (address - 0x4000);
                return romData[newAddress + (CurrentRomBank * 0x4000)];
            }

            throw new ArgumentException("Invalid ROM read address");
        }


        public ushort ReadShort(ushort address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((ushort)(address+1)) << 8) | ReadByte(address));
        }


        bool IsMBC1Rom()
        {
            return (Type == RomType.MBC1 || Type == RomType.MBC1_Ram || Type == RomType.MBC1_Ram_Battery);
        }


        bool IsMBC2Rom()
        {
            return (Type == RomType.MBC2 || Type == RomType.MBC2_Battery);
        }

        public void BankSwitch(ushort address, byte data)
        {
            /*
            // do RAM enabling
            if (address < 0x2000)
            {
                if (m_MBC1 || m_MBC2)
                {
                    DoRamBankEnable(address, data);
                }
            }
            */

            // do ROM bank change
            if ((address >= 0x200) && (address < 0x4000))
            {
                if (IsMBC1Rom() || IsMBC2Rom())
                {
                    WriteRomBankLowBits(data);
                }
            }

            // do ROM or RAM bank change
            else if ((address >= 0x4000) && (address < 0x6000))
            {
                // there is no rambank in mbc2 so always use rambank 0
                if (IsMBC1Rom())
                {
                    if (IsRomBanking)
                    {
                        WriteRomBankHighBits(data);
                    }
                    else
                    {
                       // DoRAMBankChange(value);
                    }
                }
            }

            // this will change whether we are doing ROM banking
            // or RAM banking with the above if statement
            else if ((address >= 0x6000) && (address < 0x8000))
            {
                if (IsMBC1Rom())
                {
                    UpdateRomRamMode(data);
                }
            }
        }


        void WriteRomBankLowBits(byte data)
        {
            if (IsMBC2Rom())
            {
                CurrentRomBank = (byte) (data & 0x0F);
                if (CurrentRomBank == 0)
                {
                    CurrentRomBank++;
                }
                return;
            }

            byte lower5 = (byte) (data & 0x1F);

            // Clear lower 5 bits
            CurrentRomBank &= 0xE0;

            CurrentRomBank |= lower5;

            if (CurrentRomBank == 0) CurrentRomBank++;
        }


        void WriteRomBankHighBits(byte data)
        {
            // Clear top 3 bits
            CurrentRomBank &= 0x1F;

            // Clear bottom 5 bits of the data
            data &= 0xE0;

            CurrentRomBank |= data;
            if (CurrentRomBank == 0) CurrentRomBank++;
        }


        void UpdateRomRamMode(byte data)
        {
            byte newData = (byte) (data & 0x01);
            IsRomBanking = (newData == 0) ? true : false;
            
            //if (IsRomBanking)
            //    m_CurrentRAMBank = 0;
        }
    }
}
