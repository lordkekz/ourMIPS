using System;
using System.IO;
using lib_ourMIPSSharp.CompilerComponents;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using lib_ourMIPSSharp.EmulatorComponents;
using lib_ourMIPSSharp.Errors;

namespace ourMIPSSharp_App.Models;

public class FileBackend {
    public string SourceCode { get; set; } = "";
    public DialectOptions? DialectOpts => CurrentBuilder?.Options;

    public Builder? CurrentBuilder { get; private set; }
    public Emulator? CurrentEmulator { get; private set; }
    public Debugger DebuggerInstance { get; }
    public bool Ready { get; private set; }

    public NotifyingTextWriter TextInWriter { get; } = new();
    public TextReader TextInReader { get; private set; }

    public NotifyingTextWriter TextInfoWriter { get; private set; } = new();
    public NotifyingTextWriter TextOutWriter { get; private set; } = new();
    public NotifyingTextWriter TextErrWriter { get; private set; } = new();

    public FileBackend(Func<bool> getInput) {
        TextInReader = new StringReader("");
        TextInWriter.LineWritten += (sender, args) => {
            var unreadTextIn = TextInReader!.ReadToEnd() + args.Content;
            TextInReader = new StringReader(unreadTextIn);
            if (CurrentEmulator != null) CurrentEmulator.TextIn = TextInReader;
        };
        DebuggerInstance = new Debugger(getInput, this);
    }

    public void Rebuild(DialectOptions opts = DialectOptions.None) {
        CurrentBuilder = new Builder(SourceCode, opts) {
            TextOut = TextInfoWriter,
            TextErr = TextErrWriter
        };
        
        Ready = CurrentBuilder.FullBuild();
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
            CurrentEmulator.Memory[2 * i] = 104 + 2 * i;
            _ = CurrentEmulator.Memory[2 * i + 1];
        }
    }
}