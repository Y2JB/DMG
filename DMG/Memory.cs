using System;


namespace DMG
{
	public class Memory : IMemoryReaderWriter
	{
		private IMemoryReader GameRom { get; set; }
		private IMemoryReader BootstrapRom { get; set; }
		private byte[] Ram { get; set; }

		private Random rnd = new Random();


		// Memory on the Gameboy is mapped. A memory read to a specific address can read the cart, ram, IO, OAM etc depending on the address
		public Memory(IMemoryReader bootstrap, IMemoryReader rom)
		{
			GameRom = rom;
			BootstrapRom = bootstrap;
			Ram = new byte[0x2000];
		}

		public byte ReadByte(ushort address)
		{
            if(address <= 0xFF)
            {
                // When the system boots, the bootrom (scrolling Nintendo logo) is executed starting from address 0.
                // Bootrom is 256 bytes. At the end of the boot sequence, it writ4es to a special register to disable the boot rom page and
                // this makes the first 256 bytes of the cart rom readable
                if(true)
                {
                    return BootstrapRom.ReadByte(address);
				}
                else
                {
					return GameRom.ReadByte(address);
				}
            }
			if (address <= 0x7fff)
				return GameRom.ReadByte(address);


			throw new ArgumentException("Invalid memory read");


			/*
			else if (address >= 0xA000 && address <= 0xBFFF)
				return 0; // sram[address - 0xa000];

			else if (address >= 0x8000 && address <= 0x9FFF)
				return 0; // vram[address - 0x8000];

			else if (address >= 0xC000 && address <= 0xDFFF)
				return 0; // wram[address - 0xc000];

			else if (address >= 0xE000 && address <= 0xFDFF)
				return 0; // wram[address - 0xe000];

			else if (address >= 0xFE00 && address <= 0xFEFF)
				return 0; // oam[address - 0xfe00];

			// Should return a div timer, but a random number works just as well for Tetris         XXXXXXX!!!!!!!!XXXXXXXX
			else if (address == 0xff04)
			{
				byte[] bb = new byte[1];
				rnd.NextBytes(bb);
				return bb[0];

			}


			//else if (address == 0xff40) return gpu.control;
			//else if (address == 0xff42) return gpu.scrollY;
			//else if (address == 0xff43) return gpu.scrollX;
			//else if (address == 0xff44) return gpu.scanline; // read only

			else if (address == 0xff00)
			{
				//if (!(io[0x00] & 0x20))
				//{
				//	return (unsigned char)(0xc0 | keys.keys1 | 0x10);
				//}

				//else if (!(io[0x00] & 0x10))
				//{
				//	return (unsigned char)(0xc0 | keys.keys2 | 0x20);
				//}

				//else if (!(io[0x00] & 0x30)) return 0xff;
				//else return 0;
			}

			//else if (address == 0xff0f) return interrupt.flags;
			//else if (address == 0xffff) return interrupt.enable;

			//else if (address >= 0xff80 && address <= 0xfffe)
			//	return hram[address - 0xff80];

			//else if (address >= 0xff00 && address <= 0xff7f)
			//	return io[address - 0xff00];

			return 0;
            */
		}

		public ushort ReadShort(ushort address)
		{
			// NB: Little Endian
			return (ushort)((ReadByte((ushort)(address+1)) << 8) | ReadByte(address));
		}


		public void WriteByte(ushort address, byte value)
		{
			if (address >= 0xC000 && address <= 0xDFFF)
				Ram[address - 0xc000] = value;

			else if (address >= 0xE000 && address <= 0xFDFF)
				Ram[address - 0xe000] = value;


            /*

			if (address >= 0xa000 && address <= 0xbfff)
				sram[address - 0xa000] = value;

			else if (address >= 0x8000 && address <= 0x9fff)
			{
				vram[address - 0x8000] = value;
				if (address <= 0x97ff) updateTile(address, value);
			}

			if (address >= 0xc000 && address <= 0xdfff)
				wram[address - 0xc000] = value;

			else if (address >= 0xe000 && address <= 0xfdff)
				wram[address - 0xe000] = value;

			else if (address >= 0xfe00 && address <= 0xfeff)
				oam[address - 0xfe00] = value;

			else if (address >= 0xff80 && address <= 0xfffe)
				hram[address - 0xff80] = value;

			else if (address == 0xff40) gpu.control = value;
			else if (address == 0xff42) gpu.scrollY = value;
			else if (address == 0xff43) gpu.scrollX = value;
			else if (address == 0xff46) copy(0xfe00, value << 8, 160); // OAM DMA

			else if (address == 0xff47)
			{ // write only
				int i;
				for (i = 0; i < 4; i++) backgroundPalette[i] = palette[(value >> (i * 2)) & 3];
			}

			else if (address == 0xff48)
			{ // write only
				int i;
				for (i = 0; i < 4; i++) spritePalette[0][i] = palette[(value >> (i * 2)) & 3];
			}

			else if (address == 0xff49)
			{ // write only
				int i;
				for (i = 0; i < 4; i++) spritePalette[1][i] = palette[(value >> (i * 2)) & 3];
			}

			else if (address >= 0xff00 && address <= 0xff7f)
				io[address - 0xff00] = value;

			else if (address == 0xff0f) interrupt.flags = value;
			else if (address == 0xffff) interrupt.enable = value;
            */
		}

		public void WriteShort(ushort address, ushort value)
		{
			WriteByte(address, (byte)(value & 0x00ff));
			WriteByte((ushort)(address + 1), (byte)((value & 0xff00) >> 8));
		}
	}
        
			    
}
