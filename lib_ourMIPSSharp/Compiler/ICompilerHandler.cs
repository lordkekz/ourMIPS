namespace lib_ourMIPSSharp;

public interface ICompilerHandler {
    public Compiler Comp { get; }

    public CompilerState OnInstructionStart(Token token) => CompilerState.InstructionArgs;
    public CompilerState OnInstructionArgs(Token token) => CompilerState.InstructionArgs;
    public CompilerState OnLabelDeclaration(Token token, Token colon) => CompilerState.InstructionArgs;
    public CompilerState OnMacroDeclaration(Token token) => CompilerState.MacroDeclarationArgs;
    public CompilerState OnMacroDeclarationArgs(Token token) => CompilerState.MacroDeclarationArgs;
    public CompilerState OnMacroInstructionStart(Token token) => CompilerState.MacroInstructionArgs;
    public CompilerState OnMacroInstructionArgs(Token token) => CompilerState.MacroInstructionArgs;
    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) => CompilerState.MacroInstructionArgs;

    /// <summary>
    /// Special method to be called whenever an instruction is complete. This occurs at the InstructionBreak that ends the instruction.
    /// Should not be called for macro declarations or macro ended.
    /// </summary>
    public void OnInstructionBreak(Token token) { }
}