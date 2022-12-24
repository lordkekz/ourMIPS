using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace lib_ourMIPSSharp;

public partial class Compiler {
    [GeneratedRegex(@"^(const|reg|label)[0-9]+$")]
    private static partial Regex GetYapjomaParamRegex();

    public static readonly Regex YapjomaParamRegex = GetYapjomaParamRegex();

    [GeneratedRegex(@"^\w[_\w\d]*$")]
    private static partial Regex GetCustomDescriptorRegex();

    public static readonly Regex CustomDescriptorRegex = GetCustomDescriptorRegex();

    public DialectOptions Options { get; }
    public List<Token> Tokens { get; }
    public List<Token> ResolvedTokens { get; } = new();
    public Dictionary<string, Macro> Macros { get; } = new();
    public Dictionary<string, int> Labels { get; } = new();

    public Compiler(List<Token> tokens, DialectOptions options) {
        if (!options.IsValid())
            throw new InvalidEnumArgumentException(nameof(options), (int)options, typeof(DialectOptions));

        Tokens = tokens;
        Options = options;
    }

    public void ReadMacros() {
        var h = new CompilerMacroReader(this);
        IterateTokens(h, CompilerState.InstructionStart, Tokens, 0, Tokens.Count);
        
        foreach (var macro in Macros.Values) {
            macro.Validate(Macros);
        }
    }

    public void IterateTokens(ICompilerHandler handler, CompilerState initialState, IList<Token> tokens,
        int startIndex, int endIndex) {
        var state = initialState;

        Debug.WriteLine($"[Compiler.IterateTokens] running from {startIndex} to {endIndex}");
        for (var i = startIndex; i < endIndex; i++) {
            var token = tokens[i];
            Debug.WriteLine($"[Compiler.IterateTokens] State {state}, Token {token}");
            switch (state) {
                case CompilerState.InstructionStart:
                    switch (token.Type) {
                        case TokenType.Word:
                            if (i < endIndex &&
                                tokens[i + 1].Type == TokenType.SingleChar &&
                                tokens[i + 1].Content.Equals(":")) {
                                // This lookahead may behave unexpectedly compared to a strictly non-lookahead implementation.
                                // e.g. it isn't be allowed for a comment to appear between label name and colon.

                                // This is a label declaration. Skip over the next token.
                                i += 1;
                                state = handler.OnLabelDeclaration(token, tokens[i]);
                            }
                            else {
                                // Token could be a valid instruction
                                state = handler.OnInstructionStart(token);
                            }

                            break;
                        case TokenType.Comment:
                        case TokenType.InstructionBreak:
                            // Ignore comments and empty lines.
                            break;
                        default:
                            throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                                  $"State {state} at line {token.Line}, col {token.Column}!");
                    }

                    break;
                case CompilerState.InstructionArgs:
                    switch (token.Type) {
                        case TokenType.Word:
                        case TokenType.Number:
                        case TokenType.String:
                            // Token could be a valid instruction argument
                            state = handler.OnInstructionArgs(token);
                            break;
                        case TokenType.Comment:
                        case TokenType.InstructionBreak:
                            state = CompilerState.InstructionStart;
                            handler.OnInstructionBreak(token);
                            break;
                        default:
                            throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                                  $"State {state} at line {token.Line}, col {token.Column}!");
                    }

                    break;
                case CompilerState.MacroDeclaration:
                    switch (token.Type) {
                        case TokenType.Word:
                            // Token could be a valid macro name
                            state = handler.OnMacroDeclaration(token);
                            break;
                        case TokenType.Comment:
                            // Just ignore comments, even though they can't appear here.
                            // We let the Tokenizer take care of that; this way we can easily add inline comments later.
                            break;
                        default:
                            throw new SyntaxError($"Missing macro name at line {token.Line}, col {token.Column}. " +
                                                  $"Got {token.Type} '{token.Content}' instead.");
                    }

                    break;
                case CompilerState.MacroDeclarationArgs:
                    switch (token.Type) {
                        case TokenType.Word:
                        case TokenType.SingleChar:
                            // Token could be a valid macro name
                            state = handler.OnMacroDeclarationArgs(token);
                            break;
                        case TokenType.InstructionBreak:
                            // Line break always ends macro declaration args
                            state = CompilerState.MacroInstructionStart;
                            handler.OnInstructionBreak(token);
                            break;
                        case TokenType.Comment:
                            // Just ignore comments, even though they can't appear here.
                            // We let the Tokenizer take care of that; this way we can easily add inline comments later.
                            break;
                        default:
                            throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                                  $"State {state} at line {token.Line}, col {token.Column}!");
                    }

