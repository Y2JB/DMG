using System;
using System.Collections.Generic;
using System.Text;

namespace DMG
{
    // Sprite info
    public class OamEntry
    {
        public byte Y { get { return memory.ReadByte(OamTableAddress); } }
        public byte X { get { return memory.ReadByte((ushort) (OamTableAddress + 1)); } }

        public byte TileIndex { get { return memory.ReadByte((ushort)(OamTableAddress + 2)); } }

        public byte Flags { get { return memory.ReadByte((ushort)(OamTableAddress + 3)); } }


        // 0=OBJ Above BG, 1=OBJ Behind BG color 1-3
        public byte ObjPriority { get { return (Flags & (byte)(1 << 7)) == 0 ? (byte)0 : (byte)1; } }

        // (0=Normal, 1=Vertically mirrored)
        public bool YFlip { get { return (Flags & (byte)(1 << 6)) != 0; } }

        // B(0=Normal, 1=Vertically mirrored)
        public bool XFlip { get { return (Flags & (byte)(1 << 5)) != 0; } }

        // 0=OBP0, 1=OBP1
        public byte PaletteNumber { get { return (Flags & (byte)(1 << 4)) == 0 ? (byte)0 : (byte)1; } }

        public ushort OamTableAddress { get; private set; }

        IMemoryReader memory;

        public OamEntry(ushort memoryAddress, IMemoryReader memory)
        {
            OamTableAddress = memoryAddress;
            this.memory = memory;
        }
    }
}
