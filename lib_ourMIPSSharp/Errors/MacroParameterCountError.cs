using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors;

public class MacroParameterCountError : CompilerError {
    public MacroParameterCountError(int line, int column, int length, Macro macro, int found) :
        base(line, column, length, $"{macro.Name} expects exactly {macro.Params.Count} parameters; got {found}!") { }
}