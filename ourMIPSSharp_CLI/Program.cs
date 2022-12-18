using System.Globalization;
using System.Runtime.InteropServices;using System.Runtime.InteropServices.JavaScript;
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

var num = "55h";
Console.WriteLine(num);
foreach (var format in NumberLiteralFormat.GetValuesAsUnderlyingType<NumberLiteralFormat>()) {
    var tmp = new NumberLiteral(new Token(DialectOptions.None) { Content = num });
    num = tmp.ToString((NumberLiteralFormat)format);
    Console.WriteLine(num + " " + tmp);
}
Console.WriteLine(new NumberLiteral(new Token(DialectOptions.None) { Content = num }));


// Console.WriteLine(new NumberLiteral(new Token(DialectOptions.None) {Content = "32768"}).Value);

// Console.WriteLine(Convert.ToString(12, 2));

// Console.WriteLine(new NumberLiteral("404", DialectOptions.None).ToString(NumberLiteralFormat.Decimal));

// var sourcecode = File.ReadAllText("../../../testscript.ourMIPS");
// var tokenizer = new Tokenizer(sourcecode, DialectOptions.Philosonline);
// foreach (var token in tokenizer.Tokenize()) {
//     Console.Write(token.ToString() + ", ");
// }
