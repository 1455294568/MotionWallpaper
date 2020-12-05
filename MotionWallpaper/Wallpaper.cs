using MotionWallpaper.Models;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MotionWallpaper
{
    public partial class Wallpaper : Form
    {
        private static bool isDxInit;

        private WindowRenderTarget renderTarget;

        private Timer refreshTimer;

        private Timer generateTimer;

        private Random r;

        private int startY = 0;

        private bool reStart = true;

        private List<Entity> entities;

        private string[] randomStr = { "￥10", "￥20", "￥50", "￥100" };

        private SharpDX.DirectWrite.Factory fc;

        private TextFormat formatFlow;

        private TextFormat formatTime;

        public Wallpaper()
        {
            InitializeComponent();

            long tick = DateTime.Now.Ticks;
            r = new Random((int)(tick & 0xffffffffL) | (int)(tick >> 32));

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

            InitDeviceContext();

            refreshTimer.Tick += Timer_Tick;
            refreshTimer.Enabled = true;
            refreshTimer.Interval = 15;

            generateTimer.Tick += GenerateTimer_Tick;
            generateTimer.Interval = 100;
            generateTimer.Start();

            fc = new SharpDX.DirectWrite.Factory();

            formatFlow = new TextFormat(fc, "微软雅黑", 18)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };

            formatTime = new TextFormat(fc, "微软雅黑", 28)
            {
                TextAlignment = TextAlignment.Center,
                ParagraphAlignment = ParagraphAlignment.Center
            };
        }

        

        private void GenerateTimer_Tick(object sender, EventArgs e)
        {
            entities.RemoveAll(s => s.Location.Y >= Screen.PrimaryScreen.WorkingArea.Height);
            for (int i = 0; i < 10; i++)
            {
                int x = r.Next(0, Screen.PrimaryScreen.WorkingArea.Width);
                int y = r.Next(-10, -1);

                Entity entity = new Entity();
                entity.Value = "*"; //sb.ToString();
                entity.Location = new PointF(x, y);
                entities.Add(entity);
            }

            int x2 = r.Next(0, Screen.PrimaryScreen.WorkingArea.Width);
            int y2 = r.Next(-10, -1);
            var index = r.Next(randomStr.Length);

            var entity2 = new Entity();
            entity2.Value = randomStr[index];
            entity2.Location = new PointF(x2, y2);
            entities.Add(entity2);
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


                TextLayout layout = new TextLayout(fc, DateTime.Now.ToString(), formatTime, SSize.Width, SSize.Height);
                SolidColorBrush textBrush = new SolidColorBrush(renderTarget, System.Drawing.Color.White.ToDXColor4());

                try
                {
                    renderTarget.BeginDraw();
                    renderTarget.Clear(ColorHelper.ToDXColor4(System.Drawing.Color.Black));

                    renderTarget.DrawTextLayout(new RawVector2(0f, 0f), layout, textBrush);

                    for (int i = 0; i < _entities.Count; i++)
                    {
                        var _loc = _entities[i].Location;
                        if (entities.Count() > i)
                        {
                            entities[i].Location = new PointF(_loc.X, _loc.Y + 6);
                        }
                        TextLayout _layout = new TextLayout(fc, _entities[i].Value, formatFlow, 80, 30);
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
                    //format.Dispose();
                    //fc.Dispose();
                    textBrush.Dispose();
                }
            }
        }

        public Size SSize
        {
            get { return Screen.PrimaryScreen.Bounds.Size; }
        }

        private void InitDeviceContext()
        {
            if (!isDxInit)
            {
                SharpDX.Direct2D1.Factory _2DFactory = new SharpDX.Direct2D1.Factory();
                try
                {
                    HwndRenderTargetProperties hwndRenderTargetProperties = new HwndRenderTargetProperties();
                    hwndRenderTargetProperties.Hwnd = this.Handle;
                    hwndRenderTargetProperties.PixelSize = new SharpDX.Size2(SSize.Width, SSize.Height);
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

        private void ReCreatDX()
        {
            ReleaseDeviceContext();
            InitDeviceContext();
        }

        private void ExitMenu_Click(object sender, EventArgs e)
        {
            Application.Exit();
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
    }
}
