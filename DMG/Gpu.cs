using System;
namespace DMG
{
    public class Gpu : IGpu
    {
        public enum Mode
        {
            HBlank,
            VBlank,
            Oam,
            Vram
        }

        public byte BgScrollX { get; set; }
        public byte BgScrollY { get; set; }

        public byte CurrentScanline { get; private set; }

        public byte[,] FrameBuffer { get; private set; }

        Mode mode { get; set; }

        UInt32 lastCpuTickCount;
        UInt32 elapsedTicks;

        public Gpu()
        {
            Reset();

            // Total BG size in VRam is 32x32 tiles
            // Viewport is 20x18 tiles
        }


        public void Reset()
        {
            mode = Mode.HBlank;
        }


        public void Step(UInt32 cpuTickCount)
        {
            // Here we monitor how many cycles the CPU has executed and we map the GPU state to match how the real hardware behaves. This allows
            // us to generate interupts at the right time


            // Track how many cycles the CPU has done since we last changed states
            elapsedTicks += (cpuTickCount - lastCpuTickCount);
            lastCpuTickCount = cpuTickCount;

            switch (mode)
            {
                case Mode.HBlank:                 
                    if (elapsedTicks >= 204)
                    {
                        CurrentScanline++;

                        if (CurrentScanline == 143)
                        {
                            //if (interrupt.enable & INTERRUPTS_VBLANK) interrupt.flags |= INTERRUPTS_VBLANK;

                            mode = Mode.VBlank;
                        }
                        else
                        {
                            mode = Mode.Oam;
                        }

                        // Don't lose any ticks, cannot set to zero
                        elapsedTicks -= 204;
                    }
                    break;

                case Mode.VBlank:
                    if (elapsedTicks >= 456)
                    {
                        CurrentScanline++;

                        if (CurrentScanline > 153)
                        {
                            CurrentScanline = 0;
                            mode = Mode.Oam;
                        }

                        elapsedTicks -= 456;
                    }
                    break;

                case Mode.Oam:
                    if (elapsedTicks >= 80)
                    {
                        mode = Mode.Vram;

                        elapsedTicks -= 80;
                    }
                    break;

                case Mode.Vram:
                    if (elapsedTicks >= 172)
                    {
                        mode = Mode.HBlank;

                        RenderScanline();

                        elapsedTicks -= 172;
                    }
                    
                    break;
            }
        }


        void RenderScanline()
        {

        }


    }
}
