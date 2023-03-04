using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.Models;
using ourMIPSSharp_App.ViewModels.Tools;
using ReactiveUI;
using Debugger = ourMIPSSharp_App.Models.Debugger;

namespace ourMIPSSharp_App.ViewModels.Editor;

public class DebugSessionViewModel : ViewModelBase {
    #region Properties

    public IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> DebuggerUpdatingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakEndingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakChangingObservable { get; }
    public ObservableCollection<RegisterEntry> RegisterList { get; } = new();
    public ObservableCollection<MemoryEntry> MemoryList { get; } = new();
    public DocumentViewModel Editor { get; }
    public FileBackend Backend => Editor.Backend;
    public Debugger DebuggerInstance => Editor.Backend.DebuggerInstance;

    private int _overflowFlag;
    private int _programCounter;
    private int _highlightedLine;
    private bool _isBackgroundBusy;

    public int OverflowFlag {
        get => _overflowFlag;
        set => this.RaiseAndSetIfChanged(ref _overflowFlag, value);
    }

    public int ProgramCounter {
        get => _programCounter;
        set => this.RaiseAndSetIfChanged(ref _programCounter, value);
    }

    public int HighlightedLine {
        get => _highlightedLine;
        set => this.RaiseAndSetIfChanged(ref _highlightedLine, value);
    }

    public bool IsBackgroundBusy {
        get => _isBackgroundBusy;
        set => this.RaiseAndSetIfChanged(ref _isBackgroundBusy, value);
    }

    /// <summary>
    /// Reserved for UI Thread.
    /// </summary>
    public List<Breakpoint> UIBreakpoints { get; } = new();

    #endregion

    public DebugSessionViewModel(DocumentViewModel editor) {
        if (editor.DebugSession is not null)
            throw new InvalidOperationException("Document already has a Debug Session attached!");
        Editor = editor;

        DebuggerUpdatingObservable = Observable.FromEventPattern<DebuggerUpdatingEventHandlerArgs>(
            a => DebuggerInstance.DebuggerUpdating += a,
            a => DebuggerInstance.DebuggerUpdating -= a);
        DebuggerBreakingObservable = Observable.FromEventPattern<DebuggerBreakEventHandlerArgs>(
            a => DebuggerInstance.DebuggerBreaking += a,
            a => DebuggerInstance.DebuggerBreaking -= a);
        DebuggerBreakEndingObservable = Observable.FromEventPattern<DebuggerBreakEventHandlerArgs>(
            a => DebuggerInstance.DebuggerBreakEnding += a,
            a => DebuggerInstance.DebuggerBreakEnding -= a);
        DebuggerBreakChangingObservable = DebuggerBreakingObservable.Merge(DebuggerBreakEndingObservable);

        DebuggerInstance.DebuggerBreaking += (sender, args) => HighlightedLine = args.Line;
        DebuggerInstance.DebuggerBreakEnding += (sender, args) => HighlightedLine = 0;
        DebuggerInstance.DebuggerUpdating += HandleDebuggerUpdate;
        DebuggerInstance.DebuggerSyncing += (sender, args) => {
            UpdateBreakpoints();
            Editor.DebugConsole.DoFlushNewLines();
        };

        // Init RegisterList
        for (var i = 0; i < 32; i++) {
            RegisterList.Add(new RegisterEntry((Register)i, () => Backend.CurrentEmulator?.Registers,
                DebuggerUpdatingObservable));
        }
    }

    #region Private Methods

    private void UpdateBreakpoints() {
        foreach (var bp in UIBreakpoints.Where(bp => !DebuggerInstance.Breakpoints.Contains(bp)))
            DebuggerInstance.Breakpoints.Add(bp);
        foreach (var bp in DebuggerInstance.Breakpoints) bp.Update();
        DebuggerInstance.Breakpoints.RemoveAll(bp => bp.IsDeleted);
    }

    private void HandleDebuggerUpdate(object? sender, DebuggerUpdatingEventHandlerArgs args) {
        OverflowFlag = Backend.CurrentEmulator!.Registers.FlagOverflow ? 1 : 0;
        ProgramCounter = Backend.CurrentEmulator!.Registers.ProgramCounter;

        var index = 0;
        foreach (var address in Backend.CurrentEmulator!.Memory.Keys.Order()) {
            if (index >= MemoryList.Count)
                MemoryList.Add(new MemoryEntry(address, () => Backend.CurrentEmulator!.Memory,
                    DebuggerUpdatingObservable));
            else {
                while (MemoryList[index].AddressDecimal < address)
                    MemoryList.RemoveAt(index);
                if (MemoryList[index].AddressDecimal != address)
                    MemoryList.Insert(index, new MemoryEntry(address, () => Backend.CurrentEmulator!.Memory,
                        DebuggerUpdatingObservable));
            }

            index++;
        }

        while (index < MemoryList.Count) MemoryList.RemoveAt(index);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Force terminates the emulator. Needs to run in UI thread.
    /// </summary>
    public async Task StopEmulator() {
        var em = Backend.CurrentEmulator!;
        em.ForceTerminated = true;

        if (IsBackgroundBusy) {
            var backgroundNoLongerBusy = this.WhenAnyValue(x => x.IsBackgroundBusy)
                .Where(b => !b).FirstAsync();
            await Task.Run(() => backgroundNoLongerBusy.Wait());
        }

        DebuggerInstance.Hide();
        Debug.Assert(!IsBackgroundBusy);
        if (em is { Terminated: false, ErrorTerminated: false })
            Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated by user.");
    }

    #endregion
}