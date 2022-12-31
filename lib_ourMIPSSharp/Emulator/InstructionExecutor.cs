namespace lib_ourMIPSSharp.Emulator; 

public class InstructionExecutor {
    public Emulator EmulatorInstance { get; }
    public MainStorage Memory => EmulatorInstance.Memory;
    public ProgramStorage Program => EmulatorInstance.Program;
    public RegisterStorage Registers => EmulatorInstance.Registers;

    public InstructionExecutor(Emulator em) {
        EmulatorInstance = em;
    }

    public void ExecuteInstruction(Instruction instruction) {
        // TODO implement
        throw new NotImplementedException();
    }
}