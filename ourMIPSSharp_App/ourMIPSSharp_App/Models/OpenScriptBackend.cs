using System;
using System.IO;
using System.Threading.Tasks;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using lib_ourMIPSSharp.Errors;

namespace ourMIPSSharp_App.Models;

public class OpenScriptBackend {
    public string FilePath { get; }

    public string? SourceCode => CurrentBuilder?.SourceCode;
    public DialectOptions? DialectOpts => CurrentBuilder?.Options;

    public Builder? CurrentBuilder { get; private set; }
    public Emulator? CurrentEmulator { get; private set; }
    public bool Ready { get; private set; }

    public OpenScriptBackend(string path) {
        FilePath = path;
        Rebuild(File.ReadAllText(FilePath));
    }

    public void SaveFile() {
        File.WriteAllText(FilePath, SourceCode);
    }

    public bool Rebuild(string sourceCode, DialectOptions opts = DialectOptions.None) {
        CurrentBuilder = new Builder(sourceCode, opts);
        if (!CurrentBuilder.FullBuild())
            return Ready = false;
        
        CurrentEmulator = new Emulator(CurrentBuilder.Bytecode, CurrentBuilder.StringConstants);
        return Ready = true;
    }
}