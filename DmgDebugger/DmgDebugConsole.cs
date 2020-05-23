using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using DMG;


namespace DmgDebugger
{
    public class DmgDebugConsole
    {
        List<Breakpoint> breakpoints = new List<Breakpoint>();
        List<Breakpoint> oneTimeBreakpoints = new List<Breakpoint>();

        // Keep the previous / next x instructions cached 
        const int Instruction_Depth = 6;

        // FIFO - previously executed instructions
        Queue<StoredInstruction> executionHistory = new Queue<StoredInstruction>();

        // Next sequential instructions. Forst will be next to be executed
        public List<StoredInstruction> NextInstructions { get; private set; }

        UInt32 lastTicks;

        public List<string> ConsoleText { get; private set; }
        public List<string> ConsoleCodeText { get; private set; }

        public PpuProfiler ppuProfiler{ get; set; }

        public enum Mode
        {
            Running,
            BreakPoint
        }

        enum ConsoleCommand
        {
            step,                           // intrinsically includes STEP n
            next,                           // step over 
            @continue,
            brk,
            breakpoint,
            delete,
            dumptiles,                      // param = fn else dumpttiles.txt
            dumptilemaps,
            dumpbg,
            dumpmemory,                     // Dumps a text file containing hex for 8K. EG: 10 2A FF ...
            loadmemory,                     // param must be a text file containing hex for 8K. EG: 10 2A FF ...
            loadregisters,
            mem,                            // mem 0 = read(0)   mem 0 10 = wrtie(0, 10)
            lcd,                            // Shows Stat and LCDC
            help,
            set,                            // set (register) n/nn
            ticks,
            rom,
            screenshot,
            exit

        }

        public Mode DmgMode { get; set; }

        public bool BreakpointStepAvailable { get; set; }

        private DmgSystem dmg;


        public DmgDebugConsole(DmgSystem dmg)
        {
            this.dmg = dmg;
            DmgMode = Mode.BreakPoint;

            // PPU profiler is expensive! Remember to disconnect the ppu profiler if you are not using it
            //ppuProfiler = new PpuProfiler(dmg);

            ConsoleText = new List<string>();
            ConsoleCodeText = new List<string>();

            NextInstructions = new List<StoredInstruction>();

            // SB : b $64 if [IO_LY] == 2
            //breakpoints.Add(new Breakpoint(0x0));
            //breakpoints.Add(new Breakpoint(0x40));
            //breakpoints.Add(new Breakpoint(0x50));

            //breakpoints.Add(new Breakpoint(0xFE));

            //breakpoints.Add(new Breakpoint(0x64, new ConditionalExpression(dmg.memory, 0xFF44, ConditionalExpression.EqualityCheck.Equal, 143)));


            BreakpointStepAvailable = false;
        }


