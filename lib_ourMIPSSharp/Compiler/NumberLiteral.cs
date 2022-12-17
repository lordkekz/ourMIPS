namespace lib_ourMIPSSharp;

public class NumberLiteral {
    public int Value { get; }

    public NumberLiteral(string content) {
        content = content.Trim().ToLowerInvariant();
        var sign = 1;
        if (content.StartsWith("+"))
            content = content.Substring(1);
        if (content.StartsWith("-")) {
            content = content.Substring(1);
            sign = -1;
        }
        var digits = content;
        var radix = 10;
        if (content.StartsWith("0x")) {
            digits = content.Substring(2);
            radix = 16;
        }
        else if (content.StartsWith("0b")) {
            digits = content.Substring(2);
            radix = 2;
        }
        else if (content.EndsWith("h")) {
            digits = content.Substring(0, content.Length - 1);
            radix = 16;
        }
        else if (content.EndsWith("b")) {
            digits = content.Substring(0, content.Length - 1);
            radix = 2;
        }
        Value = sign * Convert.ToInt32(digits, radix);
    }
    
    public string ToString(NumberLiteralFormat format) {
        switch (format) {
            case NumberLiteralFormat.Decimal:
                return Value.ToString();
            case NumberLiteralFormat.BinaryPrefix:
                return "0b";
        }

        return null;
    }
}

public enum NumberLiteralFormat {
    Decimal, BinaryPrefix, BinarySuffix, HexPrefix, HexSuffix
}