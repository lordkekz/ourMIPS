using System.IO;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.EmulatorComponents;

namespace ourMIPSSharp_App.Models;

public class OpenScriptBackend {
    public FileInfo File { get; private set; }
    public Builder CurrentBuilder { get; private set; }
    public Emulator CurrentEmulator { get; private set; }

    public OpenScriptBackend(string path) {
        File = new FileInfo(path);
    }
    
    // TODO continue
}