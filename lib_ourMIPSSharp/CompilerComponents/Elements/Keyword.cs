namespace lib_ourMIPSSharp.CompilerComponents.Elements;

/// <summary>
/// Represents a keyword.
/// For Instruction and Magic keywords, the underlying value is their opcode.
/// For macro keywords, their underlying value has no meaning.
/// </summary>
public enum Keyword : uint {
    None = 0,

    // Magic Instructions
    Magic_Systerm = 0b000001_00000_00000_0000000000000000,
    Magic_Str_Sysout = 0b000001_00000_00000_0000000000000001,
    Magic_Reg_Sysout = 0b000001_00000_00000_0000000000000010,
    Magic_Reg_Sysin = 0b000001_00000_00000_0000000000000011,

    // Magic Instructions (ourMIPS-Sharp-Dialect)
    Magic_Mem_Sysout = 0b000001_00000_00000_0000000000000101,
    Magic_Char_Sysout = 0b000001_00000_00000_0000000000000110,

    // Memory Instructions
    Instruction_Ldd = 0b111000_00000_00000_0000000000000000,
    Instruction_Sto = 0b111001_00000_00000_0000000000000000,

    // Arithmetic Instructions with immediates
    Instruction_Shli = 0b011000_00000_00000_0000000000000000,
    Instruction_Shri = 0b011001_00000_00000_0000000000000000,
    Instruction_Roli = 0b011010_00000_00000_0000000000000000,
    Instruction_Rori = 0b011011_00000_00000_0000000000000000,
    Instruction_Subi = 0b011100_00000_00000_0000000000000000,
    Instruction_Addi = 0b011101_00000_00000_0000000000000000,

    // Arithmetic Instructions with registers
    Instruction_Shl = 0b010000_00000_00000_0000000000000000,
    Instruction_Shr = 0b010001_00000_00000_0000000000000000,
    Instruction_Rol = 0b010010_00000_00000_0000000000000000,
    Instruction_Ror = 0b010011_00000_00000_0000000000000000,
    Instruction_Sub = 0b010100_00000_00000_0000000000000000,
    Instruction_Add = 0b010101_00000_00000_0000000000000000,

    // Bitwise Operators
    Instruction_Or = 0b010111_00000_00000_0000000000000000,
    Instruction_And = 0b010111_00000_00000_0000000000000001,
    Instruction_Xor = 0b010111_00000_00000_0000000000000010,
    Instruction_Xnor = 0b010111_00000_00000_0000000000000011,

    // Jump Instructions
    Instruction_Jmp = 0b100000_00000_00000_0000000000000000,
    Instruction_Beq = 0b100010_00000_00000_0000000000000000,
    Instruction_Bneq = 0b100011_00000_00000_0000000000000000,
    Instruction_Bgt = 0b100100_00000_00000_0000000000000000,
    Instruction_Bo = 0b100101_00000_00000_0000000000000000,

    // Stack Instructions
    Instruction_Ldpc = 0b001000_00000_00000_0000000000000000,
    Instruction_Stpc = 0b001001_00000_00000_0000000000000000,

    // Non-Instruction Keyword
    Keyword_Macro = 1,
    Keyword_EndMacro = 2,
    Keyword_Mend = 3,
    Keyword_Alias = 4
}

public static class KeywordHelper {
    // Base mask for initial check
    private const uint MaskBase = 0b111111_00000_00000_0000000000000000;

    // Mask to differentiate bitwise instructions (after initial check found Instruction_Or)
    private const uint MaskExtended = 0b111111_00000_00000_0000000000000011;

    // Mask to differentiate magic instructions (after initial check found Magic_Systerm)
    private const uint MaskMagic = 0b111111_00000_00000_0000000000000111;

    /// <summary>
    /// Extracts the Instruction Keyword from the given instruction.
    /// Returns None if the opcode doesn't correspond to an instruction.
    /// </summary>
    /// <param name="instruction">a 32-bit sequence representing an instruction</param>
    /// <returns></returns>
    public static Keyword ExtractInstruction(this int instruction) => ExtractInstruction((uint)instruction);

    /// <summary>
    /// Extracts the Instruction Keyword from the given instruction.
    /// Returns None if the opcode doesn't correspond to an instruction.
    /// </summary>
    /// <param name="instruction">a 32-bit sequence representing an instruction</param>
    /// <returns></returns>
    public static Keyword ExtractInstruction(this uint instruction) {
        var kw = (Keyword)instruction & (Keyword)MaskBase;

        if (kw == Keyword.Instruction_Or)
            // Could be any Bitwise Operator
            kw = (Keyword)instruction & (Keyword)MaskExtended;

        if (kw == Keyword.Magic_Systerm)
            // Could be any Magic Instruction
            kw = (Keyword)instruction & (Keyword)MaskMagic;

        if (!Enum.IsDefined(kw) || (uint)kw < 5)
            kw = Keyword.None;
        return kw;
    }

    /// <summary>
    /// Finds matching keyword for a token.
    /// Does check that the token type is <c>TokenType.Word</c>.
    /// If no match is found, returns Keyword.None.
    /// Note that the 'sysout' keyword is overloaded and thus cannot be mapped to the correct overload.
    /// Any of the 'sysout' overlads may be returned in that case.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public static Keyword FromToken(Token token) {
        if (token.Content is null || token.Type != TokenType.Word)
            return Keyword.None;

        return Enum.GetValues<Keyword>().FirstOrDefault(kw =>
            kw.ToString().Split('_').Last().Equals(token.Content, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Checks whether a Keyword matches a Token.
    /// Does check that the token type is <c>TokenType.Word</c>.
    /// </summary>
    /// <param name="kw"></param>
    /// <param name="token"></param>
    /// <returns><c>true</c> if the token type is <c>TokenType.Word</c> and the token content matches the keyword;
    /// <c>false</c> otherwise.</returns>
    public static bool Matches(this Keyword kw, Token token) {
        if (token.Content is null || token.Type != TokenType.Word)
            return false;

        return kw.ToString().Split('_').Last().Equals(token.Content, StringComparison.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kw"></param>
    /// <returns>Whether the keyword params are two registers and an immeidate</returns>
    public static bool IsParamsRegRegImm(this Keyword kw) =>
        //    Range of Keywords from Shli to Addi                            Range of Keywords above Ldd
        kw is >= Keyword.Instruction_Shli and <= Keyword.Instruction_Addi or >= Keyword.Instruction_Ldd;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kw"></param>
    /// <returns>Whether the keyword params are three registers</returns>
    public static bool IsParamsRegRegReg(this Keyword kw) =>
        kw is >= Keyword.Instruction_Shl and <= Keyword.Instruction_Xnor;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kw"></param>
    /// <returns>Whether the keyword params are two registers and a label</returns>
    public static bool IsParamsRegRegLabel(this Keyword kw) =>
        kw is >= Keyword.Instruction_Beq and < Keyword.Instruction_Bo;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kw"></param>
    /// <returns>Whether the keyword params fit none of the other categories.</returns>
    public static bool IsParamsOther(this Keyword kw) =>
        !kw.IsParamsRegRegImm() && !kw.IsParamsRegRegReg() && !kw.IsParamsRegRegLabel();
}