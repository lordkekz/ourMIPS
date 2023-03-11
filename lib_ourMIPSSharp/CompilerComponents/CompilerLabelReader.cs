#region

using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

#endregion

namespace lib_ourMIPSSharp.CompilerComponents;

public class CompilerLabelReader : ICompilerHandler {
    public Compiler Comp { get; }
    public DialectOptions Options => Comp.Options;
    public Dictionary<string, int> Labels => Comp.Labels;
    private int _instructionCounter = 0;

    public CompilerLabelReader(Compiler comp) {
        Comp = comp;
    }

    public CompilerState OnLabelDeclaration(Token token, Token colon) {
        var lName = token.Content;

        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            lName = lName.ToLowerInvariant();

        if (Labels.TryGetValue(lName, out var value))
            Comp.HandleError(new CompilerError(token,
                $"Duplicate label declaration for '{lName}'! Original declaration at " +
                $"instruction {value}, duplicate at {_instructionCounter}."));

        Debug.WriteLine($"[CompilerLabelReader] Found label '{lName}' at index {_instructionCounter}!");
        Labels[lName] = _instructionCounter;
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnInstructionStart(Token token) {
        if (Keyword.Keyword_Macro.Matches(token))
            throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerLabelReader));


        _instructionCounter++;
        // Assume instruction is ok. Will be checked in Compiler.GenerateBytecode.
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnMacroDeclaration(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerLabelReader));

    public CompilerState OnMacroDeclarationArgs(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerLabelReader));

    public CompilerState OnMacroInstructionStart(Token token) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerLabelReader));

    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) =>
        throw ICompilerHandler.MakeUnreachableStateException(token, nameof(CompilerLabelReader));
}