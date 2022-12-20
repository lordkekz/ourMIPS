using System.Diagnostics;
using System.Text.RegularExpressions;

namespace lib_ourMIPSSharp;

public partial class Compiler {

    [GeneratedRegex("^(const|reg|label)[0-9]+$")]
    private static partial Regex GetYapjomaParamRegex();
    public static readonly Regex YapjomaParamRegex = GetYapjomaParamRegex();
    [GeneratedRegex("^\\w[_\\w\\d]*$")]
    private static partial Regex GetCustomDescriptorRegex();
    public static readonly Regex CustomDescriptorRegex = GetCustomDescriptorRegex();
    
    public List<Token> Tokens { get; }
    public DialectOptions Options { get; }

    private Dictionary<string, Macro> _macros;
    private CompilerState _state;

    public Compiler(List<Token> tokens, DialectOptions options) {
        Tokens = tokens;
        Options = options;
    }

    public void ReadMacros() {
        _macros = new Dictionary<string, Macro>();
        _state = CompilerState.InstructionStart;
        Macro current = null;

        Debug.WriteLine("Compiler.ReadMacros running");
        for (var i = 0; i < Tokens.Count; i++) {
            var token = Tokens[i];
            Debug.WriteLine($"Compiler.ReadMacros: State {_state}, Token {token}");
            switch (_state) {
                case CompilerState.InstructionStart:
                    if (token.Type == TokenType.Word && Keyword.Keyword_Macro.Matches(token.Content)) {
                        _state = CompilerState.MacroDeclaration;
                        current = new Macro(Options);
                    }

                    break;
                case CompilerState.MacroDeclaration:
                    if (token.Type == TokenType.Word) {
                        _state = CompilerState.MacroDeclarationArgs;
                        current.SetName(token);
                        Debug.WriteLine($"Compiler.ReadMacros: Found macro name {current.Name}");
                    }
                    else
                        throw new SyntaxError(
                            $"Missing macro name at line {token.Line}, col {token.Column}. Got {token.Type} '{token.Content}' instead.");

                    break;
                case CompilerState.MacroDeclarationArgs:
                    switch (token.Type) {
                        case TokenType.Word:
                            current.AddParameter(token);
                            Debug.WriteLine($"Compiler.ReadMacros: Found macro param {token.Content}");
                            break;
                        case TokenType.SingleChar:
                            if (token.Content.Equals(":"))
                                break;
                            // TODO eventually add a check to prevent irregular comma-placement in args
                            break;
                        case TokenType.Comment:
                            _state = CompilerState.MacroDeclarationArgsEnded;
                            break;
                        case TokenType.InstructionBreak:
                            _state = CompilerState.MacroInstructionStart;
                            break;
                        default:
                            throw new SyntaxError(
                                $"Unexpected token at line {token.Line}, col {token.Column} of type {token.Type}: '{token.Content}'");
                    }

                    break;
                case CompilerState.MacroDeclarationArgsEnded:
                    if (token.Type == TokenType.InstructionBreak) {
                        _state = CompilerState.MacroInstructionStart;
                    }
                    else if (token.Type != TokenType.Comment)
                        throw new SyntaxError(
                            $"Unexpected token at line {token.Line}, col {token.Column} of type {token.Type}: '{token.Content}'");

                    break;
                case CompilerState.MacroInstructionStart:
                    switch (token.Type) {
                        case TokenType.Word:
                            switch (KeywordHelper.FromToken(token)) {
                                case Keyword.None:
                                    // token doesn't match any keyword
                                    if (i + 1 < Tokens.Count &&
                                        Tokens[i + 1].Type == TokenType.SingleChar &&
                                        Tokens[i + 1].Content.Equals(":")) {
                                        // This is a label declaration. Skip over the next token.
                                        i += 1;
                                        _state = CompilerState.MacroInstructionArgs;
                                    }
                                    else {
                                        // This must be a macro call. Add Reference.
                                        current.AddReferenceIfNotExists(token);
                                        Debug.WriteLine($"Compiler.ReadMacros: Found macro reference {token.Content}");
                                    }

                                    break;
                                case Keyword.Keyword_Macro:
                                    throw new SyntaxError(
                                        $"Nested macro definition at line {token.Line}, col {token.Column}!");
                                
                                case Keyword.Keyword_EndMacro:
                                    if (Options.HasFlag(DialectOptions.StrictKeywordMend))
                                        throw new DialectSyntaxError("Keyword 'endmacro'", token,
                                            DialectOptions.StrictKeywordMend);

                                    current.EndIndex = i;
                                    _macros.Add(current.Name, current);
                                    _state = CompilerState.MacroEnded;
                                    Debug.WriteLine($"Compiler.ReadMacros: Found endmacro");

                                    break;
                                case Keyword.Keyword_Mend:
                                    if (Options.HasFlag(DialectOptions.StrictKeywordEndmacro))
                                        throw new DialectSyntaxError("Keyword 'mend'", token,
                                            DialectOptions.StrictKeywordEndmacro);

                                    current.EndIndex = i;
                                    _macros.Add(current.Name, current);
                                    _state = CompilerState.MacroEnded;
                                    Debug.WriteLine($"Compiler.ReadMacros: Found mend");

                                    break;
                                default:
                                    _state = CompilerState.MacroInstructionArgs;
                                    break;
                            }
                            
                            break;
                        case TokenType.InstructionBreak:
                            _state = CompilerState.MacroInstructionStart;
                            break;
                    }

                    break;
                case CompilerState.MacroInstructionArgs:
                    if (token.Type == TokenType.InstructionBreak)
                        _state = CompilerState.MacroInstructionStart;

                    break;
                case CompilerState.MacroEnded:
                    switch (token.Type) {
                        case TokenType.InstructionBreak:
                            _state = CompilerState.InstructionStart;
                            break;
                        case TokenType.Comment:
                            break;
                        default:
                            throw new SyntaxError(
                                $"Unexpected token at line {token.Line}, col {token.Column} of type {token.Type}: '{token.Content}'");
                    }

                    break;
                case CompilerState.None:
                case CompilerState.LabelDeclaration:
                case CompilerState.InstructionArgs:
                    if (token.Type == TokenType.InstructionBreak)
                        _state = CompilerState.InstructionStart;
                    break;
            }
        }
        
        foreach (var macro in _macros.Values) {
            macro.Validate(_macros);
        }
    }

    public List<int> GenerateBytecode() {
        var result = new List<int>();

        return null;
    }

    private enum CompilerState {
        None,
        InstructionStart,
        InstructionArgs,
        LabelDeclaration,
        MacroDeclaration,
        MacroDeclarationArgs,
        MacroDeclarationArgsEnded,
        MacroInstructionStart,
        MacroInstructionArgs,
        MacroEnded
    }
}