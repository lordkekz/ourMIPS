using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ourMIPSSharp_App.Models;
using ourMIPSSharp_App.ViewModels.Editor;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class OpenFileViewModel : ViewModelBase {
    
    private string? _name;
    public string? Name {
        get => _name;
        private set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    public FileBackend Backend { get; }
    public ConsoleViewModel Console { get; }
    public DocumentViewModel Editor { get; }

    private bool _isBackgroundBusy;

    public bool IsBackgroundBusy {
        get => _isBackgroundBusy;
        set => this.RaiseAndSetIfChanged(ref _isBackgroundBusy, value);
    }

    private bool _isClosed;
    
    public bool IsClosed {
        get => _isClosed;
        private set => this.RaiseAndSetIfChanged(ref _isClosed, value);
    }
    
    public event EventHandler? Closing;
    
    private ApplicationState _state = ApplicationState.FileOpened;

    public ApplicationState State {
        get => _state;
        set => this.RaiseAndSetIfChanged(ref _state, value);
    }

    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    
    public OpenFileViewModel(string name) {
        Name = name;
        Backend = new FileBackend(() => Console!.GetInput());
        Console = new ConsoleViewModel(Backend);
        Editor = new DocumentViewModel(this);
        
        SaveCommand = ReactiveCommand.CreateFromTask(SaveAsync);
        CloseCommand = ReactiveCommand.CreateFromTask(CloseAsync);
    }

    public async Task CloseAsync() {
        if (Editor.HasUnsavedChanges) {
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
            await write.WriteAsync(Editor.Text);
            await write.FlushAsync();
            Editor.SavedText = Editor.Text;
        }
        catch (IOException ex) {
            System.Console.Error.WriteLine(ex);
        }
    }
}