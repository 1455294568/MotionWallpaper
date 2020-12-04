using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotionWallpaper
{
    public static class ColorHelper
    {
        public static SharpDX.Mathematics.Interop.RawColor4 ToDXColor4(this System.Drawing.Color color)
        {
            return new SharpDX.Mathematics.Interop.RawColor4(color.R, color.G, color.B, color.A);
        }
    }
}
