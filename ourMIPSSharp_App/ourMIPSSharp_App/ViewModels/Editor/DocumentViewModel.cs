using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ourMIPSSharp_App.Models;
using ReactiveUI;
using System.Reactive.Linq;
using Dock.Model.ReactiveUI.Controls;
using ourMIPSSharp_App.ViewModels.Tools;

namespace ourMIPSSharp_App.ViewModels.Editor;

public class DocumentViewModel : Document {
    #region Editor Properties

    public FileBackend Backend { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ObservableCollection<InstructionEntry> InstructionList { get; } = new();
    public event EventHandler? Closing;

    private bool _isClosed;
    private DebugSessionViewModel _debugSession;
    private readonly ObservableAsPropertyHelper<bool> _isDebugging;
    private string _editorCaretInfo;

    public bool IsClosed {
        get => _isClosed;
        private set => this.RaiseAndSetIfChanged(ref _isClosed, value);
    }

    public TextDocument Document { get; }

    /// <summary>
    /// Fired whenever the program was rebuilt.
    /// </summary>
    public event EventHandler? Rebuilt;

    public string EditorCaretInfo {
        get => _editorCaretInfo;
        private set => this.RaiseAndSetIfChanged(ref _editorCaretInfo, value);
    }

    public bool HasUnsavedChanges => !SavedText.Equals(Text);
    public string SavedText { get; set; }
    public string Text => Document.Text;

    public DebugSessionViewModel DebugSession {
        get => _debugSession;
        private set => this.RaiseAndSetIfChanged(ref _debugSession, value);
    }

    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakChangingObservable { get; }

    public bool IsDebugging => _isDebugging.Value;

    public ConsoleViewModel DebugConsole { get; }

    #endregion

    public DocumentViewModel(MainViewModel main) {
        Backend = new FileBackend(() => DebugConsole!.GetInput());
        DebugConsole = new ConsoleViewModel(Backend);

        Document = new TextDocument(Backend.SourceCode);
        SavedText = Backend.SourceCode;

        // Init Commands
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CloseCommand = ReactiveCommand.CreateFromTask(CloseAsync);
        
        
        main.IsEmulatorActiveObservable.Select(b => b && main.DebugSession == DebugSession)
            .ToProperty(this, x => x.IsDebugging, out _isDebugging);

        DebugSession = new DebugSessionViewModel(this);
        
        DebuggerBreakChangingObservable =
            this.WhenAnyValue(x => x.DebugSession)
                .Select(d => d.DebuggerBreakChangingObservable)
                .Merge();


        // Init base Document properties
        Id = Title = $"Document {DateTime.Now}";
    }

    #region Private Methods

    protected internal virtual void OnRebuilt(
        IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> debuggerBreakChangingObservable) {
        InstructionList.Clear();
        var prog = Backend.CurrentEmulator!.Program;
        for (var i = 0; i < prog.Count; i++) {
            var line = Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
            InstructionList.Add(new InstructionEntry(i, line, prog, debuggerBreakChangingObservable));
        }

        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Public Methods

    public void UpdateCaretInfo(int positionLine, int positionColumn) {
        EditorCaretInfo = $"Pos {positionLine}:{positionColumn}";
    }


    public async Task CloseAsync() {
        if (HasUnsavedChanges) {
            var saveChanges = await Interactions.AskSaveChanges.Handle(Unit.Default);
            if (saveChanges)
                await SaveAsync();
        }

        OnClosing();
        IsClosed = true;
    }

    protected virtual void OnClosing() {
        Closing?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAsync() {
        try {
            var file = await Interactions.SaveFileTo.Handle(Unit.Default);
            if (file is null) return;
            await using var stream = await file.OpenWriteAsync();
            await using var write = new StreamWriter(stream);
            await write.WriteAsync(Text);
            await write.FlushAsync();
            SavedText = Text;
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    #endregion
}