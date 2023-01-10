using System;
using System.IO;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using lib_ourMIPSSharp.Errors;

namespace ourMIPSSharp_App.Models;

public class OpenScriptBackend {
    public string FilePath { get; }

    public string SourceCode { get; set; } = "";
    public DialectOptions? DialectOpts => CurrentBuilder?.Options;

    public Builder? CurrentBuilder { get; private set; }
    public Emulator? CurrentEmulator { get; private set; }
    public bool Ready { get; private set; }

    public NotifyingTextWriter TextInWriter { get; } = new();
    public TextReader TextInReader { get; private set; }

    public NotifyingTextWriter TextInfoWriter { get; private set; } = new();
    public NotifyingTextWriter TextOutWriter { get; private set; } = new();
    public NotifyingTextWriter TextErrWriter { get; private set; } = new();

    public OpenScriptBackend(string path) {
        FilePath = path;

        TextInReader = new StringReader("");
        TextInWriter.LineWritten += (sender, args) => {
            var unreadTextIn = TextInReader!.ReadToEnd() + args.Content;
            TextInReader = new StringReader(unreadTextIn);
            if (CurrentEmulator != null) CurrentEmulator.TextIn = TextInReader;
        };

        SourceCode = File.ReadAllText(FilePath);
    }

    public void SaveFile() {
        File.WriteAllText(FilePath, SourceCode);
    }

    public bool Rebuild(DialectOptions opts = DialectOptions.None) {
        CurrentBuilder = new Builder(SourceCode, opts) {
            TextOut = TextInfoWriter,
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