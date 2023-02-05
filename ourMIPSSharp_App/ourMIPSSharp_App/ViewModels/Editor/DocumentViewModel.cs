using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.Models;
using ReactiveUI;
using Debugger = ourMIPSSharp_App.Models.Debugger;
using Observable = System.Reactive.Linq.Observable;
using System.Reactive.Linq;
using ourMIPSSharp_App.ViewModels.Tools;

namespace ourMIPSSharp_App.ViewModels.Editor; 

public class DocumentViewModel : ViewModelBase {

    #region Editor Properties

    public OpenFileViewModel File { get; }
    public TextDocument Document { get; }
    public ObservableCollection<InstructionEntry> InstructionList { get; } = new();

    /// <summary>
    /// Fired whenever the program was rebuilt.
    /// </summary>
    public event EventHandler? Rebuilt;


    private string _editorCaretInfo;

    public string EditorCaretInfo {
        get => _editorCaretInfo;
        private set => this.RaiseAndSetIfChanged(ref _editorCaretInfo, value);
    }

    public bool HasUnsavedChanges => !SavedText.Equals(Text);
    public string SavedText { get; set; }
    public string Text => Document.Text;

    private readonly ObservableAsPropertyHelper<bool> _isEditorReadonly;
    public bool IsEditorReadonly => _isEditorReadonly.Value;
    #endregion
    
    #region Debugger Properties
    
    public IObservable<EventPattern<DebuggerUpdatingEventHandlerArgs>> DebuggerUpdatingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakEndingObservable { get; }
    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakChangingObservable { get; }

    public ObservableCollection<RegisterEntry> RegisterList { get; } = new();
    public ObservableCollection<MemoryEntry> MemoryList { get; } = new();
    public Debugger DebuggerInstance => File.Backend.DebuggerInstance;

    private int _overflowFlag;
    private int _programCounter;
    private int _highlightedLine;

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
    
    /// <summary>
    /// Reserved for UI Thread.
    /// </summary>
    public List<Breakpoint> UIBreakpoints { get; } = new();

    #endregion

    public DocumentViewModel(OpenFileViewModel file) {
        File = file;
        
        Document = new TextDocument(File.Backend.SourceCode);
        SavedText = File.Backend.SourceCode;

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
        
        
        this.WhenAnyValue(x => x.File.State)
            .Select(x => !x.IsEditingAllowed())
            .ToProperty(this, x => x.IsEditorReadonly, out _isEditorReadonly);

        // Init RegisterList
        for (var i = 0; i < 32; i++) {
            RegisterList.Add(new RegisterEntry((Register)i, () => File.Backend.CurrentEmulator?.Registers,
                DebuggerUpdatingObservable));
        }
    }
    
    #region Private Methods

    protected internal virtual void OnRebuilt(
        IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> debuggerBreakChangingObservable) {
        InstructionList.Clear();
        var prog = File.Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            var line = File.Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
            InstructionList.Add(new InstructionEntry(i, line, prog, debuggerBreakChangingObservable));
        }

        Rebuilt?.Invoke(this, EventArgs.Empty);
    }
    
    private void UpdateBreakpoints() {
        foreach (var bp in UIBreakpoints.Where(bp => !DebuggerInstance.Breakpoints.Contains(bp)))
            DebuggerInstance.Breakpoints.Add(bp);
        foreach (var bp in DebuggerInstance.Breakpoints) bp.Update();
        DebuggerInstance.Breakpoints.RemoveAll(bp => bp.IsDeleted);
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

    #endregion
    
    #region Public Methods

    public void UpdateCaretInfo(int positionLine, int positionColumn) {
        EditorCaretInfo = $"Pos {positionLine}:{positionColumn}";
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

    #endregion
}