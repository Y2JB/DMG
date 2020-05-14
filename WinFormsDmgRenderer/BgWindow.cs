using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using DMG;

namespace WinFormDmgRender
{
    public partial class BgWindow : Form
    {
        DmgSystem dmg;

        BufferedGraphicsContext gfxBufferedContext;
        BufferedGraphics gfxBuffer;

        Bitmap bgBmp;

        public BgWindow(DmgSystem dmg)
        {
            InitializeComponent();

            this.dmg = dmg;

            this.ClientSize = new System.Drawing.Size(512, 512);
            this.Text = "DMG Bg Viewer";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            bgBmp = new Bitmap(256, 256);

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);


            // Gets a reference to the current BufferedGraphicsContext
            gfxBufferedContext = BufferedGraphicsManager.Current;

            // Creates a BufferedGraphics instance associated with this form, and with dimensions the same size as the drawing surface of Form1.
            gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
        }


        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);

            if (gfxBufferedContext != null)
            {
                gfxBuffer = gfxBufferedContext.Allocate(this.CreateGraphics(), this.DisplayRectangle);
            }
        }


        public void RenderBg()
        {
            dmg.ppu.RenderFullBgToImage(bgBmp, true, -1);
            gfxBuffer.Graphics.DrawImage(bgBmp, ClientRectangle);
            gfxBuffer.Render();
        }
    }
}
