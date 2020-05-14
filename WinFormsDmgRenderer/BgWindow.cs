using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormDmgRender
{
    public partial class BgWindow : Form
    {
        public BgWindow()
        {
            InitializeComponent();


            this.ClientSize = new System.Drawing.Size(300, 300);
            this.Text = "DMG Bg Viewer";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
        }
    }
}
