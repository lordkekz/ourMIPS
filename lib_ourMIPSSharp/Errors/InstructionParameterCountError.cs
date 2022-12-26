namespace lib_ourMIPSSharp; 

public class InstructionParameterCountError : CompilerError{
    public InstructionParameterCountError(Token tKw, Keyword kw, int required, int found) : base(tKw,
        $"{kw} expects exactly {required} parameters; got {found}!") { }
}