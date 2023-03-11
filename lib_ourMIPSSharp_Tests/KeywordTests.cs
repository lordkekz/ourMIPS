#region

using lib_ourMIPSSharp.CompilerComponents.Elements;

#endregion

namespace lib_ourMIPSSharp_Tests;

public class KeywordTests {
    [SetUp]
    public void Setup() { }

    [Test(Description = "Tests FromToken and Matches for valid/true values"), Sequential]
    public void TestKeywordHelperValid(
        [Values("systerm", "sysin", "sysout", "ldd", "addi", "rol", "or", "xnor", "jmp", "bo", "ldpc")]
        string str,
        [Values(Keyword.Magic_Systerm, Keyword.Magic_Reg_Sysin, Keyword.Magic_Str_Sysout, Keyword.Instruction_Ldd,
            Keyword.Instruction_Addi, Keyword.Instruction_Rol, Keyword.Instruction_Or, Keyword.Instruction_Xnor,
            Keyword.Instruction_Jmp, Keyword.Instruction_Bo, Keyword.Instruction_Ldpc)]
        Keyword expected) {
        var tok = new Token(DialectOptions.None) {
            Content = str,
            Type = TokenType.Word
        };
        Assert.AreEqual(expected, KeywordHelper.FromToken(tok));
        Assert.IsTrue(expected.Matches(tok));
    }

    [Test(Description = "Tests FromToken and Matches for invalid/false values"), Sequential]
    public void TestKeywordHelperInvalid(
        [Values("systerm", "sysin", "sysout", "ldd", "addi", "rol", "or", "xnor", "jmp", "bo", "ldpc")]
        string str,
        [Values(Keyword.Magic_Systerm, Keyword.Magic_Reg_Sysin, Keyword.Magic_Str_Sysout, Keyword.Instruction_Ldd,
            Keyword.Instruction_Addi, Keyword.Instruction_Rol, Keyword.Instruction_Or, Keyword.Instruction_Xnor,
            Keyword.Instruction_Jmp, Keyword.Instruction_Bo, Keyword.Instruction_Ldpc)]
        Keyword expected) {
        var tok = new Token(DialectOptions.None) {
            Content = str,
            Type = TestContext.CurrentContext.Random.NextEnum<TokenType>()
        };
        while (tok.Type == TokenType.Word)
            tok.Type = TestContext.CurrentContext.Random.NextEnum<TokenType>();

        Assert.AreEqual(Keyword.None, KeywordHelper.FromToken(tok));
        Assert.IsFalse(expected.Matches(tok));
    }

    [Test(Description = "Tests FromToken and Matches for valid/true values"), Sequential]
    public void TestKeywordHelperExtractInstruction(
        [Values(0b011101_01100_11010_0000000000110110,
            0b000001_01010_00011_0001111000011000,
            0b000001_01100_01011_0000011110000101,
            0b010111_01011_00110_0011010010110100,
            0b010111_10001_01000_0110001110000110,
            0, 1, 4, -1)]
        int instruction,
        [Values(Keyword.Instruction_Addi, Keyword.Magic_Systerm, Keyword.Magic_Mem_Sysout, Keyword.Instruction_Or,
            Keyword.Instruction_Xor, Keyword.None, Keyword.None, Keyword.None, Keyword.None)]
        Keyword expected) {
        Assert.AreEqual(expected, instruction.ExtractInstruction());
    }
}