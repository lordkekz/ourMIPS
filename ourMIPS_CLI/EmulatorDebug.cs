#region

using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.EmulatorComponents;

#endregion

namespace ourMIPSSharp_CLI;

public class EmulatorDebug {
    public void Main(Builder b) {
        var e = new Emulator(b.Bytecode, b.StringConstants);
        e.RunUntilTerminated();
//         var mem = new MainStorage();
//         mem.InitializePhilos("""
// {
//     "test1": {
//         "name": "Aufgabe Addition von Zahlen im Hauptspeicher",
//         "entry_mem": {
//             "0x0100": 6,
//             "0x0101": 1,
//             "0x0102": 2,
//             "0x0103": 4,
//             "0x0104": 8,
//             "0x0105": 16,
//             "0x0106": 32
//         }
//     }
// }
// """);
//         Console.WriteLine();
    }
}