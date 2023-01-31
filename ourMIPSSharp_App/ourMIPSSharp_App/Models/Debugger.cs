using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using lib_ourMIPSSharp.EmulatorComponents;
using ourMIPSSharp_App.ViewModels;

namespace ourMIPSSharp_App.Models;

public class Debugger {
    public FileBackend Backend { get; }

    /// <summary>
    /// Reserved for background thread whenever emulator is executing.
    /// </summary>
    public List<Breakpoint> Breakpoints { get; } = new();

    /// <summary>
    /// Fired whenever the debugger enters its break mode through a breakpoint, manual stepping or a pause command.
    /// </summary>
    public event EventHandler<DebuggerBreakEventHandlerArgs>? DebuggerBreaking;

    /// <summary>
    /// Fired whenever the debugger leaves its break mode by continuing execution or terminating.
    /// </summary>
    public event EventHandler<DebuggerBreakEventHandlerArgs>? DebuggerBreakEnding;

    /// <summary>
    /// Fired whenever the debugger register/memory are to be updated.
    /// </summary>
    public event EventHandler<DebuggerUpdatingEventHandlerArgs>? DebuggerUpdating;

    /// <summary>
    /// Fired whenever the debugger needs to sync with UI.
    /// </summary>
    public event EventHandler? DebuggerSyncing;

    private readonly Func<bool> _getInput;

    public Debugger(Func<bool> getInput, FileBackend backend) {
        _getInput = getInput;
        Backend = backend;
    }

    protected virtual void OnDebuggerBreaking() {
        var address = Backend.CurrentEmulator!.ProgramCounter;
        var line = Backend.CurrentBuilder!.SymbolStacks[address].Last().Line;
        Dispatcher.UIThread.InvokeAsync(() =>
            DebuggerBreaking?.Invoke(this, new DebuggerBreakEventHandlerArgs(address, line)));
    }

    protected virtual void OnDebuggerBreakEnding() {
        Dispatcher.UIThread.InvokeAsync(() =>
            DebuggerBreakEnding?.Invoke(this, new DebuggerBreakEventHandlerArgs(-1, -1)));
    }

    protected virtual void OnDebuggerUpdating(bool raisesChangeHighlight) {
        Dispatcher.UIThread.InvokeAsync(() =>
            DebuggerUpdating?.Invoke(this, new DebuggerUpdatingEventHandlerArgs(raisesChangeHighlight)));
    }

    public void StartSession() {
        OnDebuggerUpdating(false);
        OnDebuggerBreaking();
    }

    public void Step() {
        var em = Backend.CurrentEmulator!;
        OnDebuggerBreakEnding();

        var pc = em.ProgramCounter;
        while (!em.EffectivelyTerminated) {
            em.TryExecuteNext();

            if (em.ExpectingInput)
                _getInput();
            else break;
        }

        if (em.Terminated || em.ErrorTerminated) {
            OnDebuggerBreakEnding();
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
        }

        if (!em.EffectivelyTerminated) {
            OnDebuggerBreaking();
        }

        OnDebuggerUpdating(true);
    }

    public void Forward() {
        var em = Backend.CurrentEmulator!;
        OnDebuggerBreakEnding();
        var s = new Stopwatch();
        s.Start();

        while (!em.EffectivelyTerminated) {
            // Keep running in parallel until ui thread is needed for console output
            // Execute at least one instruction.
            do {
                em.TryExecuteNext();
            } while (!em.EffectivelyTerminated &&
                     !IsAtBreakpoint(em.ProgramCounter) &&
                     !em.ExpectingInput);

            OnDebuggerSyncing();

            // Await input or sth
            if (em.ExpectingInput && !_getInput()) break;

            // Pause at breakpoints; but only after at least one instruction was executed.
            if (IsAtBreakpoint(em.ProgramCounter)) {
                OnDebuggerBreaking();
                break;
            }
        }

        s.Stop();

        if (em.Terminated || em.ErrorTerminated) {
            OnDebuggerBreakEnding();
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated.");
        }

        OnDebuggerUpdating(true);
    }

    public void Run() {
        var em = Backend.CurrentEmulator;
        Backend.TextInfoWriter.WriteLine("[EMULATOR] Running program.");
        var s = new Stopwatch();
        s.Start();

        while (!em!.EffectivelyTerminated) {
            // Keep running in parallel until ui thread is needed for console output.
            // Execute at least one instruction.
            do {
                em.TryExecuteNext();
            } while (em is { EffectivelyTerminated: false, ExpectingInput: false });

            // Await input or sth
            if (em.ExpectingInput && !_getInput()) break;
        }

        s.Stop();
        Backend.TextInfoWriter.WriteLine($"[EMULATOR] Program terminated after {s.ElapsedMilliseconds}ms");

        OnDebuggerUpdating(true);
    }

    private bool IsAtBreakpoint(short pc) {
        return Backend.CurrentBuilder!.SymbolStacks.Length > pc &&
               Backend.CurrentBuilder!.SymbolStacks[pc].Any(
                   s => Breakpoints.Any(x => x.Line == s.Line));
    }

    protected virtual void OnDebuggerSyncing() {
        DebuggerSyncing?.Invoke(this, EventArgs.Empty);
    }
}