        public void RunCommand(string commandStr)
        {           
            // make lower case?


            string[] components = commandStr.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            ConsoleCommand command;

            if (Enum.TryParse(components[0], out command) == false)
            {
                // Command Alias'
                if (commandStr.Equals("s")) command = ConsoleCommand.step;
                else if (commandStr.Equals("n")) command = ConsoleCommand.next;
                else if (commandStr.Equals("c")) command = ConsoleCommand.@continue;
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
        }


        bool ExecuteCommand(ConsoleCommand command, string[] parameters)
        {
            ConsoleAddString(String.Format("{0}", command.ToString()));
            switch (command)
            {
                case ConsoleCommand.step:
                    BreakpointStepAvailable = true;
                    return true;

                case ConsoleCommand.next:
                    return NextCommand();

                case ConsoleCommand.@continue:
                    DmgMode = Mode.Running;

                    executionHistory.Clear();
                    return true;

                case ConsoleCommand.mem:
                    return MemCommand(parameters);

                case ConsoleCommand.lcd:
                    return LcdCommand();

                case ConsoleCommand.set:
                    return SetCommand(parameters);

                case ConsoleCommand.brk:
                    DmgMode = Mode.BreakPoint;
                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();
                    return true;

                case ConsoleCommand.breakpoint:
                    return BreakpointCommand(parameters);

                case ConsoleCommand.delete:
                    breakpoints.Clear();
                    return true;

                case ConsoleCommand.dumptiles:
                    dmg.DumpTileSet();
                    return true;

                case ConsoleCommand.dumptilemaps:
                    dmg.ppu.TileMaps[0].DumpTileMap();
                    dmg.ppu.TileMaps[1].DumpTileMap();
                    return true;

                case ConsoleCommand.dumpbg:
                    dmg.ppu.DumpFullCurrentBgToPng(true);
                    return true;

                case ConsoleCommand.ticks:
                    ConsoleAddString(String.Format("ticks - {0} mcycles {1} tcycles", (dmg.cpu.Ticks - lastTicks), ((dmg.cpu.Ticks - lastTicks) * 4)));
                    lastTicks = dmg.cpu.Ticks;
                    return true;

                case ConsoleCommand.rom:
                    ConsoleAddString(String.Format("ROM Info - {0}", dmg.rom.Type.ToString()));
                    return true;

                case ConsoleCommand.screenshot:
                    dmg.ppu.DumpFrameBufferToPng();
                    return true;

                case ConsoleCommand.exit:                    
                    dmg.DumpTty();
                    dmg.ppu.DumpFrameBufferToPng();
                    dmg.DumpTileSet();
                    dmg.ppu.TileMaps[0].DumpTileMap();
                    dmg.ppu.TileMaps[1].DumpTileMap();
                    dmg.ppu.DumpFullCurrentBgToPng(true);
                    return true;

                default:
                    return false;
            }
        }


        // This is 'step over'
        bool NextCommand()
        {
            oneTimeBreakpoints.Add(new Breakpoint((ushort)(dmg.cpu.PC + 1 + NextInstructions[0].OperandLength)));
            DmgMode = Mode.Running;
            return true;
        }


        bool MemCommand(string[] parameters)
        {
            if (parameters.Length == 1 || parameters.Length == 2)
            {

                ushort p1 = 0, p2 = 0;

                bool parsedParams;

                parsedParams = ParseUShortParameter(parameters[0], out p1);
                if (parsedParams && parameters.Length == 2)
                {
                    parsedParams = ParseUShortParameter(parameters[1], out p2);
                }


                if (parsedParams)
                {
                    if (parameters.Length == 1)
                    {
                        ConsoleAddString(String.Format("Ram[0x{0:X4}] == 0x{1:X2}", p1, dmg.memory.ReadByte(p1)));
                        return true;
                    }
                    else
                    {
                        if (p2 > 0xFF)
                        {
                            ConsoleAddString(String.Format("mem write value must be 0 - 255"));
                            return false;
                        }
                        dmg.memory.WriteByte(p1, (byte)p2);
                        ConsoleAddString(String.Format("Written. Ram[0x{0:X4}] == 0x{1:X2}", p1, p2));
                        return true;
                    }
                }

            }

            // Fail
            ConsoleAddString(String.Format("mem usage: 'mem n' for read, 'mem n n' for write. n can be of the form 255 or 0xFF"));
            return false;
        }


        bool LcdCommand()
        {
            ConsoleAddString(dmg.ppu.MemoryRegisters.ToString());
            return true;
        }


        // b 0xC06A if 0xff40 == 2
        bool BreakpointCommand(string[] parameters)
        {
            if (parameters.Length != 1 && parameters.Length != 5)
            {
                ConsoleAddString(String.Format("breakpoint: Invalid number of parameters. Usage:'breakpoint 0xC100'"));
                return false;
            }

            ushort p1 = 0;
            bool parsedParams;
            parsedParams = ParseUShortParameter(parameters[0], out p1);


            // Parse condtiion
            global::DmgDebugger.ConditionalExpression expression = null;

            if (parameters.Length > 1)
            {
                // Parse condition

                try
                {
                    expression = new DmgDebugger.ConditionalExpression(dmg.memory, parameters.Skip(1).ToArray());
                }
                catch (ArgumentException ex)
                {
                    ConsoleAddString(String.Format("Error Adding breakpoint.\n{0}", ex.ToString()));
                }
            }

            breakpoints.Add(new Breakpoint(p1, expression));

            ConsoleAddString(String.Format("breakpoint added at 0x{0:X4}", p1));
            return true;
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
            foreach (var s in registers)
            {
                if (String.Equals(parameters[0], s, StringComparison.OrdinalIgnoreCase))
                {
                    match = true;
                    break;
                }
            }

            if (match == false)
            {
                ConsoleAddString(String.Format("set command: invalid register"));
            }

            //Param 2 is the value
            bool parsedParams;
            ushort value;
            parsedParams = ParseUShortParameter(parameters[1], out value);

            if (parsedParams == false)
            {
                ConsoleAddString(String.Format("set command: parameter 2 must be a number"));
            }

            // This isn't pretty but it's UI code and the debugger just needs to work.
            switch (parameters[0])
            {
                case "A":
                    dmg.cpu.A = (byte)value;
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


        public bool CheckForBreakpoints()
        {
            foreach (var bp in breakpoints)
            {
                if (bp.ShouldBreak(dmg.cpu.PC))
                {
                    DmgMode = Mode.BreakPoint;

                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();

                    ConsoleAddString(String.Format("BREAK"));
                    ConsoleAddString(bp.ToString());

                    return true; ;
                }
            }

            // 'step over' breakpoints
            foreach (var bp in oneTimeBreakpoints)
            {
                if (bp.ShouldBreak(dmg.cpu.PC))
                {
                    DmgMode = Mode.BreakPoint;
                    oneTimeBreakpoints.Remove(bp);

                    PeekSequentialInstructions();
                    UpdateCodeSnapshot();

                    return true;
                }
            }

            return false;
        }

        public void UpdateCodeSnapshot()
        {
            ConsoleCodeText.Clear();
           
            // Show previous instructions
            foreach (var instruction in executionHistory)
            {
                ConsoleCodeText.Add("--- " + instruction.ToString());
            }

            // Next instruction
            ConsoleCodeText.Add(">>> " + NextInstructions[0].ToString());

            // Future instructions
            for (int i = 1; i < NextInstructions.Count; i++)
            {
                if (NextInstructions[i] != null)
                {
                    ConsoleCodeText.Add("+++ " + NextInstructions[i].ToString());
                }
            }
        }


        void ConsoleAddString(string str)
        {
            ConsoleText.Add(str);
        }


        // We are about to step
        public void OnPreBreakpointStep()
        {
            // Pop the current instruction and store it in our history 
            executionHistory.Enqueue(StoredInstruction.DeepCopy(NextInstructions[0]));
            NextInstructions.RemoveAt(0);

            // Only show last x instructions
            if (executionHistory.Count == Instruction_Depth) executionHistory.Dequeue();
        }


        // We have just completed a step, PeekSequentialInstructions will have been called
        public void OnPostBreakpointStep()
        {
            BreakpointStepAvailable = false;            

            UpdateCodeSnapshot();
        }     


        public void PeekSequentialInstructions()
        {
            if(dmg.PoweredOn == false)
            {
                return;
            }

            NextInstructions.Clear();

            int lookAheadBytes = 0;
            for (int i = 0; i < Instruction_Depth; i++)
            {
                ushort pc = (ushort)(dmg.cpu.PC + lookAheadBytes);
                byte opCode = dmg.memory.ReadByte(pc);
                lookAheadBytes++;

                var newInstruction = StoredInstruction.DeepCopy(dmg.cpu.GetInstruction(opCode));
                NextInstructions.Add(newInstruction);

                ushort operandValue = 0;
                if (newInstruction.OperandLength == 1)
                {
                    operandValue = dmg.memory.ReadByte((ushort)(dmg.cpu.PC + lookAheadBytes));
                    lookAheadBytes++;
                }
                else if (newInstruction.OperandLength == 2)
                {
                    operandValue = dmg.memory.ReadShort((ushort)(dmg.cpu.PC + lookAheadBytes));
                    lookAheadBytes += 2;
                }

                newInstruction.Operand = operandValue;
                newInstruction.PC = pc;

                if (opCode == 0xCB && dmg.cpu.GetExtendedInstruction((byte)operandValue) != null)
                {
                    newInstruction.extendedInstruction = dmg.cpu.GetExtendedInstruction((byte)operandValue).DeepCopy();
                }
            }
        }
    }

}
