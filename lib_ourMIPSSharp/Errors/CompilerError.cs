namespace lib_ourMIPSSharp;

public class CompilerError : Exception
{
    public CompilerError(string? message) : base(message) { }
}