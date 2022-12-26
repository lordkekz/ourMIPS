namespace lib_ourMIPSSharp;

public class SyntaxError : CompilerError
{
    public SyntaxError(Token t, string? message) : base(t, message) { }
    public SyntaxError(int line, int col, string? message) : base(line, col, message) { }
}