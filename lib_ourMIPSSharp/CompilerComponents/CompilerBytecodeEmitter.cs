using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.CompilerComponents;

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
            throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerBytecodeEmitter));

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

        var instruction = new Instruction(_current, _tokens, this);

        Bytecode.Add(instruction.Bytecode);
        Debug.WriteLine($"[CompilerBytecodeEmitter] Generated bytecode '{Convert.ToString(instruction.Bytecode, 2)
            .PadLeft(32, '0')}' from '{string.Join(" ", _tokens.Select(t => t.Content))}'");
        _current = Keyword.None;
        _instructionCounter++;
    }

    public short ResolveRelativeLabel(Token tok) {
        var lName = tok.Content;
        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            lName = lName.ToLowerInvariant();

        if (!Labels.TryGetValue(lName, out var lInstruction))
            throw new UndefinedSymbolError(tok);

        // Cast to ushort to prevent sign extension
        return (short)(lInstruction - _instructionCounter);
    }

    public CompilerState OnMacroDeclaration(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerBytecodeEmitter));

    public CompilerState OnMacroDeclarationArgs(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerBytecodeEmitter));

    public CompilerState OnMacroInstructionStart(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerBytecodeEmitter));

    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerBytecodeEmitter));
}