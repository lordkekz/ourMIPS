namespace lib_ourMIPSSharp; 

[Flags]
public enum DialectOptions : int {
    /// <summary>
    /// Standard dialect, allows everything.
    /// </summary>
    None = 0,

    /// <summary>
    /// Enforce that non-decimal numbers are not suffixed with 'b' or 'h'. (Yapjoma)
    /// </summary>
    StrictNonDecimalNumbers = 1,
    
    /// <summary>
    /// Enforce that non-decimal numbers are exactly 16 Bits long, implying them to be in two's complement. (Yapjoma)
    /// </summary>
    StrictNonDecimalNumberLengths = 2,
    
    /// <summary>
    /// Enforce that decimal numbers are less than 16 Bits long, implying them to be regularly signed. (Yapjoma)
    /// </summary>
    StrictDecimalNumberLengths = 4,
    
    /// <summary>
    /// Enforce that custom descriptors (aliases, macros, labels) are case-sensitive. (Philosonline)
    /// </summary>
    StrictCaseSensitiveDescriptors = 8,
    
    /// <summary>
    /// Enforce that marco blocks are ended with the 'endmacro' keyword. (Philosonline)
    /// </summary>
    StrictKeywordEndmacro = 16,
    
    /// <summary>
    /// Enforce that marco blocks are ended with the 'mend' keyword. (Yapjoma)
    /// </summary>
    StrictKeywordMend = 32,
    
    /// <summary>
    /// Enforce that no colon may be placed after the argument list of a macro. (Yapjoma)
    /// </summary>
    StrictNoColonAfterMacro = 64,
    
    /// <summary>
    /// Enforces that macros are defined before they are used. (Philosonline)
    /// </summary>
    StrictMacroDefinitionOrder = 128,
    
    /// <summary>
    /// Enforces that macro arguments are named with their type (reg/const/label) and suffixed with a number. (Yapjoma)
    /// </summary>
    StrictMacroArgumentNames = 256,
    
    /// <summary>
    /// Enforces a configuration of options that corresponds to the Philosonline dialect.
    /// </summary>
    Philosonline = StrictCaseSensitiveDescriptors | StrictKeywordEndmacro | StrictMacroDefinitionOrder,
    
    /// <summary>
    /// Enforces a configuration of options that corresponds to the Yapjoma dialect.
    /// </summary>
    Yapjoma = StrictNonDecimalNumbers | StrictNonDecimalNumberLengths | StrictDecimalNumberLengths |
              StrictKeywordMend | StrictNoColonAfterMacro | StrictMacroArgumentNames
}

public static class DialectOptionsExtensions {
    public static bool IsValid(this DialectOptions options) {
        if (options.HasFlag(DialectOptions.StrictKeywordEndmacro) &&
            options.HasFlag(DialectOptions.StrictKeywordMend))
            // There must be at least one keyword for ending macros.
            return false;
        
        return true;
    }
}