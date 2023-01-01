using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace lib_ourMIPSSharp.Emulator;

public class Emulator {
    public RegisterStorage Registers { get; } = new();
    public short ProgramCounter => Registers.ProgramCounter;
    public ProgramStorage Program { get; }
    public MainStorage Memory { get; } = new();
    public InstructionExecutor Executor { get; }
    public bool Terminated { get; set; } = false;

    public Emulator(IEnumerable<uint> instructions, string stringConstants) {
        Executor = new InstructionExecutor(this);
        Program = new ProgramStorage(instructions, stringConstants);
    }

    public void ExecuteNext() {
        if (Terminated)
            throw new EmulatorException("Cannot execute next instruction because program has terminated.");
        if (ProgramCounter < 0 || ProgramCounter >= Program.Count)
            throw new EmulatorException($"Emulator tried to read illegal instruction at {Registers.ProgramCounter}!");

        var instruction = Program[ProgramCounter];
        Executor.ExecuteInstruction(instruction);
    }

    public bool RunUntilTerminated(int timeout = int.MaxValue) {
        Console.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        for (int i = 0; i < timeout && !Terminated; i++) {
            ExecuteNext();
        }

        s.Stop();
        Console.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");
        return Terminated;
    }
}