namespace lib_ourMIPSSharp;

public class Macro {
    public DialectOptions Options { get; }
    public string Name { get; private set; }
    public int StartIndex;
    public int EndIndex;

    private List<string> _params { get; } = new();
    private List<Tuple<string, Token>> _references { get; } = new();

    public Macro(DialectOptions options) {
        Options = options;
    }

    public void SetName(Token token) {
        var name = token.Content;
        // TODO Add some checks against overriding keyword etc.

        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            name = name.ToLowerInvariant();

        Name = name;
    }

    public void AddParameter(Token token) {
        var name = token.Content;
        // TODO Add some checks depending on dialect

        _params.Add(name);
    }

    public void AddReferenceIfNotExists(Token token) {
        var name = token.Content;
        // TODO Add some checks depending on dialect

        if (!Options.HasFlag(DialectOptions.StrictCaseSensitiveDescriptors))
            name = name.ToLowerInvariant();

        if (!_references.Any(t => Name.Equals(t.Item1)))
            _references.Add(new Tuple<string, Token>(name, token));
    }

    public void ValidateNonRecursive(Dictionary<string, Macro> macros) {
        foreach (var reference in _references) {
            string m2_name = reference.Item1;
            Token t1 = reference.Item2;

            if (Name.Equals(reference.Item1))
                throw new RecursionError(this, t1);

            if (!macros.ContainsKey(m2_name))
                throw new UndefinedSymbolError(t1);
            
            var m2 = macros[m2_name];
            var reference2 = m2._references.FirstOrDefault(t => Name.Equals(t.Item1));
            if (reference2 is not null)
                throw new RecursionError(this, t1, m2, reference2.Item2);
        }
    }
}