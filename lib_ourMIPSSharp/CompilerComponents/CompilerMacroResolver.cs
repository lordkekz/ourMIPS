using System.Collections.Immutable;
using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.CompilerComponents;

public class CompilerMacroResolver : ICompilerHandler {
    public Compiler Comp { get; }
    public DialectOptions Options => Comp.Options;
    public IDictionary<string, Macro> Macros => Comp.Macros;
    public List<Token> ResolvedTokens => Comp.ResolvedTokens;
    public List<SymbolPosition[]> SymbolStackTable => Comp.SymbolStackTable;

    private readonly Dictionary<Macro, int> _counters = new();
    private readonly Stack<StackEntry> _stack = new();
    private StackEntry? _current;

    public CompilerMacroResolver(Compiler comp) {
        Comp = comp;
    }

    private record StackEntry(Macro Macro, int Line, int Column, List<Token> Params);

    public CompilerState OnInstructionStart(Token token) {
        switch (KeywordHelper.FromToken(token)) {
            case Keyword.Keyword_Macro:
                return CompilerState.MacroDeclaration;
            case Keyword.None:
                // This must be a macro call.
                Debug.WriteLine($"[CompilerMacroResolver] Found macro reference {token.Content}");

                var mName = token.Content!;
                if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
                    mName = mName.ToLowerInvariant();

                if (Macros.TryGetValue(mName, out var m)) {
                    // Macro found.
                    _current = new StackEntry(m, token.Line, token.Column, new List<Token>());
                    return CompilerState.InstructionArgs;
                }
                
                // Macro not found. If error doesn't throw, pretend this was a keyword instruction and continue.
                Comp.HandleError(new UndefinedSymbolError(token));
                break;
        }

        // This is a keyword instruction. (Or a non-fatal UndefinedSymbolError)
        ResolvedTokens.Add(token);

        // Export current stack
        SymbolStackTable.Add(_stack
            .Select(e => new SymbolPosition(e.Line, e.Column))
            .Append(new SymbolPosition(token.Line, token.Column))
            .ToArray()
        );
        
        return CompilerState.InstructionArgs;
    }

    public void OnInstructionBreak(Token token) {
        if (_current is not null) {
            var m = _current.Macro;
            if (!_counters.ContainsKey(m)) _counters[m] = 0;

            _stack.Push(_current);
            _current = null;
            Comp.IterateTokens(this, CompilerState.InstructionStart, Comp.Tokens, m.StartIndex, m.EndIndex);
            _stack.Pop();
            _counters[m] += 1;
        }
        else {
            Debug.WriteLine($"[CompilerMacroResolver] Instruction Break. Token: {token}");
            ResolvedTokens.Add(token);
        }
    }

    /// <summary>
    /// Resolve the value of a macro parameter.
    /// If token is no matching parameter is found, the original token is returned.
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    private Token ResolveParamValue(Token pToken) {
        var token = pToken;
        foreach (var entry in _stack) {
            var i = entry.Macro.GetMatchingParamIndex(token);
            if (i >= 0)
                token = entry.Params[i];
        }

        Debug.WriteLine($"[CompilerMacroResolver] Param {pToken}   ->   {token}");
        return token;
    }

    private void AddUniqueLabelName(Token token) {
        var peek = _stack.Peek();
        var newLabelName = $"@{peek.Macro.Name}[{_counters.GetValueOrDefault(peek.Macro)}]_{token.Content}";
        var newToken = new Token(Options) {
            Content = newLabelName,
            Line = token.Line,
            Column = token.Column,
            Type = TokenType.Word
        };

        ResolvedTokens.Add(newToken);
        Debug.WriteLine($"[CompilerMacroResolver] Label {token}   ->   {newToken}");
    }

    public CompilerState OnInstructionArgs(Token token) {
        if (_current is not null) {
            _current.Params.Add(token);
            return CompilerState.InstructionArgs;
        }

        if (_stack.Count > 0 && _stack.Peek().Macro.GetMatchingLabelIndex(token) >= 0)
            AddUniqueLabelName(token);
        else
            ResolvedTokens.Add(ResolveParamValue(token));
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnLabelDeclaration(Token token, Token colon) {
        if (_stack.Count == 0) {
            // If not currently in any macro, don't change names.
            ResolvedTokens.Add(token);
            ResolvedTokens.Add(colon);
            return CompilerState.InstructionArgs;
        }

        AddUniqueLabelName(token);
        ResolvedTokens.Add(colon);
        return CompilerState.InstructionArgs;
    }

    public CompilerState OnMacroInstructionStart(Token token) {
        switch (KeywordHelper.FromToken(token)) {
            case Keyword.Keyword_Macro:
                throw new SyntaxError(token, $"Nested macro definition.");
            case Keyword.Keyword_EndMacro:
            case Keyword.Keyword_Mend:
                return CompilerState.MacroEnded;
            default:
                return CompilerState.MacroInstructionArgs;
        }
    }
}

public struct SymbolPosition {
    public SymbolPosition(int line, int column) {
        Line = line;
        Column = column;
    }

    public int Line { get; }
    public int Column { get; }
}