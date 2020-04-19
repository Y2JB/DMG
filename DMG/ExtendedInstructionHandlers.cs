using System;

namespace DMG
{
    public partial class Cpu
    {
        void TestBit(byte bit, byte value)
        {
            if ((value & bit) != 0) ClearFlag(Flags.Zero);
            else SetFlag(Flags.Zero);

            ClearFlag(Flags.Negative);
            SetFlag(Flags.HalfCarry);
        }

        void BIT_0_b()
        {
            TestBit(1 << 0, B);
        }

        void BIT_0_c()
        {
            TestBit(1 << 0, C);
        }

        void BIT_0_d()
        {
            TestBit(1 << 0, D);
        }

        void BIT_0_e()
        {
            TestBit(1 << 0, E);
        }

        void BIT_0_h()
        {
            TestBit(1 << 0, H);
        }

        void BIT_0_l()
        {
            TestBit(1 << 0, L);
        }

        void BIT_0_hlp()
        {
            TestBit(1 << 0, memory.ReadByte(HL));
        }

        void BIT_0_a()
        {
            TestBit(1 << 0, A);
        }

        void BIT_1_b()
        {
            TestBit(1 << 1, B);
        }

        void BIT_1_c()
        {
            TestBit(1 << 1, C);
        }

        void BIT_1_d()
        {
            TestBit(1 << 1, D);
        }

        void BIT_1_e()
        {
            TestBit(1 << 1, E);
        }

        void BIT_1_h()
        {
            TestBit(1 << 1, H);
        }

        void BIT_1_l()
        {
            TestBit(1 << 1, L);
        }

        void BIT_1_hlp()
        {
            TestBit(1 << 1, memory.ReadByte(HL));
        }

        void BIT_1_a()
        {
            TestBit(1 << 1, A);
        }

        void BIT_2_b()
        {
            TestBit(1 << 2, B);
        }

        void BIT_2_c()
        {
            TestBit(1 << 2, C);
        }

        void BIT_2_d()
        {
            TestBit(1 << 2, D);
        }

        void BIT_2_e()
        {
            TestBit(1 << 2, E);
        }

        void BIT_2_h()
        {
            TestBit(1 << 2, H);
        }

        void BIT_2_l()
        {
            TestBit(1 << 2, L);
        }

        void BIT_2_hlp()
        {
            TestBit(1 << 2, memory.ReadByte(HL));
        }

        void BIT_2_a()
        {
            TestBit(1 << 2, A);
        }

        void BIT_3_b()
        {
            TestBit(1 << 3, B);
        }

        void BIT_3_c()
        {
            TestBit(1 << 3, C);
        }

        void BIT_3_d()
        {
            TestBit(1 << 3, D);
        }

        void BIT_3_e()
        {
            TestBit(1 << 3, E);
        }

        void BIT_3_h()
        {
            TestBit(1 << 3, H);
        }

        void BIT_3_l()
        {
            TestBit(1 << 3, L);
        }

        void BIT_3_hlp()
        {
            TestBit(1 << 3, memory.ReadByte(HL));
        }

        void BIT_3_a()
        {
            TestBit(1 << 3, A);
        }

        void BIT_4_b()
        {
            TestBit(1 << 4, B);
        }

        void BIT_4_c()
        {
            TestBit(1 << 4, C);
        }

        void BIT_4_d()
        {
            TestBit(1 << 4, D);
        }

        void BIT_4_e()
        {
            TestBit(1 << 4, E);
        }

        void BIT_4_h()
        {
            TestBit(1 << 4, H);
        }

        void BIT_4_l()
        {
            TestBit(1 << 4, L);
        }

        void BIT_4_hlp()
        {
            TestBit(1 << 4, memory.ReadByte(HL));
        }

        void BIT_4_a()
        {
            TestBit(1 << 4, A);
        }

        void BIT_5_b()
        {
            TestBit(1 << 5, B);
        }

        void BIT_5_c()
        {
            TestBit(1 << 5, C);
        }

        void BIT_5_d()
        {
            TestBit(1 << 5, D);
        }

        void BIT_5_e()
        {
            TestBit(1 << 5, E);
        }

        void BIT_5_h()
        {
            TestBit(1 << 5, H);
        }

        void BIT_5_l()
        {
            TestBit(1 << 5, L);
        }

        void BIT_5_hlp()
        {
            TestBit(1 << 5, memory.ReadByte(HL));
        }

        void BIT_5_a()
        {
            TestBit(1 << 5, A);
        }

        void BIT_6_b()
        {
            TestBit(1 << 6, B);
        }

        void BIT_6_c()
        {
            TestBit(1 << 6, C);
        }

        void BIT_6_d()
        {
            TestBit(1 << 6, D);
        }

        void BIT_6_e()
        {
            TestBit(1 << 6, E);
        }

        void BIT_6_h()
        {
            TestBit(1 << 6, H);
        }

        void BIT_6_l()
        {
            TestBit(1 << 6, L);
        }

        void BIT_6_hlp()
        {
            TestBit(1 << 6, memory.ReadByte(HL));
        }

        void BIT_6_a()
        {
            TestBit(1 << 6, A);
        }

        void BIT_7_b()
        {
            TestBit(1 << 7, B);
        }

        void BIT_7_c()
        {
            TestBit(1 << 7, C);
        }

        void BIT_7_d()
        {
            TestBit(1 << 7, D);
        }

        void BIT_7_e()
        {
            TestBit(1 << 7, E);
        }

        void BIT_7_h()
        {
            TestBit(1 << 7, H);
        }

        void BIT_7_l()
        {
            TestBit(1 << 7, L);
        }

        void BIT_7_hlp()
        {
            TestBit(1 << 7, memory.ReadByte(HL));
        }

        void BIT_7_a()
        {
            TestBit(1 << 7, A);
        }
    }
}
