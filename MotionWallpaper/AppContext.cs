using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MotionWallpaper.Models;

namespace MotionWallpaper
{
    public class AppContext : ApplicationContext
    {
        private static bool isDxInit;

        private Form mainForm;

        private WindowRenderTarget renderTarget;

        private Timer refreshTimer;

        private Timer generateTimer;

        private Random r = new Random();

        private int startY = 0;

        private bool reStart = true;

        private List<Entity> entities;

        private string[] randomStr = { "￥", "$", "888", "666" };

        public Size Size
        {
            get { return Screen.PrimaryScreen.Bounds.Size; }
        }

        public int Width
        {
            get { return Screen.PrimaryScreen.Bounds.Width; }
        }

        public int Height
        {
            get { return Screen.PrimaryScreen.Bounds.Height; }
        }

        public AppContext()
        {
            entities = new List<Entity>();

            refreshTimer = new Timer();
            generateTimer = new Timer();

            NotifyIcon icon = new NotifyIcon();
            icon.Icon = global::MotionWallpaper.Properties.Resources.logo;
            icon.Text = "Test";
            ContextMenu menu = new ContextMenu();
            MenuItem exitMenu = new MenuItem();
            exitMenu.Text = "exit";
            exitMenu.Click += ExitMenu_Click;
            menu.MenuItems.Add(exitMenu);
            icon.ContextMenu = menu;
            icon.Visible = true;

            InitWin();
            InitDeviceContext();

            refreshTimer.Tick += Timer_Tick;
            refreshTimer.Enabled = true;
            refreshTimer.Interval = 15;

            generateTimer.Tick += GenerateTimer_Tick;
            generateTimer.Interval = 100;
            generateTimer.Start();

            Application.ApplicationExit += Application_ApplicationExit;
        }

        private void ExitMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            ReleaseDeviceContext();
            CloseWin();

        }

        private void GenerateTimer_Tick(object sender, EventArgs e)
        {
            int index = r.Next(randomStr.Length);
            entities.RemoveAll(s => s.Location.Y >= Screen.PrimaryScreen.WorkingArea.Height);
            Entity entity = new Entity();
            entity.Value = randomStr[index];
            entity.Location = new PointF(r.Next(Screen.PrimaryScreen.WorkingArea.Width), 0);
            entities.Add(entity);
        }


        private void Timer_Tick(object sender, EventArgs e)
        {
            var _entities = new List<Entity>(entities);
            if (renderTarget != null)
            {
                if (reStart)
                {
                    startY = 0;
                    reStart = false;
                }
                if (startY > Height)
                {
                    reStart = true;
                }
                startY++;
                SharpDX.DirectWrite.Factory fc = new SharpDX.DirectWrite.Factory();
                TextFormat format = new TextFormat(fc, "微软雅黑", 28)
                {
                    TextAlignment = TextAlignment.Center,
                    ParagraphAlignment = ParagraphAlignment.Center
                };
                TextLayout layout = new TextLayout(fc, DateTime.Now.ToString(), format, Size.Width, Size.Height);
                SolidColorBrush textBrush = new SolidColorBrush(renderTarget, System.Drawing.Color.White.ToDXColor4());
                try
                {
                    renderTarget.BeginDraw();
                    renderTarget.Clear(ColorHelper.ToDXColor4(System.Drawing.Color.Black));
                    //for (int i = 0; i < 10; i++)
                    //{
                    //    SharpDX.RectangleF rect2 = new SharpDX.RectangleF(r.Next(this.Size.Width), startY, 100, 100);
                    //    RoundedRectangle roundedRectangle = new RoundedRectangle();
                    //    roundedRectangle.RadiusX = 4;
                    //    roundedRectangle.RadiusY = 4;
                    //    roundedRectangle.Rect = rect2;
                    //    var fillBrush2 = new SharpDX.Direct2D1.SolidColorBrush(renderTarget, ColorHelper.ToDXColor4(System.Drawing.Color.White));
                    //    renderTarget.DrawRoundedRectangle(roundedRectangle, fillBrush2);
                    //}

                    //SharpDX.RectangleF textrect = new SharpDX.RectangleF(0f, 0f, this.Size.Width, this.Size.Height);

                    renderTarget.DrawTextLayout(new RawVector2(0f, 0f), layout, textBrush);

                    for (int i = 0; i < _entities.Count; i++)
                    {
                        var _loc = _entities[i].Location;
                        if (entities.Count() > i)
                        {
                            entities[i].Location = new PointF(_loc.X, _loc.Y + 9);
                        }
                        TextLayout _layout = new TextLayout(fc, _entities[i].Value, format, 30, 30);
                        renderTarget.DrawTextLayout(new RawVector2(_entities[i].Location.X, _entities[i].Location.Y), _layout, textBrush);
                        _layout.Dispose();
                    }

                    layout.Dispose();
                    renderTarget.EndDraw();
                }
                catch (SharpDXException ex)
                {
                    if (ex.HResult == -2003238900)
                    {
                        ReCreatDX();
                    }
                }
                finally
                {
                    layout.Dispose();
                    format.Dispose();
                    fc.Dispose();
                    textBrush.Dispose();
                }
            }
        }

