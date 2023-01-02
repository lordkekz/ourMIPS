using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors;

public class CompilerError : Exception
{
    public CompilerError(Token t, string? message) : this(t.Line, t.Column, message) { }
    public CompilerError(int line, int col, string? message) : base($"[Line {line}, Col {col}] {message}") { }
}