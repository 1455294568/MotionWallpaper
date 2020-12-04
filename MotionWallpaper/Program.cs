using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using SharpDX.Direct2D1;

namespace MotionWallpaper
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.Run(new AppContext());
        }
    }
}
