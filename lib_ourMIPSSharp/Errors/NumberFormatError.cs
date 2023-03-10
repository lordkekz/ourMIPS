using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors; 

public class NumberFormatError : SyntaxError {
    public NumberFormatError(Token t, string? message) : base(t, $"Invalid number literal '{t.Content}': " + message) {{ }}
}