        private void ReCreatDX()
        {
            ReleaseDeviceContext();
            InitDeviceContext();
        }

        private void InitWin()
        {
            // Fetch the Progman window
            IntPtr progman = W32.FindWindow("Progman", null);
            IntPtr result = IntPtr.Zero;
            IntPtr workerw = IntPtr.Zero;
            IntPtr progmanPrev = IntPtr.Zero;

            progmanPrev = W32.GetWindow(progman, W32.GetWindowType.GW_HWNDPREV);

            // Send 0x052C to Progman. This message directs Progman to spawn a 
            // WorkerW behind the desktop icons. If it is already there, nothing 
            // happens.

            W32.SendMessageTimeout(progman,
                                       0x052C,
                                       new IntPtr(0),
                                       IntPtr.Zero,
                                       W32.SendMessageTimeoutFlags.SMTO_NORMAL,
                                       1000,
                                       out result);

            // Spy++ output
            // .....
            // 0x00010190 "" WorkerW
            //   ...
            //   0x000100EE "" SHELLDLL_DefView
            //     0x000100F0 "FolderView" SysListView32
            // 0x00100B8A "" WorkerW       <-- This is the WorkerW instance we are after!
            // 0x000100EC "Program Manager" Progman

            // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView 
            // as a child. 
            // If we found that window, we take its next sibling and assign it to workerw.
            var ret = W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            null);

                if (p != IntPtr.Zero)
                {
                    //W32.SetParent(p, progmanPrev);

                    // Gets the WorkerW Window after the current one.
                    workerw = W32.FindWindowEx(IntPtr.Zero,
                                               tophandle,
                                               "WorkerW",
                                               null);
                }

                return true;
            }), IntPtr.Zero);

#if false

            // Get the Device Context of the WorkerW
            IntPtr dc = W32.GetDCEx(workerw, IntPtr.Zero, (W32.DeviceContextValues)0x403);
            if (dc != IntPtr.Zero)
            {
                // Create a Graphics instance from the Device Context
                using (Graphics g = Graphics.FromHdc(dc))
                {

                    // Use the Graphics instance to draw a white rectangle in the upper 
                    // left corner. In case you have more than one monitor think of the 
                    // drawing area as a rectangle that spans across all monitors, and 
                    // the 0,0 coordinate being in the upper left corner.

                    //Font font = new Font("Arial", 28, GraphicsUnit.Pixel);
                    //SolidBrush brush = new SolidBrush(Color.Black);

                    //var timeStr = DateTime.Now.ToString("HH:mm:ss");
                    //var screenSize = Screen.PrimaryScreen.Bounds.Size;
                    //var fontSize = g.MeasureString(timeStr, font);

                    //g.FillRectangle(new SolidBrush(System.Drawing.Color.DarkGreen), 0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

                    //g.DrawString(timeStr, font, brush, screenSize.Width / 2 - fontSize.Width / 2, screenSize.Height / 2 - fontSize.Height / 2);

                    //font.Dispose();

                }

                // make sure to release the device context after use.
                W32.ReleaseDC(workerw, dc);
            }
#endif


            mainForm = new Form();
            mainForm.Load += new EventHandler((s, e) => {

                mainForm.FormBorderStyle = FormBorderStyle.None;
                W32.SetParent(mainForm.Handle, progmanPrev);
                mainForm.WindowState = FormWindowState.Maximized;
            });
            mainForm.Show();
        }

        private void InitDeviceContext()
        {
            if (!isDxInit)
            {
                SharpDX.Direct2D1.Factory _2DFactory = new SharpDX.Direct2D1.Factory();
                try
                {
                    HwndRenderTargetProperties hwndRenderTargetProperties = new HwndRenderTargetProperties();
                    hwndRenderTargetProperties.Hwnd = mainForm.Handle;
                    hwndRenderTargetProperties.PixelSize = new SharpDX.Size2(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                    hwndRenderTargetProperties.PresentOptions = 0;
                    HwndRenderTargetProperties properties = hwndRenderTargetProperties;
                    RenderTargetProperties val2 = new RenderTargetProperties();
                    val2.PixelFormat = new PixelFormat(SharpDX.DXGI.Format.Unknown, AlphaMode.Premultiplied);
                    val2.Usage = 0;
                    val2.Type = 0;
                    RenderTargetProperties renderProps = val2;
                    WindowRenderTarget val3 = new WindowRenderTarget(_2DFactory, renderProps, properties);
                    val3.AntialiasMode = AntialiasMode.PerPrimitive;
                    renderTarget = val3;
                }
                finally
                {
                    if (_2DFactory != null)
                    {
                        ((IDisposable)_2DFactory).Dispose();
                    }
                }
                isDxInit = true;
            }
        }

        private void ReleaseDeviceContext()
        {
            if (isDxInit)
            {
                if (renderTarget != null)
                {
                    renderTarget.Dispose();
                }
                isDxInit = false;
            }
        }

        private void CloseWin()
        {
            mainForm.Close();
        }
    }
}