                    break;
                case CompilerState.MacroDeclarationArgsEnded:
                    // This state has no handler method as it only serves syntax purposes.
                    if (token.Type == TokenType.InstructionBreak) {
                        state = CompilerState.MacroInstructionStart;
                        handler.OnInstructionBreak(token);
                    }
                    else if (token.Type != TokenType.Comment)
                        throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                              $"State {state} at line {token.Line}, col {token.Column}!");

                    break;
                case CompilerState.MacroInstructionStart:
                    switch (token.Type) {
                        case TokenType.Word:
                            if (i < endIndex &&
                                tokens[i + 1].Type == TokenType.SingleChar &&
                                tokens[i + 1].Content.Equals(":")) {
                                // This lookahead may behave unexpectedly compared to a strictly non-lookahead implementation.
                                // e.g. it isn't be allowed for a comment to appear between label name and colon.

                                // This is a macro label declaration. Skip over the next token.
                                i += 1;
                                state = handler.OnMacroLabelDeclaration(token, tokens[i+1]);
                            }
                            else {
                                // Token could be a valid instruction
                                state = handler.OnMacroInstructionStart(token);
                            }

                            break;
                        case TokenType.InstructionBreak:
                            state = CompilerState.MacroInstructionStart;
                            break;
                    }

                    break;
                case CompilerState.MacroInstructionArgs:
                    switch (token.Type) {
                        case TokenType.Word:
                        case TokenType.Number:
                        case TokenType.String:
                            // Token could be a valid instruction argument
                            state = handler.OnMacroInstructionArgs(token);
                            break;
                        case TokenType.Comment:
                        case TokenType.InstructionBreak:
                            state = CompilerState.MacroInstructionStart;
                            handler.OnInstructionBreak(token);
                            break;
                        default:
                            throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                                  $"State {state} at line {token.Line}, col {token.Column}!");
                    }

                    break;
                case CompilerState.MacroEnded:
                    // This state has no handler method as it only serves syntax purposes.
                    switch (token.Type) {
                        case TokenType.InstructionBreak:
                            state = CompilerState.InstructionStart;
                            handler.OnInstructionBreak(token);
                            break;
                        case TokenType.Comment:
                            break;
                        default:
                            throw new SyntaxError($"Unexpected Token '{token.Content}' of Type {token.Type} in " +
                                                  $"State {state} at line {token.Line}, col {token.Column}!");
                    }

                    break;
                case CompilerState.None:
                    if (token.Type == TokenType.InstructionBreak) {
                        state = CompilerState.InstructionStart;
                        handler.OnInstructionBreak(token);
                    }
                    break;
            }
        }
        Debug.WriteLine("[Compiler.IterateTokens] ended");
    }

    public IList<Token> ResolveMacros() {
        var h = new CompilerMacroResolver(this);
        IterateTokens(h, CompilerState.InstructionStart, Tokens, 0, Tokens.Count);
        return ResolvedTokens;
    }

    public void ReadLabels() {
        var h = new CompilerLabelReader(this);
        IterateTokens(h, CompilerState.InstructionStart, ResolvedTokens, 0, Tokens.Count);
    }

    public List<int> GenerateBytecode() {
        return new List<int>();
    }
}

public enum CompilerState {
    None,
    InstructionStart,
    InstructionArgs,
    MacroDeclaration,
    MacroDeclarationArgs,
    MacroDeclarationArgsEnded,
    MacroInstructionStart,
    MacroInstructionArgs,
    MacroEnded
}