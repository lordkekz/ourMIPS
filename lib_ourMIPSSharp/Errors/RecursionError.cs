namespace lib_ourMIPSSharp;

public class RecursionError : CompilerError {
    public RecursionError(Macro m1, Token t1) :
        base($"Macro must not be recursive. Macro '{m1.Name}' directly references itself at "
             + $"line {t1.Line}, col {t1.Column}!") { }

    public RecursionError(Macro m1, Token t1, Macro m2, Token t2) :
        base($"Macro must not be recursive. Macro '{m1.Name}' references '{m2.Name}' at line {t1.Line}, "
             + $"col {t1.Column}, which again references '{m1.Name}' at line {t2.Line}, col {t2.Column}!") { }
}