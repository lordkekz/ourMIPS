using System.Diagnostics;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.EmulatorComponents;

public class Emulator {
    public RegisterStorage Registers { get; }
    public short ProgramCounter => Registers.ProgramCounter;
    public ProgramStorage Program { get; }
    public MainStorage Memory { get; } = new();
    public InstructionExecutor Executor { get; }
    public bool Terminated { get; set; }
    public bool ForceTerminated { get; set; }
    public bool ErrorTerminated { get; set; }
    public bool EffectivelyTerminated => Terminated || ForceTerminated || ErrorTerminated;
    public TextWriter TextErr { get; set; } = Console.Error;
    public TextWriter TextInfo { get; set; } = Console.Out;
    public TextWriter TextOut { get; set; } = Console.Out;
    public TextReader TextIn { get; set; }

    public Emulator(IEnumerable<uint> instructions, string stringConstants) {
        Registers = new RegisterStorage(this);
        Executor = new InstructionExecutor(this);
        Program = new ProgramStorage(instructions, stringConstants);
    }
    
    public void ExecuteNext() {
        if (EffectivelyTerminated)
            throw new EmulatorException("Cannot execute next instruction because program has terminated.");
        if (ProgramCounter < 0 || ProgramCounter >= Program.Count)
            throw new EmulatorException($"Owner tried to read illegal instruction at {Registers.ProgramCounter}!");

        var instruction = Program[ProgramCounter];
        Executor.ExecuteInstruction(instruction);
    }

    public bool TryExecuteNext() {
        try {
            ExecuteNext();
            return true;
        }
        catch (EmulatorException ex) {
            TextErr.WriteLine($"{ex.GetType().Name}: {ex.Message}");
            ErrorTerminated = true;
        }

        return false;
    }

    public bool RunUntilTerminated(int timeout = int.MaxValue) {
        TextInfo.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        for (int i = 0; i < timeout && !EffectivelyTerminated; i++) {
            TryExecuteNext();
        }

        s.Stop();
        TextInfo.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");
        return Terminated;
    }
}