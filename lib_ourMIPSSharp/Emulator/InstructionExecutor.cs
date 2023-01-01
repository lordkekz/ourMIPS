namespace lib_ourMIPSSharp.Emulator;

public class InstructionExecutor {
    public Emulator EmulatorInstance { get; }
    public MainStorage Memory => EmulatorInstance.Memory;
    public ProgramStorage Program => EmulatorInstance.Program;
    public RegisterStorage Registers => EmulatorInstance.Registers;

    private bool _flagOverflow = false;

    public InstructionExecutor(Emulator em) {
        EmulatorInstance = em;
    }

    public void ExecuteInstruction(Instruction instruction) {
        // reset flags
        _flagOverflow = Registers.FlagOverflow;
        Registers.FlagOverflow = false;

        // execute instruction method
        switch (instruction.Command) {
            default:
            case Keyword.None:
                throw new EmulatorException($"Unknown Operation {instruction.Command}");

            case Keyword.Magic_Systerm:
                ExecuteSysterm(instruction);
                break;
            case Keyword.Magic_Str_Sysout:
                ExecuteStrSysout(instruction);
                break;
            case Keyword.Magic_Reg_Sysout:
                ExecuteRegSysout(instruction);
                break;
            case Keyword.Magic_Reg_Sysin:
                ExecuteRegSysin(instruction);
                break;

            case Keyword.Instruction_Ldd:
                ExecuteLdd(instruction);
                break;
            case Keyword.Instruction_Sto:
                ExecuteSto(instruction);
                break;

            case Keyword.Instruction_Shli:
                ExecuteShli(instruction);
                break;
            case Keyword.Instruction_Shri:
                ExecuteShri(instruction);
                break;
            case Keyword.Instruction_Roli:
                ExecuteRoli(instruction);
                break;
            case Keyword.Instruction_Rori:
                ExecuteRori(instruction);
                break;
            case Keyword.Instruction_Subi:
                ExecuteSubi(instruction);
                break;
            case Keyword.Instruction_Addi:
                ExecuteAddi(instruction);
                break;

            case Keyword.Instruction_Shl:
                ExecuteShl(instruction);
                break;
            case Keyword.Instruction_Shr:
                ExecuteShr(instruction);
                break;
            case Keyword.Instruction_Rol:
                ExecuteRol(instruction);
                break;
            case Keyword.Instruction_Ror:
                ExecuteRor(instruction);
                break;
            case Keyword.Instruction_Sub:
                ExecuteSub(instruction);
                break;
            case Keyword.Instruction_Add:
                ExecuteAdd(instruction);
                break;

            case Keyword.Instruction_Or:
                ExecuteOr(instruction);
                break;
            case Keyword.Instruction_And:
                ExecuteAnd(instruction);
                break;
            case Keyword.Instruction_Xor:
                ExecuteXor(instruction);
                break;
            case Keyword.Instruction_Xnor:
                ExecuteXnor(instruction);
                break;

            case Keyword.Instruction_Jmp:
                ExecuteJmp(instruction);
                break;
            case Keyword.Instruction_Beq:
                ExecuteBeq(instruction);
                break;
            case Keyword.Instruction_Bneq:
                ExecuteBneq(instruction);
                break;
            case Keyword.Instruction_Bgt:
                ExecuteBgt(instruction);
                break;
            case Keyword.Instruction_Bo:
                ExecuteBo(instruction);
                break;

            case Keyword.Instruction_Ldpc:
                ExecuteLdpc(instruction);
                break;
            case Keyword.Instruction_Stpc:
                ExecuteStpc(instruction);
                break;
        }

        // increment program counter
        Registers.ProgramCounter++;
    }

    private void ExecuteSysterm(Instruction i) {
        EmulatorInstance.Terminated = true;
    }

    private void ExecuteStrSysout(Instruction i) {
        Console.WriteLine(Program.GetStringConstant(i.Immediate));
    }

    private void ExecuteRegSysout(Instruction i) {
        Console.WriteLine(Registers[i.Registers[0]].ToString());
    }

    private void ExecuteRegSysin(Instruction i) {
        // TODO improve exception handling
        // TODO add a way to interface with UI
        int val;
        while (!int.TryParse(Console.In.ReadLine(), out val)) { }

        Registers[i.Registers[0]] = val;
    }

