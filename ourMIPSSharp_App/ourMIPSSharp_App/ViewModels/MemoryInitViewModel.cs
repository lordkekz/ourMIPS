using System.Collections.Generic;
using System.Reactive;
using AvaloniaEdit.Document;
using ReactiveUI;

namespace ourMIPSSharp_App.ViewModels;

public class MemoryInitViewModel : ViewModelBase {
    #region Properties

    public ReactiveCommand<Unit, Unit> ImportCommand { get; }
    public ReactiveCommand<Unit, Unit> ExportCommand { get; }

    private TextDocument _document = new();

    public TextDocument Document {
        get => _document;
        set => this.RaiseAndSetIfChanged(ref _document, value);
    }

    public MainViewModel Main { get; }

    #endregion

    public MemoryInitViewModel(MainViewModel main) {
        Main = main;
    }
}