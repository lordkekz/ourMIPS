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

    public TextWriter TextInWriter { get; private set; }
    public TextReader TextInReader { get; private set; }
    
    public TextWriter TextOutWriter { get; private set; }
    public TextReader TextOutReader { get; private set; }
    
    public TextWriter TextErrWriter { get; private set; }
    public TextReader TextErrReader { get; private set; }

    public OpenScriptBackend(string path) {
        FilePath = path;
        
        var s = new MemoryStream();
        TextInWriter = new StreamWriter(s);
        TextInReader = new StreamReader(s);
        //
        // s = new MemoryStream();
        // TextErrWriter = TextOutWriter = new StreamWriter(s);
        // TextErrReader = TextOutReader = new StreamReader(s);
        TextErrWriter = TextOutWriter = new StringWriter();

        Rebuild(File.ReadAllText(FilePath));
    }

    public void SaveFile() {
        File.WriteAllText(FilePath, SourceCode);
    }

    public bool Rebuild(string sourceCode, DialectOptions opts = DialectOptions.None) {
        TextErrWriter = TextOutWriter = new StringWriter();
        
        CurrentBuilder = new Builder(sourceCode, opts) {
            TextOut = TextOutWriter,
            TextErr = TextErrWriter
        };
        if (!CurrentBuilder.FullBuild())
            return Ready = false;

        Ready = true;
        MakeEmulator();
        return true;
    }

    public void MakeEmulator() {
        if (!Ready)
            return;

        CurrentEmulator = new Emulator(CurrentBuilder!.Bytecode, CurrentBuilder!.StringConstants) {
            TextOut = TextOutWriter,
            TextErr = TextErrWriter,
            TextIn = TextInReader
        };
        
        for (int i = 0; i < 1000; i++) {
            CurrentEmulator.Memory[2 * i] = 504 + 2 * i;
            _ = CurrentEmulator.Memory[2 * i + 1];
        }
    }
}