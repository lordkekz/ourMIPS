using System.Diagnostics;

namespace lib_ourMIPSSharp;

public class CompilerLabelReader : ICompilerHandler {
    public Compiler Comp { get; }
    public DialectOptions Options => Comp.Options;
    public IList<Token> ResolvedTokens => Comp.ResolvedTokens;
    public Dictionary<string, int> Labels => Comp.Labels;

    public CompilerLabelReader(Compiler comp) {
        Comp = comp;
    }

    public CompilerState OnLabelDeclaration(Token token, Token colon) {
        var lName = token.Content;

        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            lName = lName.ToLowerInvariant();

        var index = ResolvedTokens.IndexOf(token);

        if (Labels.TryGetValue(lName, out var value))
            throw new SyntaxError(
                $"Duplicate label declaration for '{lName}'! Original declaration at index {value} (corresponds " +
                $"to line {ResolvedTokens[value].Line}, col {ResolvedTokens[value].Column}). Duplicate declaration at " +
                $"index {index} (corresponds to line {token.Line}, col {token.Column}).");
        
        Debug.WriteLine($"[CompilerLabelReader] Found label '{lName}' at index {index}!");
        Labels[lName] = index;
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnInstructionStart(Token token) {
        if (Keyword.Keyword_Macro.Matches(token))
            throw new UnreachableException(
                $"Illegal macro keyword for CompilerLabelReader. " +
                $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
                $"corresponding to line {token.Line}, col {token.Column}.");

        // Assume instruction is ok. Will be checked in Compiler.GenerateBytecode.
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnMacroDeclaration(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroDeclaration for CompilerLabelReader. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroDeclarationArgs(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroDeclarationArgs for CompilerLabelReader. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroInstructionStart(Token token) =>
        throw new UnreachableException(
            "Illegal state MacroInstructionStart for CompilerLabelReader. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");

    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) =>
        throw new UnreachableException(
            "Illegal MacroLabelDeclaration for CompilerLabelReader. " +
            $"There should not be any macros in ResolvedTokens. Read token '{token.Content}' of type {token.Type};" +
            $"corresponding to line {token.Line}, col {token.Column}.");
}