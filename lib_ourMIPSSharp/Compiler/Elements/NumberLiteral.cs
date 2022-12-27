using System.ComponentModel;

namespace lib_ourMIPSSharp;

public record NumberLiteral {
    public short Value { get; private set; }
    public Token SourceToken { get; }
    public NumberLiteralFormat SourceFormat { get; private set; }

    public NumberLiteral(Token token) {
        SourceToken = token;
        var opts = token.Options;
        var content = token.Content;

        if (!opts.IsValid())
            throw new InvalidEnumArgumentException(nameof(opts), (int)opts, typeof(DialectOptions));

        content = content.Trim().ToLowerInvariant();
        short sign = 1;
        var signed = false;
        if (content.StartsWith("+")) {
            content = content.Substring(1);
            signed = true;
        }

        if (content.StartsWith("-")) {
            content = content.Substring(1);
            sign = -1;
            signed = true;
        }

        try {
            if (content.EndsWith("h")) {
                if (opts.HasFlag(DialectOptions.StrictNonDecimalNumbers))
                    throw new DialectSyntaxError("Hexadecimal number suffix 'h'",
                        token, DialectOptions.StrictNonDecimalNumbers);

                SourceFormat = NumberLiteralFormat.HexSuffix;
                InitHex(content.Substring(0, content.Length - 1), signed, sign);
            }
            else if (content.StartsWith("0x")) {
                SourceFormat = NumberLiteralFormat.HexPrefix;
                InitHex(content.Substring(2), signed, sign);
            }
            else if (content.EndsWith("b")) {
                if (opts.HasFlag(DialectOptions.StrictNonDecimalNumbers))
                    throw new DialectSyntaxError("Binary number suffix 'b'",
                        token, DialectOptions.StrictNonDecimalNumbers);

                SourceFormat = NumberLiteralFormat.BinarySuffix;
                InitBin(content.Substring(0, content.Length - 1), signed, sign);
            }
            else if (content.StartsWith("0b")) {
                SourceFormat = NumberLiteralFormat.BinaryPrefix;
                InitBin(content.Substring(2), signed, sign);
            }
            else {
                SourceFormat = NumberLiteralFormat.Decimal;
                InitDec(content, signed, sign);
            }
        }
        catch (ArgumentOutOfRangeException ex) {
            throw new NumberFormatError(SourceToken, "Doesn't contain digits.");
        }
        catch (FormatException ex) {
            throw new NumberFormatError(SourceToken, "Contains illegal digits.");
        }
        catch (OverflowException ex) {
            throw new NumberFormatError(SourceToken, "Too big.");
        }
    }

    private void InitDec(string digits, bool signed, short sign) {
        var rawValue = Convert.ToUInt16(digits, 10);

        if ((sign == 1 && rawValue > short.MaxValue) || (sign == -1 && rawValue > short.MaxValue + 1)) {
            if (SourceToken.Options.HasFlag(DialectOptions.StrictDecimalNumberLengths))
                throw new DialectSyntaxError("Decimal number (>=2^15) interpreted in two's complement",
                    SourceToken, DialectOptions.StrictDecimalNumberLengths);

            if (signed)
                throw new NumberFormatError(SourceToken, "Explicitly signed number exceeds max value.");

            // Interpret raw value in two's complement
            Value = (short)rawValue;
        }
        else {
            Value = (short)(sign * (short)rawValue);
        }
    }

    private void InitBin(string digits, bool signed, short sign) {
        var rawValue = Convert.ToUInt16(digits, 2);

        if (digits.Length < 16 && SourceToken.Options.HasFlag(DialectOptions.StrictNonDecimalNumberLengths))
            throw new DialectSyntaxError("Binary number <16 bits",
                SourceToken, DialectOptions.StrictNonDecimalNumberLengths);

        if ((sign == 1 && rawValue > short.MaxValue) || (sign == -1 && rawValue > short.MaxValue + 1)) {
            if (signed)
                throw new NumberFormatError(SourceToken, "Explicitly signed number exceeds max value.");

            // Interpret raw value in two's complement
            Value = (short)rawValue;
        }
        else {
            Value = (short)(sign * (short)rawValue);
        }
    }

    private void InitHex(string digits, bool signed, short sign) {
        var rawValue = Convert.ToUInt16(digits, 16);

        if (digits.Length < 4 && SourceToken.Options.HasFlag(DialectOptions.StrictNonDecimalNumberLengths))
            throw new DialectSyntaxError("Hexadecimal number <4 digits",
                SourceToken, DialectOptions.StrictNonDecimalNumberLengths);

        if ((sign == 1 && rawValue > short.MaxValue) || (sign == -1 && rawValue > short.MaxValue + 1)) {
            if (signed)
                throw new NumberFormatError(SourceToken, "Explicitly signed number exceeds max value.");

            // Interpret raw value in two's complement
            Value = (short)rawValue;
        }
        else {
            Value = (short)(sign * (short)rawValue);
        }
    }

    public override string ToString() => ToString(NumberLiteralFormat.Decimal);

    public string ToString(NumberLiteralFormat format) {
        switch (format) {
            default:
            case NumberLiteralFormat.Decimal:
                return Value.ToString();
            case NumberLiteralFormat.BinaryPrefix:
                return "0b" + Convert.ToString(Value, 2).PadLeft(16, '0');
            case NumberLiteralFormat.BinarySuffix:
                return Convert.ToString(Value, 2).PadLeft(16, '0') + "b";
            case NumberLiteralFormat.HexPrefix:
                return "0x" + Convert.ToString(Value, 16).PadLeft(4, '0');
            case NumberLiteralFormat.HexSuffix:
                return Convert.ToString(Value, 16).PadLeft(4, '0') + "h";
        }
    }
}

public enum NumberLiteralFormat {
    Decimal,
    BinaryPrefix,
    BinarySuffix,
    HexPrefix,
    HexSuffix
}