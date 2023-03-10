using System.ComponentModel;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.EmulatorComponents;

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
        
        // reset; if still expecting input, will be set again in ExecuteRegSysin
        EmulatorInstance.ExpectingInput = false;

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

        // increment program counter (unless instruction wasn't executed)
        if (!EmulatorInstance.ExpectingInput)
            Registers.ProgramCounter++;
    }

    private void ExecuteSysterm(Instruction i) {
        EmulatorInstance.Terminated = true;
    }

    private void ExecuteStrSysout(Instruction i) {
        EmulatorInstance.TextOut.WriteLine(Program.GetStringConstant(i.ImmVal));
    }

    private void ExecuteRegSysout(Instruction i) {
        EmulatorInstance.TextOut.WriteLine(Registers[i.Registers[0]].ToString());
    }

    /// <summary>
    /// Tries to execute the sysin instruction.
    /// Does so by reading lines from textin until a valid number is found or there are no lines to read or the emulator
    /// forcefully terminates. The Emulator instance's ExpectingInput flag will be set if execution failed because there
    /// was no line to read.
    /// </summary>
    /// <param name="i"></param>
    private void ExecuteRegSysin(Instruction i) {
        var val = 0;
        string? line;
        do {
            line = EmulatorInstance.TextIn.ReadLine();
        } while (!EmulatorInstance.ForceTerminated &&
                 line is not null &&
                 !int.TryParse(line, out val));

        if (EmulatorInstance.ForceTerminated ||
            (EmulatorInstance.ExpectingInput = line is null))
            return;

        Registers[i.Registers[0]] = val;
    }

    private void ExecuteLdd(Instruction i) {
        // Load Memory at ri+v into register rj
        var valRi = Registers[i.Registers[0]];
        Registers[i.Registers[1]] = Memory[valRi + i.ImmVal];
    }

    private void ExecuteSto(Instruction i) {
        // Store value of rj in memory at ri+v
        var valRi = Registers[i.Registers[0]];
        Memory[valRi + i.ImmVal] = Registers[i.Registers[1]];
    }

    private void ExecuteShli(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.ImmVal % 32;
        Registers[i.Registers[1]] = (int)(valRi << v);
    }

    private void ExecuteShri(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.ImmVal % 32;
        Registers[i.Registers[1]] = (int)(valRi >> v);
    }

    private void ExecuteRoli(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.ImmVal % 32;
        Registers[i.Registers[1]] = (int)(valRi << v | valRi >> (32 - v));
    }

    private void ExecuteRori(Instruction i) {
        // Cast to uint to prevent sign expansion
        var valRi = (uint)Registers[i.Registers[0]];
        var v = i.ImmVal % 32;
        Registers[i.Registers[1]] = (int)(valRi >> v | valRi << (32 - v));
    }

    private void ExecuteSubi(Instruction i) {
        var valRi = Registers[i.Registers[0]];

        // Extract signs; treat zero as positive (since 0 has sign 0 in two's complement)
        var signRi = Math.Sign(valRi);
        signRi = signRi == 0 ? +1 : signRi;
        var signV = Math.Sign(i.ImmVal);
        signV = signV == 0 ? +1 : signV;

        // Actual operation
        Registers[i.Registers[1]] = valRi - i.ImmVal;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi - i.ImmVal);
        Registers.FlagOverflow = signRi != signV && signRi != signResult;
    }

    private void ExecuteAddi(Instruction i) {
        var valRi = Registers[i.Registers[0]];

        // Extract signs; treat zero as positive (since 0 has sign 0 in two's complement)
        var signRi = Math.Sign(valRi);
        signRi = signRi == 0 ? +1 : signRi;
        var signV = Math.Sign(i.ImmVal);
        signV = signV == 0 ? +1 : signV;

        // Actual operation
        Registers[i.Registers[1]] = valRi + i.ImmVal;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi + i.ImmVal);
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

        // Extract signs; treat zero as positive (since 0 has sign 0 in two's complement)
        var signRi = Math.Sign(valRi);
        signRi = signRi == 0 ? +1 : signRi;
        var signRj = Math.Sign(valRj);
        signRj = signRj == 0 ? +1 : signRj;

        // Actual operation
        Registers[i.Registers[2]] = valRi - valRj;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi - valRj);
        Registers.FlagOverflow = signRi != signRj && signRi != signResult;
    }

    private void ExecuteAdd(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];

        // Extract signs; treat zero as positive (since 0 has sign 0 in two's complement)
        var signRi = Math.Sign(valRi);
        signRi = signRi == 0 ? +1 : signRi;
        var signRj = Math.Sign(valRj);
        signRj = signRj == 0 ? +1 : signRj;

        // Actual operation
        Registers[i.Registers[2]] = valRi + valRj;
        // Set Overflow if sign changed even though it shouldn't have
        var signResult = Math.Sign(valRi + valRj);
        Registers.FlagOverflow = signRi == signRj && signRi != signResult;
    }

    private void ExecuteOr(Instruction i) {
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = (uint)Registers[i.Registers[1]];
        Registers[i.Registers[2]] = (int)(valRi | valRj);
    }

    private void ExecuteAnd(Instruction i) {
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = (uint)Registers[i.Registers[1]];
        Registers[i.Registers[2]] = (int)(valRi & valRj);
    }

    private void ExecuteXor(Instruction i) {
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = (uint)Registers[i.Registers[1]];
        Registers[i.Registers[2]] = (int)(valRi ^ valRj);
    }

    private void ExecuteXnor(Instruction i) {
        var valRi = (uint)Registers[i.Registers[0]];
        var valRj = (uint)Registers[i.Registers[1]];
        Registers[i.Registers[2]] = (int)~(valRi ^ valRj);
    }

    private void ExecuteJmp(Instruction i) {
        // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
        Registers.ProgramCounter += (short)(i.ImmVal - 1);
    }

    private void ExecuteBeq(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi == valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.ImmVal - 1);
    }

    private void ExecuteBneq(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi != valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.ImmVal - 1);
    }

    private void ExecuteBgt(Instruction i) {
        var valRi = Registers[i.Registers[0]];
        var valRj = Registers[i.Registers[1]];
        if (valRi > valRj)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.ImmVal - 1);
    }

    private void ExecuteBo(Instruction i) {
        if (_flagOverflow)
            // -1 to account for the incrementation of ProgramCounter in ExecuteInstruction
            Registers.ProgramCounter += (short)(i.ImmVal - 1);
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