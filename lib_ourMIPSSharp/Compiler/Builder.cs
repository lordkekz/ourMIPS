using System.Collections.Immutable;
using System.Diagnostics;

namespace lib_ourMIPSSharp; 

/// <summary>
/// Manages the build pipeline.
/// </summary>
public class Builder {
    public string SourceCode { get; }
    public DialectOptions Options { get; }
    public ImmutableArray<Token> Tokens { get; private set; }
    public ImmutableArray<Token> ResolvedTokens { get; private set; }
    public ImmutableArray<int> Bytecode { get; private set; }

    private Tokenizer _tokenizer;
    private Compiler _compiler;

    public Builder(string sourcecode, DialectOptions? opts) {
        SourceCode = sourcecode;
        Options = opts ?? DialectOptions.None;
    }

    public bool FullBuild() {
        Console.WriteLine("[BUILDER] Starting build...");
        var stopwatch = Stopwatch.StartNew();
        try {
            Console.WriteLine($"[BUILDER] Tokenizing source code...");
            _tokenizer = new Tokenizer(SourceCode, Options);
            var tokens = _tokenizer.Tokenize();
            Tokens = tokens.ToImmutableArray();
            
            Console.WriteLine($"[BUILDER] Reading macros (1st iteration of compiler)...");
            _compiler = new Compiler(tokens, Options);
            _compiler.ReadMacros();
            
            Console.WriteLine($"[BUILDER] Resolving macros (2nd iteration of compiler)...");
            var resolvedTokens = _compiler.ResolveMacros();
            ResolvedTokens = resolvedTokens.ToImmutableArray();
            
            // Console.WriteLine($"[BUILDER] Reading labels (3rd iteration of compiler)...");
            // _compiler.ReadLabels();
            //
            // Console.WriteLine($"[BUILDER] Generating Bytecode (4th iteration of compiler)...");
            // var bytecode = _compiler.GenerateBytecode();
            // ResolvedTokens = resolvedTokens.ToImmutableArray();
            
            stopwatch.Stop();
            Console.WriteLine($"[BUILDER] Build succeeded after {stopwatch.ElapsedMilliseconds}ms.");
            return true;
        }
        catch (CompilerError err) {
            stopwatch.Stop();
            Console.WriteLine($"[BUILDER] Build failed after {stopwatch.ElapsedMilliseconds}ms: ");
            Console.Error.WriteLine(err);
        }
        catch (Exception err) {
            stopwatch.Stop();
            Console.WriteLine($"[BUILDER] Build failed with internal exception after {stopwatch.ElapsedMilliseconds}ms!");
            Console.Error.WriteLine(err);
        }

        return false;
    }
}