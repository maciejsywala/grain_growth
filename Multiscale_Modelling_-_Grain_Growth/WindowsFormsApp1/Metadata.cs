using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public static class Metadata
    {
        public enum PixelState
        {
            Default = 0,
            Blue,
            Green,
            Red,
            Orange,
            Pink,
            Purple,
            Aqua,
            DualPhase,
            Inclusion,
            Border
        }

        public enum InclusionType
        {
            Square,
            Circular
        }

        public enum StructureType
        {
            Substructure,
            DualPhase
        }
    }
}
