namespace lib_ourMIPSSharp; 

public class UndefinedSymbolError : CompilerError {
    public UndefinedSymbolError(Token t, string type = "macro or instruction") : base(t, $"Unknown {type} '{t.Content}'") { }
}