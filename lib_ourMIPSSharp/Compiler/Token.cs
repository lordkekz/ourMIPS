namespace lib_ourMIPSSharp;

public class Token {
    public TokenType Type { get; set; } = TokenType.None;
    public string Content { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }

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
            default:
            case TokenType.None:
                s = "";
                break;
        }

        return $"{s} {Content}";
    }

    public enum TokenType
    {
        None, Word, InstructionBreak, Number, String
    }
}