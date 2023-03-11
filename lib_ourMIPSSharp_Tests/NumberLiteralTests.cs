#region

using System.ComponentModel;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

#endregion

namespace lib_ourMIPSSharp_Tests;

public class NumberLiteralTests {
    [SetUp]
    public void Setup() { }

    [Test(Description = "Tests invalid DialectOptions")]
    public void TestInvalidDialectOptions() {
        var tok = new Token((DialectOptions)(-1));
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<InvalidEnumArgumentException>(ex);
    }

    [Test(Description = "Tests serializing and parsing of all NumberLiteralFormats")]
    public void TestNumberFormat([Values] NumberLiteralFormat format, [Random(100)] short val) {
        var str = val.ToString();
        var tok = new Token(DialectOptions.None) { Content = str };
        var nl = new NumberLiteral(tok);
        Assert.AreEqual(val, nl.Value);
        Assert.AreEqual(tok, nl.SourceToken);
        Assert.AreEqual(NumberLiteralFormat.Decimal, nl.SourceFormat);

        str = nl.ToString(format);
        tok = new Token(DialectOptions.None) { Content = str };
        nl = new NumberLiteral(tok);
        Assert.AreEqual(val, nl.Value);
        Assert.AreEqual(tok, nl.SourceToken);
        Assert.AreEqual(format, nl.SourceFormat);
    }

    [Test(Description = "Tests that NumberFormatErrors are thrown when appropriate")]
    public void TestNumberFormatErrors(
        [Values("0xz", "-5--", "0bFFF", "h", "0xFFFFF", "-123456789", "-40000", "+40000", "-FF44h", "+FF00h",
            "+1010000000000000b", "-1111111111111111b", "+1111111111111111b")] string str) {
        var tok = new Token(DialectOptions.None) { Content = str };
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<NumberFormatError>(ex);
    }

    [Test(Description = "Tests hex and binary numbers with maximum length"), Sequential]
    public void TestFullNonDecimals(
        [Values("FFFFh", "1111111111111111b")] string str,
        [Values(-1, -1)] short expected) {
        var tok = new Token(DialectOptions.StrictNonDecimalNumberLengths) { Content = str };
        Assert.AreEqual(expected, new NumberLiteral(tok).Value);
    }

    [Test(Description = "Tests hex numbers with <4 digits (including dialect error)"), Sequential]
    public void TestShortHexadecimal(
        [Values("0x0", "-0x00", "+0x2b", "-2bh")] string str,
        [Values(0, 0, 43, -43)] short expected) {
        var tok = new Token(DialectOptions.None) { Content = str };
        Assert.AreEqual(expected, new NumberLiteral(tok).Value);
        
        tok = new Token(DialectOptions.StrictNonDecimalNumberLengths) { Content = str };
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<DialectSyntaxError>(ex);
        Assert.AreEqual(
            $"[Line -1, Col -1] Hexadecimal number <4 digits is not allowed due to DialectOption StrictNonDecimalNumberLengths.",
            ex.Message
        );
    }

    [Test(Description = "Tests binary numbers with <16 digits (including dialect error)"), Sequential]
    public void TestShortBinary(
        [Values("0b0", "-0b00", "+0b0101011", "-0101011b")] string str,
        [Values(0, 0, 43, -43)] short expected) {
        var tok = new Token(DialectOptions.None) { Content = str };
        Assert.AreEqual(expected, new NumberLiteral(tok).Value);
        
        tok = new Token(DialectOptions.StrictNonDecimalNumberLengths) { Content = str };
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<DialectSyntaxError>(ex);
        Assert.AreEqual(
            $"[Line -1, Col -1] Binary number <16 bits is not allowed due to DialectOption StrictNonDecimalNumberLengths.",
            ex.Message
        );
    }

    [Test(Description = "Tests decimal numbers that are out of range for short (including dialect error)"), Sequential]
    public void TestLongDecimal(
        [Values("32768", "32769", "65535", "65534")] string str,
        [Values(-32768, -32767, -1, -2)] short expected) {
        var tok = new Token(DialectOptions.None) { Content = str };
        Assert.AreEqual(expected, new NumberLiteral(tok).Value);
        
        tok = new Token(DialectOptions.StrictDecimalNumberLengths) { Content = str };
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<DialectSyntaxError>(ex);
        Assert.AreEqual(
            $"[Line -1, Col -1] Decimal number (>=2^15) interpreted in two's complement is not allowed due to DialectOption StrictDecimalNumberLengths.",
            ex.Message
        );
    }

    [Test(Description = "Tests serializing and parsing of all NumberLiteralFormats")]
    public void TestStrictNonDecimalNumbers(
        [Values("FF1h", "1010b", "-0h", "-0b")] string str) {
        var tok = new Token(DialectOptions.StrictNonDecimalNumbers) { Content = str };
        var ex = Assert.Catch(() => new NumberLiteral(tok));
        Assert.IsInstanceOf<DialectSyntaxError>(ex);
    }
}