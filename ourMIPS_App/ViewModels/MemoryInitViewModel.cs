#region

using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvaloniaEdit.Document;
using ReactiveUI;

#endregion

namespace ourMIPS_App.ViewModels;

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
        ImportCommand = ReactiveCommand.CreateFromTask(ExecuteImportCommand);
        ExportCommand = ReactiveCommand.CreateFromTask(ExecuteExportCommand);
    }

    private async Task ExecuteImportCommand() {
        try {
            var file = await Interactions.OpenProgramFile.Handle("Open test environment file...");
            if (file is null) return;
            await using var stream = await file.OpenReadAsync();
            using var reader = new StreamReader(stream);
            Document.Text = await reader.ReadToEndAsync();
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }

    private async Task ExecuteExportCommand() {
        try {
            var file = await Interactions.SaveFileTo.Handle(("Save test environment file...", "test_env", "json"));
            if (file is null) return;

            var t = Document.Text;

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
        }
        catch (IOException ex) {
            Console.Error.WriteLine(ex);
        }
    }
}