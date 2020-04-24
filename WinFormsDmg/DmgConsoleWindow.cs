using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WinFormsDmg
{
    public partial class DmgConsoleWindow : Form
    {
        TextBox console = new TextBox();
        
        public DmgConsoleWindow(Form renderWnd)
        {     
            InitializeComponent();


            MaximizeBox = false;
            MinimizeBox = false;

            console.Location = new System.Drawing.Point(20, 10);
            console.Width = 200;
            console.Height = 400;
            console.Text = "AF - 0xFF00";
            this.Controls.Add(console);
        }


    }
}
