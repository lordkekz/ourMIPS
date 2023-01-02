namespace lib_ourMIPSSharp.CompilerComponents.Elements;

public class Token {
    public TokenType Type { get; set; } = TokenType.None;
    public string? Content { get; set; }
    public int Line { get; set; } = -1;
    public int Column { get; set; } = -1;
    public DialectOptions Options { get; }

    public Token(DialectOptions opts) {
        Options = opts;
    }
    
    public override string ToString() {
        string s;
        switch (Type) {
            case TokenType.Word:
                s = "W";
                break;
            case TokenType.InstructionBreak:
                s = "B";
                break;
            case TokenType.Number:
                s = "N";
                break;
            case TokenType.String:
                s = "S";
                break;
            case TokenType.Comment:
                s = "C";
                break;
            case TokenType.SingleChar:
                s = "O";
                break;
            default:
            case TokenType.None:
                s = "?";
                break;
        }

        return $"{s} {Content}";
    }
}

public enum TokenType
{
    None, Word, InstructionBreak, Number, String, Comment, SingleChar
}