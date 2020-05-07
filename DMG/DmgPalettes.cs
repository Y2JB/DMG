using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace DMG
{
    public class DmgPalettes
    {
        //Original Gameboy
        //static Color White = Color.FromArgb(0xFF, 155, 188, 15);
        //static Color LightGrey = Color.FromArgb(0xFF, 139, 172, 15);
        //static Color DarkGrey = Color.FromArgb(0xFF, 48, 98, 48);
        //static Color Black = Color.FromArgb(0xFF, 15, 56, 15);

        // Sharp
        //static Color White = Color.FromArgb(0xFF, 0xF5, 0xFA, 0xEF);
        //static Color LightGrey = Color.FromArgb(0xFF, 0x86, 0xC2, 0x70);
        //static Color DarkGrey = Color.FromArgb(0xFF, 0x2F, 0x69, 0x57); 
        //static Color Black = Color.FromArgb(0xFF, 0x0B, 0x19, 0x20); 

        // Soft               
        //static Color White = Color.FromArgb(0xFF, 0xE0, 0xE0, 0xAA);
        //static Color LightGrey = Color.FromArgb(0xB0, 0xB8, 0x7C);
        //static Color DarkGrey = Color.FromArgb(0xFF, 0x72, 0x82, 0x5B);
        //static Color Black = Color.FromArgb(0xFF, 0x39, 0x34, 0x17);
        
        // Sameboy               
        //static Color White = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF);
        //static Color LightGrey = Color.FromArgb(0xAA, 0xAA, 0xAA);
        //static Color DarkGrey = Color.FromArgb(0xFF, 0x85, 0x85, 0x85);
        //static Color Black = Color.FromArgb(0xFF, 0x00, 0x00, 0x00);

        // Zelda Dx Palette 
        static Color White = Color.FromArgb(0xFF, 0xFF, 0xFF, 0xB5);
        static Color LightGrey = Color.FromArgb(0xFF, 0x7B, 0xC6, 0x7B);
        static Color DarkGrey = Color.FromArgb(0xFF, 0x6B, 0x8C, 0x42); 
        static Color Black = Color.FromArgb(0xFF, 0x5A, 0x39, 0x21);

        Color[] GameboyPalette = new Color[4] { White, LightGrey, DarkGrey, Black };

        byte bg, obj0, obj1;

        // Gameboy palettes
        public byte BackgroundGbPalette { get { return bg; } set { bg = value; UpdatePalette(bgPalette, bg); } }
        public byte ObjGbPalette0 { get { return obj0; } set { obj0 = value; UpdatePalette(spritePalette0, obj0); } }
        public byte ObjGbPalette1 { get { return obj1; } set { obj1 = value; UpdatePalette(spritePalette1, obj1); } }

        // Game palettes, used for emu rendering 
        public Color[] BackgroundPalette { get { return bgPalette; } set { bgPalette = value; } }
        public Color[] ObjPalette0 { get { return spritePalette0; } set { spritePalette0 = value; } }
        public Color[] ObjPalette1 { get { return spritePalette1; } set { spritePalette1 = value; } }

        Color[] bgPalette = new Color[4];
        Color[] spritePalette0 = new Color[4];
        Color[] spritePalette1 = new Color[4];


        void UpdatePalette(Color[] palette, byte newGbPalette)
        {
            // Bits 0&1 tell you what pixel index is for col 0, 2&3 col 1, 4&5  col 2, 6&7 col 3
            // The value in those bits is an index into the set order which goes white, lgrey, dgrey, black
            int c0 = newGbPalette & 0x3;
            int c1 = (newGbPalette >> 2) & 0x3;
            int c2 = (newGbPalette >> 4) & 0x3;
            int c3 = (newGbPalette >> 6) & 0x3;
            palette[0] = GameboyPalette[c0];
            palette[1] = GameboyPalette[c1];
            palette[2] = GameboyPalette[c2];
            palette[3] = GameboyPalette[c3];
        }
    }
}
