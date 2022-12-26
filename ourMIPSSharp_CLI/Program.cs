using System.Diagnostics;
using lib_ourMIPSSharp;

// Console.WriteLine(Convert.ToInt16("44", 10));
// Console.WriteLine(Convert.ToInt16("-44", 10));
// Console.WriteLine(Convert.ToInt16("101100", 2));
// Console.WriteLine(Convert.ToInt16("1111111111010100", 2));
// Console.WriteLine(Convert.ToInt16("2C", 16));
// Console.WriteLine(Convert.ToInt16("FFD4", 16));

// Console.WriteLine(Convert.ToInt16("32767", 10));
// Console.WriteLine(Convert.ToInt16("32768", 10)); // Overflow
// Console.WriteLine(Convert.ToInt16("1111111111111111", 2));
// Console.WriteLine(Convert.ToInt16("10000000000000000", 2)); // Overflow
// Console.WriteLine(Convert.ToInt16("0FFFF", 16));
// Console.WriteLine(Convert.ToInt16("10000", 16)); // Overflow

// var num = "55h";
// Console.WriteLine(num);
// foreach (var format in Enum.GetValues(typeof(NumberLiteralFormat))) {
//     var tmp = new NumberLiteral(new Token(DialectOptions.None) { Content = num });
//     num = tmp.ToString((NumberLiteralFormat)format);
//     Console.WriteLine(num + " " + tmp);
// }
// Console.WriteLine(new NumberLiteral(new Token(DialectOptions.None) { Content = num }));

// Console.WriteLine(RegisterHelper.FromString("r9"));

// Console.WriteLine(new NumberLiteral(new Token(DialectOptions.None) {Content = "32768"}).Value);

// Console.WriteLine(Convert.ToString(12, 2));

// Console.WriteLine(new NumberLiteral("404", DialectOptions.None).ToString(NumberLiteralFormat.Decimal));

// var reg = Register.Zero;
// var reg2 = RegisterHelper.FromString("zero");
//
// Console.WriteLine(reg + " " + reg2);

// Prints the tokens of the MIPS program to the debug output
void PrintTokens(Builder builder)
{
    var debugprint = "Tokens:\n";
    foreach (var token in builder.Tokens)
    {
        debugprint += token + "    ";
    }
    Debug.WriteLine(debugprint);
}

// Prints the resolved tokens of the MIPS program to the debug output
void PrintResolvedTokens(Builder builder)
{
    var debugprint = "ResolvedTokens:\n";
    foreach (var token in builder.ResolvedTokens)
    {
        debugprint += token.Content + " ";
        if (token.Type == TokenType.InstructionBreak)
        {
            debugprint += "\n";
        }
    }
    Debug.WriteLine(debugprint);
}

// Prints the labels of the MIPS program to the debug output
void PrintLabels(Builder builder)
{
    var debugprint = "Labels:\n";
    foreach (var pair in builder.Labels)
    {
        debugprint += $"{pair.Value} : {pair.Key}\n";
    }
    Debug.WriteLine(debugprint);
}

// Prints the string constants of the MIPS program to the debug output
void PrintStringConstants(Builder builder)
{
    Debug.WriteLine($"StringConstants: '{builder.StringConstants.Replace("\0", "\\0")}'");
}

// Prints the bytecode of the MIPS program to the debug output
void PrintBytecode(Builder builder) {
    var debugprint = $"Bytecode: (length: {builder.Bytecode.Length})\n";
    foreach (var instruction in builder.Bytecode)
    {
        debugprint += $"{Convert.ToString(instruction, 2).PadLeft(32, '0')}\n";
    }
    Debug.WriteLine(debugprint);
}

// Main program
void Main()
{
    // Read the source code
    var sourceCode = File.ReadAllText("../../../mult_philos.ourMIPS");

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
    } catch (NullReferenceException ex) {
        if (success)
            Debug.WriteLine(ex);
        else {
            Debug.WriteLine("\n\n(Build failed; end of Debug)");
        }
    }

    Console.WriteLine("Terminating.");
}

Main();