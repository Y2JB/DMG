using System;
using System.IO;
using System.Runtime.CompilerServices;
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

        const int MaxRamSize = 0x8000;

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

        string romFileName;

        public byte CurrentRomBank { get; private set;  }
        bool IsRomBanking { get; set; }

        // MBC1 carts had Max of 4 ram banks (8K per bank). NB: Only MCB1 supported Ram Banks
        public byte CurrentRamBank { get; private set; }
        bool ramBankingEnabled;
        byte[] ramBanks;

        public Rom(string fn)
        {
            romFileName = fn;

            romData = new MemoryStream(File.ReadAllBytes(fn)).ToArray();

            RomName = Encoding.UTF8.GetString(romData, RomNameOffset, 16).TrimEnd((Char)0);

            // RomBank 0 is classed as the first 16K of the ROM which is always available. Therefore the current ROM bank is always 1 or more.
            CurrentRomBank = 1;
            IsRomBanking = false;

            // Enough to cover the max ram they ever added to a cart (4 * 8K)
            ramBanks = new byte[MaxRamSize];
            CurrentRamBank = 0;
            ramBankingEnabled = false;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ushort ReadShort(ushort address)
        {
            // NB: Little Endian
            return (ushort)((ReadByte((ushort)(address+1)) << 8) | ReadByte(address));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte ReadRamBankByte(ushort address)
        {
            // Address has already been adjusted
            return ramBanks[address + (CurrentRamBank * 0x2000)];
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WriteRamBankByte(ushort address, byte value)
        {
            // Address has already been adjusted
            if (ramBankingEnabled)
            {
                ramBanks[address + (CurrentRamBank * 0x2000)] = value;
            }
        }


        bool IsMBC1Rom()
        {
            return (Type == RomType.MBC1 || Type == RomType.MBC1_Ram || Type == RomType.MBC1_Ram_Battery);
        }


        bool IsMBC2Rom()
        {
            return (Type == RomType.MBC2 || Type == RomType.MBC2_Battery);
        }


        // ROM & RAM bank switching 
        public void BankSwitch(ushort address, byte data)
        {           
            // RAM banking enabling
            if (address < 0x2000)
            {
                if (IsMBC1Rom() || IsMBC2Rom())
                {
                    EnableRamBanking(address, data);
                }
            }
            

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
                        SelectRAMBank(data);
                    }
                }
            }

            // Pick if we are doing ROM banking or RAM banking 
            else if ((address >= 0x6000) && (address < 0x8000))
            {
                if (IsMBC1Rom())
                {
                    UpdateRomRamMode(data);
                }
            }
        }


        void EnableRamBanking(ushort address, byte data)
        {
            if (IsMBC2Rom())
            {
                // MBC2 has the same logic as MBC1 except there is an additional clause that bit 4 of the address byte must be 0.
                if ((address | (1 << 4)) != 0) return;
            }

            byte testData = (byte) (data & 0x0F);
            if (testData == 0x0A)
            {
                ramBankingEnabled = true;
            }
            else if (testData == 0x00)
            {
                ramBankingEnabled = false;
            }
        }


        void SelectRAMBank(byte data)
        {
            CurrentRamBank = (byte) (data & 0x03);
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
            
            if (IsRomBanking)
            {
                CurrentRamBank = 0;
            }
        }


        public void LoadMbc1BatteryBackData()
        {
            return;
            try
            {
                using (FileStream fs = File.Open(Path.ChangeExtension(romFileName, "sav"), FileMode.Open))
                {
                    using (BinaryReader bw = new BinaryReader(fs))
                    {
                        bw.Read(ramBanks, 0, MaxRamSize);
                    }
                }
            }
            catch(FileNotFoundException)
            {
            }
        }


        public void SaveMbc1BatteryBackData()
        {          
            using (FileStream fs = File.Open(Path.ChangeExtension(romFileName, "sav"), FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(ramBanks, 0, MaxRamSize);
                }
            }
        }
    }
}
