using System.Diagnostics;
using lib_ourMIPSSharp;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;

namespace ourMIPSSharp_CLI;

public class CompilerDebug {
    void PrintTokens(Builder builder) {
        var debugprint = "Tokens:\n";
        foreach (var token in builder.Tokens) {
            debugprint += token + "    ";
        }

        Debug.WriteLine(debugprint);
    }

    /// Prints the resolved tokens of the MIPS program to the debug output
    void PrintResolvedTokens(Builder builder) {
        var debugprint = "ResolvedTokens:\n";
        foreach (var token in builder.ResolvedTokens) {
            debugprint += token.Content + " ";
            if (token.Type == TokenType.InstructionBreak) {
                debugprint += "\n";
            }
        }

        Debug.WriteLine(debugprint);
    }

    /// Prints the labels of the MIPS program to the debug output
    void PrintLabels(Builder builder) {
        var debugprint = "Labels:\n";
        foreach (var pair in builder.Labels) {
            debugprint += $"{pair.Value} : {pair.Key}\n";
        }

        Debug.WriteLine(debugprint);
    }

    /// Prints the string constants of the MIPS program to the debug output
    void PrintStringConstants(Builder builder) {
        Debug.WriteLine($"StringConstants: '{builder.StringConstants.Replace("\0", "\\0")}'");
    }

    /// Prints the bytecode of the MIPS program to the debug output
    void PrintBytecode(Builder builder) {
        var debugprint = $"Bytecode: (length: {builder.Bytecode.Length})\n";
        foreach (var instruction in builder.Bytecode) {
            debugprint += $"{Convert.ToString(instruction, 2).PadLeft(32, '0')}\n";
        }

        Debug.WriteLine(debugprint);
    }

    /// Main program
    public Builder Main() {
        // Read the source code
        // var sourceCode = File.ReadAllText("../../../sort_philos.ourMIPS");
        var sourceCode = File.ReadAllText("../../../../lib_ourMIPSSharp_Tests/Samples/sort_philos.ourMIPS");

        // Build the MIPS program
        var builder = new Builder(sourceCode, DialectOptions.None);

        // Perform a full build of the program
        var success = builder.FullBuild();

        try {
            // Print debug information
            Debug.WriteLine("\n\nDEBUG STUFF:");
            PrintTokens(builder);
            PrintResolvedTokens(builder);
            PrintLabels(builder);
            PrintStringConstants(builder);
            PrintBytecode(builder);
        }
        catch (NullReferenceException ex) {
            if (success)
                Debug.WriteLine(ex);
            else {
                Debug.WriteLine("\n\n(Build failed; end of Debug)");
            }
        }

        return builder;
    }
}