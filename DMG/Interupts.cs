using System;
namespace DMG
{
    public class Interupts
    {
        public bool InteruptsMasterEnable { get; set; }


        // I think the next two fields are how it works!!

        // Bitfield to tell us which interupts are enabled
        public byte InteruptEnable { get; set;  }

        // https://gbdev.gg8.se/wiki/articles/Interrupts
        // When an interrupt signal changes from low to high, then the corresponding bit in the IF register becomes set.
        public byte InteruptFlags { get; set; }

        public Interupts()
        {
        }
    }
}
