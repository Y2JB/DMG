using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Windows.Input;

using DMG;
using WinFormsDmg;

namespace WinFormDmgRender
{
    public partial class DmgRenderWindow : Form
    {
        DmgSystem dmg;

        DmgConsoleWindow consoleWindow;

        Stopwatch timer = new Stopwatch();
        long elapsedMs;
        int framesDrawn;
        int fps;

        BufferedGraphicsContext gfxBufferedContext;
        BufferedGraphics gfxBuffer;

        public DmgRenderWindow()
        {
            InitializeComponent();
            
            // 4X gameboy resolution
            Width = 640;
            Height = 576;
            DoubleBuffered = true;

            dmg = new DmgSystem();
            dmg.PowerOn();
            dmg.OnFrame = () => this.Draw();

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            consoleWindow = new DmgConsoleWindow(dmg);

            consoleWindow.Show();

            System.Windows.Forms.Application.Idle += new EventHandler(OnApplicationIdle);

            timer.Start();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            consoleWindow.Location = new Point(Location.X + Width + 20, Location.Y);


            
            // Gets a reference to the current BufferedGraphicsContext
            gfxBufferedContext = BufferedGraphicsManager.Current;


            // TODO : window SIZE!!!
            // Creates a BufferedGraphics instance associated with Form1, and with
            // dimensions the same size as the drawing surface of Form1.
            gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
        }

        private void OnKeyDown(Object o, KeyEventArgs a)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, KeyEventArgs>(OnKeyDown), o, a);
                return;
            }
            if (a.KeyCode == Keys.Up) dmg.pad.UpdateKeyState(Joypad.GbKey.Up, true);
            else if (a.KeyCode == Keys.Down) dmg.pad.UpdateKeyState(Joypad.GbKey.Down, true);
            else if (a.KeyCode == Keys.Left) dmg.pad.UpdateKeyState(Joypad.GbKey.Left, true);
            else if (a.KeyCode == Keys.Right) dmg.pad.UpdateKeyState(Joypad.GbKey.Right, true);
            else if (a.KeyCode == Keys.Z) dmg.pad.UpdateKeyState(Joypad.GbKey.B, true);
            else if (a.KeyCode == Keys.X) dmg.pad.UpdateKeyState(Joypad.GbKey.A, true);
            else if (a.KeyCode == Keys.Enter) dmg.pad.UpdateKeyState(Joypad.GbKey.Start, true);
            else if (a.KeyCode == Keys.Back) dmg.pad.UpdateKeyState(Joypad.GbKey.Select, true);
        }

        private void OnKeyUp(Object o, KeyEventArgs a)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, KeyEventArgs>(OnKeyUp), o, a);
                return;
            }

            if (a.KeyCode == Keys.Up) dmg.pad.UpdateKeyState(Joypad.GbKey.Up, false);
            else if (a.KeyCode == Keys.Down) dmg.pad.UpdateKeyState(Joypad.GbKey.Down, false);
            else if (a.KeyCode == Keys.Left) dmg.pad.UpdateKeyState(Joypad.GbKey.Left, false);
            else if (a.KeyCode == Keys.Right) dmg.pad.UpdateKeyState(Joypad.GbKey.Right, false);
            else  if (a.KeyCode == Keys.Z) dmg.pad.UpdateKeyState(Joypad.GbKey.B, false);
            else if (a.KeyCode == Keys.X) dmg.pad.UpdateKeyState(Joypad.GbKey.A, false);
            else if (a.KeyCode == Keys.Enter) dmg.pad.UpdateKeyState(Joypad.GbKey.Start, false);
            else if (a.KeyCode == Keys.Back) dmg.pad.UpdateKeyState(Joypad.GbKey.Select, false);
        }

        private void OnApplicationIdle(object sender, EventArgs e)
        {
            
            while (IsApplicationIdle())
            {
                if (timer.ElapsedMilliseconds - elapsedMs >= 1000)
                {
                    elapsedMs = timer.ElapsedMilliseconds;
                    fps = framesDrawn;
                    framesDrawn = 0;
                }

                if (consoleWindow.DmgMode == DmgConsoleWindow.Mode.Running)
                {
                    dmg.Step();

                    consoleWindow.CheckForBreakpoints();
                }

                else if (consoleWindow.DmgMode == DmgConsoleWindow.Mode.BreakPoint &&
                         consoleWindow.BreakpointStepAvailable)
                {
                    dmg.Step();
                    consoleWindow.OnBreakpointStep();
                }
            }
        }

        private void Draw()
        {
            framesDrawn++;

            gfxBuffer.Graphics.DrawImage(dmg.FrameBuffer, new Rectangle(0, 0 , ClientRectangle.Width, ClientRectangle.Height));


            //gfxBuffer.Graphics.FillRectangle(new SolidBrush(Color.Red), new Rectangle(0, 0, 1000, 1000));
            gfxBuffer.Graphics.DrawString(String.Format("{0:D2} fps", fps), new Font("Verdana", 8),  new SolidBrush(Color.Black), new Point(ClientRectangle.Width - 75, 10));

            gfxBuffer.Render();            
        }




        [StructLayout(LayoutKind.Sequential)]
        public struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParameter;
            public IntPtr LParameter;
            public uint Time;
            public Point Location;
        }

        bool IsApplicationIdle()
        {
            NativeMessage result;
            return PeekMessage(out result, IntPtr.Zero, (uint)0, (uint)0, (uint)0) == 0;
        }

        [DllImport("user32.dll")]
        public static extern int PeekMessage(out NativeMessage message, IntPtr window, uint filterMin, uint filterMax, uint remove);
    }




}
