using System.Collections.Immutable;
using System.Diagnostics;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.Errors;

namespace lib_ourMIPSSharp.CompilerComponents;

/// <summary>
/// Manages the build pipeline.
/// </summary>
public class Builder {
    public string SourceCode { get; }
    public DialectOptions Options { get; }
    public ImmutableArray<Token> Tokens { get; private set; }
    public ImmutableArray<Token> ResolvedTokens { get; private set; }
    public ImmutableDictionary<string, int> Labels { get; private set; }
    public ImmutableArray<uint> Bytecode { get; private set; }
    public string StringConstants { get; private set; }
    
    public TextWriter TextErr { get; set; } = Console.Error;
    public TextWriter TextOut { get; set; } = Console.Out;

    private Tokenizer _tokenizer;
    private Compiler _compiler;

    public Builder(string sourcecode, DialectOptions opts = DialectOptions.None) {
        SourceCode = sourcecode;
        Options = opts;
    }

    public bool FullBuild() {
        TextOut.WriteLine("[BUILDER] Starting build...");
        var stopwatch = Stopwatch.StartNew();
        try {
            TextOut.WriteLine($"[BUILDER] Tokenizing source code...");
            _tokenizer = new Tokenizer(SourceCode, Options);
            var tokens = _tokenizer.Tokenize();
            Tokens = tokens.ToImmutableArray();
            
            TextOut.WriteLine($"[BUILDER] Reading macros (1st iteration of compiler)...");
            _compiler = new Compiler(tokens, Options);
            _compiler.ReadMacros();
            
            TextOut.WriteLine($"[BUILDER] Resolving macros (2nd iteration of compiler)...");
            var resolvedTokens = _compiler.ResolveMacros();
            ResolvedTokens = resolvedTokens.ToImmutableArray();
            
            TextOut.WriteLine($"[BUILDER] Reading labels (3rd iteration of compiler)...");
            _compiler.ReadLabels();
            Labels = _compiler.Labels.ToImmutableDictionary();
            
            TextOut.WriteLine($"[BUILDER] Generating Bytecode (4th iteration of compiler)...");
            var bytecode = _compiler.GenerateBytecode();
            Bytecode = bytecode.ToImmutableArray();
            StringConstants = _compiler.StringConstants;
            
            stopwatch.Stop();
            TextOut.WriteLine($"[BUILDER] Build succeeded after {stopwatch.ElapsedMilliseconds}ms.");
            return true;
        }
        catch (CompilerError err) {
            stopwatch.Stop();
            TextOut.WriteLine($"[BUILDER] Build failed after {stopwatch.ElapsedMilliseconds}ms: ");
            TextErr.WriteLine(err);
        }
        catch (Exception err) {
            stopwatch.Stop();
            TextOut.WriteLine($"[BUILDER] Build failed with internal exception after {stopwatch.ElapsedMilliseconds}ms!");
            TextErr.WriteLine(err);
        }

        return false;
    }
}