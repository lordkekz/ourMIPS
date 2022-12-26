using System.Text.RegularExpressions;

namespace lib_ourMIPSSharp;

public class Macro {
    public DialectOptions Options { get; }
    public string? Name { get; private set; }
    public int StartIndex = -1;
    public int EndIndex = -1;
    public List<string> Params { get; } = new();
    public List<string> Labels { get; } = new();

    private List<Tuple<string, Token>> _references = new();
    private List<Tuple<string, Token[]>>? _all_references;

    public Macro(DialectOptions options) {
        Options = options;
    }

    public void SetName(Token token) {
        var name = token.Content;

        if (KeywordHelper.FromToken(token) != Keyword.None)
            throw new SyntaxError(token, $"Illegal macro name '{token.Content}' (matches keyword).");

        if (!Compiler.CustomDescriptorRegex.IsMatch(token.Content))
            throw new SyntaxError(token, $"Illegal macro name '{token.Content}'.");
        
        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            name = name.ToLowerInvariant();

        Name = name;
    }

    public void AddParameter(Token token) {
        var name = token.Content;

        if (Options.HasFlag(DialectOptions.StrictMacroArgumentNames) && !Compiler.YapjomaParamRegex.IsMatch(token.Content))
            throw new DialectSyntaxError("Custom macro argument name", token, DialectOptions.StrictMacroArgumentNames);

        if (!Compiler.CustomDescriptorRegex.IsMatch(token.Content))
            throw new SyntaxError(token, $"Illegal macro parameter name '{token.Content}'");

        Params.Add(name);
    }

    public void AddLabel(Token token) {
        var name = token.Content;

        if (!Compiler.CustomDescriptorRegex.IsMatch(token.Content))
            throw new SyntaxError(token, $"Illegal label name '{token.Content}'");

        Labels.Add(name);
    }

    public void AddReferenceIfNotExists(Token token) {
        var name = token.Content;

        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            name = name.ToLowerInvariant();

        if (!_references.Any(t => Name.Equals(t.Item1)))
            _references.Add(new Tuple<string, Token>(name, token));
    }

    public List<Tuple<string, Token[]>> Validate(Dictionary<string, Macro> macros) {
        if (_all_references is not null)
            return _all_references;
        
        _all_references = new List<Tuple<string, Token[]>>();
        foreach (var (m2_name, t1) in _references) {
            // Direct recursion
            if (Name.Equals(m2_name))
                throw new RecursionError(this, t1);

            // References unknown macro
            if (!macros.ContainsKey(m2_name))
                throw new UndefinedSymbolError(t1);
            
            // Find referenced macro
            var m2 = macros[m2_name];
            
            // Defined before referenced macro
            if (Options.HasFlag(DialectOptions.StrictMacroDefinitionOrder) && m2.EndIndex >= StartIndex)
                throw new DialectSyntaxError($"Usage of macro '{m2_name}' before its definition", t1,
                    DialectOptions.StrictMacroDefinitionOrder);

            // Recursively validate whether m2 references this
            foreach (var (m3_name, tokens) in m2.Validate(macros)) {
                if (Name.Equals(m3_name))
                    throw new RecursionError(this, t1, tokens);
                
                // Add references of m2 to references of this, but add t1 to token stacktrace
                _all_references.Add(new Tuple<string, Token[]>(m3_name, tokens.Append(t1).ToArray()));
            }
            
            // Add m2 to this' references. This is safe because if m2 referenced m2, it would have thrown an error.
            _all_references.Add(new Tuple<string, Token[]>(m2_name, new []{t1}));
        }

        return _all_references;
    }

    public override string ToString() {
        return $"macro {Name}({string.Join(", ", Params)})";
    }

    public int GetMatchingParamIndex(string pName) {
        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            pName = pName.ToLowerInvariant();

        return Params.IndexOf(pName);
    }
    
    public int GetMatchingParamIndex(Token token) => token.Type == TokenType.Word ? GetMatchingParamIndex(token.Content) : -1;

    public int GetMatchingLabelIndex(string lName) {
        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            lName = lName.ToLowerInvariant();

        return Labels.IndexOf(lName);
    }
    
    public int GetMatchingLabelIndex(Token token) => token.Type == TokenType.Word ? GetMatchingLabelIndex(token.Content) : -1;
}