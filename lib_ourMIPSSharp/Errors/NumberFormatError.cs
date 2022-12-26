namespace lib_ourMIPSSharp; 

public class NumberFormatError : SyntaxError {
    public NumberFormatError(Token t, string? message) : base(t, $"Invalid number literal '{t.Content}': " + message) {{ }}
}