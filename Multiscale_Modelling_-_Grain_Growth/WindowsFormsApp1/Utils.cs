using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public static class Utils
    {
        public static Brush GetColorBaseOnPixelState(Metadata.PixelState state)
        {
            switch (state)
            {
                case Metadata.PixelState.Default:
                {
                    return Brushes.White;
                }
                case Metadata.PixelState.Red:
                {
                    return Brushes.Red;
                }
                case Metadata.PixelState.Inclusion:
                {
                    return Brushes.LawnGreen;
                }
                case Metadata.PixelState.Blue:
                {
                    return Brushes.Blue;
                }
                case Metadata.PixelState.Green:
                {
                    return Brushes.Green;
                }
                case Metadata.PixelState.Orange:
                {
                    return Brushes.Orange;
                }
                case Metadata.PixelState.Purple:
                {
                    return Brushes.Purple;
                }
                case Metadata.PixelState.Pink:
                {
                    return Brushes.Pink;
                }
                case Metadata.PixelState.Aqua:
                {
                    return Brushes.Aqua;
                }
                case Metadata.PixelState.Border:
                {
                    return Brushes.Black;
                }
                case Metadata.PixelState.DualPhase:
                {
                    return Brushes.Yellow;
                }
                default:
                {
                    return Brushes.White;
                }
            }
        }
    }
}
