using System;
using System.Text;

namespace DMG
{

	// Gameboy Memory Map
	// 0000	3FFF	16KB ROM bank 00
	// 4000	7FFF	16KB ROM Bank 01~NN
	// 8000	9FFF	8KB Video RAM(VRAM)
	// A000 BFFF    8KB External RAM(cart ram extension)
	// C000 CFFF    4KB Work RAM(WRAM) bank 0	
	// D000 DFFF    4KB Work RAM(WRAM) bank 1~N
	// E000 FDFF    Mirror of C000 ~DDFF(ECHO RAM)
	// FE00 FE9F    Sprite attribute table(OAM)
	// FEA0 FEFF    Not Usable
	// FF00 FF7F    I/O Registers
	// FF80 FFFE    High RAM (HRAM) - Zero Page (takes fewer cycles to execute stuff), typically contains the stack
	// FFFF FFFF    Interrupts Enable Register(IE)


	public class Memory : IMemoryReaderWriter
	{
		private IMemoryReader GameRom { get; set; }
		private IMemoryReader BootstrapRom { get; set; }
		public byte[] Ram { get; set; }
		public byte[] VRam { get; set; }
		public byte[] Io { get; set; }
		public byte[] HRam { get; set; }

		Gpu gpu;
		Interupts interupts;
		//private Random rnd = new Random();
		byte bootRomMask = 0;

		DmgSystem dmg;

		// Memory on the Gameboy is mapped. A memory read to a specific address can read the cart, ram, IO, OAM etc depending on the address
		public Memory(DmgSystem dmg)
		{
			this.dmg = dmg;

			GameRom = dmg.rom;
			BootstrapRom = dmg.bootstrapRom;
			gpu = dmg.gpu;
			interupts = dmg.interupts;

			Ram = new byte[0x2000];
			VRam = new byte[0x2000];
			Io = new Byte[0x100];
			HRam = new Byte[0x80];
		}

		public byte ReadByte(ushort address)
		{
			if (address <= 0xFF)
			{
				// When the system boots, the bootrom (scrolling Nintendo logo) is executed starting from address 0.
				// Bootrom is 256 bytes. At the end of the boot sequence, it writ4es to a special register to disable the boot rom page and
				// this makes the first 256 bytes of the cart rom readable
				if (bootRomMask == 0)
				{
					return BootstrapRom.ReadByte(address);
				}
				else
				{
					return GameRom.ReadByte(address);
				}
			}


			if (address <= 0x7fff)
			{
				return GameRom.ReadByte(address);
			}
			else if (address >= 0xC000 && address <= 0xDFFF)
			{
				return Ram[address - 0xC000];
			}
			else if (address >= 0xE000 && address <= 0xFDFF)
			{
				return Ram[address - 0xE000];
			}
			else if (address >= 0x8000 && address <= 0x9FFF)
			{
				return VRam[address - 0x8000];
			}
			else if (address == 0xFF40)
			{
				return gpu.MemoryRegisters.LCDC.Register;
			}
			else if (address == 0xFF41)
			{
				return gpu.MemoryRegisters.STAT.Register;
			}
			else if (address == 0xFF42)
			{
				return gpu.MemoryRegisters.BgScrollY;
			}
			else if (address == 0xFF43)
			{
				return gpu.MemoryRegisters.BgScrollX;
			}
			else if (address == 0xFF44)
			{
				// Read only
				return gpu.CurrentScanline;
			}
			else if (address == 0xFF0F)
			{
				return interupts.InteruptFlags;
			}
			else if (address == 0xFFFF)
			{
				return interupts.InteruptEnable;
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				return HRam[address - 0xFF80];
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				return Io[address - 0xFF00];
			}


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

			throw new ArgumentException("Invalid memory read");
		}


		public ushort ReadShort(ushort address)
		{
			// NB: Little Endian
			return (ushort)((ReadByte((ushort)(address+1)) << 8) | ReadByte(address));
		}


		public void WriteByte(ushort address, byte value)
		{
			if (address >= 0x2000 && address <= 0x3FFF)
			{
				// TODO: Writing to this address space selects the lower 5 bits of the ROM Bank Number(in range 01 - 1Fh)
			}
			else if (address >= 0xC000 && address <= 0xDFFF)
			{
				Ram[address - 0xc000] = value;
			}
			else if (address >= 0xE000 && address <= 0xFDFF)
			{
				Ram[address - 0xe000] = value;
			}
			else if (address >= 0x8000 && address <= 0x9fff)
			{
				// TODO: model that CPU cannot access vram during Pixel Transfer and if it does it gets 0xFF

				// TODO: model that CPU cannot access OAM during OAM Search or Pixel Transfer and if it does it gets 0xFF

				VRam[address - 0x8000] = value;
				//if (address <= 0x97ff) updateTile(address, value);
			}
			else if (address >= 0xFE00 && address <= 0xFEFF)
			{
				// OAM write
			}
            // Serial Port output
			else if (address == 0xFF01)
			{
				dmg.Tty.Append(Encoding.ASCII.GetString(new[] { value }));
			}
			// Serial Port clock
			else if (address == 0xFF02)
			{
			}
			else if (address == 0xFF40)
			{
				gpu.MemoryRegisters.LCDC.Register = value;
			}
			else if (address == 0xFF41)
			{
				gpu.MemoryRegisters.STAT.Register = value;
			}
			else if (address == 0xFF42)
			{
				gpu.MemoryRegisters.BgScrollY = value;
			}
			else if (address == 0xFF43)
			{
				gpu.MemoryRegisters.BgScrollX = value;
			}
			else if (address == 0xFF50)
			{
				bootRomMask = value;
			}
			else if (address == 0xFF0F)
			{
				interupts.InteruptFlags = value;
			}
			else if (address == 0xFFFF)
			{
				interupts.InteruptEnable = value;
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				Io[address - 0xFF00] = value;
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				HRam[address - 0xFF80] = value;
			}
			else
			{
				Console.WriteLine(String.Format("Invalid memory write addr 0x{0:X4} val 0x{1:X2}", address, value));
				throw new ArgumentException(String.Format("Invalid memory write addr 0x{0:X4} val 0x{1:X2}", address, value));
			}




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
