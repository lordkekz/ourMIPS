using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors;

public class SyntaxError : CompilerError
{
    public SyntaxError(Token t, string? message) : base(t, message) { }
    public SyntaxError(int line, int col, string? message) : base(line, col, message) { }
}