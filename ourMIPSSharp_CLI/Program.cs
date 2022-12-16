using System.Runtime.InteropServices;
using lib_ourMIPSSharp;

var sourcecode = File.ReadAllText("../../../testscript.ourMIPS");
var tokenizer = new Tokenizer(sourcecode);
foreach (var token in tokenizer.Tokenize()) {
    Console.Write(token.ToString() + ", ");
}