using System.Diagnostics;
using lib_ourMIPSSharp;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.EmulatorComponents;

namespace ourMIPSSharp_CLI;

public class EmulatorDebug {
    public void Main(Builder b) {
        var e = new Emulator(b.Bytecode, b.StringConstants);
        e.RunUntilTerminated();
    }
}