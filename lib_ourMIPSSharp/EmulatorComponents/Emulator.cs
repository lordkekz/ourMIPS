using System.Diagnostics;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.EmulatorComponents;

public class Emulator {
    public RegisterStorage Registers { get; } = new();
    public short ProgramCounter => Registers.ProgramCounter;
    public ProgramStorage Program { get; }
    public MainStorage Memory { get; } = new();
    public InstructionExecutor Executor { get; }
    public bool Terminated { get; set; }
    public bool ForceTerminated { get; set; }
    public bool ErrorTerminated { get; set; }
    public TextWriter TextErr { get; set; } = Console.Error;
    public TextWriter TextOut { get; set; } = Console.Out;
    public TextReader TextIn { get; set; }

    public Emulator(IEnumerable<uint> instructions, string stringConstants) {
        Executor = new InstructionExecutor(this);
        Program = new ProgramStorage(instructions, stringConstants);
    }
    
    public void ExecuteNext() {
        if (Terminated || ForceTerminated || ErrorTerminated)
            throw new EmulatorException("Cannot execute next instruction because program has terminated.");
        if (ProgramCounter < 0 || ProgramCounter >= Program.Count)
            throw new EmulatorException($"Emulator tried to read illegal instruction at {Registers.ProgramCounter}!");

        var instruction = Program[ProgramCounter];
        Executor.ExecuteInstruction(instruction);
    }

    public bool TryExecuteNext() {
        try {
            ExecuteNext();
            return true;
        }
        catch (EmulatorException ex) {
            TextErr.WriteLine(ex);
            ErrorTerminated = true;
        }

        return false;
    }

    public bool RunUntilTerminated(int timeout = int.MaxValue) {
        TextOut.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        for (int i = 0; i < timeout && !ErrorTerminated && !Terminated && !ForceTerminated; i++) {
            TryExecuteNext();
        }

        s.Stop();
        TextOut.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");
        return Terminated;
    }
}