namespace lib_ourMIPSSharp.Errors;

public class CompilerErrorException : Exception {
    public CompilerError Error { get; }

    public CompilerErrorException(CompilerError err) : base(err.ToString()) {
        Error = err;
    }
}