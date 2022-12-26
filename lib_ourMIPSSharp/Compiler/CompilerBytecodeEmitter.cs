using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;

namespace lib_ourMIPSSharp;

public class CompilerBytecodeEmitter : ICompilerHandler {
    public Compiler Comp { get; }
    public DialectOptions Options => Comp.Options;
    public IList<Token> ResolvedTokens => Comp.ResolvedTokens;
    public Dictionary<string, int> Labels => Comp.Labels;
    public List<uint> Bytecode => Comp.Bytecode;

    private int _instructionCounter = 0;
    private Keyword _current = Keyword.None;
    private List<Token> _tokens = new();

    public CompilerBytecodeEmitter(Compiler comp) {
        Comp = comp;
    }

    public CompilerState OnInstructionStart(Token token) {
        if (Keyword.Keyword_Macro.Matches(token))
            throw new UnreachableException(
                $"Illegal macro keyword for CompilerBytecodeEmitter. " +
                $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
                $"corresponding to line {token.Line}, col {token.Column}.");

        _current = KeywordHelper.FromToken(token);
        if (_current is Keyword.None)
            // This should never happen, as any instruction name that isn't a keyword will be interpreted as a macro,
            // and a potential error will already be caught.
            throw new UnreachableException(
                $"CompilerBytecodeEmitter encountered unknown instruction '{token.Content}' at line {token.Line}, " +
                $"col {token.Column}! This should have been caught before.");
        
        _tokens.Clear();
        _tokens.Add(token);
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnInstructionArgs(Token token) {
        if (_current != Keyword.None)
            _tokens.Add(token);
        return CompilerState.InstructionArgs;
    }

    public void OnInstructionBreak(Token token) {
        if (_current == Keyword.None) return;
        var tKw = _tokens[0];
        var instruction = (uint)_current;

        // A complete instruction line was read.

        if (_current.IsParamsRegRegReg()) {
            if (_tokens.Count != 4)
                throw new SyntaxError(
                    $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly three parameters; got {_tokens.Count - 1}!");

            instruction = PutRegister(instruction, 1);
            instruction = PutRegister(instruction, 2);
            instruction = PutRegister(instruction, 3);
        }
        else if (_current.IsParamsRegRegImm()) {
            if (_tokens.Count != 4)
                throw new SyntaxError(
                    $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly three parameters; got {_tokens.Count - 1}!");

            instruction = PutRegister(instruction, 1);
            instruction = PutRegister(instruction, 2);

            var imm = new NumberLiteral(_tokens[3]);
            // Place bits of imm in D. (Cast to ushort to prevent sign extension)
            instruction |= (ushort)imm.Value;
        }
        else if (_current.IsParamsRegRegLabel()) {
            if (_tokens.Count != 4)
                throw new SyntaxError(
                    $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly three parameters; got {_tokens.Count - 1}!");

            instruction = PutRegister(instruction, 1);
            instruction = PutRegister(instruction, 2);

            // Resolve relative distance of label to instruction immediate
            var lName = _tokens[3].Content;
            if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
                lName = lName.ToLowerInvariant();

            if (!Labels.TryGetValue(lName, out var lInstruction))
                throw new SyntaxError($"Unknown label '{lName}' at line {token.Line}, col {token.Column}!");

            // Cast to ushort to prevent sign extension
            instruction |= (ushort)(lInstruction - _instructionCounter);
        }
        else {
            switch (_current) {
                case Keyword.Instruction_Jmp:
                    if (_tokens.Count != 2)
                        throw new SyntaxError(
                            $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly one parameter; got {_tokens.Count - 1}!");

                    var lName = _tokens[1].Content;
                    if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
                        lName = lName.ToLowerInvariant();

                    if (!Labels.TryGetValue(lName, out var lInstruction))
                        throw new SyntaxError($"Unknown label '{lName}' at line {token.Line}, col {token.Column}!");

                    // Cast to ushort to prevent sign extension
                    instruction |= (ushort)(lInstruction - _instructionCounter);
                    break;
                case Keyword.Instruction_Ldpc:
                case Keyword.Instruction_Stpc:
                    if (_tokens.Count != 2)
                        throw new SyntaxError(
                            $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly one parameter; got {_tokens.Count - 1}!");

                    instruction = PutRegister(instruction, 1);
                    break;
                case Keyword.Magic_Systerm:
                    break;
                case Keyword.Magic_Reg_Sysin:
                    if (_tokens.Count != 2)
                        throw new SyntaxError(
                            $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly one parameter; got {_tokens.Count - 1}!");

                    instruction = PutRegister(instruction, 1);
                    break;
                case Keyword.Magic_Reg_Sysout:
                case Keyword.Magic_Str_Sysout:
                    // Overloaded keyword; can't immediately be parsed correctly.
                    if (_tokens.Count != 2)
                        throw new SyntaxError(
                            $"Instruction '{_current}' at line {tKw.Line}, col {tKw.Column} expects exactly one parameter; got {_tokens.Count - 1}!");
                    var tParam = _tokens[1];
                    if (tParam.Type == TokenType.String) {
                        instruction |= (uint) (ushort)Comp.StringConstants.Length << 10; 
                        Comp.StringConstants += tParam.Content + '\0';
                    }
                    else instruction = PutRegister(instruction, 1);
                    break;
            }
        }

        Bytecode.Add(instruction);
        Debug.WriteLine($"[CompilerBytecodeEmitter] Generated bytecode '{Convert.ToString(instruction, 2)
            .PadLeft(32, '0')}' from '{string.Join(" ", _tokens.Select(t => t.Content))}'");
        _current = Keyword.None;
        _instructionCounter++;
    }

    private uint PutRegister(uint instruction, int index) {
        var tReg = _tokens[index];
        var reg = RegisterHelper.FromString(tReg.Content);
        if (reg == Register.None)
            throw new SyntaxError($"Unknown register '{tReg.Content}' at line {tReg.Line}, col {tReg.Column}");

        return instruction | ((uint)reg << (11 + 5 * (3 - index)));
    }

    public CompilerState OnMacroDeclaration(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroDeclaration for CompilerBytecodeEmitter. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroDeclarationArgs(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroDeclarationArgs for CompilerBytecodeEmitter. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroInstructionStart(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroInstructionStart for CompilerBytecodeEmitter. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) =>
        throw new UnreachableException(
            "Illegal MacroLabelDeclaration for CompilerBytecodeEmitter. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");
}