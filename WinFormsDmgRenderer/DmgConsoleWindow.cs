using DMG;
using DmgDebugger;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Windows.Forms;


// The console window also contains all the logic for controling the DMG debugging 
// Yes i'm mixing UI and business logic. Do i care?


namespace WinFormsDmg
{
    public partial class DmgConsoleWindow : Form
    {
        RichTextBox console = new RichTextBox();     
        TextBox commandInput = new TextBox();
        TextBox dmgSnapshot = new TextBox();
        Button okButton = new Button();

        DmgSystem dmg;
        DmgDebugConsole dbgConsole;

        List<string> commandHistory = new List<string>();
        int historyIndex = -1;

        public DmgConsoleWindow(DmgSystem dmg, DmgDebugConsole dbgConsole)
        {
            this.dmg = dmg;
            this.dbgConsole = dbgConsole;
            //DmgMode = Mode.BreakPoint;

            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(880, 800);
            this.Text = "DMG Console";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            // This is the only way i found to stop the annoying bong sound effect when pressing enter on a text box!
            this.Controls.Add(okButton);
            okButton.Visible = false;
            this.AcceptButton = okButton;

            console.Location = new System.Drawing.Point(10, 10);
            console.Multiline = true;
            console.ReadOnly = true;
            console.Width = 500;
            console.Height = 730;            
            console.Enabled = true;        
            this.Controls.Add(console);

            dmgSnapshot.Location = new System.Drawing.Point(console.Location.X + console.Width + 10, 10);
            dmgSnapshot.Multiline = true;
            dmgSnapshot.Width = 420;
            dmgSnapshot.Height = 730;
            dmgSnapshot.Enabled = false;
            this.Controls.Add(dmgSnapshot);

            commandInput.Location = new System.Drawing.Point(10, console.Location.Y + console.Height + 10);
            commandInput.Width = console.Width + dmgSnapshot.Width + 10;
            commandInput.KeyUp += CommandInput_KeyUp;
            this.Controls.Add(commandInput);
            commandInput.Focus();


            // SB : b $64 if [IO_LY] == 2
            //breakpoints.Add(0x0);
            //breakpoints.Add(0x40);
            //breakpoints.Add(0x50);

            //breakpoints.Add(new Breakpoint(0x64));      // loads scanline      
            //breakpoints.Add(0x68);
            //breakpoints.Add(0x6a);
            //breakpoints.Add(new Breakpoint(0x70));



            //BreakpointStepAvailable = false;

            RefreshDmgSnapshot();
        }


        

        public void RefreshDmgSnapshot()
        {
            dmgSnapshot.Text = String.Format("CPU State");

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText((dbgConsole.DmgMode == DmgDebugConsole.Mode.BreakPoint) ? "BREAK" : "RUNNING");

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(dmg.cpu.ToString());

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(dmg.cpu.NextInstruction.ToString());


            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(String.Format("PPU State"));

            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(String.Format("Scanline: {0}", dmg.ppu.CurrentScanline));


            // off, sleeping....
            string ppuState;
            if (dmg.ppu.MemoryRegisters.LCDC.LcdEnable == 0) ppuState = "LCD: Off";
            else
            {
                ppuState = String.Format("LCD: {0} ({1} / {2})\n ({3} / {4})", dmg.ppu.Mode.ToString(), dmg.ppu.ElapsedTicks(), dmg.ppu.TotalTicksForState(), dmg.ppu.ElapsedTicks() * 4, dmg.ppu.TotalTicksForState() * 4);
            }
            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(ppuState);


            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(String.Format("ScrollX {0} ScrollY {1}", dmg.ppu.MemoryRegisters.BgScrollX, dmg.ppu.MemoryRegisters.BgScrollY));   
        }


        public void RefreshConsoleText()
        {
            console.Text = String.Empty;

            foreach(var str in dbgConsole.consoleText)
            {
                console.AppendText(str);
                console.AppendText(Environment.NewLine);
            }
            
            console.ScrollToCaret();
        }


        private void CommandInput_KeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;

            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (commandInput.Text != String.Empty)
                    {
                        //ConsoleAddString(commandInput.Text);

                        commandHistory.Add(commandInput.Text);

                        dbgConsole.RunCommand(commandInput.Text);

                        commandInput.Text = String.Empty;
                        historyIndex = -1;

                        RefreshDmgSnapshot();
                        RefreshConsoleText();
                    }
                    break;

                case Keys.Up:                
                    if (historyIndex < commandHistory.Count - 1) historyIndex++;
                    commandInput.Text = commandHistory[commandHistory.Count - historyIndex - 1];
                    commandInput.Select(commandInput.Text.Length, 0);
                    break;

                case Keys.Down:           
                    if (historyIndex > -1) historyIndex--;

                    if (historyIndex >= 0)
                    {
                        commandInput.Text = commandHistory[commandHistory.Count - historyIndex - 1];
                        commandInput.Select(commandInput.Text.Length, 0);
                    }
                    else commandInput.Text = String.Empty;
                    break;
            
            }
        }
    }
}
