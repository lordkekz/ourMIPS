using NUnit.Framework;

namespace lib_ourMIPSSharp_Tests; 

[TestFixture]
public class InstructionTests {
    [SetUp]
    public void Setup() { }
    
    // [Test(Description = "Tests FromToken and Matches for valid/true values"), Sequential]
    // public void TestKeywordHelperValid(
    //     [Values("systerm", "sysin", "sysout", "ldd", "addi", "rol", "or", "xnor", "jmp", "bo", "ldpc")]
    //     string str) {
    //     var tok = new Token(DialectOptions.None) {
    //         Content = str,
    //         Type = TokenType.Word
    //     };
    //     Assert.AreEqual(expected, KeywordHelper.FromToken(tok));
    //     Assert.IsTrue(expected.Matches(tok));
    // }
}