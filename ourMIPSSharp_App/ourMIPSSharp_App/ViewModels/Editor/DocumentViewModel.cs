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
using DynamicData.Binding;
using lib_ourMIPSSharp.CompilerComponents.Elements;
using ourMIPSSharp_App.ViewModels.Tools;

namespace ourMIPSSharp_App.ViewModels.Editor;

public class DocumentViewModel : Document {
    #region Editor Properties

    public FileBackend Backend { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ObservableCollection<InstructionEntry> InstructionList { get; } = new();
    public ObservableCollection<ProblemEntry> ProblemList { get; } = new();
    public event EventHandler? Closing;

    private bool _isClosed;
    private DebugSessionViewModel _debugSession;
    private readonly ObservableAsPropertyHelper<bool> _isDebugging;
    private readonly ObservableAsPropertyHelper<bool> _hasUnsavedChanges;
    private readonly ObservableAsPropertyHelper<TextDocument> _document;
    private string _editorCaretInfo;
    private string _name;

    public bool IsClosed {
        get => _isClosed;
        private set => this.RaiseAndSetIfChanged(ref _isClosed, value);
    }

    public TextDocument MainDocument { get; }
    public TextDocument DebugDocument { get; } = new();
    public TextDocument Document => _document.Value;

    /// <summary>
    /// Fired whenever the program was rebuilt.
    /// </summary>
    public event EventHandler? Rebuilt;

    public string EditorCaretInfo {
        get => _editorCaretInfo;
        private set => this.RaiseAndSetIfChanged(ref _editorCaretInfo, value);
    }

    public string Name {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public bool HasUnsavedChanges => _hasUnsavedChanges.Value;
    public string SavedText { get; private set; }
    public string Text => Document.Text;

    public DebugSessionViewModel DebugSession {
        get => _debugSession;
        private set => this.RaiseAndSetIfChanged(ref _debugSession, value);
    }

    public IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> DebuggerBreakChangingObservable { get; }

    public bool IsDebugging => _isDebugging.Value;

    public ConsoleViewModel DebugConsole { get; }

    #endregion

    public DocumentViewModel(MainViewModel main, string name = "", string sourceCode = "") {
        Main = main;
        Name = name;
        Backend = new FileBackend(async () => await DebugConsole!.GetInputAsync()) { SourceCode = sourceCode };
        DebugConsole = new ConsoleViewModel(Backend);

        MainDocument = new TextDocument(sourceCode);
        SavedText = sourceCode;

        // Init Commands
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CloseCommand = ReactiveCommand.CreateFromTask(CloseAsync);


        var isDebugging =
            this.WhenAnyValue(x => x.DebugSession, x => x.Main.State)
                .Select(t => t.Item1 == Main.DebugSession && t.Item2.IsDebuggerActive());
        isDebugging.ToProperty(this, x => x.IsDebugging, out _isDebugging);

        isDebugging.Select(b => b ? DebugDocument : MainDocument)
            .ToProperty(this, x => x.Document, out _document);

        DebugSession = new DebugSessionViewModel(this);

        DebuggerBreakChangingObservable =
            this.WhenAnyValue(x => x.DebugSession)
                .Select(d => d.DebuggerBreakChangingObservable)
                .Merge();

        var savedTextChangedObservable = this.WhenAnyValue(x => x.SavedText);
        var documentTextChangedObservable = Document.WhenValueChanged(d => d.Text);

        documentTextChangedObservable.Subscribe(_ => SilentRebuildIfReady());

        savedTextChangedObservable.Merge(documentTextChangedObservable).Select(_ => !Document.Text.Equals(SavedText))
            .ToProperty(this, x => x.HasUnsavedChanges, out _hasUnsavedChanges);

        // Init base Document properties
        Id = $"Document {DateTime.Now}";
        CanFloat = false;

        this.WhenAnyValue(x => x.Name, x => x.IsDebugging, x => x.HasUnsavedChanges)
            .Subscribe(x => Title = (x.Item2 ? "Debugging: " : "") + x.Item1 + (x.Item3 ? "*" : ""));
    }

    public MainViewModel Main { get; }

    #region Private Methods

    protected internal virtual void OnRebuilt(
        IObservable<EventPattern<DebuggerBreakEventHandlerArgs>> debuggerBreakChangingObservable) {
        if (Backend.Ready) {
            InstructionList.Clear();
            var prog = Backend.CurrentEmulator!.Program;
            for (var i = 0; i < prog.Count; i++) {
                var line = Backend.CurrentBuilder!.SymbolStacks[i].Last().Line;
                InstructionList.Add(new InstructionEntry(i, line, prog, debuggerBreakChangingObservable));
            }
        }

        UpdateProblems();

        Rebuilt?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateProblems() {
        ProblemList.Clear();
        foreach (var error in Backend.Errors)
            ProblemList.Add(new ProblemEntry(error));
    }

    private const int SilentRebuildWaitMillis = 2000;
    
    /// <summary>
    /// Run silent builds to update error list
    /// </summary>
    private async void SilentRebuildIfReady() {
        if (Backend.SilentRebuildIfReady(SilentRebuildWaitMillis, Text, Main.Settings?.Options ?? DialectOptions.None))
            UpdateProblems();
        else {
            await Task.Delay(SilentRebuildWaitMillis);
            if (Backend.SilentRebuildIfReady(SilentRebuildWaitMillis, Text, Main.Settings?.Options ?? DialectOptions.None))
                UpdateProblems();
        }
    }

    #endregion

    #region Public Methods

    public void UpdateCaretInfo(int positionLine, int positionColumn) {
        EditorCaretInfo = $"Pos {positionLine}:{positionColumn}";
    }

    public override bool OnClose() {
        _ = CloseAsync();
        return true;
    }

    public async Task CloseAsync() {
        if (!IsClosed &&
            Main.Layout != null &&
            Factory?.FindDockable(Main.Layout, d => d == this) != null) {
            IsClosed = true;
            Factory.CloseDockable(this);
            return;
        }

        if (HasUnsavedChanges) {
            var saveChanges = await Interactions.AskSaveChanges.Handle(Unit.Default);
            if (saveChanges)
                await SaveAsync();
        }

        OnClosing();
    }

    protected virtual void OnClosing() {
        Closing?.Invoke(this, EventArgs.Empty);
    }

    private async Task SaveAsync() {
        try {
            var file = await Interactions.SaveFileTo.Handle(("Save program file...", "program", "txt"));
            if (file is null) return;

            var t = Text;

            // Try using System.IO (because Avalonia doesn't always clear existing file contents)
            if (file.TryGetUri(out var uri) && File.Exists(uri.AbsolutePath)) {
                await File.WriteAllTextAsync(uri.AbsolutePath, t);
            }
            else {
                // Fallback to platform-agnostic Avalonia storage
                await using var stream = await file.OpenWriteAsync();
                await using var write = new StreamWriter(stream);
                await write.WriteAsync(t);
                await write.FlushAsync();
            }

            SavedText = t;
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    #endregion
}