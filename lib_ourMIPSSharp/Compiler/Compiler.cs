using System.Diagnostics;

namespace lib_ourMIPSSharp;

public class Compiler {
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
                        // TODO add case for colon once it's implemented
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
                            if (KeywordHelper.FromToken(token) == Keyword.None) {
                                // Assume instructions that aren't keywords to be macros
                                // TODO check to make sure this isn't a label definition
                                current.AddReferenceIfNotExists(token);
                                Debug.WriteLine($"Compiler.ReadMacros: Found macro reference {token.Content}");
                            }

                            if (Keyword.Keyword_Macro.Matches(token.Content))
                                throw new SyntaxError(
                                    $"Nested macro definition at line {token.Line}, col {token.Column}!");
                            
                            _state = CompilerState.MacroInstructionArgs;

                            if (Keyword.Keyword_EndMacro.Matches(token.Content)) {
                                if (Options.HasFlag(DialectOptions.StrictKeywordMend))
                                    throw new DialectSyntaxError("Keyword 'endmacro'", token,
                                        DialectOptions.StrictKeywordMend);

                                current.EndIndex = i;
                                _macros.Add(current.Name, current);
                                _state = CompilerState.MacroEnded;
                                Debug.WriteLine($"Compiler.ReadMacros: Found endmacro");
                            }

                            if (Keyword.Keyword_Mend.Matches(token.Content)) {
                                if (Options.HasFlag(DialectOptions.StrictKeywordEndmacro))
                                    throw new DialectSyntaxError("Keyword 'mend'", token,
                                        DialectOptions.StrictKeywordEndmacro);

                                current.EndIndex = i;
                                _macros.Add(current.Name, current);
                                _state = CompilerState.MacroEnded;
                                Debug.WriteLine($"Compiler.ReadMacros: Found mend");
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