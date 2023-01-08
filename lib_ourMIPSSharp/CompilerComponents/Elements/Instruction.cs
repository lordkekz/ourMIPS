using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.CompilerComponents.Elements;

public class Instruction {
    public Keyword Command { get; }
    public List<Register> Registers { get; } = new();
    public short? Immediate { get; private set; }
    public short ImmVal => Immediate ?? default;
    public uint Bytecode { get; private set; }
    private Token[]? _tokens;

    public Instruction(uint instruction) {
        Bytecode = instruction;
        Command = instruction.ExtractInstruction();

        if (!Command.IsParamsOther()) {
            ExtractRegister(1);
            ExtractRegister(2);

            if (Command.IsParamsRegRegReg()) ExtractRegister(3);
            else ExtractImmediate();
        }
        else {
            switch (Command) {
                case Keyword.Instruction_Bo:
                case Keyword.Instruction_Jmp:
                    ExtractImmediate();
                    break;
                case Keyword.Instruction_Ldpc:
                case Keyword.Instruction_Stpc:
                case Keyword.Magic_Reg_Sysin:
                    ExtractRegister(1);
                    break;
                case Keyword.Magic_Reg_Sysout:
                    ExtractRegister(1);
                    break;
                case Keyword.Magic_Str_Sysout:
                    // Overloaded keyword; can't immediately be parsed correctly.
                    Immediate = (short)(Bytecode >> 10);
                    break;
                case Keyword.Magic_Systerm:
                    break;
            }
        }
    }
    
    private void ExtractRegister(int index) {
        var reg = (Register)(31 & (Bytecode >> (11 + 5 * (3 - index))));
        Registers.Add(reg);
    }
    
    private void ExtractImmediate() {
        Immediate = (short)(65535 & Bytecode);
    }

    public Instruction(Keyword kw, List<Token> toks, CompilerBytecodeEmitter cbe) {
        var tKw = toks[0];
        _tokens = toks.ToArray();
        Command = kw;
        Bytecode = (uint) kw;

        if (!kw.IsParamsOther()) {
            if (_tokens.Length != 4)
                throw new InstructionParameterCountError(tKw, kw, 3, _tokens.Length - 1);

            PutRegister(1);
            PutRegister(2);

            if (kw.IsParamsRegRegReg()) PutRegister(3);
            else if (kw.IsParamsRegRegImm()) PutImmediate(3);
            else if (kw.IsParamsRegRegLabel()) PutLabel(3, cbe);
        }
        else if (kw == Keyword.Magic_Systerm) {
            if (_tokens.Length != 1)
                throw new InstructionParameterCountError(tKw, kw, 0, _tokens.Length - 1);
        }
        else {
            if (_tokens.Length != 2)
                throw new InstructionParameterCountError(tKw, kw, 1, _tokens.Length - 1);

            switch (kw) {
                case Keyword.Instruction_Bo:
                case Keyword.Instruction_Jmp:
                    PutLabel(1, cbe);
                    break;
                case Keyword.Instruction_Ldpc:
                case Keyword.Instruction_Stpc:
                case Keyword.Magic_Reg_Sysin:
                    PutRegister(1);
                    break;
                case Keyword.Magic_Reg_Sysout:
                case Keyword.Magic_Str_Sysout:
                    // Overloaded keyword; can't immediately be parsed correctly.

                    var tParam = _tokens[1];
                    if (tParam.Type == TokenType.String) {
                        // sysout with string
                        // update with correct Command
                        Command = Keyword.Magic_Str_Sysout;
                        Bytecode = (uint) Command;
                        
                        // Put immediate to point to string constant
                        Immediate = (short)cbe.Comp.StringConstants.Length;
                        Bytecode |= (uint)(ushort)ImmVal << 10;
                        cbe.Comp.StringConstants += tParam.Content + '\0';
                    }
                    else {
                        // sysout with register decimal value
                        // update with correct Command
                        Command = Keyword.Magic_Reg_Sysout;
                        Bytecode = (uint) Command;
                        
                        // Put register to output
                        PutRegister(1);
                    }

                    break;
            }
        }
    }

    private void PutRegister(int index) {
        var tReg = _tokens[index];
        var reg = RegisterHelper.FromString(tReg.Content);
        if (reg == Register.None)
            throw new UndefinedSymbolError(tReg, "register");

        Registers.Add(reg);
        Bytecode |= (uint)reg << (11 + 5 * (3 - index));
    }

    private void PutImmediate(int index) {
        var imm = new NumberLiteral(_tokens[index]);
        // Place bits of imm in D. (Cast to ushort to prevent sign extension)
        Immediate = imm.Value;
        Bytecode |= (ushort)imm.Value;
    }

    private void PutLabel(int index, CompilerBytecodeEmitter cbe) {
        var imm = cbe.ResolveRelativeLabel(_tokens[index]);
        Immediate = imm;
        Bytecode |= (ushort)imm;
    }

    public string ToString() {
        var result = Command.ToString().Split('_').Last().ToLower();
        result = Registers.Aggregate(result, (current, reg) => current + $" r{(int)reg}");
        if (Immediate is not null) {
            result += " " + ImmVal.ToString(NumberLiteralFormat.Decimal);
        }

        return result;
    }
}