using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace lib_ourMIPSSharp.Errors; 

public class CompilerWarning : CompilerError {
        public CompilerWarning(Token t, string? message) : base(t, message, CompilerSeverity.Warning) { }
        public CompilerWarning(int line, int column, int length, string? message) :
                base(line, column, length, message, CompilerSeverity.Warning) { }
}