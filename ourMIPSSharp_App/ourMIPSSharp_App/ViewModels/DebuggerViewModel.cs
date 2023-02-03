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
using ReactiveUI;
using Debugger = ourMIPSSharp_App.Models.Debugger;

namespace ourMIPSSharp_App.ViewModels;

public class DebuggerViewModel : ViewModelBase {
    public IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> DebuggerUpdatingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakEndingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakChangingObservable { get; }

    public ObservableCollection<RegisterEntry> RegisterList { get; } = new();
    public ObservableCollection<MemoryEntry> MemoryList { get; } = new();
    public OpenFileViewModel File { get; }
    public Debugger DebuggerInstance => File.Backend.DebuggerInstance;

    private int _overflowFlag;

    public int OverflowFlag {
        get => _overflowFlag;
        set => this.RaiseAndSetIfChanged(ref _overflowFlag, value);
    }

    private int _programCounter;

    public int ProgramCounter {
        get => _programCounter;
        set => this.RaiseAndSetIfChanged(ref _programCounter, value);
    }

    /// <summary>
    /// Reserved for UI Thread.
    /// </summary>
    public List<Breakpoint> UIBreakpoints { get; } = new();


    private int _highlightedLine;

    public int HighlightedLine {
        get => _highlightedLine;
        set => this.RaiseAndSetIfChanged(ref _highlightedLine, value);
    }

    private void UpdateBreakpoints() {
        foreach (var bp in UIBreakpoints.Where(bp => !DebuggerInstance.Breakpoints.Contains(bp)))
            DebuggerInstance.Breakpoints.Add(bp);
        foreach (var bp in DebuggerInstance.Breakpoints) bp.Update();
        DebuggerInstance.Breakpoints.RemoveAll(bp => bp.IsDeleted);
    }

    public DebuggerViewModel(OpenFileViewModel file) {
        File = file;

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
            File.Console.DoFlushNewLines();
        };

        // Init RegisterList
        for (var i = 0; i < 32; i++) {
            RegisterList.Add(new RegisterEntry((Register)i, () => File.Backend.CurrentEmulator?.Registers,
                DebuggerUpdatingObservable));
        }
    }

    private void HandleDebuggerUpdate(object? sender, DebuggerUpdatingEventHandlerArgs args) {
        OverflowFlag = File.Backend.CurrentEmulator!.Registers.FlagOverflow ? 1 : 0;
        ProgramCounter = File.Backend.CurrentEmulator!.Registers.ProgramCounter;

        var index = 0;
        foreach (var address in File.Backend.CurrentEmulator!.Memory.Keys.Order()) {
            if (index >= MemoryList.Count)
                MemoryList.Add(new MemoryEntry(address, () => File.Backend.CurrentEmulator!.Memory,
                    DebuggerUpdatingObservable));
            else {
                while (MemoryList[index].AddressDecimal < address)
                    MemoryList.RemoveAt(index);
                if (MemoryList[index].AddressDecimal != address)
                    MemoryList.Insert(index, new MemoryEntry(address, () => File.Backend.CurrentEmulator!.Memory,
                        DebuggerUpdatingObservable));
            }

            index++;
        }

        while (index < MemoryList.Count) MemoryList.RemoveAt(index);
    }

    /// <summary>
    /// Force terminates the emulator. Needs to run in UI thread.
    /// </summary>
    public async Task StopEmulator() {
        File.Backend.CurrentEmulator!.ForceTerminated = true;

        if (File.IsBackgroundBusy) {
            var backgroundNoLongerBusy = File.WhenAnyValue(x => x.IsBackgroundBusy).Where(b => !b).FirstAsync();
            await Task.Run(() => backgroundNoLongerBusy.Wait());
        }

        DebuggerInstance.Hide();
        Debug.Assert(!File.IsBackgroundBusy);
        File.Backend.TextInfoWriter.WriteLine("[EMULATOR] Program terminated by user.");
    }
}