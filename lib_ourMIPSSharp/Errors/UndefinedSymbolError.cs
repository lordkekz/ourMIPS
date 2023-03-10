using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors; 

public class UndefinedSymbolError : CompilerError {
    public UndefinedSymbolError(Token t, string type = "macro or instruction") : base(t, $"Unknown {type} '{t.Content}'") { }
}