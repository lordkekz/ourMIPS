namespace lib_ourMIPSSharp; 

public class UndefinedSymbolError : CompilerError {
    public UndefinedSymbolError(Token t) :
        base($"Undefined symbol '{t.Content}' at line {t.Line}, col {t.Column}!") { }
}