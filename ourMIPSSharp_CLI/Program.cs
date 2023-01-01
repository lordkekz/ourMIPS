using System.Diagnostics;
using lib_ourMIPSSharp;
using ourMIPSSharp_CLI;

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

var b = new CompilerDebug().Main();
new EmulatorDebug().Main(b);
Console.WriteLine("Terminating.");