    private void ExecuteLdd(Instruction i) {
        // Load Memory at ri+v into register rj
        var valRi = Registers[i.Registers[0]];
        Registers[i.Registers[1]] = Memory[valRi + i.Immediate];
    }

    private void ExecuteSto(Instruction i) {
        // Store value of rj in memory at ri+v
        var valRi = Registers[i.Registers[0]];
        Memory[valRi + i.Immediate] = Registers[i.Registers[1]];
    }

    private void ExecuteShli(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.Immediate % 32;
        Registers[i.Registers[1]] = (int)(valRi << v);
    }

    private void ExecuteShri(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.Immediate % 32;
        Registers[i.Registers[1]] = (int)(valRi >> v);
    }

    private void ExecuteRoli(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.Immediate % 32;
        Registers[i.Registers[1]] = (int)(valRi << v | valRi >> (32 - v));
    }

    private void ExecuteRori(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.Immediate % 32;
        Registers[i.Registers[1]] = (int)(valRi >> v | valRi << (32 - v));
    }

    private void ExecuteSubi(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var signRi = Math.Sign(valRi);
        var signV = Math.Sign(i.Immediate);
        // Actual operation
        Registers[i.Registers[1]] = valRi - i.Immediate;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi - i.Immediate);
        Registers.FlagOverflow = signRi != signV && signRi != signResult;
    }

    private void ExecuteAddi(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var signRi = Math.Sign(valRi);
        var signV = Math.Sign(i.Immediate);
        // Actual operation
        Registers[i.Registers[1]] = valRi + i.Immediate;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi + i.Immediate);
        Registers.FlagOverflow = signRi == signV && signRi != signResult;
    }

    private void ExecuteShl(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]] % 32;
        Registers[i.Registers[2]] = (int)(valRi << valRj);
    }

    private void ExecuteShr(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]] % 32;
        Registers[i.Registers[2]] = (int)(valRi >> valRj);
    }

    private void ExecuteRol(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]] % 32;
        Registers[i.Registers[2]] = (int)(valRi << valRj | valRi >> (32 - valRj));
    }

    private void ExecuteRor(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]] % 32;
        Registers[i.Registers[2]] = (int)(valRi >> valRj | valRi << (32 - valRj));
    }

    private void ExecuteSub(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        var signRi = Math.Sign(valRi);
        var signRj = Math.Sign(i.Immediate);
        // Actual operation
        Registers[i.Registers[2]] = valRi - valRj;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi - valRj);
        Registers.FlagOverflow = signRi != signRj && signRi != signResult;
    }

    private void ExecuteAdd(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        var signRi = Math.Sign(valRi);
        var signRj = Math.Sign(i.Immediate);
        // Actual operation
        Registers[i.Registers[2]] = valRi + valRj;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi + valRj);
        Registers.FlagOverflow = signRi != signRj && signRi != signResult;
    }

    private void ExecuteOr(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        Registers[i.Registers[2]] = valRi | valRj;
    }

    private void ExecuteAnd(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        Registers[i.Registers[2]] = valRi & valRj;
    }

    private void ExecuteXor(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        Registers[i.Registers[2]] = valRi ^ valRj;
    }

    private void ExecuteXnor(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        Registers[i.Registers[2]] = ~(valRi | valRj);
    }

    private void ExecuteJmp(Instruction i) {
        // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
        Registers.ProgramCounter += (short)(i.Immediate - 1);
    }

    private void ExecuteBeq(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi == valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.Immediate - 1);
    }

    private void ExecuteBneq(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi != valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.Immediate - 1);
    }

    private void ExecuteBgt(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi > valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.Immediate - 1);
    }

    private void ExecuteBo(Instruction i) {
        if (_flagOverflow)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.Immediate - 1);
    }

    private void ExecuteLdpc(Instruction i) {
        Registers[i.Registers[0]] = Registers.ProgramCounter;
    }

    private void ExecuteStpc(Instruction i) {
        // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
        Registers.ProgramCounter = (short)(Registers[i.Registers[0]] - 1);
    }
    
    // TODO implement custom ourMIPSSharp instructions
}