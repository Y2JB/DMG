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

        List<string> commandHistory = new List<string>();
        int historyIndex = -1;

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
            set,                            // set (register) n/nn

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

            this.ClientSize = new System.Drawing.Size(860, 470);
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
            dmgSnapshot.Width = 230;
            dmgSnapshot.Height = 400;
            dmgSnapshot.Enabled = false;
            this.Controls.Add(dmgSnapshot);

            commandInput.Location = new System.Drawing.Point(10, console.Location.Y + console.Height + 10);
            commandInput.Width = console.Width + dmgSnapshot.Width + 10;
            commandInput.KeyUp += CommandInput_KeyUp;
            this.Controls.Add(commandInput);
            commandInput.Focus();



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
                else if (commandStr.Equals("x")) command = ConsoleCommand.exit;

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

                case ConsoleCommand.set:
                    return SetCommand(parameters);

                case ConsoleCommand.brk:
                    DmgMode = Mode.BreakPoint;
                    return true;

                case ConsoleCommand.exit:
                    Application.Exit();
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
                    parsedParams = ParseUShortParameter(parameters[1], out p2);
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


        bool SetCommand(string[] parameters)
        {
            if (parameters.Length != 2)
            {
                // Fail
                ConsoleAddString(String.Format("set usgage: set a 10, ser HL 0xFF..."));
                return false;
            }

            parameters[0] = parameters[0].ToUpper();
            // Param 1 is the register id
            string[] registers = new string[] { "A", "B", "C", "D", "E", "F", "H", "L", "AF", "BC", "DE", "HL", "SP", "PC" };
            bool match = false;
            foreach(var s in registers)
            {
                if(String.Equals(parameters[0], s, StringComparison.OrdinalIgnoreCase))
                {
                    match = true;
                    break;
                }
            }

            if(match == false)
            {
                ConsoleAddString(String.Format("set command: invalid register"));
            }

            //Param 2 is the value
            bool parsedParams;
            ushort value;
            parsedParams = ParseUShortParameter(parameters[1], out value);

            if(parsedParams == false)
            {
                ConsoleAddString(String.Format("set command: parameter 2 must be a number"));
            }

            // This isn't pretty but it's UI code and the debugger just needs to work.
            switch(parameters[0])
            {
                case "A":
                    dmg.cpu.A = (byte) value;
                    break;

                case "B":
                    dmg.cpu.B = (byte)value;
                    break;

                case "C":
                    dmg.cpu.C = (byte)value;
                    break;

                case "D":
                    dmg.cpu.D = (byte)value;
                    break;

                case "E":
                    dmg.cpu.E = (byte)value;
                    break;

                case "F":
                    dmg.cpu.F = (byte)value;
                    break;

                case "H":
                    dmg.cpu.H = (byte)value;
                    break;

                case "L":
                    dmg.cpu.L = (byte)value;
                    break;

                case "AF":
                    dmg.cpu.AF = value;
                    break;

                case "BC":
                    dmg.cpu.BC = value;
                    break;

                case "DE":
                    dmg.cpu.DE = value;
                    break;

                case "HL":
                    dmg.cpu.HL = value;
                    break;

                case "SP":
                    dmg.cpu.SP = value;
                    break;

                case "PC":
                    dmg.cpu.PC = value;
                    break;
            }
            return true;
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
            if (dmg.cpu.IsHalted)
            {
                dmgSnapshot.Text = "HALTED";
            }

            else
            {
                dmgSnapshot.Text = dmg.cpu.ToString();
                dmgSnapshot.AppendText(Environment.NewLine);
                dmgSnapshot.AppendText(dmg.cpu.NextInstruction.ToString());
            }         
        }


        void ConsoleAddString(string str)
        {
            console.AppendText(str + Environment.NewLine);
        }


        private void CommandInput_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    if (commandInput.Text != String.Empty)
                    {
                        ConsoleAddString(commandInput.Text);

                        ProcessCommand(commandInput.Text);

                        commandInput.Text = String.Empty;
                        historyIndex = -1;
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
