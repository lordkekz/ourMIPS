namespace lib_ourMIPSSharp;

public class RecursionError : CompilerError {
    public RecursionError(Macro m1, Token t1) :
        base($"Macro '{m1.Name}' directly references itself at line {t1.Line}, col {t1.Column}!") { }

    public RecursionError(Macro m1, Token t1, Token[] tokens) : base(MakeDeepMessage(m1, t1, tokens)) {}

    private static string MakeDeepMessage(Macro m1, Token t1, Token[] tokens) {
        var msg = $"Macro '{m1.Name}' indirectly references itself with {tokens.Length + 1} steps: ";
        msg += $"{t1.Content} at {t1.Line}:{t1.Column}";
        for (var i = tokens.Length - 1; i >= 0; i--) {
            var t = tokens[i];
            msg += $" -> {t.Content} at {t.Line}:{t.Column}";
        }

        return msg;
    }
}