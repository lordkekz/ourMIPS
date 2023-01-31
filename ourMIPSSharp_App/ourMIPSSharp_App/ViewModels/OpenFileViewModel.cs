using ourMIPSSharp_App.Models;
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
    public EditorViewModel Editor { get; }
    public DebuggerViewModel Debugger { get; }

    private bool _isBackgroundBusy;

    public bool IsBackgroundBusy {
        get => _isBackgroundBusy;
        set => this.RaiseAndSetIfChanged(ref _isBackgroundBusy, value);
    }

    public OpenFileViewModel(string name) {
        Name = name;
        Backend = new FileBackend(() => Console!.GetInput());
        Console = new ConsoleViewModel(Backend);
        Editor = new EditorViewModel(this);
        Debugger = new DebuggerViewModel(this);
    }
}