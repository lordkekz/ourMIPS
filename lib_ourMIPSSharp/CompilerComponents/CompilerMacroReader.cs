using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.CompilerComponents;

public class CompilerMacroReader : ICompilerHandler {
    public Compiler Comp { get; }
    public DialectOptions Options => Comp.Options;
    private Macro _current;

    public CompilerMacroReader(Compiler comp) {
        Comp = comp;
        _current = new Macro(Options);
    }

    public CompilerState OnInstructionStart(Token token) {
        if (!Keyword.Keyword_Macro.Matches(token))
            // Assume instruction is ok. Will be checked in Compiler.GenerateBytecode.
            return CompilerState.InstructionArgs;

        // Found a macro keyword
        _current = new Macro(Options);
        return CompilerState.MacroDeclaration;
    }

    public CompilerState OnMacroDeclaration(Token token) {
        Debug.WriteLine($"[CompilerMacroReader] Found macro name {_current.Name}");
        _current.SetName(token);
        return CompilerState.MacroDeclarationArgs;
    }

    public CompilerState OnMacroDeclarationArgs(Token token) {
        if (token.Type != TokenType.Word) return CompilerState.MacroDeclarationArgs;

        Debug.WriteLine($"[CompilerMacroReader] Found macro param {token.Content}");
        _current.AddParameter(token);
        return CompilerState.MacroDeclarationArgs;
    }

    public CompilerState OnMacroInstructionStart(Token token) {
        if (_current.StartIndex == -1)
            // Set start index to index of first instruction in macro body
            _current.StartIndex = Comp.Tokens.IndexOf(token);

        switch (KeywordHelper.FromToken(token)) {
            case Keyword.None:
                // Token doesn't match any keyword.
                // This must be a macro call. Add Reference.
                _current.AddReferenceIfNotExists(token);
                Debug.WriteLine($"[CompilerMacroReader] Found macro reference {token.Content}");

                return CompilerState.MacroInstructionArgs;
            case Keyword.Keyword_Macro:
                throw new SyntaxError(token, $"Nested macro definition.");

            case Keyword.Keyword_EndMacro:
                Debug.WriteLine($"[CompilerMacroReader] Found endmacro");
                if (Options.HasFlag(DialectOptions.StrictKeywordMend))
                    Comp.HandleError(new DialectSyntaxError("Keyword 'endmacro'", token,
                        DialectOptions.StrictKeywordMend));

                _current.EndIndex = Comp.Tokens.IndexOf(token);
                Comp.Macros.Add(_current.Name, _current);
                return CompilerState.MacroEnded;
            case Keyword.Keyword_Mend:
                Debug.WriteLine($"[CompilerMacroReader] Found mend");
                if (Options.HasFlag(DialectOptions.StrictKeywordEndmacro))
                    Comp.HandleError(new DialectSyntaxError("Keyword 'mend'", token,
                        DialectOptions.StrictKeywordEndmacro));

                _current.EndIndex = Comp.Tokens.IndexOf(token);
                Comp.Macros.Add(_current.Name, _current);
                return CompilerState.MacroEnded;
            default:
                return CompilerState.MacroInstructionArgs;
        }
    }

    public CompilerState OnMacroLabelDeclaration(Token token, Token colon) {
        if (_current.StartIndex == -1)
            // Set start index to index of first label declaration in macro body
            _current.StartIndex = Comp.Tokens.IndexOf(token);

        _current.AddLabel(token);
        return CompilerState.MacroInstructionArgs;
    }
}