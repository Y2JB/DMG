using DMG;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;


// The console window also contains all the logic for controling the DMG debugging 
// Yes i'm mixing UI and business logic. Do i care?


namespace WinFormsDmg
{
    public partial class DmgConsoleWindow : Form
    {
        TextBox console = new TextBox();     
        TextBox commandInput = new TextBox();
        TextBox dmgSnapshot = new TextBox();

        //List<string> consoleContent = new List<string>();
        List<string> commandHistory = new List<string>();

        // FIFO
        Queue<Instruction> executionHistory = new Queue<Instruction>();

        public enum Mode
        {
            Running,
            BreakPoint
        }

        enum ConsoleCommand
        {
            step,                           // intrinsically includes STEP n
            run,
            brk,
            breakpoint,
            dumptiles,                      // param = fn else dumpttiles.txt
            dumpmemory,                     // Dumps a text file containing hex for 8K. EG: 10 2A FF ...
            loadmemory,                     // param must be a text file containing hex for 8K. EG: 10 2A FF ...
            loadregisters,
            mem,                            // mem 0 = read(0)   mem 0 10 = wrtie(0, 10)
            help,
            AF, BC, CD, HL, SP, PC,
            A, B, C, D, E, F, H, L,

            exit

        }

        public Mode DmgMode { get; private set; }

        public bool BreakpointStepAvailable { get; set; }

        private DmgSystem dmg;


        public DmgConsoleWindow(DmgSystem dmg)
        {
            this.dmg = dmg;
            DmgMode = Mode.BreakPoint;

            InitializeComponent();

            this.ClientSize = new System.Drawing.Size(800, 480);
            this.Text = "DMG Console";
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            
            console.Location = new System.Drawing.Point(10, 10);
            console.Multiline = true;
            console.ScrollBars = ScrollBars.Vertical;
            console.Width = 600;
            console.Height = 400;            
            console.Enabled = false;
            this.Controls.Add(console);

            dmgSnapshot.Location = new System.Drawing.Point(console.Location.X + console.Width + 10, 10);
            dmgSnapshot.Multiline = true;
            dmgSnapshot.Width = 200;
            dmgSnapshot.Height = 400;
            dmgSnapshot.Enabled = false;
            this.Controls.Add(dmgSnapshot);

            commandInput.Location = new System.Drawing.Point(10, console.Location.Y + console.Height + 10);
            commandInput.Width = console.Width + dmgSnapshot.Width + 10;
            commandInput.KeyUp += CommandInput_KeyUp;
            this.Controls.Add(commandInput);




            BreakpointStepAvailable = false;

            RefreshDmgSnapshot();
        }


        void ProcessCommand(string commandStr)
        {
            commandHistory.Add(commandStr);


            // make lower case?


            string[] components = commandStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            ConsoleCommand command;
            
            if(Enum.TryParse(components[0], out command) == false)
            {
                // Command Alias'
                if (commandStr.Equals("s")) command = ConsoleCommand.step;
                else if (commandStr.Equals("r")) command = ConsoleCommand.run;
                else if (commandStr.Equals("b")) command = ConsoleCommand.brk;

                // error
                else
                {
                    ConsoleAddString(String.Format("Unknown command {0}", commandStr));
                    return;
                }
            }

            // Trim the first item
            var cmdParams = components.Where(w => w != components[0]).ToArray();

            ExecuteCommand(command, cmdParams);
            RefreshDmgSnapshot();
        }


        bool ExecuteCommand(ConsoleCommand command, string[] parameters)
        {
            switch(command)
            {
                case ConsoleCommand.step:
                    BreakpointStepAvailable = true;
                    return true;

                case ConsoleCommand.run:
                    DmgMode = Mode.Running;

                    executionHistory.Clear();
                    return true;

                case ConsoleCommand.mem:
                    return MemCommand(parameters);

                case ConsoleCommand.brk:
                    DmgMode = Mode.BreakPoint;
                    return true;

                default:
                    return false;
            }
        }

        bool MemCommand(string[] parameters)
        {           
            if(parameters.Length == 1 || parameters.Length == 2)
            {

                ushort p1=0, p2=0;

                bool parsedParams;

                parsedParams = ParseUShortParameter(parameters[0], out p1);
                if (parsedParams && parameters.Length == 2)
                {
                    ParseUShortParameter(parameters[1], out p2);
                }
                   

                if (parsedParams)
                {
                    if(parameters.Length == 1)
                    {
                        ConsoleAddString(String.Format("Ram[0x{0:X4}] == 0x{1:X2}", p1, dmg.memory.ReadByte(p1)));
                        return true;
                    }
                    else
                    {
                        if(p2 > 0xFF)
                        {
                            ConsoleAddString(String.Format("mem write value must be 0 - 255"));
                            return false;
                        }
                        dmg.memory.WriteByte(p1, (byte) p2);
                        ConsoleAddString(String.Format("Written. Ram[0x{0:X4}] == 0x{1:X2}", p1, p2));
                        return true;
                    }
                }
                
            }

            // Fail
            ConsoleAddString(String.Format("mem usage: 'mem n' for read, 'mem n n' for write. n can be of the form 255 or 0xFF"));
            return false;
        }

        // Try to parse a base 10 or base 16 number from string
        bool ParseUShortParameter(string p, out ushort value)
        {
            if (ushort.TryParse(p, out value) == false)
            {
                // Is it hex?
                if (p.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                {
                    p = p.Substring(2);
                }
                return ushort.TryParse(p, NumberStyles.HexNumber, CultureInfo.CurrentCulture, out value);
            }
            return true;
        }

        public void OnBreakpointStep()
        {
            BreakpointStepAvailable = false;

            // Take a copy of this instruction and store it in our history 
            executionHistory.Enqueue(dmg.cpu.PreviousInstruction);

            // Only show last 5 instructions
            if (executionHistory.Count == 6) executionHistory.Dequeue();
            
            foreach (var instruction in executionHistory.Reverse())
            {
                ConsoleAddString(instruction.ToString());
            }

            RefreshDmgSnapshot();
        }

        void RefreshDmgSnapshot()
        {
            dmgSnapshot.Text = dmg.cpu.ToString();
            dmgSnapshot.AppendText(Environment.NewLine);
            dmgSnapshot.AppendText(dmg.cpu.NextInstruction.ToString());
        }


        void ConsoleAddString(string str)
        {
            console.AppendText(str + Environment.NewLine);
        }


        private void CommandInput_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter && commandInput.Text != String.Empty)
            {
                ConsoleAddString(commandInput.Text);

                ProcessCommand(commandInput.Text);

                commandInput.Text = "";
            }
        }
    }
}
