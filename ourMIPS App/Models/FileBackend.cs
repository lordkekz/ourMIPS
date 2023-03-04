using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading.Tasks;
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
    public ImmutableArray<CompilerError> Errors { get; private set; }
    public int WarningCount { get; private set; }
    public int ErrorCount { get; private set; }

    public NotifyingTextWriter TextInWriter { get; } = new();
    public TextReader TextInReader { get; private set; }

    public NotifyingTextWriter TextInfoWriter { get; private set; } = new();
    public NotifyingTextWriter TextOutWriter { get; private set; } = new();
    public NotifyingTextWriter TextErrWriter { get; private set; } = new();

    private DateTime _lastRebuildOrSilentRebuild = DateTime.Now;

    public FileBackend(Func<Task<bool>> getInput) {
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
        
        Ready = CurrentBuilder.FullBuild(false) && CurrentBuilder.ErrorCount == 0;
        Errors = CurrentBuilder.Errors;
        ErrorCount = CurrentBuilder.ErrorCount;
        WarningCount = CurrentBuilder.WarningCount;
        _lastRebuildOrSilentRebuild = DateTime.Now;
    }

    public void MakeEmulator() {
        if (!Ready)
            return;

        CurrentEmulator = new Emulator(CurrentBuilder!.Bytecode, CurrentBuilder!.StringConstants) {
            TextOut = TextOutWriter,
            TextErr = TextErrWriter,
            TextIn = TextInReader
        };
    }

    public bool SilentRebuildIfReady(double millis, string tempSourceCode, DialectOptions opts = DialectOptions.None) {
        // Only rebuild if last build is outdated.
        if (DateTime.Now - _lastRebuildOrSilentRebuild < TimeSpan.FromMilliseconds(millis)) return false;
        
        // Perform a silent, temporary build to get updated errors
        var builder = new Builder(tempSourceCode, opts);
        builder.FullBuild(false);
        Errors = builder.Errors;
        ErrorCount = builder.ErrorCount;
        WarningCount = builder.WarningCount;
        _lastRebuildOrSilentRebuild = DateTime.Now;
        return true;
    }
}