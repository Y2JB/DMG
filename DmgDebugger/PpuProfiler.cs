using DMG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace DmgDebugger
{
    public class PpuProfiler
    {
        DmgSystem dmg;

        public Dictionary<UInt32, PpuFrameMetaData> FrameHistory { get; private set; }


        public PpuProfiler(DmgSystem dmg)
        {
            this.dmg = dmg;

            FrameHistory = new Dictionary<UInt32, PpuFrameMetaData>();

            dmg.OnFrameStart = OnStartFrame;
            dmg.OnFrameEnd = OnEndFrame;
            dmg.OnOamSearchComplete = OnOamSearchComplete;
        }


        public void OnStartFrame(UInt32 frameNumber)
        {
            FrameHistory.Add(frameNumber, new PpuFrameMetaData(dmg.cpu.Ticks));
        }


        public void OnEndFrame(UInt32 frameNumber, bool partialFrame)
        {
            //if(FrameHistory.ContainsKey(frameNumber) == false)
            //{
            //    return;
            //}

            PpuFrameMetaData fd = FrameHistory[frameNumber];
            fd.FrameEndTick = dmg.cpu.Ticks;
            fd.PartialFrame = partialFrame;

            // Don't let the history grow and grow but always have at least 100 frames of data
            if (FrameHistory.Count > 150)
            {
                FrameHistory = FrameHistory.Where(kvp => kvp.Key >= (frameNumber - 100)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }
        }


        public void OnOamSearchComplete(UInt32 frame, UInt32 line, List<OamEntry> oamSearchResults)
        {
            var lineData = new PpuLineMetaData();
            lineData.OamCount = oamSearchResults.Count;
            for (int i =0; i < oamSearchResults.Count; i++)
            {
                var spr = oamSearchResults[i];

                lineData.oamPositions[i].X = spr.X;
                lineData.oamPositions[i].Y = spr.Y;
            }

            FrameHistory[frame].LineMetaData[line] = lineData;
        }

    }

    public class PpuFrameMetaData
    {
        // Measure in cpu ticks (m cycles)
        public UInt32 FrameStartTick { get; set; }
        public UInt32 FrameEndTick { get; set; }
        public UInt32 FrameTickLength { get { return FrameEndTick - FrameStartTick; } }

        // Was the frame interrupted by an lcd disable?
        public bool PartialFrame { get; set; }

        public PpuLineMetaData[] LineMetaData{ get; private set; }

        public PpuFrameMetaData(UInt32 stratFrameTicks)
        {
            FrameStartTick = stratFrameTicks;
            LineMetaData = new PpuLineMetaData[Ppu.Screen_Y_Resolution];
        }
    }


    // Lightweioght line data
    public class PpuLineMetaData
    {
        public int OamCount { get; set; }
        public Point[] oamPositions { get; private set; }

        public PpuLineMetaData()
        {
            oamPositions = new Point[Ppu.Max_Sprites];
        }
    }
    
}