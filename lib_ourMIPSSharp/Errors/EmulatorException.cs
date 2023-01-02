namespace lib_ourMIPSSharp.Errors; 

public class EmulatorException : Exception {
    public EmulatorException(string? message) : base(message) { }
}