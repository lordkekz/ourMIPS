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

var sourcecode = File.ReadAllText("../../../test_sysout.ourMIPS");
var builder = new Builder(sourcecode, DialectOptions.None);
var success = builder.FullBuild();

var debugprint = "\n\n### Debug stuff\nTokens:\n";
foreach (var token in builder.Tokens) {
    debugprint += token + "    ";
}
Debug.WriteLine(debugprint);

debugprint = "ResolvedTokens:\n";
foreach (var token in builder.ResolvedTokens) {
    debugprint += token.Content + " ";
    if (token.Type == TokenType.InstructionBreak)
        debugprint += "\n";
}
Debug.WriteLine(debugprint);

debugprint = "Labels:\n";
foreach (var pair in builder.Labels) {
    debugprint += $"{pair.Value} : {pair.Key}\n";
}
Debug.WriteLine(debugprint);

Debug.WriteLine($"StringConstants: '{builder.StringConstants.Replace("\0", "\\0")}'");

debugprint = $"Bytecode: (length: {builder.Bytecode.Length})\n";
foreach (var instruction in builder.Bytecode) {
    debugprint += $"{Convert.ToString(instruction, 2).PadLeft(32, '0')}\n";
}
Debug.WriteLine(debugprint);

Console.WriteLine("Terminating.");