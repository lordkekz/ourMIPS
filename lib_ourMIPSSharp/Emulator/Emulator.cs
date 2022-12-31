namespace lib_ourMIPSSharp.Emulator; 

public class Emulator {
    public RegisterStorage Registers { get; }
    public int ProgramCounter => Registers.ProgramCounter;
    public ProgramStorage Program { get; }
    public MainStorage Memory { get; }
    public InstructionExecutor Executor { get; }

    public Emulator() {
        Executor = new InstructionExecutor(this);
    }
    
    public void ExecuteNext() {
        if (ProgramCounter < 0 || ProgramCounter >= Program.Count)
            throw new EmulatorException($"Emulator tried to read illegal instruction at {Registers.ProgramCounter}!");

        var instruction = Program[ProgramCounter];
        Executor.ExecuteInstruction(instruction);
    }
}