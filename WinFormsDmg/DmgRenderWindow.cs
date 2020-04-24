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

using DMG;
using WinFormsDmg;

namespace WinFormDmgRender
{
    public partial class DmgRenderWindow : Form
    {
        DmgSystem dmg;

        DmgConsoleWindow consoleWindow;

        public DmgRenderWindow()
        {
            InitializeComponent();
            
            Width = 640;
            Height = 576;

            consoleWindow = new DmgConsoleWindow(this);
            //consoleWindow.Owner = this;

            //var p = new Point(Location.X + 1800, Location.Y);
            //consoleWindow.Location = p;
            consoleWindow.Show();


            System.Windows.Forms.Application.Idle += new EventHandler(OnApplicationIdle);

            dmg = new DmgSystem();
            dmg.PowerOn();

            dmg.OnFrame = () => this.Draw();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            consoleWindow.Location = new Point(Location.X + Width + 20, Location.Y);
            
        }


        private void OnApplicationIdle(object sender, EventArgs e)
        {
            
            while (IsApplicationIdle())
            {
                dmg.Step();
            }
        }

        private void Draw()
        {
            Graphics g = this.CreateGraphics();          
                    
            var image = new Bitmap(160, 144);
           

            for (int y = 0; y < 144; y++)
            {
                for (int x = 0; x < 160; x++)
                {
                    image.SetPixel(x, y, dmg.FrameBuffer[x + (y * 160)]);
                }
            }

            g.DrawImage(image, new Rectangle(0, 0 , ClientRectangle.Width, ClientRectangle.Height));
            //image.Save("../../../../dump/x.png");

            /*
            // This example assumes the existence of a form called Form1.
            BufferedGraphicsContext currentContext;
            BufferedGraphics myBuffer;
            // Gets a reference to the current BufferedGraphicsContext
            currentContext = BufferedGraphicsManager.Current;
            // Creates a BufferedGraphics instance associated with Form1, and with
            // dimensions the same size as the drawing surface of Form1.
            myBuffer = currentContext.Allocate(this.CreateGraphics(),
               this.DisplayRectangle);

            myBuffer.Graphics.
                */

            /*
            RGBType pixels[250 * 250];

            RGBType* temp = pixels;
            for (int x = 0; x < 250; x++)
            {
                for (int y = 0; y < 250; y++)
                {
                    temp->r = 0;
                    temp->g = 1;
                    temp->b = 1;
                    temp++;
                }
            }
            
            uint[] tex = new uint[1024];
            gl.GenTextures(1, tex);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, tex);
            gl.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameteri(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
            gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGB, 250, 250, 0, OpenGL.GL_RGB, OpenGL.GL_FLOAT, NULL);
            gl.TexSubImage2D(OpenGL.GL_TEXTURE_2D, 0, 0, 0, 250, 250, OpenGL.GL_RGB, OpenGL.GL_FLOAT, pixels);
            */


            /*
            IntPtr unmanagedPointer = Marshal.AllocHGlobal(framebuffer.Length);
            Marshal.Copy(framebuffer, 0, unmanagedPointer, framebuffer.Length);
            // Call unmanaged code
            Marshal.FreeHGlobal(unmanagedPointer);

            //gl.ClearColor(1.0f, 0.0f, 0.0f, 1.0f);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT);
            gl.RasterPos((float)-1, (float)1);
            gl.PixelZoom(1, -1);
            gl.DrawPixels(160, 144, OpenGL.GL_UNSIGNED_BYTE, framebuffer);
            */



            //# ifdef WIN
            //            LDFS_SwapBuffers();
            //#endif

            /*
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);	// Clear The Screen And The Depth Buffer
            gl.LoadIdentity();                  // Reset The View

            // gl.Color(1.0f, 1.0f, 1.0f);
            // gl.FontBitmaps.DrawText(gl, 0, 0, "Arial", "Argh");


            gl.Translate(-1.5f, 0.0f, -6.0f);				// Move Left And Into The Screen

            gl.Rotate(rtri, 0.0f, 1.0f, 0.0f);				// Rotate The Pyramid On It's Y Axis

            gl.Begin(OpenGL.GL_TRIANGLES);					// Start Drawing The Pyramid

            gl.Color(1.0f, 0.0f, 0.0f);			// Red
            gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Front)
            gl.Color(0.0f, 1.0f, 0.0f);			// Green
            gl.Vertex(-1.0f, -1.0f, 1.0f);			// Left Of Triangle (Front)
            gl.Color(0.0f, 0.0f, 1.0f);			// Blue
            gl.Vertex(1.0f, -1.0f, 1.0f);			// Right Of Triangle (Front)

            gl.Color(1.0f, 0.0f, 0.0f);			// Red
            gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Right)
            gl.Color(0.0f, 0.0f, 1.0f);			// Blue
            gl.Vertex(1.0f, -1.0f, 1.0f);			// Left Of Triangle (Right)
            gl.Color(0.0f, 1.0f, 0.0f);			// Green
            gl.Vertex(1.0f, -1.0f, -1.0f);			// Right Of Triangle (Right)

            gl.Color(1.0f, 0.0f, 0.0f);			// Red
            gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Back)
            gl.Color(0.0f, 1.0f, 0.0f);			// Green
            gl.Vertex(1.0f, -1.0f, -1.0f);			// Left Of Triangle (Back)
            gl.Color(0.0f, 0.0f, 1.0f);			// Blue
            gl.Vertex(-1.0f, -1.0f, -1.0f);			// Right Of Triangle (Back)

            gl.Color(1.0f, 0.0f, 0.0f);			// Red
            gl.Vertex(0.0f, 1.0f, 0.0f);			// Top Of Triangle (Left)
            gl.Color(0.0f, 0.0f, 1.0f);			// Blue
            gl.Vertex(-1.0f, -1.0f, -1.0f);			// Left Of Triangle (Left)
            gl.Color(0.0f, 1.0f, 0.0f);			// Green
            gl.Vertex(-1.0f, -1.0f, 1.0f);			// Right Of Triangle (Left)
            gl.End();						// Done Drawing The Pyramid

            gl.LoadIdentity();
            gl.Translate(1.5f, 0.0f, -7.0f);				// Move Right And Into The Screen

            gl.Rotate(rquad, 1.0f, 1.0f, 1.0f);			// Rotate The Cube On X, Y & Z

            gl.Begin(OpenGL.GL_QUADS);					// Start Drawing The Cube

            gl.Color(0.0f, 1.0f, 0.0f);			// Set The Color To Green
            gl.Vertex(1.0f, 1.0f, -1.0f);			// Top Right Of The Quad (Top)
            gl.Vertex(-1.0f, 1.0f, -1.0f);			// Top Left Of The Quad (Top)
            gl.Vertex(-1.0f, 1.0f, 1.0f);			// Bottom Left Of The Quad (Top)
            gl.Vertex(1.0f, 1.0f, 1.0f);			// Bottom Right Of The Quad (Top)


            gl.Color(1.0f, 0.5f, 0.0f);			// Set The Color To Orange
            gl.Vertex(1.0f, -1.0f, 1.0f);			// Top Right Of The Quad (Bottom)
            gl.Vertex(-1.0f, -1.0f, 1.0f);			// Top Left Of The Quad (Bottom)
            gl.Vertex(-1.0f, -1.0f, -1.0f);			// Bottom Left Of The Quad (Bottom)
            gl.Vertex(1.0f, -1.0f, -1.0f);			// Bottom Right Of The Quad (Bottom)

            gl.Color(1.0f, 0.0f, 0.0f);			// Set The Color To Red
            gl.Vertex(1.0f, 1.0f, 1.0f);			// Top Right Of The Quad (Front)
            gl.Vertex(-1.0f, 1.0f, 1.0f);			// Top Left Of The Quad (Front)
            gl.Vertex(-1.0f, -1.0f, 1.0f);			// Bottom Left Of The Quad (Front)
            gl.Vertex(1.0f, -1.0f, 1.0f);			// Bottom Right Of The Quad (Front)

            gl.Color(1.0f, 1.0f, 0.0f);			// Set The Color To Yellow
            gl.Vertex(1.0f, -1.0f, -1.0f);			// Bottom Left Of The Quad (Back)
            gl.Vertex(-1.0f, -1.0f, -1.0f);			// Bottom Right Of The Quad (Back)
            gl.Vertex(-1.0f, 1.0f, -1.0f);			// Top Right Of The Quad (Back)
            gl.Vertex(1.0f, 1.0f, -1.0f);			// Top Left Of The Quad (Back)

            gl.Color(0.0f, 0.0f, 1.0f);			// Set The Color To Blue
            gl.Vertex(-1.0f, 1.0f, 1.0f);			// Top Right Of The Quad (Left)
            gl.Vertex(-1.0f, 1.0f, -1.0f);			// Top Left Of The Quad (Left)
            gl.Vertex(-1.0f, -1.0f, -1.0f);			// Bottom Left Of The Quad (Left)
            gl.Vertex(-1.0f, -1.0f, 1.0f);			// Bottom Right Of The Quad (Left)

            gl.Color(1.0f, 0.0f, 1.0f);			// Set The Color To Violet
            gl.Vertex(1.0f, 1.0f, -1.0f);			// Top Right Of The Quad (Right)
            gl.Vertex(1.0f, 1.0f, 1.0f);			// Top Left Of The Quad (Right)
            gl.Vertex(1.0f, -1.0f, 1.0f);			// Bottom Left Of The Quad (Right)
            gl.Vertex(1.0f, -1.0f, -1.0f);			// Bottom Right Of The Quad (Right)
            gl.End();                       // Done Drawing The Q

            gl.Flush();

            rtri += 3.0f;// 0.2f;						// Increase The Rotation Variable For The Triangle 
            rquad -= 3.0f;// 0.15f;						// Decrease The Rotation Variable For The Quad 

    */
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
