using System.Globalization;
using System.Runtime.InteropServices;using System.Runtime.InteropServices.JavaScript;
using lib_ourMIPSSharp;

Console.WriteLine(new NumberLiteral("404", DialectOptions.None).ToString(NumberLiteralFormat.Decimal));

var sourcecode = File.ReadAllText("../../../testscript.ourMIPS");
var tokenizer = new Tokenizer(sourcecode, DialectOptions.Philosonline);
foreach (var token in tokenizer.Tokenize()) {
    Console.Write(token.ToString() + ", ");
}