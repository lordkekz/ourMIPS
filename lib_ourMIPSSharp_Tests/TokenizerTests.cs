using System.ComponentModel;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp_Tests;

public class TokenizerTests {
    private readonly List<string> _sourceCodes = new();
    private readonly List<string> _printResults = new();

    [SetUp]
    public void Setup() {
        _sourceCodes.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS"));
        _printResults.Add(File.ReadAllText("../../../Samples/mult_philos.ourMIPS.tokens")
            .Replace("\r", ""));
    }
    
    [Test(Description = "Tests Tokenizer checking for invalid options.")]
    public void TestInvalidOptions() {
        var ex = Assert.Catch(() => new Tokenizer("", (DialectOptions)(-1), true));
        Assert.IsInstanceOf<InvalidEnumArgumentException>(ex);
    }

    [Test(Description = "Tests Tokenizer checking for invalid options."), Sequential]
    public void TestSyntaxErrors(
        [Values("@", "\nab\n??", "a5b a&gj", "hi\"ho\"", "hi \"h\no\"")]
        string sourceCode,
        [Values(
            "[Line 1, Col 1] Unexpected character '@'!",
            "[Line 3, Col 1] Unexpected character '?'!",
            "[Line 1, Col 6] Unexpected character '&'!",
            "[Line 1, Col 3] Unexpected character '\"'!",
            "[Line 1, Col 6] Unexpected line break during string!"
        )]
        string expectedMessage) {
        var t = new Tokenizer(sourceCode, DialectOptions.None, true);
        var ex = Assert.Catch(() => t.Tokenize());
        Assert.IsInstanceOf<SyntaxError>(ex);
        Assert.AreEqual(expectedMessage, ex.Message);
    }

    [Test(Description = "Tests that RegisterHelper.FromString parses valid register descriptors correctly."),
     Sequential]
    public void TestCorrectResults(
        [Range(0, 0)] int index) {
        var t = new Tokenizer(_sourceCodes[index], DialectOptions.None, true);
        var tokens1 = t.Tokenize();
        var str = tokens1.Aggregate("", (current, token) => current + (token.ToString() + '\n'));
        Assert.AreEqual(_printResults[index], str);
    }
    
    [Test(Description = "Tests Tokenizer checking for invalid options.")]
    public void Test() {
        var ex = Assert.Catch(() => new Tokenizer("", (DialectOptions)(-1), true));
        Assert.IsInstanceOf<InvalidEnumArgumentException>(ex);
    }
}