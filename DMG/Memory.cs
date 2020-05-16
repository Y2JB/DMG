using System;
using System.Runtime.CompilerServices;
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


	public class Memory : IDmgMemoryReaderWriter
	{
		private IRom GameRom { get; set; }
		private IMemoryReader BootstrapRom { get; set; }
		public byte[] Ram { get; set; }
		public byte[] VRam { get; set; }
		public byte[] OamRam { get; set; }
		public byte[] Io { get; set; }
		public byte[] HRam { get; set; }

		Ppu ppu;
		Interrupts interrupts;

		byte bootRomMask = 0;

		DmgSystem dmg;

		// Memory on the Gameboy is mapped. A memory read to a specific address can read the cart, ram, IO, OAM etc depending on the address
		public Memory(DmgSystem dmg)
		{
			this.dmg = dmg;

			GameRom = dmg.rom;
			BootstrapRom = dmg.bootstrapRom;
			ppu = dmg.ppu;
			interrupts = dmg.interrupts;

			Ram = new byte[0x2000];
			VRam = new byte[0x2000];
			OamRam = new Byte[0x100];
			Io = new Byte[0x100];
			HRam = new Byte[0x80];		
		}


		public byte ReadByte(ushort address)
		{
			if (address <= 0xFF)
			{
				// When the system boots, the bootrom (scrolling Nintendo logo) is executed starting from address 0.
				// Bootrom is 256 bytes. At the end of the boot sequence, it writes to a special register to disable the boot rom page and
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

			// ROM space is 0x0000 - 0x7FFFF. To access anything over that requires bank switching (which most games use)
			// The lower half of the ROM is fixed and always available 0 - 0x3FFF (16K)
			// The upper half of ROM can be bank switched 
			if (address <= 0x7fff)
			{
				// Bank switching is done inside the ROM code 
				return GameRom.ReadByte(address);
			}
			else if ((address >= 0xA000) && (address <= 0xBFFF))
			{
				return GameRom.ReadRamBankByte((ushort)(address - 0xA000));
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
				// CPU cannot access vram during Pixel Transfer and if it does it gets 0xFF				
				if (ppu.Mode == PpuMode.PixelTransfer &&
					ppu.PpuAccessingVram == false)
				{
					return 0xFF;
				}
				return VRam[address - 0x8000];
			}
			else if (address >= 0xFE00 && address <= 0xFEFF)
			{
				// CPU cannot access OAM during OAM Search or Pixel Transfer and if it does it gets 0xFF
				if( (ppu.Mode == PpuMode.OamSearch || ppu.Mode == PpuMode.PixelTransfer) &&
					 ppu.PpuAccessingVram == false)
				{
					return 0xFF;
				}
				// OAM table read. Should only be accessed by PPU.
				return OamRam[address - 0xFE00];
			}			
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				return HRam[address - 0xFF80];
			}
			else if (address == 0xFF00)
			{
				// Joypad
				return dmg.pad.Register;
			}
			else if (address == 0xFF04)
			{
				// Divider register (RNG)
				return dmg.timer.DividerRegister;
			}
			else if (address == 0xFF07)
			{
				// Timer Controller register 
				return dmg.timer.TimerControllerRegister;
			}
			else if (address == 0xFF40)
			{
				return ppu.MemoryRegisters.LCDC.Register;
			}
			else if (address == 0xFF41)
			{
				return ppu.MemoryRegisters.STAT.Register;
			}
			else if (address == 0xFF42)
			{
				return ppu.MemoryRegisters.BgScrollY;
			}
			else if (address == 0xFF43)
			{
				return ppu.MemoryRegisters.BgScrollX;
			}
			else if (address == 0xFF44)
			{
				// Read only
				return ppu.CurrentScanline;
			}
			else if (address == 0xFF45)
			{
				return ppu.MemoryRegisters.STAT.LYC;
			}
			else if (address == 0xFF47)
			{
				return ppu.Palettes.BackgroundGbPalette;
			}
			else if (address == 0xFF48)
			{
				return ppu.Palettes.ObjGbPalette0;
			}
			else if (address == 0xFF49)
			{
				return ppu.Palettes.ObjGbPalette1;
			}
			else if (address == 0xFF4A)
			{
				return ppu.MemoryRegisters.WindowY;
			}
			else if (address == 0xFF4B)
			{
				return ppu.MemoryRegisters.WindowX;
			}
			else if (address == 0xFF0F)
			{
				return interrupts.InterruptFlags;
			}
			else if (address == 0xFFFF)
			{
				return interrupts.InterruptEnableRegister;
			}
			else if (address >= 0xFF10 && address < 0xFF40)
			{
				return dmg.spu.ReadRegister(address);
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{
				return Io[address - 0xFF00];
			}

			throw new ArgumentException("Invalid memory read");
		}

		public byte ReadByteAndCycle(ushort address)
		{			
			dmg.cpu.CycleCpu(1);
			return ReadByte(address);
		}

		public ushort ReadShort(ushort address)
		{
			// NB: Little Endian
			return (ushort)((ReadByte((ushort)(address+1)) << 8) | ReadByte(address));
		}

		public ushort ReadShortAndCycle(ushort address)
		{
			byte b1 = ReadByte((ushort)(address + 1));
			dmg.cpu.CycleCpu(1);
			byte b2 = ReadByte(address);
			dmg.cpu.CycleCpu(1);

			// NB: Little Endian
			return (ushort)((b1 << 8) | b2);
		}


		void DmaCopy(ushort destination, ushort source, int length)
		{
			// DMA midframe happens but can cause OAM data to change between oam search and pixel transfer which will crash the renderer.
			// If a midframe DMA happens, just mark OAM 'dirty' and skip rendering sprintes that line.
			if (ppu.Mode == PpuMode.OamSearch ||
				ppu.Mode == PpuMode.PixelTransfer)
			{
				ppu.OamDirty = true;
			}
			
			for (int i = 0; i < length; i++)
			{
				WriteByte((ushort)(destination + i), ReadByte((ushort)(source + i)));
			}
		}


		public void WriteByte(ushort address, byte value)
		{
			if (address < 0x8000)
			{
				// Rom Banking is triggered by 'writing' to the Rom
				GameRom.BankSwitch(address, value);
			}
			else if ((address >= 0xA000) && (address < 0xC000))
			{
				// Ram Bank on cart
				GameRom.WriteRamBankByte((ushort)(address - 0xA000), value);
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
				// CPU cannot access vram during Pixel Transfer and if it does it gets 0xFF
				if(ppu.Mode == PpuMode.PixelTransfer)
				{
					return;
				}

				VRam[address - 0x8000] = value;

				// Whenever we write to a tile in vram, update the rendering data. (Remember tile maps start at 0x9800)
				if (address <= 0x97ff)
				{
					// Calculate the start address of the tile
					int tileAddress = (address - (address % 16));
					Tile tile = ppu.GetTileByVRamAdrressFast((ushort)tileAddress);
					tile.Parse(VRam, tileAddress - 0x8000);
				}
			}
			else if (address >= 0xFE00 && address <= 0xFEFF)
			{
				// CPU cannot access OAM during OAM Search or Pixel Transfer and if it does it gets 0xFF
				if ((ppu.Mode == PpuMode.OamSearch || ppu.Mode == PpuMode.PixelTransfer) &&
					 ppu.PpuAccessingVram == false)
				{
					return;
				}

				// Writing to OAM Table 
				OamRam[address - 0xFE00] = value;
			}
			else if (address >= 0xFF80 && address <= 0xFFFE)
			{
				HRam[address - 0xFF80] = value;
			}						
			else if (address == 0xFF00)
			{
				// Joypad writes 2 bits to select if it is reading pad or buttons 			
				dmg.pad.Register = value;
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
			else if (address == 0xFF04)
			{
				// When writing to DIV, the whole counter is reset so the timer is also affected.
				// Writing any value to the divider register restes it 
				dmg.timer.DividerRegister = 0;
			}
			else if (address == 0xFF07)
			{
				// Timer Controller register, update if the value is changing  
				value &= 0x07;
				if ((dmg.timer.TimerControllerRegister & 0x03) != (value & 0x03))
				{
					dmg.timer.ResetTIMACycles();			
				}
				dmg.timer.TimerControllerRegister = value;
			}
			else if (address == 0xFF40)
			{
				ppu.MemoryRegisters.LCDC.Register = value;
			}
			else if (address == 0xFF41)
			{
				ppu.MemoryRegisters.STAT.Register = value;
			}
			else if (address == 0xFF42)
			{
				ppu.MemoryRegisters.BgScrollY = value;
			}
			else if (address == 0xFF43)
			{
				ppu.MemoryRegisters.BgScrollX = value;
			}
			else if (address == 0xFF44)
			{
				// reset scanline if the program tries to write to it
				//ppu.CurrentScanline = 0;
			}
			else if (address == 0xFF45)
			{
				ppu.MemoryRegisters.STAT.LYC = value;
			}
			else if (address == 0xFF46)
			{
				// OAM DMA
				DmaCopy(0xfe00, (ushort)(value << 8), 160);
			}
			else if (address == 0xFF47)
			{
				ppu.Palettes.BackgroundGbPalette = value;
			}
			else if (address == 0xFF48)
			{
				ppu.Palettes.ObjGbPalette0 = value;
			}
			else if (address == 0xFF49)
			{
				ppu.Palettes.ObjGbPalette1 = value;
			}
			else if (address == 0xFF4A)
			{
				ppu.MemoryRegisters.WindowY = value;
			}
			else if (address == 0xFF4B)
			{
				ppu.MemoryRegisters.WindowX = value;
			}
			else if (address == 0xFF50)
			{
				bootRomMask = value;
			}
			else if (address == 0xFF0F)
			{
				interrupts.InterruptFlags = value;
			}
			else if (address == 0xFFFF)
			{
				interrupts.InterruptEnableRegister = value;
			}
			else if (address >= 0xFF10 && address < 0xFF40)
			{
				dmg.spu.WriteRegister(address, value);
			}
			else if (address >= 0xFF00 && address <= 0xFF7F)
			{				
				// TODO: Move all the specific IO calls FF40 etc etc into this else if
				Io[address - 0xFF00] = value;
			}

			else
			{
				Console.WriteLine(String.Format("Invalid memory write addr 0x{0:X4} val 0x{1:X2}", address, value));
				throw new ArgumentException(String.Format("Invalid memory write addr 0x{0:X4} val 0x{1:X2}", address, value));
			}

		}


		public void WriteByteAndCycle(ushort address, byte value)
		{
			WriteByte(address, value);
			dmg.cpu.CycleCpu(1);
		}


		public void WriteShort(ushort address, ushort value)
		{
			WriteByte(address, (byte)(value & 0x00ff));
			WriteByte((ushort)(address + 1), (byte)((value & 0xff00) >> 8));
		}


		public void WriteShortAndCycle(ushort address, ushort value)
		{
			WriteByte(address, (byte)(value & 0x00ff));
			dmg.cpu.CycleCpu(1);
			WriteByte((ushort)(address + 1), (byte)((value & 0xff00) >> 8));
			dmg.cpu.CycleCpu(1);
		}



	}


}
