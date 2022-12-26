namespace lib_ourMIPSSharp;

public class UnexpectedTokenError : SyntaxError {
    public UnexpectedTokenError(Token t, CompilerState s) : base(t,
        $"Unexpected Token '{t.Content}' of Type {t.Type} in State {s}") { }
    public UnexpectedTokenError(Token t, CompilerState s, string message) : base(t,
        $"Unexpected Token '{t.Content}' of Type {t.Type} in State {s}. {message}") { }
}