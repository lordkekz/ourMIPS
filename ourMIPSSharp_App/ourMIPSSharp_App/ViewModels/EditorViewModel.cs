using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection.PortableExecutable;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using ourMIPSSharp_App.Models;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class EditorViewModel : ViewModelBase {

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

    public bool HasUnsavedChanges { get; private set; }
    public string Text => Document.Text;

    public void UpdateCaretInfo(int positionLine, int positionColumn) {
        EditorCaretInfo = $"Pos {positionLine}:{positionColumn}";
    }

    protected internal virtual void OnRebuilt(
        IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> debuggerBreakChangingObservable) {
        if (!Dispatcher.UIThread.CheckAccess()) {
            // Switch to UI Thread
            Dispatcher.UIThread.InvokeAsync(() => OnRebuilt(debuggerBreakChangingObservable)).Wait();
            return;
        }

        InstructionList.Clear();
        var prog = File.Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            var line = File.Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
            InstructionList.Add(new InstructionEntry(i, line, prog, debuggerBreakChangingObservable));
        }

        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    public EditorViewModel(OpenFileViewModel file) {
        File = file;
        Document = new TextDocument(File.Backend.SourceCode);
    }
}