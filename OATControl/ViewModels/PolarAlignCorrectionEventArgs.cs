using System;

namespace OATControl.ViewModels
{
    public class PolarAlignCorrectionEventArgs : EventArgs
    {
        public float AltAdjust { get; set; }
        public float AzAdjust { get; set; }

        public PolarAlignCorrectionEventArgs(float altAdjust, float azAdjust)
        {
            AltAdjust = altAdjust;
            AzAdjust = azAdjust;
        }
    }
} 